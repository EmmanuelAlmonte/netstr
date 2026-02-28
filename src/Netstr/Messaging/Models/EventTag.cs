namespace Netstr.Messaging.Models
{
    public static class EventTag
    {
        public const string Event = "e";
        public const string ReplaceableEvent = "a";
        public const string Kind = "k";
        public const string PublicKey = "p";
        public const string Deduplication = "d";
        public const string Nonce = "nonce";
        public const string Challenge = "challenge";
        public const string Relay = "r";
        public const string AuthRelay = "relay";  // NIP-42 AUTH events use full "relay" tag
        public const string Protected = "-";
        public const string Expiration = "expiration";
        
        // NIP-57 Zap tags
        public const string Amount = "amount";
        public const string Bolt11 = "bolt11";
        public const string Description = "description";
        public const string Preimage = "preimage";
        public const string Lnurl = "lnurl";
        public const string Relays = "relays";
    }
}
