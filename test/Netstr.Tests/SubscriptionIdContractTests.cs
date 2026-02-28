using FluentAssertions;
using Netstr.Messaging;
using Netstr.Messaging.Models;
using System.Net.WebSockets;

namespace Netstr.Tests
{
    public class SubscriptionIdContractTests
    {
        [Fact]
        public async Task RejectsEmptySubscriptionId_ForReqAndCount()
        {
            var factory = new WebApplicationFactory();
            factory.CreateDefaultClient();

            using WebSocket ws = await factory.ConnectWebSocketAsync();

            await ws.SendReqAsync("", [new SubscriptionFilterRequest { Kinds = [1] }]);
            var reqClosed = await ws.ReceiveOnceAsync();

            reqClosed[0].GetString().Should().Be("CLOSED");
            reqClosed[1].GetString().Should().Be("");
            reqClosed[2].GetString().Should().Be(Messages.InvalidSubscriptionIdEmpty);

            await ws.SendCountAsync("", [new SubscriptionFilterRequest { Kinds = [1] }]);
            var countClosed = await ws.ReceiveOnceAsync();

            countClosed[0].GetString().Should().Be("CLOSED");
            countClosed[1].GetString().Should().Be("");
            countClosed[2].GetString().Should().Be(Messages.InvalidSubscriptionIdEmpty);
        }

        [Fact]
        public async Task EnforcesMaxSubscriptionIdLength64_ByDefault_ForReqAndCount()
        {
            var factory = new WebApplicationFactory();
            factory.CreateDefaultClient();

            using WebSocket ws = await factory.ConnectWebSocketAsync();

            var okId = new string('a', 64);
            var tooLongId = new string('a', 65);

            await ws.SendReqAsync(okId, [new SubscriptionFilterRequest { Kinds = [1] }]);
            var reqOk = await ws.ReceiveOnceAsync();
            reqOk[0].GetString().Should().Be("EOSE");

            await ws.SendReqAsync(tooLongId, [new SubscriptionFilterRequest { Kinds = [1] }]);
            var reqClosed = await ws.ReceiveOnceAsync();
            reqClosed[0].GetString().Should().Be("CLOSED");
            reqClosed[1].GetString().Should().Be(tooLongId);
            reqClosed[2].GetString().Should().Be(Messages.InvalidSubscriptionIdTooLong);

            await ws.SendCountAsync(tooLongId, [new SubscriptionFilterRequest { Kinds = [1] }]);
            var countClosed = await ws.ReceiveOnceAsync();
            countClosed[0].GetString().Should().Be("CLOSED");
            countClosed[1].GetString().Should().Be(tooLongId);
            countClosed[2].GetString().Should().Be(Messages.InvalidSubscriptionIdTooLong);
        }
    }
}

