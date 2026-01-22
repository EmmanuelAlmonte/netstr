using Netstr.Messaging.Models;
using System.Collections.Concurrent;

namespace Netstr.Messaging
{
    public interface IUserCache
    {
        void Initialize(IEnumerable<User> users);

        User? GetByPublicKey(string publicKey);

        User Vanish(string publicKey, DateTimeOffset timestamp);
    }

    public class UserCache : IUserCache
    {
        // Use MemoryCache with CacheItemPolicy NotRemovable for users which vanished?
        private readonly ConcurrentDictionary<string, User> users = new();

        public User? GetByPublicKey(string publicKey)
        {
            this.users.TryGetValue(publicKey, out var user);

            return user;
        }

        public void Initialize(IEnumerable<User> users)
        {
            foreach (var user in users)
            {
                this.users.TryAdd(user.PublicKey, user);
            }
        }

        public User Vanish(string publicKey, DateTimeOffset timestamp)
        {
            return this.users.AddOrUpdate(
                publicKey,
                key => new User { PublicKey = key, LastVanished = timestamp },
                (key, user) => user with { LastVanished = timestamp });
        }
    }
}