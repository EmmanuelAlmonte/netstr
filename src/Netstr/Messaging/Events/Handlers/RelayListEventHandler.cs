using Microsoft.EntityFrameworkCore;
using Netstr.Data;
using Netstr.Messaging.Models;

namespace Netstr.Messaging.Events.Handlers
{
    public class RelayListEventHandler : IEventHandler
    {
        private readonly ILogger<RelayListEventHandler> logger;
        private readonly IDbContextFactory<NetstrDbContext> dbFactory;

        public RelayListEventHandler(
            ILogger<RelayListEventHandler> logger,
            IDbContextFactory<NetstrDbContext> dbFactory)
        {
            this.logger = logger;
            this.dbFactory = dbFactory;
        }

        public bool CanHandleEvent(Event e) => (EventKind)e.Kind == EventKind.RelayList;

        public async Task HandleEventAsync(IWebSocketAdapter sender, Event e)
        {
            this.logger.LogInformation(
                "RelayList Event Received:\nFull Event:\n{@Event}\nTags:\n{@Tags}\nContent:\n{Content}",
                e,
                e.Tags,
                e.Content
            );

            try
            {
                using var context = this.dbFactory.CreateDbContext();
                var changes = await context.UpsertRelayConfigsAsync(e);

                this.logger.LogInformation("Updated {Count} relay configurations for user {PubKey}", changes, e.PublicKey);
                sender.SendOk(e.Id);
            }
            catch (Exception error)
            {
                this.logger.LogError(error, "Failed to update relay configurations for user {PubKey}", e.PublicKey);
                sender.SendNotOk(e.Id, "Failed to update relay configurations");
            }
        }
    }
}
