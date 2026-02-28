using System;
using Netstr.Messaging.Models;

namespace Netstr.Messaging.Events.Validators
{
    /// <summary>
    /// Validator for NIP-65 Relay List events (kind: 10002).
    /// </summary>
    public static class RelayListValidator
    {
        /// <summary>
        /// Validates a relay list event according to NIP-65 specifications.
        /// Each tag should be in the format: ["r", "relay_url", "read"/"write"].
        /// </summary>
        /// <param name="event">The event to validate</param>
        /// <exception cref="EventProcessingException">Thrown when the event format is invalid</exception>
        public static void Validate(Event @event)
        {
            if (@event.Kind != (long)EventKind.RelayList)
            {
                throw new EventProcessingException(@event, "Event must be of kind 10002 (RelayList)");
            }

            ArgumentNullException.ThrowIfNull(@event.Tags, nameof(@event.Tags));

            if (@event.Tags.Count() == 0)
            {
                throw new EventProcessingException(@event, "Relay list event must contain at least one relay tag");
            }

            foreach (var tag in @event.Tags)
            {
                ArgumentNullException.ThrowIfNull(tag, "Tag array cannot be null");

                if (tag.Length < 2)
                {
                    throw new EventProcessingException(@event, "Each tag must contain at least 'r' and a relay URL");
                }

                var tagType = tag[0];
                if (tagType == null || tagType != "r")
                {
                    throw new EventProcessingException(@event, "Each tag must start with 'r'");
                }

                var url = tag[1];
                if (url == null || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    throw new EventProcessingException(@event, $"Invalid relay URL format: {url ?? "null"}");
                }

                // Validate read/write markers if present
                if (tag.Length > 2)
                {
                    for (int i = 2; i < tag.Length; i++)
                    {
                        var marker = tag[i];
                        if (marker == null || (marker != "read" && marker != "write"))
                        {
                            throw new EventProcessingException(@event, $"Invalid relay permission marker: {marker ?? "null"}. Must be 'read' or 'write'");
                        }
                    }
                }
            }
        }
    }
}
