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
        private readonly IOptions<WhitelistOptions> options;
        private readonly HashSet<string> allowedPublicKeys;

        public WhitelistValidator(
            ILogger<WhitelistValidator> logger,
            IOptions<WhitelistOptions> options)
        {
            this.logger = logger;
            this.options = options;
            this.allowedPublicKeys = new HashSet<string>(
                options.Value.AllowedPublicKeys ?? Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
        }

        public string? Validate(Event e, ClientContext context)
        {
            var whitelistOptions = this.options.Value;
            
            if (!whitelistOptions.Enabled || !whitelistOptions.RestrictPublishing)
            {
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
