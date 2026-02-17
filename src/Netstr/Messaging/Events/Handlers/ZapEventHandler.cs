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
            if (e.Kind == (long)EventKind.ZapRequest)
            {
                sender.SendNotOk(e.Id, Messages.InvalidZapRequestRelayPublish);
                return;
            }

            using var db = this.db.CreateDbContext();

            if (await db.Events.IsDeleted(e.Id))
            {
                this.logger.LogInformation($"Event {e.Id} was already deleted");
                sender.SendNotOk(e.Id, Messages.InvalidDeletedEvent);
                return;
            }

            var newEntity = e.ToEntity(DateTimeOffset.UtcNow);

            db.Add(newEntity);
            await db.SaveChangesAsync();

            // Reply
            sender.SendOk(e.Id);

            // Broadcast
            BroadcastEvent(e);
        }
    }
}
