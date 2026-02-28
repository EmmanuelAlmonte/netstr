using Microsoft.Extensions.Options;
using Netstr.Options;

namespace Netstr.Messaging.Subscriptions
{
    public interface ISubscriptionsAdapterFactory
    {
        ISubscriptionsAdapter CreateAdapter(IWebSocketAdapter webSocketAdapter);
    }

    public class SubscriptionsAdapterFactory : ISubscriptionsAdapterFactory
    {
        private readonly ILogger<SubscriptionsAdapter> logger;
        private readonly IOptions<LimitsOptions> limits;

        public SubscriptionsAdapterFactory(ILogger<SubscriptionsAdapter> logger, IOptions<LimitsOptions> limits)
        {
            this.logger = logger;
            this.limits = limits;
        }

        public ISubscriptionsAdapter CreateAdapter(IWebSocketAdapter webSocketAdapter)
        {
            return new SubscriptionsAdapter(this.logger, webSocketAdapter, this.limits);
        }
    }
}
