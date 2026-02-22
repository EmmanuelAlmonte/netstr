using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Netstr.Data;
using Netstr.Extensions;
using Netstr.Messaging.Models;
using Netstr.Options;

namespace Netstr.Messaging.Events.Handlers
{
    /// <summary>
    /// Delete events are special type of regular event which mark other events as deleted.
    /// </summary>
    public class DeleteEventHandler : EventHandlerBase
    {
        private static readonly long[] CannotDeleteKinds = [ (long)EventKind.Delete, (long)EventKind.RequestToVanish ];

        private record ReplaceableEventRef(int Kind, string PublicKey, string? Deduplication) { }

        private readonly IDbContextFactory<NetstrDbContext> db;

        public DeleteEventHandler(
            ILogger<DeleteEventHandler> logger,
            IOptions<AuthOptions> auth,
            IWebSocketAdapterCollection adapters,
            IDbContextFactory<NetstrDbContext> db)
            : base(logger, auth, adapters)
        {
            this.db = db;
        }

        public override bool CanHandleEvent(Event e) => e.IsDelete();

        protected override async Task HandleEventCoreAsync(IWebSocketAdapter sender, Event e)
        {
            using var db = this.db.CreateDbContext();
            var now = DateTimeOffset.UtcNow;

            // delete events (= mark as deleted)
            var regularEventIds = GetRegularEventIds(e.Tags);
            var replaceableQuery = GetReplaceableQuery(db, e);

            var events = await db.Events
                .Where(x => regularEventIds.Contains(x.EventId) || replaceableQuery.Contains(x.EventId))
                .Select(x => new
                {
                    x.Id,
                    WrongKey = x.EventPublicKey != e.PublicKey,          // only delete own events
                    WrongKind = CannotDeleteKinds.Contains(x.EventKind), // cannnot delete some events
                    AlreadyDeleted = x.DeletedAt.HasValue                // was previously deleted
                })
                .ToArrayAsync();

            if (events.Any(x => x.WrongKey || x.WrongKind))
            {
                this.logger.LogWarning("Someone's trying to delete someone else's or undeletable event.");
                sender.SendNotOk(e.Id, Messages.InvalidCannotDelete);
                return;
            }

            // do not "re-delete" already deleted events
            var eventsToDelete = events
                .Where(x => !x.AlreadyDeleted)
                .Select(x => x.Id)
                .ToArray();

            // Use execution strategy to handle transactions with retry logic
            var strategy = db.Database.CreateExecutionStrategy();
            var updateStart = DateTimeOffset.UtcNow;

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync();

                await db.Events
                    .Where(x => eventsToDelete.Contains(x.Id))
                    .ExecuteUpdateAsync(x => x.SetProperty(x => x.DeletedAt, now));

                db.Add(e.ToEntity(now));

                // save
                await db.SaveChangesAsync();
                await tx.CommitAsync();
            });

            var updateTime = DateTimeOffset.UtcNow - updateStart;

            if (updateTime.TotalMilliseconds > 2000)
            {
                this.logger.LogWarning("Slow delete operation for event {EventId}: {Duration}ms, deleted {Count} events",
                    e.Id, updateTime.TotalMilliseconds, eventsToDelete.Length);
            }

            this.logger.LogInformation("Deleted {Count} events in {Duration}ms",
                eventsToDelete.Length, updateTime.TotalMilliseconds);

            // reply
            sender.SendOk(e.Id);

            // broadcast
            BroadcastEvent(e);
        }

        private IEnumerable<string> GetRegularEventIds(string[][] tags)
        {
            return tags
                .Where(x => x.Length >= 2 && x[0] == EventTag.Event)
                .Select(x => x[1])
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct();
        }

        private IQueryable<string> GetReplaceableQuery(NetstrDbContext db, Event e)
        {
            var replacableEvents = e.Tags
                .Where(x => x.Length >= 2 && x[0] == EventTag.ReplaceableEvent)
                .Select(x => ParseReplaceableTag(x[1]))
                .WhereNotNull()
                .ToArray();

            var replaceableQuery = db.Events.Where(x => false);

            foreach (var re in replacableEvents)
            {
                var query = db.Events.Where(x => x.EventKind == re.Kind && x.EventDeduplication == re.Deduplication && x.EventPublicKey == re.PublicKey);
                replaceableQuery = replaceableQuery.Union(query);
            }

            return replaceableQuery
                .Where(x => x.EventCreatedAt <= e.CreatedAt) // only delete those before the deletion request
                .Select(x => x.EventId);
        }

        private ReplaceableEventRef? ParseReplaceableTag(string tag)
        {
            var parsed = tag.Split(":", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parsed.Length < 2)
            {
                return null;
            }

            if (!int.TryParse(parsed[0], out var kind))
            {
                return null;
            }

            return new(kind, parsed[1], parsed.Length > 2 ? parsed[2] : null);
        }
    }
}
