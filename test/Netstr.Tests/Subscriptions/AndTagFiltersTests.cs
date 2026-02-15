using FluentAssertions;
using System.Net.WebSockets;
using System.Text;

namespace Netstr.Tests.Subscriptions
{
    public class AndTagFiltersTests
    {
        [Fact]
        public async Task AndTagFilters_AreRejected_WhenDisabled()
        {
            var factory = new WebApplicationFactory
            {
                AllowAndTagFilters = false
            };

            using WebSocket ws = await factory.ConnectWebSocketAsync();

            var req = @"[ ""REQ"", ""id"", { ""&p"": [""abc""] } ]";
            await ws.SendAsync(Encoding.UTF8.GetBytes(req), WebSocketMessageType.Text, true, CancellationToken.None);

            var result = await ws.ReceiveOnceAsync();
            result[0].GetString().Should().Be("CLOSED");
        }

        [Fact]
        public async Task AndTagFilters_Work_WhenEnabled()
        {
            var factory = new WebApplicationFactory
            {
                AllowAndTagFilters = true
            };

            using WebSocket ws = await factory.ConnectWebSocketAsync();

            var req = @"[ ""REQ"", ""id"", { ""&p"": [""abc""] } ]";
            await ws.SendAsync(Encoding.UTF8.GetBytes(req), WebSocketMessageType.Text, true, CancellationToken.None);

            var result = await ws.ReceiveOnceAsync();
            result[0].GetString().Should().Be("EOSE");
        }
    }
}

