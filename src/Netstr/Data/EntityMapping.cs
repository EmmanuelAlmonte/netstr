using Netstr.Messaging.Models;

namespace Netstr.Data
{
    public static class EntityMapping
    {
        public static EventEntity ToEntity(this Event e, DateTimeOffset firstSeen)
        {
            return new EventEntity
            {
                FirstSeen = firstSeen,
                EventContent = e.Content,
                EventCreatedAt = e.CreatedAt,
                EventId = e.Id,
                EventKind = e.Kind,
                EventPublicKey = e.PublicKey,
                EventSignature = e.Signature,
                EventExpiration = e.GetExpirationValue(),
                EventDeduplication = e.IsAddressable() 
                    ? e.GetDeduplicationValue() 
                    : null,
                Tags = e.Tags
                    .GroupBy(x => new { Name = x.First(), Value = x.Skip(1).FirstOrDefault() })
                    .Select(g => new TagEntity
                    {
                        Name = g.Key.Name,
                        Value = g.Key.Value,
                        OtherValues = g.First().Skip(2).ToArray()
                    })
                    .ToArray(),
            };
        }
    }
}
