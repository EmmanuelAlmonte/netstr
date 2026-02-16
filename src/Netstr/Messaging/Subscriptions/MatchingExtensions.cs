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
        /// Builds a single query that handles OR semantics between filters by applying all predicates,
        /// but does not apply Include/OrderBy/Take. Intended for COUNT and other "no truncation" scenarios.
        /// </summary>
        public static IQueryable<EventEntity> WhereAnyFilterMatchesBase(
            this IQueryable<EventEntity> entities,
            IEnumerable<SubscriptionFilter> filters,
            IEnumerable<long> protectedKinds,
            IReadOnlyCollection<string> authenticatedPublicKeys,
            bool useFullTextSearch = false)
        {
            var filterArray = filters.ToArray();
            if (!filterArray.Any())
            {
                return entities.Where(x => false); // Return empty result
            }

            IQueryable<EventEntity> query = entities.Where(x => false); // Start with empty query

            foreach (var filter in filterArray)
            {
                var filterQuery = ApplyFilterPredicates(
                    entities,
                    filter,
                    protectedKinds,
                    authenticatedPublicKeys,
                    useFullTextSearch);
                query = query.Union(filterQuery);
            }

            return query;
        }

        /// <summary>
        /// Filters database events based on supplied filters for an initial REQ stored-events query.
        /// Applies ordering and limits (per-filter, clamped by <paramref name="maxLimit"/>) and then unions/dedupes.
        /// </summary>
        public static IQueryable<EventEntity> WhereAnyFilterMatchesForInitialQuery(
            this IQueryable<EventEntity> entities,
            IEnumerable<SubscriptionFilter> filters,
            IEnumerable<long> protectedKinds,
            IReadOnlyCollection<string> authenticatedPublicKeys,
            int maxLimit,
            bool useFullTextSearch = false)
        {
            var filterArray = filters.ToArray();
            if (!filterArray.Any())
            {
                return entities.Where(x => false).AsNoTracking();
            }

            var max = maxLimit > 0 ? maxLimit : int.MaxValue;
            var canRankSingleSearchFilter =
                useFullTextSearch &&
                filterArray.Length == 1 &&
                SearchQueryParser.Parse(filterArray[0].Search).HasBasicTerms;

            IQueryable<EventEntity> query = entities.Where(x => false);

            foreach (var filter in filterArray)
            {
                var perFilterLimit = filter.Limit.HasValue ? Math.Min(filter.Limit.Value, max) : max;

                var filterQuery = ApplyFilterPredicates(
                    entities,
                    filter,
                    protectedKinds,
                    authenticatedPublicKeys,
                    useFullTextSearch)
                    .OrderBySearchQuality(filter.Search, useFullTextSearch)
                    .Take(perFilterLimit);

                query = query.Union(filterQuery);
            }

            IQueryable<EventEntity> result = query.Include(x => x.Tags);

            // NIP-50 quality ordering is only applied when there's exactly 1 search filter (simple, consistent semantics).
            // Multi-filter ranking requires per-filter ranking aggregation; keep standard ordering for now.
            result = canRankSingleSearchFilter
                ? result.OrderBySearchQuality(filterArray[0].Search, useFullTextSearch)
                : result.OrderByDescending(x => x.EventCreatedAt).ThenBy(x => x.EventId);

            return result.AsNoTracking();
        }

        /// <summary>
        /// Filters database events based on supplied filters with no auth for an initial REQ stored-events query.
        /// </summary>
        public static IQueryable<EventEntity> WhereAnyFilterMatchesForInitialQuery(
            this IQueryable<EventEntity> entities,
            IEnumerable<SubscriptionFilter> filters,
            int maxLimit)
        {
            return WhereAnyFilterMatchesForInitialQuery(
                entities,
                filters,
                [],
                Array.Empty<string>(),
                maxLimit,
                useFullTextSearch: false);
        }

        private static IQueryable<EventEntity> ApplyFilterPredicates(
            IQueryable<EventEntity> entities,
            SubscriptionFilter filter,
            IEnumerable<long> protectedKinds,
            IReadOnlyCollection<string> authenticatedPublicKeys,
            bool useFullTextSearch)
        {
            return entities
                .Where(x =>
                    (filter.Authors.Contains(x.EventPublicKey) || !filter.Authors.Any()) &&
                    (filter.Ids.Contains(x.EventId) || !filter.Ids.Any()) &&
                    (filter.Kinds.Contains(x.EventKind) || !filter.Kinds.Any()) &&
                    (filter.Since <= x.EventCreatedAt || !filter.Since.HasValue) &&
                    (filter.Until >= x.EventCreatedAt || !filter.Until.HasValue))
                .WhereMatchesSearch(filter.Search, useFullTextSearch)
                .WhereOrTags(filter.OrTags)
                .WhereAndTags(filter.AndTags)
                .Where(x =>
                    !protectedKinds.Contains(x.EventKind) ||
                    authenticatedPublicKeys.Contains(x.EventPublicKey) ||
                    x.Tags.Any(tag => tag.Name == EventTag.PublicKey &&
                                      authenticatedPublicKeys.Contains(tag.Value)));
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

    }
}
