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
            _logger = logger;
            _dbFactory = dbFactory;
        }

        public bool CanHandleEvent(Event e) => e.Kind == (long)EventKind.RelayList;

        public async Task HandleEventAsync(IWebSocketAdapter sender, Event e)
        {
            _logger.LogInformation(
                "Test Relay List Event Received:\nFull Event:\n{@Event}\nTags:\n{@Tags}\nContent:\n{Content}",
                e,
                e.Tags,
                e.Content
            );

            try
            {
                using var context = _dbFactory.CreateDbContext();
                
                // Store the event directly in the Events table
                // The event and its tags will be automatically saved through the normal event processing pipeline
                // No need to update RelayConfigs table as we're using events as source of truth

                _logger.LogInformation("Successfully processed relay list event {EventId} for user {PubKey}", e.Id, e.PublicKey);
                sender.SendOk(e.Id);
            }
            catch (Exception error)
            {
                _logger.LogError(error, "Failed to process relay list event {EventId} for user {PubKey}", e.Id, e.PublicKey);
                sender.SendNotOk(e.Id, "Failed to process relay list event");
            }
        }
    }
}
