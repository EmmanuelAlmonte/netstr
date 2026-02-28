using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Netstr.Data
{
    /// <summary>
    /// Entity representing a relay configuration for a user according to NIP-65.
    /// </summary>
    public class RelayConfigEntity
    {
        /// <summary>
        /// Primary key for the relay configuration.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The public key of the user who owns this relay configuration.
        /// </summary>
        [Required]
        [MaxLength(64)]
        public string PubKey { get; set; } = string.Empty;

        /// <summary>
        /// The URL of the relay.
        /// </summary>
        [Required]
        [MaxLength(2048)]
        public string RelayUrl { get; set; } = string.Empty;

        /// <summary>
        /// Whether this relay is used for reading.
        /// </summary>
        public bool Read { get; set; }

        /// <summary>
        /// Whether this relay is used for writing.
        /// </summary>
        public bool Write { get; set; }

        /// <summary>
        /// When this configuration was last updated.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Creates a new relay configuration entity.
        /// </summary>
        public RelayConfigEntity() { }

        /// <summary>
        /// Creates a new relay configuration entity with the specified values.
        /// </summary>
        public RelayConfigEntity(string pubKey, string relayUrl, bool read, bool write)
        {
            PubKey = pubKey;
            RelayUrl = relayUrl;
            Read = read;
            Write = write;
            LastUpdated = DateTime.UtcNow;
        }
    }
}
