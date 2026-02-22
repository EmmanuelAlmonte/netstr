using Microsoft.Extensions.Options;
using Netstr.Messaging.Models;
using Netstr.Options;

namespace Netstr.Messaging.Events.Validators
{
    /// <summary>
    /// Validates that the event's public key is in the whitelist if whitelist is enabled.
    /// </summary>
    public class WhitelistValidator : IEventValidator
    {
        private readonly ILogger<WhitelistValidator> logger;
        private readonly IOptionsMonitor<WhitelistOptions> options;
        private HashSet<string> allowedPublicKeys = null!;

        public WhitelistValidator(
            ILogger<WhitelistValidator> logger,
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
            
            this.logger.LogInformation("Whitelist updated with {Count} public keys", this.allowedPublicKeys.Count);
        }

        public string? Validate(Event e, ClientContext context)
        {
            var whitelistOptions = this.options.CurrentValue;
            
            if (!whitelistOptions.Enabled || !whitelistOptions.RestrictPublishing)
            {
                return null;
            }

            // Check if this event kind is exempt from whitelist restrictions
            if (whitelistOptions.ExemptKinds.Contains(e.Kind))
            {
                this.logger.LogInformation($"Event kind {e.Kind} is exempt from whitelist restrictions");
                return null;
            }

            if (!this.allowedPublicKeys.Contains(e.PublicKey))
            {
                this.logger.LogWarning($"Rejected event from non-whitelisted public key: {e.PublicKey}");
                return Messages.WhitelistRestricted;
            }

            return null;
        }
    }
}
