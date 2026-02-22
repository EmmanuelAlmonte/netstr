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
    [Binding]
    public class RelayListSteps : StepsBase
    {
        private readonly ScenarioContext context;

        public RelayListSteps(ScenarioContext context, TestContext testContext)
            : base(testContext)
        {
            this.context = context;
        }

        [Given("I have published relay configurations")]
        public async Task GivenIHavePublishedRelayConfigurations()
        {
            var tags = new[]
            {
                new[] { "r", "wss://relay1.com", "read", "write" },
                new[] { "r", "wss://relay2.com", "read" }
            };

            await this.PublishEvent(new Event
            {
                Kind = (int)EventKind.RelayList,
                Tags = tags.ToList(),
                Content = string.Empty
            });

            await Task.Delay(100); // Allow time for processing
        }

        [When(@"I publish an event with kind 10002 and tags:")]
        public async Task WhenIPublishAnEventWithKindAndTags(Table table)
        {
            var tags = table.Rows.Select(row => row.Values.ToArray()).ToList();
            
            await this.PublishEvent(new Event
            {
                Kind = (int)EventKind.RelayList,
                Tags = tags,
                Content = string.Empty
            });
        }

        [When(@"I publish an event with kind 10002 and no tags")]
        public async Task WhenIPublishAnEventWithKindAndNoTags()
        {
            await this.PublishEvent(new Event
            {
                Kind = (int)EventKind.RelayList,
                Tags = new List<string[]>(),
                Content = string.Empty
            });
        }

        [When(@"I request relay configurations for my public key")]
        public async Task WhenIRequestRelayConfigurationsForMyPublicKey()
        {
            var response = await this.Client.GetAsync($"/api/relay/{this.Alice.PublicKey}");
            context.Set(response);
        }

        [Then(@"the relay configurations should be stored for my public key")]
        public async Task ThenTheRelayConfigurationsShouldBeStoredForMyPublicKey()
        {
            using var scope = this.Factory.Services.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<NetstrDbContext>();

            var configs = await db.RelayConfigs
                .Where(r => r.PubKey == this.Alice.PublicKey)
                .ToListAsync();

            configs.Should().NotBeEmpty();
            configs.Should().Contain(c => c.RelayUrl == "wss://relay1.com" && c.Read && c.Write);
            configs.Should().Contain(c => c.RelayUrl == "wss://relay2.com" && c.Read && !c.Write);
            configs.Should().Contain(c => c.RelayUrl == "wss://relay3.com" && !c.Read && c.Write);
        }

        [Then(@"my old relay configurations should be replaced")]
        public async Task ThenMyOldRelayConfigurationsShouldBeReplaced()
        {
            using var scope = this.Factory.Services.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<NetstrDbContext>();

            var configs = await db.RelayConfigs
                .Where(r => r.PubKey == this.Alice.PublicKey)
                .ToListAsync();

            configs.Should().NotContain(c => c.RelayUrl == "wss://relay2.com");
        }

        [Then(@"the new relay configurations should be stored")]
        public async Task ThenTheNewRelayConfigurationsShouldBeStored()
        {
            using var scope = this.Factory.Services.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<NetstrDbContext>();

            var configs = await db.RelayConfigs
                .Where(r => r.PubKey == this.Alice.PublicKey)
                .ToListAsync();

            configs.Should().NotBeEmpty();
            configs.Should().Contain(c => c.RelayUrl == "wss://relay1.com" && c.Read && !c.Write);
            configs.Should().Contain(c => c.RelayUrl == "wss://relay4.com" && !c.Read && c.Write);
        }

        [Then(@"I should receive my relay configurations")]
        public async Task ThenIShouldReceiveMyRelayConfigurations()
        {
            var response = context.Get<HttpResponseMessage>();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var configs = JsonSerializer.Deserialize<List<RelayConfigEntity>>(content);

            configs.Should().NotBeEmpty();
            configs.Should().Contain(c => c.RelayUrl == "wss://relay1.com");
            configs.Should().Contain(c => c.RelayUrl == "wss://relay2.com");
        }
    }
}
