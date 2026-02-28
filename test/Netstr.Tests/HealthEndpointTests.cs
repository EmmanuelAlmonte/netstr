using FluentAssertions;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Netstr.Tests
{
    public class HealthEndpointTests
    {
        [Fact]
        public async Task HealthEndpoint_ShouldReturnOk()
        {
            using var factory = new WebApplicationFactory();
            using var client = factory.CreateClient();

            using var response = await client.GetAsync("/healthz");
            var payload = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<Dictionary<string, string>>(payload);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            json.Should().NotBeNull();
            json!["status"].Should().Be("ok");
        }
    }
}
