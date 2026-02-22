using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netstr.Data;
using Netstr.Messaging.Models;
using Netstr.Messaging.Subscriptions;
using Netstr.Messaging.Subscriptions.Validators;
using Netstr.Options;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Netstr.Messaging.MessageHandlers
{
    /// <summary>
    /// Handler which processes REQ messages.
    /// </summary>
    public class SubscribeMessageHandler : FilterMessageHandlerBase
    {
        private static readonly Regex DummyIdPattern = new Regex("^a{64}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly IDbContextFactory<NetstrDbContext> db;

        public SubscribeMessageHandler(
            IDbContextFactory<NetstrDbContext> db,
            IEnumerable<ISubscriptionRequestValidator> validators,
            IOptions<LimitsOptions> limits,
            IOptions<AuthOptions> auth,
            ILogger<SubscribeMessageHandler> logger)
            : base(validators, limits, auth, logger)
        {
            this.db = db;
        }

        protected override string AcceptedMessageType => MessageType.Req;

        protected override async Task HandleMessageCoreAsync(
            IWebSocketAdapter adapter,
            string subscriptionId,
            IEnumerable<SubscriptionFilter> filters,
            IEnumerable<JsonDocument> remainingParameters)
        {
            // Detect and ignore nostr-tools dummy connectivity probe
            // nostr-tools sends REQ with ids: ["aaaa...64 times"] as a connectivity test
            if (IsDummyProbe(filters))
            {
                this.logger.LogDebug("Ignored dummy subscription {SubscriptionId} from {ClientId} (connectivity probe)",
                    subscriptionId, adapter.Context.ClientId);

                // Send NOTICE to inform client (optional but helpful)
                adapter.SendNotice(Messages.IgnoredDummyProbe);

                // Send EOSE to maintain proper NIP-01 flow
                adapter.SendEose(subscriptionId);

                return; // Short-circuit - no DB query or subscription creation
            }

            var maxSubscriptions = this.limits.Value.Subscriptions.MaxSubscriptions;
            if (maxSubscriptions > 0 && adapter.Subscriptions.GetAll().Where(x => x.Key != subscriptionId).Count() >= maxSubscriptions)
            {
                throw new SubscriptionProcessingException(subscriptionId, Messages.InvalidTooManySubscriptions);
            }

            using var context = this.db.CreateDbContext();

            // add sub
            var subscription = adapter.Subscriptions.Add(subscriptionId, filters);

            // get stored events
            var entities = await GetFilteredEvents(context, filters, adapter.Context.PublicKey).ToArrayAsync();
            var events = entities.Select(CreateEvent).ToArray();

            this.logger.LogInformation($"Found {entities.Length} stored events for subscription {subscriptionId}");
            if (entities.Length > 0)
            {
                this.logger.LogInformation($"First event: {entities[0].EventId}, Kind: {entities[0].EventKind}");
            }

            // send stored events (also sends EOSE)
            subscription.SendStoredEvents(events);
        }

        private Event CreateEvent(EventEntity e)
        {
            return new Event
            {
                Id = e.EventId,
                Content = e.EventContent,
                CreatedAt = e.EventCreatedAt,
                Kind = e.EventKind,
                PublicKey = e.EventPublicKey,
                Signature = e.EventSignature,
                Tags = e.Tags.Select(tag =>
                {
                    if (tag.Value == null)
                    {
                        return (string[])[tag.Name];
                    };

                    return (string[])[tag.Name, tag.Value, ..tag.OtherValues];
                }).ToArray()
            };
        }

        private static bool IsDummyProbe(IEnumerable<SubscriptionFilter> filters)
        {
            // Check if any filter contains a single id matching the dummy pattern "aaaa...64 times"
            return filters.Any(filter =>
                filter.Ids != null &&
                filter.Ids.Length > 0 &&
                filter.Ids.Any(id => !string.IsNullOrEmpty(id) && DummyIdPattern.IsMatch(id))
            );
        }
    }
}
