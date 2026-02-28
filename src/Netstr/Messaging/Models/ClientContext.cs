namespace Netstr.Messaging.Models
{
    /// <summary>
    /// Holds basic info about a client.
    /// </summary>
    public class ClientContext
    {
        private readonly object authenticatedPublicKeysLock = new();
        private readonly HashSet<string> authenticatedPublicKeys = [];

        public ClientContext(string clientId, string ipAddress)
        {
            ClientId = clientId;
            IpAddress = ipAddress;
            Challenge = Guid.NewGuid().ToString();
        }

        public string ClientId { get; }

        public string IpAddress { get; }

        public string Challenge { get; }

        public IReadOnlyCollection<string> AuthenticatedPublicKeys
        {
            get
            {
                lock (this.authenticatedPublicKeysLock)
                {
                    return this.authenticatedPublicKeys.ToArray();
                }
            }
        }

        public string? PublicKey
        {
            get
            {
                return this.AuthenticatedPublicKeys.FirstOrDefault();
            }
        }

        public bool IsAuthenticated() => this.AuthenticatedPublicKeys.Count > 0;

        public bool IsAuthenticated(string publicKey)
        {
            lock (this.authenticatedPublicKeysLock)
            {
                return this.authenticatedPublicKeys.Contains(publicKey);
            }
        }

        public bool IsAuthenticatedForAny(params string[] publicKeys)
        {
            lock (this.authenticatedPublicKeysLock)
            {
                return publicKeys.Any(publicKey => this.authenticatedPublicKeys.Contains(publicKey));
            }
        }

        public bool IsAuthenticatedForAny(IEnumerable<string> publicKeys)
        {
            lock (this.authenticatedPublicKeysLock)
            {
                return publicKeys.Any(publicKey => this.authenticatedPublicKeys.Contains(publicKey));
            }
        }

        public void Authenticate(string publicKey)
        {
            if (string.IsNullOrWhiteSpace(publicKey))
            {
                throw new ArgumentException("public key cannot be null or whitespace", nameof(publicKey));
            }

            lock (this.authenticatedPublicKeysLock)
            {
                this.authenticatedPublicKeys.Add(publicKey);
            }
        }

        public override string ToString()
        {
            return $"Id: {ClientId}, IP: {IpAddress}, PublicKeys: [{string.Join(", ", this.AuthenticatedPublicKeys)}]";
        }
    }
}
