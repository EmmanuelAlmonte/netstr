using Microsoft.EntityFrameworkCore;
using Netstr.Messaging.Models;
using Microsoft.Extensions.Options;
using Netstr.Options;
using System.Linq;

namespace Netstr.Data
{
    /// <summary>
    /// Extension methods for working with relay configurations in the database.
    /// </summary>
    public static class RelayConfigDbExtensions
    {
        /// <summary>
        /// Updates or inserts relay configurations for a user based on a NIP-65 event.
        /// </summary>
        /// <param name="db">The database context</param>
        /// <param name="event">The relay list event</param>
        /// <returns>The number of configurations updated/inserted</returns>
        public static async Task<int> UpsertRelayConfigsAsync(this NetstrDbContext db, Event @event)
        {
            ArgumentNullException.ThrowIfNull(db, nameof(db));
            ArgumentNullException.ThrowIfNull(@event, nameof(@event));

            if (!@event.Kind.Equals(EventKind.RelayList))
            {
                throw new ArgumentException("Event must be a relay list event", nameof(@event));
            }

            if (string.IsNullOrEmpty(@event.PublicKey))
            {
                throw new ArgumentException("Event must have a valid public key", nameof(@event));
            }

            var existingConfigs = await db.RelayConfigs
                .Where(r => r.PubKey == @event.PublicKey)
                .ToDictionaryAsync(r => r.RelayUrl, r => r);

            var now = DateTime.UtcNow;
            var changes = 0;

            foreach (var tag in @event.Tags.Where(t => t?.Length >= 2 && t[0] == "r"))
            {
                var url = tag[1];
                var read = tag.Length > 2 && tag.Contains("read");
                var write = tag.Length > 2 && tag.Contains("write");

                if (existingConfigs.TryGetValue(url, out var config))
                {
                    if (config.Read != read || config.Write != write)
                    {
                        config.Read = read;
                        config.Write = write;
                        config.LastUpdated = now;
                        changes++;
                    }
                }
                else
                {
                    db.RelayConfigs.Add(new RelayConfigEntity(@event.PublicKey, url, read, write));
                    changes++;
                }
            }

            // Remove configurations not present in the new list
            var urlsToKeep = @event.Tags
                .Where(t => t?.Length >= 2 && t[0] == "r")
                .Select(t => t[1])
                .ToHashSet();

            var configsToRemove = existingConfigs.Values
                .Where(c => !urlsToKeep.Contains(c.RelayUrl))
                .ToList();

            if (configsToRemove.Any())
            {
                db.RelayConfigs.RemoveRange(configsToRemove);
                changes += configsToRemove.Count;
            }

            if (changes > 0)
                await db.SaveChangesAsync();

            return changes;
        }

        /// <summary>
        /// Gets all relay configurations for a user.
        /// </summary>
        /// <param name="db">The database context</param>
        /// <param name="pubKey">The user's public key</param>
        /// <returns>List of relay configurations</returns>
        public static Task<List<RelayConfigEntity>> GetRelayConfigsAsync(
            this NetstrDbContext db,
            string pubKey)
        {
            return db.RelayConfigs
                .Where(r => r.PubKey == pubKey)
                .OrderBy(r => r.RelayUrl)
                .ToListAsync();
        }
    }
}
