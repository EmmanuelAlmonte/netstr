using System.Text.Json.Serialization;

namespace Netstr.Messaging.Models
{
    /// <summary>
    /// User metadata structure for kind 0 events
    /// </summary>
    public class UserMetadata
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("about")]
        public string? About { get; set; }
        
        [JsonPropertyName("picture")]
        public string? Picture { get; set; }
        
        [JsonPropertyName("banner")]
        public string? Banner { get; set; }
        
        [JsonPropertyName("nip05")]
        public string? Nip05 { get; set; }
        
        [JsonPropertyName("lud06")]
        public string? Lud06 { get; set; }
        
        [JsonPropertyName("lud16")]
        public string? Lud16 { get; set; }
        
        [JsonPropertyName("website")]
        public string? Website { get; set; }
        
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
    }
}