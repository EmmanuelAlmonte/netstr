using Microsoft.EntityFrameworkCore;
using Netstr.Data;
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
        /// Filters events by search term using PostgreSQL full-text search (NIP-50)
        /// </summary>
        public static IQueryable<EventEntity> WhereMatchesSearch(
            this IQueryable<EventEntity> query, 
            string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return query;

            var normalizedSearchTerm = searchTerm.Trim();

            // Use PostgreSQL full-text search for better performance
            try
            {
                // Convert search term to tsquery format
                var tsQuery = ConvertToTsQuery(normalizedSearchTerm);
                
                return query.Where(e => 
                    EF.Functions.ToTsVector("english", e.EventContent)
                        .Matches(EF.Functions.ToTsQuery("english", tsQuery)));
            }
            catch
            {
                // Fallback to simple LIKE search if full-text search fails
                return query.Where(e => e.EventContent.ToLower().Contains(normalizedSearchTerm.ToLower()));
            }
        }

        /// <summary>
        /// Converts a search term to PostgreSQL tsquery format
        /// </summary>
        private static string ConvertToTsQuery(string searchTerm)
        {
            // Split terms and join with AND operator
            var terms = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(term => term.Replace("'", "''")) // Escape single quotes
                .Where(term => !string.IsNullOrWhiteSpace(term))
                .Select(term => $"'{term}'")
                .ToArray();

            return string.Join(" & ", terms);
        }
    }
}
