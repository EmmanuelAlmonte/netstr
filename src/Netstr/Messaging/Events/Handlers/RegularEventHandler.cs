using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netstr.Data;
using Netstr.Messaging.Models;
using Netstr.Options;

namespace Netstr.Messaging.Events.Handlers
{
    /// <summary>
    /// Regular events are stored by the relay. Duplicates are ignored.
    /// </summary>
    public class RegularEventHandler : EventHandlerBase
    {
        private readonly IDbContextFactory<NetstrDbContext> db;

        public RegularEventHandler(
            ILogger<RegularEventHandler> logger,
            IOptions<AuthOptions> auth,
            IWebSocketAdapterCollection adapters,
            IDbContextFactory<NetstrDbContext> db)
            : base(logger, auth, adapters)
        {
            this.db = db;
        }

        // this event handler also serves as a fallback for all unknown events
        public override bool CanHandleEvent(Event e) => true;

        protected override async Task HandleEventCoreAsync(IWebSocketAdapter sender, Event e)
        {
            using var db = this.db.CreateDbContext();

            if (await db.Events.IsDeleted(e.Id))
            {
                this.logger.LogInformation($"Event {e.Id} was already deleted");
                sender.SendNotOk(e.Id, Messages.InvalidDeletedEvent);
                return;
            }

            var entity = e.ToEntity(DateTimeOffset.UtcNow);
            db.Add(entity);

            // save with metrics tracking
            var saveStart = DateTimeOffset.UtcNow;
            var changes = await db.SaveChangesAsync();
            var saveTime = DateTimeOffset.UtcNow - saveStart;

            if (saveTime.TotalMilliseconds > 1000)
            {
                this.logger.LogWarning("Slow database save for event {EventId}: {Duration}ms",
                    e.Id, saveTime.TotalMilliseconds);
            }

            this.logger.LogDebug("Saved event {EventId} (Kind: {Kind}) in {Duration}ms",
                e.Id, e.Kind, saveTime.TotalMilliseconds);

            // reply
            sender.SendOk(e.Id);

            // broadcast
            BroadcastEvent(e);
        }
    }
}
