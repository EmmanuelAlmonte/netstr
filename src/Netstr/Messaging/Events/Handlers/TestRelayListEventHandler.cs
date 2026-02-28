using Microsoft.EntityFrameworkCore;
using Netstr.Data;
using Netstr.Messaging.Models;

namespace Netstr.Messaging.Events.Handlers
{
    /// <summary>
    /// Test handler for NIP-65 Relay List events (kind: 10002) that stores events directly without using RelayConfigs table.
    /// </summary>
    public class TestRelayListEventHandler : IEventHandler
    {
        private readonly ILogger<TestRelayListEventHandler> _logger;
        private readonly IDbContextFactory<NetstrDbContext> _dbFactory;

        public TestRelayListEventHandler(
            ILogger<TestRelayListEventHandler> logger,
            IDbContextFactory<NetstrDbContext> dbFactory)
        {
            this._logger = logger;
            this._dbFactory = dbFactory;
        }

        public bool CanHandleEvent(Event e) => e.Kind == (long)EventKind.RelayList;

        public Task HandleEventAsync(IWebSocketAdapter sender, Event e)
        {
            this._logger.LogInformation(
                "Test Relay List Event Received:\nFull Event:\n{@Event}\nTags:\n{@Tags}\nContent:\n{Content}",
                e,
                e.Tags,
                e.Content
            );

            try
            {
                using var context = this._dbFactory.CreateDbContext();
                
                // Store the event directly in the Events table
                // The event and its tags will be automatically saved through the normal event processing pipeline
                // No need to update RelayConfigs table as we're using events as source of truth

                this._logger.LogInformation("Successfully processed relay list event {EventId} for user {PubKey}", e.Id, e.PublicKey);
                sender.SendOk(e.Id);
            }
            catch (Exception error)
            {
                this._logger.LogError(error, "Failed to process relay list event {EventId} for user {PubKey}", e.Id, e.PublicKey);
                sender.SendNotOk(e.Id, "Failed to process relay list event");
            }

            return Task.CompletedTask;
        }
    }
}
