using Netstr.Messaging.Models;
using System.Text.RegularExpressions;

namespace Netstr.Messaging.Events.Validators
{
    /// <summary>
    /// Validates NIP-02 Follow List events (kind 3).
    /// Follow lists contain "p" tags referencing other users' public keys.
    /// Content is not used per spec but may contain data for backwards compatibility.
    /// </summary>
    public class FollowListValidator : IEventValidator
    {
        private const string InvalidPubkeyFormat = "invalid: follow list contains invalid pubkey format";
        private const string InvalidRelayUrl = "invalid: follow list contains invalid relay URL";
        private const string InvalidTagFormat = "invalid: follow list must only contain 'p' tags";

        // Regex for validating 64-character hex pubkeys
        private static readonly Regex HexPubkeyPattern = new(@"^[0-9a-fA-F]{64}$", RegexOptions.Compiled);

        public string? Validate(Event e, ClientContext context)
        {
            // Only validate follow list events (kind 3)
            if (e.Kind != (long)EventKind.FollowList)
            {
                return null;
            }

            // NIP-02: Content is not used but may contain JSON for backwards compatibility
            // We don't validate content - it can be empty or contain relay data

            // Validate tags
            foreach (var tag in e.Tags)
            {
                if (tag.Length == 0)
                {
                    continue; // Skip empty tags
                }

                // Follow list should only contain "p" tags
                if (tag[0] != EventTag.PublicKey)
                {
                    return InvalidTagFormat;
                }

                // "p" tag must have at least the pubkey
                if (tag.Length < 2)
                {
                    return InvalidPubkeyFormat;
                }

                // Validate pubkey format (64-char hex)
                var pubkey = tag[1];
                if (string.IsNullOrEmpty(pubkey) || !HexPubkeyPattern.IsMatch(pubkey))
                {
                    return InvalidPubkeyFormat;
                }

                // If relay URL is provided (optional), validate it
                if (tag.Length >= 3 && !string.IsNullOrEmpty(tag[2]))
                {
                    var relayUrl = tag[2];
                    if (!Uri.IsWellFormedUriString(relayUrl, UriKind.Absolute))
                    {
                        return InvalidRelayUrl;
                    }
                }

                // Petname (tag[3]) is optional and can be any string, no validation needed
            }

            return null;
        }
    }
}
