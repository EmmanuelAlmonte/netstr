using FluentAssertions;
using Netstr.Messaging;
using Netstr.Messaging.Models;
using Netstr.Tests.NIPs;

namespace Netstr.Tests
{
    public class Nip59And78ConformanceTests
    {
        private readonly WebApplicationFactory factory;

        public Nip59And78ConformanceTests()
        {
            this.factory = new WebApplicationFactory();
        }

        [Fact]
        public async Task NIP_59_Kind13_WithTags_IsRejected()
        {
            using var ws = await this.factory.ConnectWebSocketAsync();

            var e = new Event
            {
                Id = "",
                Content = "sealed rumor",
                CreatedAt = DateTimeOffset.UtcNow,
                Kind = 13,
                PublicKey = Alice.PublicKey,
                Signature = "",
                Tags = [["p", Alice.PublicKey]]
            };

            e = Helpers.FinalizeEvent(e, Alice.PrivateKey);

            await ws.SendEventAsync(e);
            var ok = await ws.ReceiveOnceAsync();

            ok[0].GetString()?.Should().Be("OK");
            ok[1].GetString()?.Should().Be(e.Id);
            ok[2].GetBoolean().Should().BeFalse();
            ok[3].GetString()?.Should().Be(Messages.InvalidEmptyTagsForKind13);
        }

        [Fact]
        public async Task NIP_59_Kind13_WithoutTags_IsAccepted()
        {
            using var ws = await this.factory.ConnectWebSocketAsync();

            var e = new Event
            {
                Id = "",
                Content = "sealed rumor",
                CreatedAt = DateTimeOffset.UtcNow,
                Kind = 13,
                PublicKey = Alice.PublicKey,
                Signature = "",
                Tags = []
            };

            e = Helpers.FinalizeEvent(e, Alice.PrivateKey);

            await ws.SendEventAsync(e);
            var ok = await ws.ReceiveOnceAsync();

            ok[0].GetString()?.Should().Be("OK");
            ok[1].GetString()?.Should().Be(e.Id);
            ok[2].GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task NIP_78_ApplicationSpecificDataWithoutDTag_IsRejected()
        {
            using var ws = await this.factory.ConnectWebSocketAsync();

            var e = new Event
            {
                Id = "",
                Content = "app data",
                CreatedAt = DateTimeOffset.UtcNow,
                Kind = (long)EventKind.ApplicationSpecificData,
                PublicKey = Alice.PublicKey,
                Signature = "",
                Tags = [["foo", "bar"]]
            };

            e = Helpers.FinalizeEvent(e, Alice.PrivateKey);

            await ws.SendEventAsync(e);
            var ok = await ws.ReceiveOnceAsync();

            ok[0].GetString()?.Should().Be("OK");
            ok[1].GetString()?.Should().Be(e.Id);
            ok[2].GetBoolean().Should().BeFalse();
            ok[3].GetString()?.Should().Contain("missing 'd' tag identifier");
        }

        [Fact]
        public async Task NIP_78_ApplicationSpecificDataWithDTag_IsAccepted()
        {
            using var ws = await this.factory.ConnectWebSocketAsync();

            var e = new Event
            {
                Id = "",
                Content = "app data",
                CreatedAt = DateTimeOffset.UtcNow,
                Kind = (long)EventKind.ApplicationSpecificData,
                PublicKey = Alice.PublicKey,
                Signature = "",
                Tags = [["d", "my-app"], ["foo", "bar"]]
            };

            e = Helpers.FinalizeEvent(e, Alice.PrivateKey);

            await ws.SendEventAsync(e);
            var ok = await ws.ReceiveOnceAsync();

            ok[0].GetString()?.Should().Be("OK");
            ok[1].GetString()?.Should().Be(e.Id);
            ok[2].GetBoolean().Should().BeTrue();
        }
    }
}
