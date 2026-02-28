using Microsoft.EntityFrameworkCore;
using Netstr.Data;
using Netstr.Messaging.Subscriptions;
using System.Linq.Expressions;

namespace Netstr.Messaging.Events
{
    public static class DbExtensions
    {
        public static Task<bool> IsDeleted(this DbSet<EventEntity> db, string id)
        {
            return db.AnyAsync(x => x.EventId == id && x.DeletedAt.HasValue);
        }

        /// <summary>
        /// Filters events by search term (NIP-50).
        /// </summary>
        public static IQueryable<EventEntity> WhereMatchesSearch(
            this IQueryable<EventEntity> query,
            string? searchTerm)
        {
            return WhereMatchesSearch(query, searchTerm, useFullTextSearch: true);
        }

        /// <summary>
        /// Filters events by search term. For PostgreSQL, full-text search can be enabled via <paramref name="useFullTextSearch"/>.
        /// For other providers (e.g. SQLite tests), falls back to a simple case-insensitive substring match.
        /// </summary>
        public static IQueryable<EventEntity> WhereMatchesSearch(
            this IQueryable<EventEntity> query,
            string? searchTerm,
            bool useFullTextSearch)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return query;
            }

            var parsed = SearchQueryParser.Parse(searchTerm);
            if (string.IsNullOrWhiteSpace(parsed.BasicTerms))
            {
                // Only extensions (key:value) present; unsupported extensions must not reduce recall.
                return query;
            }

            var basicTerms = parsed.BasicTerms.Trim();

            if (useFullTextSearch)
            {
                // Convert search term to tsquery format (AND semantics).
                var tsQuery = ConvertToTsQuery(basicTerms);

                return query.Where(e =>
                    EF.Functions.ToTsVector("english", e.EventContent)
                        .Matches(EF.Functions.ToTsQuery("english", tsQuery)));
            }

            // Provider-agnostic fallback: require all basic terms as substrings.
            var terms = basicTerms
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var term in terms)
            {
                var local = term;
                query = query.Where(e => e.EventContent.ToLower().Contains(local));
            }

            return query;
        }

        /// <summary>
        /// Applies NIP-50 "quality" ordering for search results when full-text search is enabled.
        /// Falls back to the standard NIP-01 ordering (created_at desc, id asc).
        /// </summary>
        public static IQueryable<EventEntity> OrderBySearchQuality(
            this IQueryable<EventEntity> query,
            string? searchTerm,
            bool useFullTextSearch)
        {
            var parsed = SearchQueryParser.Parse(searchTerm);
            if (useFullTextSearch && !string.IsNullOrWhiteSpace(parsed.BasicTerms))
            {
                var basicTerms = parsed.BasicTerms.Trim();
                var tsQuery = ConvertToTsQuery(basicTerms);

                return query
                    .OrderByDescending(e =>
                        EF.Functions.ToTsVector("english", e.EventContent)
                            .RankCoverDensity(EF.Functions.ToTsQuery("english", tsQuery)))
                    .ThenByDescending(e => e.EventCreatedAt)
                    .ThenBy(e => e.EventId);
            }

            return query
                .OrderByDescending(e => e.EventCreatedAt)
                .ThenBy(e => e.EventId);
        }

        /// <summary>
        /// Converts a basic term string to PostgreSQL tsquery format
        /// </summary>
        private static string ConvertToTsQuery(string basicTerms)
        {
            // Split terms and join with AND operator
            var terms = basicTerms.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(term => term.Replace("'", "''")) // Escape single quotes
                .Where(term => !string.IsNullOrWhiteSpace(term))
                .Select(term => $"'{term}'")
                .ToArray();

            return string.Join(" & ", terms);
        }
    }
}
