using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Net.Http.Headers;
using System.Net;
using System.Text.Json;

namespace Netstr.Tests.NIPs
{
    public class Nip11SupportedNipsTests
    {
        [Theory]
        [InlineData("Development")]
        [InlineData("Production")]
        public async Task MetadataDocumentAdvertisesNip60AtRuntime(string environment)
        {
            using var factory = new WebApplicationFactory().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
            });

            using var client = factory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/");
            request.Headers.TryAddWithoutValidation(HeaderNames.Accept, "application/nostr+json");

            using var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var fields = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);

            fields.Should().NotBeNull();
            fields.Should().ContainKey("supported_nips");

            var supportedNips = fields!["supported_nips"]
                .EnumerateArray()
                .Select(x => x.GetInt32())
                .ToArray();

            supportedNips.Should().Contain(60);
        }
    }
}
