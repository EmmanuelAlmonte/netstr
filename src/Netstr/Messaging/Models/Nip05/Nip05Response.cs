using System.Text.Json.Serialization;

namespace Netstr.Messaging.Models.Nip05
{
    /// <summary>
    /// Response format for NIP-05 DNS-based identity verification
    /// from /.well-known/nostr.json endpoints
    /// </summary>
    public class Nip05Response
    {
        /// <summary>
        /// Mapping of names to public keys
        /// </summary>
        [JsonPropertyName("names")]
        public Dictionary<string, string>? Names { get; set; }
        
        /// <summary>
        /// Optional mapping of public keys to relay URLs
        /// </summary>
        [JsonPropertyName("relays")]
        public Dictionary<string, string[]>? Relays { get; set; }
    }
}