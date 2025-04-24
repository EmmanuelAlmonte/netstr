using System.Collections.Generic;
using System.Linq;

namespace Netstr.Messaging.Models
{
    /// <summary>
    /// Extension methods for working with NIP-57 Zap events.
    /// </summary>
    public static class ZapEventExtensions
    {
        /// <summary>
        /// Determines if the event is a Zap Request.
        /// </summary>
        public static bool IsZapRequest(this Event e) => e.Kind == (long)EventKind.ZapRequest;
        
        /// <summary>
        /// Determines if the event is a Zap Receipt.
        /// </summary>
        public static bool IsZapReceipt(this Event e) => e.Kind == (long)EventKind.ZapReceipt;
        
        /// <summary>
        /// Gets the recipient's public key from a Zap event.
        /// </summary>
        public static string? GetRecipientPubkey(this Event e) => 
            e.Tags.FirstOrDefault(t => t.Length > 1 && t[0] == EventTag.PublicKey)?[1];
        
        /// <summary>
        /// Gets the bolt11 invoice from a Zap Receipt event.
        /// </summary>
        public static string? GetBolt11(this Event e) => 
            e.Tags.FirstOrDefault(t => t.Length > 1 && t[0] == EventTag.Bolt11)?[1];
        
        /// <summary>
        /// Gets the amount in millisats from a Zap event.
        /// </summary>
        public static string? GetAmount(this Event e) => 
            e.Tags.FirstOrDefault(t => t.Length > 1 && t[0] == EventTag.Amount)?[1];
        
        /// <summary>
        /// Gets the relay URLs from a Zap Request event.
        /// </summary>
        public static IEnumerable<string> GetRelayUrls(this Event e) => 
            e.Tags.FirstOrDefault(t => t.Length > 1 && t[0] == EventTag.Relays)?.Skip(1) ?? Array.Empty<string>();
    }
}
