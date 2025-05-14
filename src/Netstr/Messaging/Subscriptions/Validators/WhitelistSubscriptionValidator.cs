using Microsoft.Extensions.Options;
using Netstr.Messaging.MessageHandlers;
using Netstr.Messaging.Models;
using Netstr.Options;

namespace Netstr.Messaging.Subscriptions.Validators
{
    /// <summary>
    /// Validates that the subscriber's public key is in the whitelist if whitelist is enabled.
    /// </summary>
    public class WhitelistSubscriptionValidator : ISubscriptionRequestValidator
    {
        private readonly ILogger<WhitelistSubscriptionValidator> logger;
        private readonly IOptions<WhitelistOptions> options;
        private readonly HashSet<string> allowedPublicKeys;

        public WhitelistSubscriptionValidator(
            ILogger<WhitelistSubscriptionValidator> logger,
            IOptions<WhitelistOptions> options)
        {
            this.logger = logger;
            this.options = options;
            this.allowedPublicKeys = new HashSet<string>(
                options.Value.AllowedPublicKeys ?? Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
        }

        public bool IsApplicable(FilterMessageHandlerBase handler)
        {
            // This validator is applicable to all filter message handlers
            return true;
        }

        public string? CanSubscribe(string id, ClientContext context, IEnumerable<SubscriptionFilter> filters)
        {
            var whitelistOptions = this.options.Value;
            
            if (!whitelistOptions.Enabled || !whitelistOptions.RestrictSubscribing)
            {
                return null;
            }

            // If client is not authenticated, we can't check the public key
            if (!context.IsAuthenticated())
            {
                return "auth-required: authentication required for subscription";
            }

            if (!this.allowedPublicKeys.Contains(context.PublicKey))
            {
                this.logger.LogWarning($"Rejected subscription from non-whitelisted public key: {context.PublicKey}");
                return Messages.WhitelistRestricted;
            }

            return null;
        }
    }
}
