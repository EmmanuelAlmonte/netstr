using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using Xunit;

namespace Netstr.Tests.NIPs
{
    public class Nip11NonRootPathTests
    {
        [Fact]
        public async Task MetadataAndWebsocketUpgradeAreServedOnConfiguredNonRootPath()
        {
            const string webSocketsPath = "/relay";

            using var factory = new WebApplicationFactory().WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Connection:WebSocketsPath"] = webSocketsPath
                    });
                });
            });

            using var client = factory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, webSocketsPath);
            request.Headers.TryAddWithoutValidation(HeaderNames.Accept, "text/html, application/nostr+json; q=0.7");
            request.Headers.TryAddWithoutValidation(HeaderNames.Origin, "https://example.com");

            using var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/nostr+json");
            response.Headers.Should().ContainKey(HeaderNames.AccessControlAllowOrigin);
            response.Headers.Should().ContainKey(HeaderNames.AccessControlAllowHeaders);
            response.Headers.Should().ContainKey(HeaderNames.AccessControlAllowMethods);

            var content = await response.Content.ReadAsStringAsync();
            var fields = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);
            fields.Should().NotBeNull();
            fields.Should().ContainKey("name");
            fields.Should().ContainKey("supported_nips");

            var wsClient = factory.Server.CreateWebSocketClient();
            using var socket = await wsClient.ConnectAsync(new Uri($"ws://localhost{webSocketsPath}"), CancellationToken.None);

            socket.State.Should().Be(WebSocketState.Open);
        }
    }
}
