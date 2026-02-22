using Microsoft.EntityFrameworkCore;
using Netstr.Data;
using Netstr.Messaging.Events;
using Netstr.Messaging.Models;

namespace Netstr.Messaging.Subscriptions
{
    public static class MatchingExtensions
    {
        /// <summary>
        /// Returns whether the given event <paramref name="e"/> satisfies conditions in any of the given <paramref name="filters"/>
        /// </summary>
        public static bool IsAnyMatch(this IEnumerable<SubscriptionFilter> filters, Event e)
        {
            return filters.Any(x => SubscriptionFilterMatcher.IsMatch(x, e));
        }

        /// <summary>
        /// Filters database events based on supplied filters.
        /// </summary>
        public static IQueryable<EventEntity> WhereAnyFilterMatches(
            this DbSet<EventEntity> entities,
            IEnumerable<SubscriptionFilter> filters,
            IEnumerable<long> protectedKinds,
            string? authenticatedPublicKey,
            int maxLimit)
        {
            var filterArray = filters.ToArray();
            if (!filterArray.Any())
            {
                return entities.Where(x => false).AsNoTracking(); // Return empty result
            }

            // Build a single query that handles OR semantics between filters
            IQueryable<EventEntity> query = entities.Where(x => false); // Start with empty query

            foreach (var filter in filterArray)
            {
                var filterQuery = entities
                    .Where(x =>
                        (filter.Authors.Contains(x.EventPublicKey) || !filter.Authors.Any()) &&
                        (filter.Ids.Contains(x.EventId) || !filter.Ids.Any()) &&
                        (filter.Kinds.Contains(x.EventKind) || !filter.Kinds.Any()) &&
                        (filter.Since <= x.EventCreatedAt || !filter.Since.HasValue) &&
                        (filter.Until >= x.EventCreatedAt || !filter.Until.HasValue))
                    .WhereMatchesSearch(filter.Search)
                    .WhereOrTags(filter.OrTags)
                    .WhereAndTags(filter.AndTags)
                    .Where(x => !protectedKinds.Contains(x.EventKind) || x.EventPublicKey == authenticatedPublicKey || x.Tags.Any(tag => tag.Name == EventTag.PublicKey && tag.Value == authenticatedPublicKey));

                // Union with previous results to implement OR semantics
                query = query.Union(filterQuery);
            }

            // Calculate effective limit: use the client's requested limit if specified, otherwise fallback to maxLimit
            // When multiple filters have limits, use the minimum (most restrictive)
            var specifiedLimits = filterArray.Where(f => f.Limit.HasValue).Select(f => f.Limit!.Value);
            var effectiveLimit = specifiedLimits.Any() ? specifiedLimits.Min() : maxLimit;

            return query
                .Include(x => x.Tags)
                .OrderByDescending(x => x.EventCreatedAt)
                .ThenBy(x => x.EventId)
                .Take(effectiveLimit)
                .AsNoTracking();
        }

        /// <summary>
        /// Filters database events based on supplied filters with no auth.
        /// </summary>
        public static IQueryable<EventEntity> WhereAnyFilterMatches(
            this DbSet<EventEntity> entities,
            IEnumerable<SubscriptionFilter> filters,
            int maxLimit)
        {
            return WhereAnyFilterMatches(entities, filters, [], null, maxLimit);
        }

        private static IQueryable<EventEntity> WhereOrTags(this IQueryable<EventEntity> entities, IDictionary<string, string[]> tags)
        {
            foreach (var tag in tags)
            {
                entities = entities.Where(e => e.Tags.Any(etag => etag.Name == tag.Key && tag.Value.Contains(etag.Value)));
            }

            return entities;
        }

        private static IQueryable<EventEntity> WhereAndTags(this IQueryable<EventEntity> entities, IDictionary<string, string[]> tags)
        {
            foreach (var tag in tags)
            {
                foreach (var tagValue in tag.Value)
                {
                    entities = entities.Where(e => e.Tags.Any(etag => etag.Name == tag.Key && etag.Value == tagValue));
                }
            }

            return entities;
        }

        private static IQueryable<EventEntity> WhereMatchesSearchAny(this IQueryable<EventEntity> entities, SubscriptionFilter[] filters)
        {
            // Apply search filters (for now, apply each one - this could be optimized further)
            foreach (var filter in filters.Where(f => !string.IsNullOrEmpty(f.Search)))
            {
                entities = entities.WhereMatchesSearch(filter.Search);
            }
            return entities;
        }

        private static IQueryable<EventEntity> WhereOrTagsAny(this IQueryable<EventEntity> entities, SubscriptionFilter[] filters)
        {
            // Apply OR tag filters from any filter
            foreach (var filter in filters)
            {
                entities = entities.WhereOrTags(filter.OrTags);
            }
            return entities;
        }

        private static IQueryable<EventEntity> WhereAndTagsAny(this IQueryable<EventEntity> entities, SubscriptionFilter[] filters)
        {
            // Apply AND tag filters from any filter
            foreach (var filter in filters)
            {
                entities = entities.WhereAndTags(filter.AndTags);
            }
            return entities;
        }
    }
}
