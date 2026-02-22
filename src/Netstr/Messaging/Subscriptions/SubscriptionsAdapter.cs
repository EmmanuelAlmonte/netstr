using Microsoft.Extensions.Options;
using Netstr.Messaging.Models;
using Netstr.Options;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Netstr.Messaging.Subscriptions
{
    public interface ISubscriptionsAdapter : IDisposable
    {
        SubscriptionAdapter Add(string id, IEnumerable<SubscriptionFilter> filters);

        void RemoveById(string id);

        IDictionary<string, SubscriptionAdapter> GetAll();
    }

    public class SubscriptionsAdapter : ISubscriptionsAdapter
    {
        private readonly ConcurrentDictionary<string, SubscriptionAdapter> subscriptions;
        private readonly ILogger<SubscriptionsAdapter> logger;
        private readonly IWebSocketAdapter ws;
        private readonly int maxQueueSize;

        public SubscriptionsAdapter(ILogger<SubscriptionsAdapter> logger, IWebSocketAdapter webSocketAdapter, IOptions<LimitsOptions> limits)
        {
            this.subscriptions = new();
            this.logger = logger;
            this.ws = webSocketAdapter;
            // Ensure a minimum queue size of 100 if not configured
            this.maxQueueSize = Math.Max(limits.Value.Events.MaxPendingEvents, 100);
        }

        public SubscriptionAdapter Add(string id, IEnumerable<SubscriptionFilter> filters)
        {
            return this.subscriptions.AddOrUpdate(
                id,
                x =>
                {
                    this.logger.LogInformation($"Adding new subscription {x} for client {this.ws.Context.ClientId}");
                    return new SubscriptionAdapter(this.ws, x, filters.ToArray(), this.maxQueueSize);
                },
                (x, existing) =>
                {
                    this.logger.LogInformation($"Replacing existing subscription {x} for client {this.ws.Context.ClientId}");
                    existing.Dispose();
                    return new SubscriptionAdapter(this.ws, x, filters.ToArray(), this.maxQueueSize);
                });
        }

        public void Dispose()
        {
            foreach (var adapter in this.subscriptions.Values)
            {
                adapter.Dispose();
            }

            this.subscriptions.Clear();
        }

        public IDictionary<string, SubscriptionAdapter> GetAll()
        {
            return this.subscriptions.ToImmutableDictionary(x => x.Key, x => x.Value);
        }

        public void RemoveById(string id)
        {
            if (this.subscriptions.TryRemove(id, out var subscription))
            {
                this.logger.LogInformation($"Removing subscription {id} for client {this.ws.Context.ClientId}");

                subscription?.Dispose();
            }
        }
    }
}
