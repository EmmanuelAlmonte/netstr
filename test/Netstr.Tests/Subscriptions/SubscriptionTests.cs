using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Netstr.Data;
using Netstr.Messaging.Models;
using Netstr.Messaging;
using Netstr.Options;
using Netstr.Tests.NIPs;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Netstr.Tests.Subscriptions
{
    public class SubscriptionTests
    {
        private readonly WebApplicationFactory factory;

        public SubscriptionTests()
        {
            this.factory = new WebApplicationFactory();
        }

        [Fact]
        public async Task UnknownFilterTest()
        {
            using WebSocket ws = await this.factory.ConnectWebSocketAsync();

            var sub = new { unknown = "unknown" };

            await ws.SendAsync([ "REQ", "id", sub ]);
            
            var result = await ws.ReceiveOnceAsync();

            result[0].GetString().Should().Be("CLOSED");
        }

        [Fact]
        public async Task UnknownFilterTagTest()
        {
            using WebSocket ws = await this.factory.ConnectWebSocketAsync();

            var sub = @"[ ""REQ"", ""id"", { ""#abc"": [] }]";

            await ws.SendAsync(Encoding.UTF8.GetBytes(sub), WebSocketMessageType.Text, true, CancellationToken.None);

            var result = await ws.ReceiveOnceAsync();

            result[0].GetString().Should().Be("CLOSED");
        }

        [Fact]
        public async Task RejectsReqWithInvalidIdsFilter()
        {
            using WebSocket ws = await this.factory.ConnectWebSocketAsync();

            await ws.SendReqAsync("id", [new Messaging.Models.SubscriptionFilterRequest { Ids = ["not-a-hex-id"] }]);

            var result = await ws.ReceiveOnceAsync();

            result[0].GetString().Should().Be("CLOSED");
            result[1].GetString().Should().Be("id");
            result[2].GetString().Should().Be(Messages.InvalidCannotProcessFilters);
        }

        [Fact]
        public async Task RejectsReqWithInvalidAuthorsFilter()
        {
            using WebSocket ws = await this.factory.ConnectWebSocketAsync();

            await ws.SendReqAsync("id", [new Messaging.Models.SubscriptionFilterRequest { Authors = ["not-a-hex-author"] }]);

            var result = await ws.ReceiveOnceAsync();

            result[0].GetString().Should().Be("CLOSED");
            result[1].GetString().Should().Be("id");
            result[2].GetString().Should().Be(Messages.InvalidCannotProcessFilters);
        }

        [Fact]
        public async Task RejectsReqWithUppercaseIdsFilter()
        {
            using WebSocket ws = await this.factory.ConnectWebSocketAsync();

            await ws.SendReqAsync("id", [new Messaging.Models.SubscriptionFilterRequest { Ids = ["5758137EC7F38F3D6C3EF103E28CD9312652285DAB3497FE5E5F6C5C0EF45E75"] }]);

            var result = await ws.ReceiveOnceAsync();

            result[0].GetString().Should().Be("CLOSED");
            result[1].GetString().Should().Be("id");
            result[2].GetString().Should().Be(Messages.InvalidCannotProcessFilters);
        }

        [Fact]
        public async Task RejectsReqWithUppercaseTagEFilter()
        {
            using WebSocket ws = await this.factory.ConnectWebSocketAsync();

            var sub = @"[ ""REQ"", ""id"", { ""#e"": [""5758137EC7F38F3D6C3EF103E28CD9312652285DAB3497FE5E5F6C5C0EF45E75""] }]";

            await ws.SendAsync(Encoding.UTF8.GetBytes(sub), WebSocketMessageType.Text, true, CancellationToken.None);

            var result = await ws.ReceiveOnceAsync();

            result[0].GetString().Should().Be("CLOSED");
            result[1].GetString().Should().Be("id");
            result[2].GetString().Should().Be(Messages.InvalidCannotProcessFilters);
        }

        [Fact]
        public async Task RejectsReqWithUppercaseTagPFilter()
        {
            using WebSocket ws = await this.factory.ConnectWebSocketAsync();

            var sub = @"[ ""REQ"", ""id"", { ""#p"": [""5BC683A5D12133A96AC5502C15FE1C2287986CFF7BAF6283600360E6BB01F627""] }]";

            await ws.SendAsync(Encoding.UTF8.GetBytes(sub), WebSocketMessageType.Text, true, CancellationToken.None);

            var result = await ws.ReceiveOnceAsync();

            result[0].GetString().Should().Be("CLOSED");
            result[1].GetString().Should().Be("id");
            result[2].GetString().Should().Be(Messages.InvalidCannotProcessFilters);
        }
    }
}
