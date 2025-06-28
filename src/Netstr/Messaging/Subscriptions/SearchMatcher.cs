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
                return true;

            if (string.IsNullOrWhiteSpace(eventItem.Content))
                return false;

            var content = eventItem.Content.ToLowerInvariant();
            var normalizedSearchTerm = searchTerm.ToLowerInvariant().Trim();

            // Check for advanced search extensions
            if (normalizedSearchTerm.Contains(':'))
            {
                return MatchesAdvancedSearch(eventItem, normalizedSearchTerm);
            }

            // Basic text search - split on spaces and require all terms
            var terms = normalizedSearchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return terms.All(term => content.Contains(term));
        }

        /// <summary>
        /// Handles advanced search with extensions like "include:spam", "domain:example.com"
        /// </summary>
        private static bool MatchesAdvancedSearch(Event eventItem, string searchTerm)
        {
            var parts = ParseSearchTerms(searchTerm);
            var content = eventItem.Content.ToLowerInvariant();

            foreach (var (extension, value) in parts.Extensions)
            {
                if (!ApplySearchExtension(eventItem, extension, value))
                    return false;
            }

            // Apply basic text search if there are remaining terms
            if (!string.IsNullOrEmpty(parts.BasicSearch))
            {
                var terms = parts.BasicSearch.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (!terms.All(term => content.Contains(term)))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Parses search terms into extensions and basic text search
        /// </summary>
        private static (string BasicSearch, List<(string Extension, string Value)> Extensions) ParseSearchTerms(string searchTerm)
        {
            var extensions = new List<(string, string)>();
            var basicTerms = new List<string>();

            var terms = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var term in terms)
            {
                if (term.Contains(':') && !term.StartsWith("http"))
                {
                    var colonIndex = term.IndexOf(':');
                    var extension = term[..colonIndex];
                    var value = term[(colonIndex + 1)..];
                    extensions.Add((extension, value));
                }
                else
                {
                    basicTerms.Add(term);
                }
            }

            return (string.Join(' ', basicTerms), extensions);
        }

        /// <summary>
        /// Applies a search extension filter
        /// </summary>
        private static bool ApplySearchExtension(Event eventItem, string extension, string value)
        {
            return extension.ToLowerInvariant() switch
            {
                "include" => ApplyIncludeFilter(eventItem, value),
                "domain" => ApplyDomainFilter(eventItem, value),
                "kind" => ApplyKindFilter(eventItem, value),
                "since" => ApplySinceFilter(eventItem, value),
                "until" => ApplyUntilFilter(eventItem, value),
                _ => true // Unknown extensions are ignored
            };
        }

        private static bool ApplyIncludeFilter(Event eventItem, string value)
        {
            // Include filter for specific content types
            var content = eventItem.Content.ToLowerInvariant();
            return value.ToLowerInvariant() switch
            {
                "spam" => false, // Could integrate with spam detection
                "replies" => eventItem.Tags.Any(tag => tag.Length > 1 && tag[0] == "e"),
                "mentions" => eventItem.Tags.Any(tag => tag.Length > 1 && tag[0] == "p"),
                _ => content.Contains(value.ToLowerInvariant())
            };
        }

        private static bool ApplyDomainFilter(Event eventItem, string domain)
        {
            // Filter by domain mentioned in content
            var content = eventItem.Content.ToLowerInvariant();
            return content.Contains(domain.ToLowerInvariant());
        }

        private static bool ApplyKindFilter(Event eventItem, string kindValue)
        {
            if (long.TryParse(kindValue, out var kind))
            {
                return eventItem.Kind == kind;
            }
            return false;
        }

        private static bool ApplySinceFilter(Event eventItem, string sinceValue)
        {
            if (long.TryParse(sinceValue, out var sinceTimestamp))
            {
                var sinceDate = DateTimeOffset.FromUnixTimeSeconds(sinceTimestamp);
                return eventItem.CreatedAt >= sinceDate;
            }
            return false;
        }

        private static bool ApplyUntilFilter(Event eventItem, string untilValue)
        {
            if (long.TryParse(untilValue, out var untilTimestamp))
            {
                var untilDate = DateTimeOffset.FromUnixTimeSeconds(untilTimestamp);
                return eventItem.CreatedAt <= untilDate;
            }
            return false;
        }
    }
}