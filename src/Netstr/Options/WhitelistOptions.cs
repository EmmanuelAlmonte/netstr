namespace Netstr.Options
{
    public record WhitelistOptions
    {
        /// <summary>
        /// Whether the whitelist is enabled.
        /// </summary>
        public bool Enabled { get; init; } = false;

        /// <summary>
        /// List of public keys that are allowed to interact with the relay.
        /// </summary>
        public string[] AllowedPublicKeys { get; init; } = [];

        /// <summary>
        /// Whether to apply the whitelist to publishing events.
        /// </summary>
        public bool RestrictPublishing { get; init; } = true;

        /// <summary>
        /// Whether to apply the whitelist to subscribing.
        /// </summary>
        public bool RestrictSubscribing { get; init; } = false;
        
        /// <summary>
        /// The owner's public key that cannot be removed from the whitelist.
        /// </summary>
        public string OwnerPublicKey { get; init; } = string.Empty;

        /// <summary>
        /// List of event kinds that are exempt from whitelist restrictions.
        /// </summary>
        public long[] ExemptKinds { get; init; } = [];
    }
}
