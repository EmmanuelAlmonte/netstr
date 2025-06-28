using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Netstr.Data;
using Netstr.Messaging.Models;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Netstr.Tests.NIPs.Steps
{
    public partial class Steps
    {

        [Given("I have published relay configurations")]
        public async Task GivenIHavePublishedRelayConfigurations()
        {
            var c = this.scenarioContext.Get<Clients>()["Alice"];
            
            var tags = new string[][]
            {
                new[] { "r", "wss://relay1.com", "read", "write" },
                new[] { "r", "wss://relay2.com", "read" }
            };

            var e = new Event
            {
                Id = "",
                Signature = "",
                Content = "",
                CreatedAt = DateTimeOffset.UtcNow,
                PublicKey = c.Keys.PublicKey,
                Tags = tags,
                Kind = (long)EventKind.RelayList
            };

            e = Helpers.FinalizeEvent(e, c.Keys.PrivateKey);
            await c.WebSocket.SendEventAsync(e);

            await Task.Delay(100); // Allow time for processing
        }

        [When(@"I publish an event with kind 10002 and tags:")]
        public async Task WhenIPublishAnEventWithKindAndTags(Table table)
        {
            var c = this.scenarioContext.Get<Clients>()["Alice"];
            var tags = table.Rows.Select(row => row.Values.ToArray()).ToArray();
            
            var e = new Event
            {
                Id = "",
                Signature = "",
                Content = "",
                CreatedAt = DateTimeOffset.UtcNow,
                PublicKey = c.Keys.PublicKey,
                Tags = tags,
                Kind = (long)EventKind.RelayList
            };

            e = Helpers.FinalizeEvent(e, c.Keys.PrivateKey);
            await c.WebSocket.SendEventAsync(e);
        }

        [When(@"I publish an event with kind 10002 and no tags")]
        public async Task WhenIPublishAnEventWithKindAndNoTags()
        {
            var c = this.scenarioContext.Get<Clients>()["Alice"];
            
            var e = new Event
            {
                Id = "",
                Signature = "",
                Content = "",
                CreatedAt = DateTimeOffset.UtcNow,
                PublicKey = c.Keys.PublicKey,
                Tags = Array.Empty<string[]>(),
                Kind = (long)EventKind.RelayList
            };

            e = Helpers.FinalizeEvent(e, c.Keys.PrivateKey);
            await c.WebSocket.SendEventAsync(e);
        }

        [When(@"I request relay configurations for my public key")]
        public async Task WhenIRequestRelayConfigurationsForMyPublicKey()
        {
            var c = this.scenarioContext.Get<Clients>()["Alice"];
            var response = await c.HttpClient.GetAsync($"/api/relay/{c.Keys.PublicKey}");
            this.scenarioContext.Set(response);
        }

        [Then(@"the relay configurations should be stored for my public key")]
        public async Task ThenTheRelayConfigurationsShouldBeStoredForMyPublicKey()
        {
            var c = this.scenarioContext.Get<Clients>()["Alice"];
            using var scope = this.factory.Services.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<NetstrDbContext>();

            var configs = await db.RelayConfigs
                .Where(r => r.PubKey == c.Keys.PublicKey)
                .ToListAsync();

            configs.Should().NotBeEmpty();
            configs.Should().Contain(c => c.RelayUrl == "wss://relay1.com" && c.Read && c.Write);
            configs.Should().Contain(c => c.RelayUrl == "wss://relay2.com" && c.Read && !c.Write);
            configs.Should().Contain(c => c.RelayUrl == "wss://relay3.com" && !c.Read && c.Write);
        }

        [Then(@"my old relay configurations should be replaced")]
        public async Task ThenMyOldRelayConfigurationsShouldBeReplaced()
        {
            var c = this.scenarioContext.Get<Clients>()["Alice"];
            using var scope = this.factory.Services.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<NetstrDbContext>();

            var configs = await db.RelayConfigs
                .Where(r => r.PubKey == c.Keys.PublicKey)
                .ToListAsync();

            configs.Should().NotContain(c => c.RelayUrl == "wss://relay2.com");
        }

        [Then(@"the new relay configurations should be stored")]
        public async Task ThenTheNewRelayConfigurationsShouldBeStored()
        {
            var c = this.scenarioContext.Get<Clients>()["Alice"];
            using var scope = this.factory.Services.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<NetstrDbContext>();

            var configs = await db.RelayConfigs
                .Where(r => r.PubKey == c.Keys.PublicKey)
                .ToListAsync();

            configs.Should().NotBeEmpty();
            configs.Should().Contain(c => c.RelayUrl == "wss://relay1.com" && c.Read && !c.Write);
            configs.Should().Contain(c => c.RelayUrl == "wss://relay4.com" && !c.Read && c.Write);
        }

        [Then(@"I should receive my relay configurations")]
        public async Task ThenIShouldReceiveMyRelayConfigurations()
        {
            var response = this.scenarioContext.Get<HttpResponseMessage>();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var configs = JsonSerializer.Deserialize<List<RelayConfigEntity>>(content);

            configs.Should().NotBeEmpty();
            configs.Should().Contain(c => c.RelayUrl == "wss://relay1.com");
            configs.Should().Contain(c => c.RelayUrl == "wss://relay2.com");
        }
    }
}
