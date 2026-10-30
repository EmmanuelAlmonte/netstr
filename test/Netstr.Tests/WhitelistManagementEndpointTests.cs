using System.Net;
using System.Text;
using Microsoft.AspNetCore.Routing;

namespace Netstr.Tests
{
    public class WhitelistManagementEndpointTests
    {
        public static TheoryData<HttpMethod, string> FormerManagementRoutes =>
            new()
            {
                { HttpMethod.Get, "/api/whitelist" },
                { HttpMethod.Get, "/api/whitelist/keys" },
                { HttpMethod.Post, "/api/whitelist/keys" },
                { HttpMethod.Delete, "/api/whitelist/keys/test-public-key" },
                { HttpMethod.Put, "/api/whitelist/settings" },
                { HttpMethod.Put, "/api/whitelist/owner" },
                { HttpMethod.Get, "/api/whitelist/exempt-kinds" },
                { HttpMethod.Post, "/api/whitelist/exempt-kinds" },
                { HttpMethod.Delete, "/api/whitelist/exempt-kinds/9735" },
                { HttpMethod.Put, "/api/whitelist/exempt-kinds" }
            };

        [Fact]
        public void FormerWhitelistManagementRoutes_AreNotRegistered()
        {
            using var factory = CreateFactory(useForwardedHeaders: true);
            using var client = factory.CreateClient();
            var routeEndpoints = factory.Services
                .GetRequiredService<EndpointDataSource>()
                .Endpoints
                .OfType<RouteEndpoint>();

            Assert.DoesNotContain(
                routeEndpoints,
                endpoint => endpoint.RoutePattern.RawText?.StartsWith(
                    "api/whitelist",
                    StringComparison.OrdinalIgnoreCase) is true);
        }

        [Theory]
        [MemberData(nameof(FormerManagementRoutes))]
        public async Task FormerWhitelistManagementRoute_IsNotMapped(
            HttpMethod method,
            string path)
        {
            foreach (var useForwardedHeaders in new[] { false, true })
            {
                using var factory = CreateFactory(useForwardedHeaders);
                using var client = factory.CreateClient();

                foreach (var forwardedFor in new string?[] { null, "127.0.0.1", "203.0.113.10" })
                {
                    using var request = new HttpRequestMessage(method, path);
                    request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

                    if (forwardedFor is not null)
                    {
                        request.Headers.Add("X-Forwarded-For", forwardedFor);
                        request.Headers.Add("X-Forwarded-Proto", "https");
                        request.Headers.Add("X-Forwarded-Host", "relay.example");
                    }

                    using var response = await client.SendAsync(request);

                    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                }
            }
        }

        private static Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> CreateFactory(
            bool useForwardedHeaders) =>
            new WebApplicationFactory().WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Connection:UseForwardedHeaders"] = useForwardedHeaders.ToString()
                    })));
    }
}
