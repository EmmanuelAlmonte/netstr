using Microsoft.Extensions.Options;
using Netstr.Messaging.Models;
using Netstr.Options;
using System.Linq;

namespace Netstr.Messaging.Events.Validators
{
    /// <summary>
    /// Validates NIP-57 Zap events.
    /// </summary>
    public class ZapEventValidator : IEventValidator
    {
        private const string InvalidZapReceiptTags = "invalid: zap receipt missing required tags";

        public string? Validate(Event e, ClientContext context)
        {
            return (EventKind)e.Kind switch
            {
                EventKind.ZapRequest => Messages.InvalidZapRequestRelayPublish,
                EventKind.ZapReceipt => ValidateZapReceipt(e),
                _ => null // Not a zap event
            };
        }

        private static string? ValidateZapReceipt(Event e)
        {
            // Validate required tags: p (recipient), bolt11, description
            bool hasRecipient = e.Tags.Any(t => t.Length > 0 && t[0] == EventTag.PublicKey);
            bool hasBolt11 = e.Tags.Any(t => t.Length > 0 && t[0] == EventTag.Bolt11);
            bool hasDescription = e.Tags.Any(t => t.Length > 0 && t[0] == EventTag.Description);
            
            return (hasRecipient && hasBolt11 && hasDescription) ? null : InvalidZapReceiptTags;
        }
    }
}
