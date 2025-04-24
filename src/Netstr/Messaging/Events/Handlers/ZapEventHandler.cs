using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netstr.Data;
using Netstr.Messaging.Models;
using Netstr.Options;

namespace Netstr.Messaging.Events.Handlers
{
    /// <summary>
    /// Handles NIP-57 Zap events (ZapRequest and ZapReceipt).
    /// </summary>
    public class ZapEventHandler : EventHandlerBase
    {
        private readonly IDbContextFactory<NetstrDbContext> db;

        public ZapEventHandler(
            ILogger<ZapEventHandler> logger,
            IOptions<AuthOptions> auth,
            IWebSocketAdapterCollection adapters,
            IDbContextFactory<NetstrDbContext> db)
            : base(logger, auth, adapters)
        {
            this.db = db;
        }

        public override bool CanHandleEvent(Event e) => 
            e.Kind == (long)EventKind.ZapRequest || e.Kind == (long)EventKind.ZapReceipt;

        protected override async Task HandleEventCoreAsync(IWebSocketAdapter sender, Event e)
        {
            using var db = this.db.CreateDbContext();

            if (await db.Events.IsDeleted(e.Id))
            {
                this.logger.LogInformation($"Event {e.Id} was already deleted");
                sender.SendNotOk(e.Id, Messages.InvalidDeletedEvent);
                return;
            }

            var newEntity = e.ToEntity(DateTimeOffset.UtcNow);
            
            // Check for duplicates
            var existing = await db.Events
                .AsNoTracking()
                .Where(x => x.EventId == e.Id)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                this.logger.LogInformation($"Event {e.Id} already exists");
                sender.SendOk(e.Id); // Still return OK for duplicates
                return;
            }

            db.Add(newEntity);
            await db.SaveChangesAsync();

            // Reply
            sender.SendOk(e.Id);

            // Broadcast
            BroadcastEvent(e);
        }
    }
}
