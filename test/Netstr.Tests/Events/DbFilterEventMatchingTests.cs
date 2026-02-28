using FluentAssertions;
using Microsoft.Data.Sqlite;
using Netstr.Data;
using Netstr.Messaging.Models;
using Netstr.Messaging.Subscriptions;

namespace Netstr.Tests.Events
{
    public class DbFilterEventMatchingTests : IDisposable
    {
        private readonly SqliteConnection connection;
        private readonly NetstrDbContext context;

        public DbFilterEventMatchingTests()
        {
            (this.connection, this.context, var _) = TestDbContext.InitializeAndSeed();
        }

        public void Dispose()
        {
            this.connection.Dispose();
            this.context.Dispose();
        }

        [Fact]
        public void FindEventsByIds()
        {
            var db = this.context;
            var filter = new SubscriptionFilter
            {
                Ids = [
                    "1a621c1ff8f6ea2641205bcb8a2908c80f7e70338179ac6f0dab8dfebf748132",
                    "444b1e4cf4eea42d35c7f1be58ab9cf6a942153593251d66e0471084a3430dae"
                ]
            };

            var results = db.Events.WhereAnyFilterMatchesForInitialQuery([filter], 100).Select(x => x.EventId).ToArray();

            results.Should().BeEquivalentTo(filter.Ids);
        }

        [Fact]
        public void FindEventsByAuthors()
        {
            var db = this.context;
            var filter = new SubscriptionFilter
            {
                Authors = [
                    "55b702c167c85eb1c2d5ab35d68bedd1a35b94c01147364d2395c2f66f35a503",
                    "32e1827635450ebb3c5a7d12c1f8e7b2b514439ac10a67eef3d9fd9c5c68e245",
                    "blah"
                ]
            };

            var results = db.Events.WhereAnyFilterMatchesForInitialQuery([filter], 100).Select(x => x.EventId).ToArray();

            string[] expectedIds = [
                "1a621c1ff8f6ea2641205bcb8a2908c80f7e70338179ac6f0dab8dfebf748132",
                "444b1e4cf4eea42d35c7f1be58ab9cf6a942153593251d66e0471084a3430dae",
                "e527fe8b0f64a38c6877f943a9e8841074056ba72aceb31a4c85e6d10b27095a"
            ];

            results.Should().BeEquivalentTo(expectedIds);
        }

        [Fact]
        public void FindEventsByKinds()
        {
            var db = this.context;
            var filter = new SubscriptionFilter
            {
                Kinds = [5, 6, 150]
            };

            var results = db.Events.WhereAnyFilterMatchesForInitialQuery([filter], 100).Select(x => x.EventId).ToArray();

            string[] expectedIds = [
                "20942205680e130a7602fd735fe715f52edf814a0b6e6e7f0990a02b257504ed",
                "444cec7f44c53eee60ba62858920c74173aa6bbb76c622f484a88cfcca2e07ad",
                "23677e3d035be5de01172de203103e292126d542897086bf797d8794fe6b1081",
            ];

            results.Should().BeEquivalentTo(expectedIds);
        }

        [Fact]
        public void FindEventsByWalletAndNutzapKinds()
        {
            var db = this.context;
            var firstSeen = DateTimeOffset.UtcNow;
            var author = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

            var walletRecord = new Event
            {
                Id = "5111111111111111111111111111111111111111111111111111111111111111",
                PublicKey = author,
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(1722000000),
                Kind = (long)EventKind.CashuWalletEvent,
                Tags = [],
                Content = "wallet record",
                Signature = "sig-wallet-record"
            };

            var walletResponse = new Event
            {
                Id = "5222222222222222222222222222222222222222222222222222222222222222",
                PublicKey = author,
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(1722000001),
                Kind = (long)EventKind.WalletResponse,
                Tags = [],
                Content = "wallet response",
                Signature = "sig-wallet-response"
            };

            var walletToken = new Event
            {
                Id = "5444444444444444444444444444444444444444444444444444444444444444",
                PublicKey = author,
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(1722000002),
                Kind = (long)EventKind.CashuWalletToken,
                Tags = [],
                Content = "wallet token",
                Signature = "sig-wallet-token"
            };

            var walletHistory = new Event
            {
                Id = "5555555555555555555555555555555555555555555555555555555555555555",
                PublicKey = author,
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(1722000003),
                Kind = (long)EventKind.CashuWalletHistory,
                Tags = [],
                Content = "wallet history",
                Signature = "sig-wallet-history"
            };

            var nutzapInfo = new Event
            {
                Id = "5666666666666666666666666666666666666666666666666666666666666666",
                PublicKey = author,
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(1722000004),
                Kind = (long)EventKind.NutzapMintRecommendation,
                Tags = [],
                Content = "nutzap info",
                Signature = "sig-nutzap-info"
            };

            var nutzap = new Event
            {
                Id = "5777777777777777777777777777777777777777777777777777777777777777",
                PublicKey = author,
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(1722000005),
                Kind = (long)EventKind.Nutzap,
                Tags = [],
                Content = "nutzap event",
                Signature = "sig-nutzap"
            };

            var unrelated = new Event
            {
                Id = "5333333333333333333333333333333333333333333333333333333333333333",
                PublicKey = author,
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(1722000006),
                Kind = (long)EventKind.ShortTextNote,
                Tags = [],
                Content = "not wallet",
                Signature = "sig-not-wallet"
            };

            db.Events.AddRange(
                walletRecord.ToEntity(firstSeen),
                walletResponse.ToEntity(firstSeen),
                walletToken.ToEntity(firstSeen),
                walletHistory.ToEntity(firstSeen),
                nutzapInfo.ToEntity(firstSeen),
                nutzap.ToEntity(firstSeen),
                unrelated.ToEntity(firstSeen));
            db.SaveChanges();

            var filter = new SubscriptionFilter
            {
                Kinds =
                [
                    (long)EventKind.CashuWalletEvent,
                    (long)EventKind.WalletResponse,
                    (long)EventKind.CashuWalletToken,
                    (long)EventKind.CashuWalletHistory,
                    (long)EventKind.NutzapMintRecommendation,
                    (long)EventKind.Nutzap
                ]
            };

            var results = db.Events.WhereAnyFilterMatchesForInitialQuery([filter], 100).Select(x => x.EventId).ToArray();

            results.Should().Contain(walletRecord.Id);
            results.Should().Contain(walletResponse.Id);
            results.Should().Contain(walletToken.Id);
            results.Should().Contain(walletHistory.Id);
            results.Should().Contain(nutzapInfo.Id);
            results.Should().Contain(nutzap.Id);
            results.Should().NotContain(unrelated.Id);
        }

        [Fact]
        public void FindEventsBySinceAndUntil()
        {
            var db = this.context;
            var filter = new SubscriptionFilter
            {
                Since = DateTimeOffset.FromUnixTimeSeconds(1645030752),
                Until = DateTimeOffset.FromUnixTimeSeconds(1660424316)
            };

            var results = db.Events.WhereAnyFilterMatchesForInitialQuery([filter], 100).Select(x => x.EventId).ToArray();

            string[] expectedIds = [
                "cf8de9db67a1d7203512d1d81e6190f5e53abfdc0ac90275f67172b65a5b09a0",
                "444b1e4cf4eea42d35c7f1be58ab9cf6a942153593251d66e0471084a3430dae",
                "23677e3d035be5de01172de203103e292126d542897086bf797d8794fe6b1081",
                "0d684e8ec2431de586aa3cafbee2f6d308d19b28805e53deabcac3220e9136a5",
            ];

            results.Should().BeEquivalentTo(expectedIds);
        }

        [Fact]
        public void FindEventsWithLimit()
        {
            var db = this.context;
            var filter = new SubscriptionFilter
            {
                Limit = 2
            };

            var results = db.Events.WhereAnyFilterMatchesForInitialQuery([filter], 100).Select(x => x.EventId).ToArray();

            string[] expectedIds = [
                "444cec7f44c53eee60ba62858920c74173aa6bbb76c622f484a88cfcca2e07ad",
                "20942205680e130a7602fd735fe715f52edf814a0b6e6e7f0990a02b257504ed"
            ];

            results.Should().BeEquivalentTo(expectedIds);
        }

        [Fact]
        public void FindEventsWithMultipleFilters()
        {
            var db = this.context;
            var filters = new[]
            {
                new SubscriptionFilter { Limit = 5 },
                new SubscriptionFilter { Limit = 1 },
                new SubscriptionFilter { Ids = [
                    "e527fe8b0f64a38c6877f943a9e8841074056ba72aceb31a4c85e6d10b27095a",
                    "1a621c1ff8f6ea2641205bcb8a2908c80f7e70338179ac6f0dab8dfebf748132",
                    "23677e3d035be5de01172de203103e292126d542897086bf797d8794fe6b1081",
                    "20942205680e130a7602fd735fe715f52edf814a0b6e6e7f0990a02b257504ed"] },
                new SubscriptionFilter { Authors = ["e8b487c079b0f67c695ae6c4c2552a47f38adfa2533cc5926bd2c102942fdcb7"] },
                new SubscriptionFilter { Kinds = [5], Since = DateTimeOffset.FromUnixTimeSeconds(1660449145) },
            };

            var results = db.Events.WhereAnyFilterMatchesForInitialQuery(filters, 3).Select(x => x.EventId).ToArray();

            var expectedIds = new[] {
                "444cec7f44c53eee60ba62858920c74173aa6bbb76c622f484a88cfcca2e07ad",
                "1a621c1ff8f6ea2641205bcb8a2908c80f7e70338179ac6f0dab8dfebf748132",
                "20942205680e130a7602fd735fe715f52edf814a0b6e6e7f0990a02b257504ed",
                "cf8de9db67a1d7203512d1d81e6190f5e53abfdc0ac90275f67172b65a5b09a0",
                "23677e3d035be5de01172de203103e292126d542897086bf797d8794fe6b1081"
            };

            results.Should().BeEquivalentTo(expectedIds);
        }

        [Fact]
        public void FindEventsWithTags()
        {
            var db = this.context;
            var filters = new[]
            {
                new SubscriptionFilter {
                    OrTags = new () {
                        ["p"] = [ "abcd", "4d5ce768123563bc583697db5e84841fb528f7b708d966f2e546286ce3c72077" ],
                        ["e"] = [ "8da089fad0df548e490d93eccc413ecee63cc9da4901051b0bdcb801032f05d3" ]
                    }
                },
            };

            var results = db.Events.WhereAnyFilterMatchesForInitialQuery(filters, 100).Select(x => x.EventId).ToArray();

            var expectedIds = new[] { "23677e3d035be5de01172de203103e292126d542897086bf797d8794fe6b1081" };

            results.Should().BeEquivalentTo(expectedIds);
        }
    }
}
