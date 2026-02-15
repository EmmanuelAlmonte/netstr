using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Netstr.Data;
using Netstr.Messaging.Models;
using Netstr.Options.Limits;
using System.Net.WebSockets;

namespace Netstr.Tests
{
    public class CountSemanticsTests
    {
        [Fact]
        public async Task Count_Ignores_FilterLimit_And_MaxInitialLimit()
        {
            var factory = new WebApplicationFactory
            {
                SubscriptionLimits = new SubscriptionLimits
                {
                    // Intentionally tiny to reproduce the stored-events truncation bug in COUNT.
                    MaxInitialLimit = 1
                }
            };

            factory.CreateDefaultClient();

            using (var db = factory.Services.GetRequiredService<IDbContextFactory<NetstrDbContext>>().CreateDbContext())
            {
                var now = DateTimeOffset.UtcNow;
                db.Events.AddRange(
                    CreateEvent("e1", "a", 1, now.AddMinutes(-3)),
                    CreateEvent("e2", "b", 1, now.AddMinutes(-2)),
                    CreateEvent("e3", "c", 1, now.AddMinutes(-1)));
                db.SaveChanges();
            }

            using WebSocket ws = await factory.ConnectWebSocketAsync();

            await ws.SendCountAsync("c1", [new SubscriptionFilterRequest { Kinds = [1], Limit = 1 }]);

            var received = await ws.ReceiveOnceAsync();

            received[0].GetString().Should().Be("COUNT");
            received[1].GetString().Should().Be("c1");
            received[2].GetProperty("count").GetInt32().Should().Be(3);
        }

        [Fact]
        public async Task Count_WithMultipleFilters_OrsAndCountsUniqueEvents()
        {
            var factory = new WebApplicationFactory();
            factory.CreateDefaultClient();

            using (var db = factory.Services.GetRequiredService<IDbContextFactory<NetstrDbContext>>().CreateDbContext())
            {
                var now = DateTimeOffset.UtcNow;
                db.Events.AddRange(
                    CreateEvent("e1", "a", 1, now.AddMinutes(-2)), // matches both filters below
                    CreateEvent("e2", "a", 2, now.AddMinutes(-1))); // matches author filter only
                db.SaveChanges();
            }

            using WebSocket ws = await factory.ConnectWebSocketAsync();

            await ws.SendCountAsync("c2", [
                new SubscriptionFilterRequest { Authors = ["a"] },
                new SubscriptionFilterRequest { Kinds = [1] }
            ]);

            var received = await ws.ReceiveOnceAsync();

            received[0].GetString().Should().Be("COUNT");
            received[1].GetString().Should().Be("c2");
            received[2].GetProperty("count").GetInt32().Should().Be(2);
        }

        private static EventEntity CreateEvent(string id, string pubkey, long kind, DateTimeOffset createdAt)
        {
            return new EventEntity
            {
                EventId = id,
                EventPublicKey = pubkey,
                EventKind = kind,
                EventCreatedAt = createdAt,
                EventContent = $"content-{id}",
                EventSignature = "sig",
                FirstSeen = createdAt,
                Tags = []
            };
        }
    }
}

