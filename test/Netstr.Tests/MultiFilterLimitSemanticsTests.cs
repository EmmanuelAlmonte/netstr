using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Netstr.Data;
using Netstr.Messaging.Models;
using Netstr.Options.Limits;
using System.Net.WebSockets;
using System.Text.Json;

namespace Netstr.Tests
{
    public class MultiFilterLimitSemanticsTests
    {
        [Fact]
        public async Task Req_AppliesLimitPerFilter_ThenUnionsResults()
        {
            var factory = new WebApplicationFactory
            {
                SubscriptionLimits = new SubscriptionLimits
                {
                    MaxInitialLimit = 100
                }
            };

            factory.CreateDefaultClient();

            using (var db = factory.Services.GetRequiredService<IDbContextFactory<NetstrDbContext>>().CreateDbContext())
            {
                var t0 = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);

                // kind=1: 100, 90, 80, 70
                db.Events.AddRange(
                    CreateEvent("k1-100", "a", 1, t0.AddSeconds(100)),
                    CreateEvent("k1-090", "a", 1, t0.AddSeconds(90)),
                    CreateEvent("k1-080", "a", 1, t0.AddSeconds(80)),
                    CreateEvent("k1-070", "a", 1, t0.AddSeconds(70)));

                // kind=2: 95, 85, 75, 65
                db.Events.AddRange(
                    CreateEvent("k2-095", "b", 2, t0.AddSeconds(95)),
                    CreateEvent("k2-085", "b", 2, t0.AddSeconds(85)),
                    CreateEvent("k2-075", "b", 2, t0.AddSeconds(75)),
                    CreateEvent("k2-065", "b", 2, t0.AddSeconds(65)));

                db.SaveChanges();
            }

            using WebSocket ws = await factory.ConnectWebSocketAsync();

            var replies = new List<JsonElement[]>();
            _ = ws.ReceiveAsync(replies.Add);

            await ws.SendReqAsync("sub", [
                new SubscriptionFilterRequest { Kinds = [1], Limit = 2 },
                new SubscriptionFilterRequest { Kinds = [2], Limit = 2 }
            ]);

            await Task.Delay(1000);

            var forSub = replies.Where(x => x.Length >= 2 && x[1].GetString() == "sub").ToArray();
            forSub.Should().NotBeEmpty();

            // Ensure we received EOSE and exactly 4 stored events (2 per filter).
            forSub.Select(x => x[0].GetString()).Should().Contain("EOSE");

            var events = forSub
                .Where(x => x[0].GetString() == "EVENT")
                .Select(x => x[2])
                .ToArray();

            events.Should().HaveCount(4);

            // Overall ordering should be by created_at desc, tie-broken by id asc (NIP-01).
            var createdAts = events.Select(e => e.GetProperty("created_at").GetInt64()).ToArray();
            createdAts.Should().ContainInOrder(1_700_000_100, 1_700_000_095, 1_700_000_090, 1_700_000_085);
        }

        [Fact]
        public async Task Req_AppliesLimitAfterSearchRankingAcrossFilters()
        {
            var factory = new WebApplicationFactory
            {
                SubscriptionLimits = new SubscriptionLimits
                {
                    MaxInitialLimit = 2
                }
            };

            factory.CreateDefaultClient();

            using (var db = factory.Services.GetRequiredService<IDbContextFactory<NetstrDbContext>>().CreateDbContext())
            {
                db.Events.AddRange(
                    CreateEvent("cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc", "a", 1, DateTimeOffset.UtcNow.AddMinutes(5), "alpha beta note"),
                    CreateEvent("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "a", 1, DateTimeOffset.UtcNow.AddMinutes(2), "alpha beta note"),
                    CreateEvent("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", "a", 1, DateTimeOffset.UtcNow.AddMinutes(3), "alpha beta note"));
                db.SaveChanges();
            }

            using WebSocket ws = await factory.ConnectWebSocketAsync();

            var replies = new List<JsonElement[]>();
            _ = ws.ReceiveAsync(replies.Add);

            await ws.SendReqAsync("search_sub", [
                new SubscriptionFilterRequest { Kinds = [1], Search = "alpha", Limit = 1 },
                new SubscriptionFilterRequest { Kinds = [1], Search = "beta", Limit = 1 }
            ]);

            await Task.Delay(1000);

            var events = replies
                .Where(x => x.Length >= 3 && x[0].GetString() == MessageType.Event && x[1].GetString() == "search_sub")
                .Select(x => x[2])
                .ToArray();

            events.Select(x => x.GetProperty("id").GetString()).Should().Equal(
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
        }

        private static EventEntity CreateEvent(string id, string pubkey, long kind, DateTimeOffset createdAt, string? content = null)
        {
            return new EventEntity
            {
                EventId = id,
                EventPublicKey = pubkey,
                EventKind = kind,
                EventCreatedAt = createdAt,
                EventContent = content ?? $"content-{id}",
                EventSignature = "sig",
                FirstSeen = createdAt,
                Tags = []
            };
        }
    }
}
