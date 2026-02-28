namespace Netstr.Messaging.Subscriptions
{
    public readonly record struct SearchQuery(string BasicTerms, IReadOnlyList<(string Key, string Value)> Extensions)
    {
        public bool HasBasicTerms => !string.IsNullOrWhiteSpace(BasicTerms);
    }

    /// <summary>
    /// Parses NIP-50 search strings into basic terms and key:value extensions.
    /// Extensions are removed from <see cref="SearchQuery.BasicTerms"/> so unsupported extensions don't reduce recall.
    /// </summary>
    public static class SearchQueryParser
    {
        public static SearchQuery Parse(string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return new SearchQuery(string.Empty, Array.Empty<(string, string)>());
            }

            var extensions = new List<(string, string)>();
            var basicTerms = new List<string>();

            var terms = search.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var term in terms)
            {
                var colonIndex = term.IndexOf(':');
                if (colonIndex > 0 && !term.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    var key = term[..colonIndex].ToLowerInvariant();
                    var value = term[(colonIndex + 1)..];
                    extensions.Add((key, value));
                }
                else
                {
                    basicTerms.Add(term);
                }
            }

            return new SearchQuery(string.Join(' ', basicTerms).Trim(), extensions);
        }
    }
}

