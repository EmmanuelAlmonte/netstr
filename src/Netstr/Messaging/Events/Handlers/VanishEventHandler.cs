﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netstr.Data;
using Netstr.Extensions;
using Netstr.Messaging.Models;
using Netstr.Options;

namespace Netstr.Messaging.Events.Handlers
{
    public class VanishEventHandler : EventHandlerBase
    {
        private readonly IDbContextFactory<NetstrDbContext> db;
        private readonly IUserCache userCache;
        private readonly IHttpContextAccessor http;

        private readonly static string AllRelaysValue = "ALL_RELAYS";

        public VanishEventHandler(
            ILogger<EventHandlerBase> logger, 
            IOptions<AuthOptions> auth, 
            IWebSocketAdapterCollection adapters,
            IDbContextFactory<NetstrDbContext> db,
            IUserCache userCache,
            IHttpContextAccessor http)
            : base(logger, auth, adapters)
        {
            this.db = db;
            this.userCache = userCache;
            this.http = http;
        }

        public override bool CanHandleEvent(Event e) => e.IsRequestToVanish();

        protected override async Task HandleEventCoreAsync(IWebSocketAdapter sender, Event e)
        {
            var ctx = this.http.HttpContext?.Request ?? throw new InvalidOperationException("HttpContext not set");
            var path = ctx.GetNormalizedUrl();
            var relays = e.GetTagValues(EventTag.Relay)
                .Concat(e.GetTagValues(EventTag.AuthRelay))
                .Select(x => HttpExtensions.NormalizeRelayUrl(x))
                .Distinct();

            // check 'relay' tag matches current url or is set to ALL_RELAYS
            if (!relays.Any(x => x == path || x == AllRelaysValue))
            {
                sender.SendNotOk(e.Id, string.Format(Messages.InvalidWrongTagValue, EventTag.AuthRelay));
                return;
            }

            using var db = this.db.CreateDbContext();

            var vanishStart = DateTimeOffset.UtcNow;

            // Use execution strategy to handle transactions with retry logic
            var strategy = db.Database.CreateExecutionStrategy();
            var deletedResult = await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync();

                var eventsToDelete = db.Events
                    .Where(x =>
                        (x.EventPublicKey == e.PublicKey ||
                        (x.EventKind == (long)EventKind.GiftWrap && x.Tags.Any(t => t.Name == EventTag.PublicKey && t.Value == e.PublicKey))) &&
                        x.EventCreatedAt <= e.CreatedAt);

                var deletedEventIds = await eventsToDelete
                    .Select(x => x.EventId)
                    .ToArrayAsync();

                // delete all user's events (or tagged GiftWraps) from before the vanish event
                var deleted = await eventsToDelete.ExecuteDeleteAsync();

                // insert vanish entity to db
                db.Events.Add(e.ToEntity(DateTimeOffset.UtcNow));

                // save
                await db.SaveChangesAsync();
                await tx.CommitAsync();

                return (DeletedCount: deleted, DeletedEventIds: deletedEventIds);
            });

            this.userCache.TrackVanishDeletedEvents(deletedResult.DeletedEventIds);

            var vanishTime = DateTimeOffset.UtcNow - vanishStart;

            if (vanishTime.TotalMilliseconds > 5000)
            {
                this.logger.LogWarning("Slow vanish operation for user {PubKey}: {Duration}ms, deleted {Count} events",
                    e.PublicKey, vanishTime.TotalMilliseconds, deletedResult.DeletedCount);
            }

            this.logger.LogInformation("Vanish request processed for user {PubKey}: deleted {Count} events in {Duration}ms",
                e.PublicKey, deletedResult.DeletedCount, vanishTime.TotalMilliseconds);

            // set vanished in cache
            this.userCache.Vanish(e.PublicKey, e.CreatedAt);

            // reply
            sender.SendOk(e.Id);

            // broadcast
            BroadcastEvent(e);
        }
    }
}
