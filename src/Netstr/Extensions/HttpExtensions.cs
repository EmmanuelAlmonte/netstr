namespace Netstr.Extensions
{
    public static class HttpExtensions
    {
        /// <summary>
        /// Gets the current normalized URL (host+path) where the relay is running. 
        /// </summary>
        public static string GetNormalizedUrl(this HttpRequest ctx)
        {
            return NormalizeRelay(ctx.Host.ToString());
        }

        private static string NormalizeRelay(string? relayUrl)
        {
            return NormalizeRelayUrl(relayUrl, removePort: true);
        }

        public static string NormalizeRelayUrl(string? relayUrl, bool removePort = false)
        {
            if (string.IsNullOrWhiteSpace(relayUrl))
            {
                return string.Empty;
            }

            var normalized = relayUrl.Trim();

            if (string.Equals(normalized, "ALL_RELAYS", StringComparison.OrdinalIgnoreCase))
            {
                return "ALL_RELAYS";
            }

            var hostOnly = normalized;

            var schemeIndex = normalized.IndexOf("://", StringComparison.Ordinal);
            if (schemeIndex >= 0)
            {
                hostOnly = normalized[(schemeIndex + 3)..];
            }

            var pathStart = hostOnly.IndexOf('/');
            if (pathStart >= 0)
            {
                hostOnly = hostOnly[..pathStart];
            }

            var queryStart = hostOnly.IndexOf('?');
            if (queryStart >= 0)
            {
                hostOnly = hostOnly[..queryStart];
            }

            if (removePort && hostOnly.StartsWith('['))
            {
                var closing = hostOnly.IndexOf(']');
                if (closing > 0)
                {
                    return hostOnly[..(closing + 1)].ToLowerInvariant();
                }
            }

            if (removePort)
            {
                var colonIndex = hostOnly.IndexOf(':');
                if (colonIndex > 0)
                {
                    hostOnly = hostOnly[..colonIndex];
                }
            }

            return hostOnly.ToLowerInvariant();
        }
    }
}
