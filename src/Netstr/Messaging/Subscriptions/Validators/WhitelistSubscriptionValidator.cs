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
        private readonly IOptionsMonitor<WhitelistOptions> options;
        private HashSet<string> allowedPublicKeys = null!;

        public WhitelistSubscriptionValidator(
            ILogger<WhitelistSubscriptionValidator> logger,
            IOptionsMonitor<WhitelistOptions> options)
        {
            this.logger = logger;
            this.options = options;
            
            // Initialize the whitelist
            this.UpdateAllowedPublicKeys(options.CurrentValue);
            
            // Subscribe to changes
            options.OnChange(UpdateAllowedPublicKeys);
        }

        private void UpdateAllowedPublicKeys(WhitelistOptions options)
        {
            this.allowedPublicKeys = new HashSet<string>(
                options.AllowedPublicKeys ?? Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
            
            this.logger.LogInformation("Subscription whitelist updated with {Count} public keys", this.allowedPublicKeys.Count);
        }

        public bool IsApplicable(FilterMessageHandlerBase handler)
        {
            // This validator is applicable to all filter message handlers
            return true;
        }

        public string? CanSubscribe(string id, ClientContext context, IEnumerable<SubscriptionFilter> filters)
        {
            var whitelistOptions = this.options.CurrentValue;
            
            if (!whitelistOptions.Enabled || !whitelistOptions.RestrictSubscribing)
            {
                return null;
            }

            // If client is not authenticated, we can't check the public key
            if (!context.IsAuthenticated())
            {
                return "auth-required: authentication required for subscription";
            }

            if (!context.AuthenticatedPublicKeys.Any(contextKey => this.allowedPublicKeys.Contains(contextKey)))
            {
                this.logger.LogWarning("Rejected subscription from non-whitelisted public key(s): {Keys}", string.Join(", ", context.AuthenticatedPublicKeys));
                return Messages.WhitelistRestricted;
            }

            return null;
        }
    }
}
