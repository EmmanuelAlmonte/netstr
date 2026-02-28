using Netstr.Data;
using Netstr.Messaging.Models;
using Netstr.Messaging.Subscriptions;
using System.Linq;

namespace Netstr.Tests.Subscriptions
{
    public class MatchingExtensionsTests : IDisposable
    {
        private readonly NetstrDbContext context;
        private readonly Microsoft.Data.Sqlite.SqliteConnection connection;

        public MatchingExtensionsTests()
        {
            (this.connection, var seededContext, _) = TestDbContext.InitializeAndSeed(seed: false);
            this.context = seededContext;
        }

        [Fact]
        public void ProtectedFiltersCheckAnyAuthenticatedPubKeyForAuthorOrRecipient()
        {
            var alice = "5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75";
            var bob = "79be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798";
            var carol = "ab1d9f0f6e53b9f6f7c4e6efcb17e1e6c8a2d8f6f7e3f7b8f4a2d6f7a9be1d0";

            this.context.Events.AddRange(
                ProtectedEventEntity("multi-auth-1", alice),
                ProtectedEventEntity("multi-auth-2", bob),
                ProtectedEventEntity("multi-auth-3", carol, bob));

            this.context.SaveChanges();

            var filter = new SubscriptionFilter([], [], [ (long)EventKind.EncryptedDirectMessage ], null, null, null, null, [], []);
            var protectedKinds = new[] { (long)EventKind.EncryptedDirectMessage };

            var allByAlice = this.QueryAuthors(filter, protectedKinds, [alice]);
            Assert.Single(allByAlice);
            Assert.Contains(alice, allByAlice);

            var allByBob = this.QueryAuthors(filter, protectedKinds, [bob]);
            Assert.Equal(2, allByBob.Length);
            Assert.Contains(bob, allByBob);
            Assert.Contains(carol, allByBob);

            var allByBoth = this.QueryAuthors(filter, protectedKinds, [alice, bob]);
            Assert.Equal(3, allByBoth.Length);
            Assert.Contains(alice, allByBoth);
            Assert.Contains(bob, allByBoth);
            Assert.Contains(carol, allByBoth);

            var unauthenticated = this.QueryAuthors(filter, protectedKinds, Array.Empty<string>());
            Assert.Empty(unauthenticated);
        }

        private string[] QueryAuthors(
            SubscriptionFilter filter,
            long[] protectedKinds,
            string[] authenticatedPublicKeys)
        {
            return this.context.Events
                .WhereAnyFilterMatchesForInitialQuery([filter], protectedKinds, authenticatedPublicKeys, 100)
                .Select(x => x.EventPublicKey)
                .OrderBy(x => x)
                .ToArray();
        }

        private static EventEntity ProtectedEventEntity(string id, string publicKey, string? recipient = null)
        {
            return new EventEntity
            {
                EventId = id,
                EventPublicKey = publicKey,
                EventCreatedAt = DateTimeOffset.UtcNow,
                EventKind = (long)EventKind.EncryptedDirectMessage,
                EventContent = "protected content",
                EventSignature = "protected-signature",
                FirstSeen = DateTimeOffset.UtcNow,
                Tags = recipient == null
                    ? []
                    : [new TagEntity
                    {
                        Name = EventTag.PublicKey,
                        Value = recipient,
                        OtherValues = []
                    }],
            };
        }

        public void Dispose()
        {
            this.context.Dispose();
            this.connection.Dispose();
        }
    }
}
