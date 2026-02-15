using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Netstr.Data;
using Netstr.Messaging.Models;
using Netstr.Tests.NIPs;
using System.Net.WebSockets;
using System.Text.Json;

namespace Netstr.Tests
{
    public class SearchSemanticsIntegrationTests
    {
        [Fact]
        public async Task Search_IgnoresExtensions_ForStoredAndRealtimeMatching()
        {
            var factory = new WebApplicationFactory();
            factory.CreateDefaultClient();

            using (var db = factory.Services.GetRequiredService<IDbContextFactory<NetstrDbContext>>().CreateDbContext())
            {
                var now = DateTimeOffset.UtcNow;
                db.Events.AddRange(
                    CreateEvent("stored-foo", "pk", 1, now.AddMinutes(-2), "foo stored"),
                    CreateEvent("stored-bar", "pk", 1, now.AddMinutes(-1), "bar stored"));
                db.SaveChanges();
            }

            using WebSocket ws = await factory.ConnectWebSocketAsync();

            var replies = new List<JsonElement[]>();
            _ = ws.ReceiveAsync(replies.Add);

            await ws.SendReqAsync("s", [new SubscriptionFilterRequest { Kinds = [1], Search = "foo include:spam" }]);

            await Task.Delay(1000);

            var storedEvents = replies
                .Where(x => x.Length >= 3 && x[0].GetString() == "EVENT" && x[1].GetString() == "s")
                .Select(x => x[2].GetProperty("content").GetString())
                .ToArray();

            storedEvents.Should().BeEquivalentTo(["foo stored"]);
            replies.Select(x => x[0].GetString()).Should().Contain("EOSE");

            // Publish a realtime event and ensure it matches the same filter.
            var realtime = new Event
            {
                Id = "",
                Content = "foo realtime",
                CreatedAt = DateTimeOffset.UtcNow,
                Kind = 1,
                PublicKey = Alice.PublicKey,
                Tags = [],
                Signature = ""
            };
            realtime = Helpers.FinalizeEvent(realtime, Alice.PrivateKey);

            await ws.SendEventAsync(realtime);

            await Task.Delay(1000);

            replies
                .Where(x => x.Length >= 3 && x[0].GetString() == "EVENT" && x[1].GetString() == "s")
                .Select(x => x[2].GetProperty("content").GetString())
                .Should()
                .Contain("foo realtime");
        }

        [Fact]
        public async Task Search_DomainExtensionIsIgnored_AndDoesNotReduceRecall()
        {
            var factory = new WebApplicationFactory();
            factory.CreateDefaultClient();

            using (var db = factory.Services.GetRequiredService<IDbContextFactory<NetstrDbContext>>().CreateDbContext())
            {
                var now = DateTimeOffset.UtcNow;
                db.Events.AddRange(
                    CreateEvent("stored-foo", "pk", 1, now.AddMinutes(-2), "foo stored"),
                    CreateEvent("stored-bar", "pk", 1, now.AddMinutes(-1), "bar stored"));
                db.SaveChanges();
            }

            using WebSocket ws = await factory.ConnectWebSocketAsync();

            var replies = new List<JsonElement[]>();
            _ = ws.ReceiveAsync(replies.Add);

            await ws.SendReqAsync("s", [new SubscriptionFilterRequest { Kinds = [1], Search = "domain:example.com foo" }]);

            await Task.Delay(1000);

            var storedEvents = replies
                .Where(x => x.Length >= 3 && x[0].GetString() == "EVENT" && x[1].GetString() == "s")
                .Select(x => x[2].GetProperty("content").GetString())
                .ToArray();

            storedEvents.Should().BeEquivalentTo(["foo stored"]);
        }

        private static EventEntity CreateEvent(string id, string pubkey, long kind, DateTimeOffset createdAt, string content)
        {
            return new EventEntity
            {
                EventId = id,
                EventPublicKey = pubkey,
                EventKind = kind,
                EventCreatedAt = createdAt,
                EventContent = content,
                EventSignature = "sig",
                FirstSeen = createdAt,
                Tags = []
            };
        }
    }
}

