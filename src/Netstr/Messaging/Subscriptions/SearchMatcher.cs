using Netstr.Messaging.Models;

namespace Netstr.Messaging.Subscriptions
{
    /// <summary>
     /// Utility class for matching events against search terms (NIP-50)
     /// </summary>
    public static class SearchMatcher
    {
        /// <summary>
        /// Checks if an event matches the given search term
        /// </summary>
        /// <param name="eventItem">The event to match</param>
        /// <param name="searchTerm">The search term to match against</param>
        /// <returns>True if the event matches the search term</returns>
        public static bool MatchesSearch(Event eventItem, string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(eventItem.Content))
            {
                return false;
            }

            var content = eventItem.Content.ToLowerInvariant();
            var parsed = SearchQueryParser.Parse(searchTerm);

            // NIP-50 extensions are optional; unsupported extensions must be ignored.
            foreach (var (key, value) in parsed.Extensions)
            {
                if (!ApplyExtension(key, value))
                {
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(parsed.BasicTerms))
            {
                return true;
            }

            // Basic text search - split on spaces and require all terms.
            var terms = parsed.BasicTerms.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return terms.All(term => content.Contains(term));
        }

        private static bool ApplyExtension(string key, string value)
        {
            // NIP-50: include:spam turns off spam filtering. We don't exclude spam today, so it's a no-op.
            if (key.Equals("include", StringComparison.OrdinalIgnoreCase) &&
                value.Equals("spam", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return true;
        }
    }
}
