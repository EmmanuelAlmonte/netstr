using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netstr.Data;
using Netstr.Json;
using Netstr.Messaging.Models;
using Netstr.Messaging.Negentropy;
using Netstr.Messaging.Subscriptions.Validators;
using Netstr.Options;
using Netstr.Options.Limits;
using System.Text.Json;

namespace Netstr.Messaging.MessageHandlers.Negentropy
{
    public class NegentropyOpenHandler : FilterMessageHandlerBase
    {
        private readonly IDbContextFactory<NetstrDbContext> db;

        public NegentropyOpenHandler(
            IDbContextFactory<NetstrDbContext> db,
            IEnumerable<ISubscriptionRequestValidator> validators,
            IOptions<LimitsOptions> limits,
            IOptions<AuthOptions> auth,
            IOptions<FiltersOptions> filters,
            ILogger<NegentropyOpenHandler> logger)
            : base(validators, limits, auth, filters, logger)
        {
            this.db = db;
        }

        protected override string AcceptedMessageType => MessageType.Negentropy.Open;

        protected override bool SingleFilter => true;

        protected override (SubscriptionFilter[] Filters, int ConsumedFilterParameters) ParseFilters(
            string subscriptionId,
            JsonDocument[] parameters)
        {
            var filtersParameter = parameters[2].RootElement;

            if (filtersParameter.ValueKind != JsonValueKind.Array)
            {
                return base.ParseFilters(subscriptionId, parameters);
            }

            var filters = new List<SubscriptionFilter>();

            foreach (var filterElement in filtersParameter.EnumerateArray())
            {
                if (filterElement.ValueKind != JsonValueKind.Object)
                {
                    RaiseSubscriptionException(subscriptionId, Messages.InvalidCannotProcessFilters);
                }

                using var filterDoc = JsonDocument.Parse(filterElement.GetRawText());
                filters.Add(GetSubscriptionFilter(subscriptionId, filterDoc));
            }

            if (filters.Count == 0)
            {
                RaiseSubscriptionException(subscriptionId, Messages.InvalidCannotProcessFilters);
            }

            // For NEG-OPEN we consume exactly one parameter for filters (object or array),
            // and whatever follows belongs to the negentropy query payload.
            return (filters.ToArray(), 1);
        }

        protected override async Task HandleMessageCoreAsync(
            IWebSocketAdapter adapter, 
            string subscriptionId, 
            IEnumerable<SubscriptionFilter> filters,
            IEnumerable<JsonDocument> remainingParameters)
        {
            var maxSubscriptions = this.limits.Value.Negentropy.MaxSubscriptions;
            if (maxSubscriptions > 0 && adapter.Negentropy.GetOpenSubscriptions().Where(x => x != subscriptionId).Count() >= maxSubscriptions)
            {
                adapter.SendNegentropyError(subscriptionId, Messages.InvalidTooManySubscriptions);
                return;
            }

            using var context = this.db.CreateDbContext();
            
            var query = remainingParameters.First().DeserializeRequired<string>();
            var events = await GetFilteredEvents(context, filters, adapter.Context.AuthenticatedPublicKeys)
                .Select(x => new NegentropyEvent(x.EventId, x.EventCreatedAt.UtcTicks))
                .ToArrayAsync();

            try
            {
                adapter.Negentropy.Open(subscriptionId, query, events);
            }
            catch (Exception ex)
            {
                throw new NegentropyProcessingException(subscriptionId, Messages.Negentropy.InvalidMessage, ex.Message);
            }
        }

        protected override void RaiseSubscriptionException(string subscriptionId, string message, string? logMessage = null)
        {
            throw new NegentropyProcessingException(subscriptionId, message, logMessage);
        }

        protected override SubscriptionLimits GetLimits()
        {
            return this.limits.Value.Negentropy;
        }
    }
}
