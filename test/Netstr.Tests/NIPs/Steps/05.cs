using System.Text.Json;
using Netstr.Messaging.Models;
using StackExchange.Redis;
using TechTalk.SpecFlow;

namespace Netstr.Tests.NIPs.Steps
{
    public partial class Steps
    {

        [When(@"(.*) publishes a metadata event with NIP-05 identifier ""(.*)""")]
        public async Task WhenUserPublishesMetadataEventWithNip05Identifier(string user, string nip05Identifier)
        {
            var metadata = new UserMetadata
            {
                Name = user,
                Nip05 = nip05Identifier,
                About = "Test user with NIP-05 identifier"
            };

            var content = JsonSerializer.Serialize(metadata);
            await WhenUserPublishesEvent(user, "0", content, Array.Empty<string[]>());
        }

        [When(@"(.*) publishes a metadata event without NIP-05 identifier")]
        public async Task WhenUserPublishesMetadataEventWithoutNip05Identifier(string user)
        {
            var metadata = new UserMetadata
            {
                Name = user,
                About = "Test user without NIP-05 identifier"
            };

            var content = JsonSerializer.Serialize(metadata);
            await WhenUserPublishesEvent(user, "0", content, Array.Empty<string[]>());
        }

        [When(@"(.*) publishes a metadata event with empty NIP-05 identifier")]
        public async Task WhenUserPublishesMetadataEventWithEmptyNip05Identifier(string user)
        {
            var metadata = new UserMetadata
            {
                Name = user,
                Nip05 = "",
                About = "Test user with empty NIP-05 identifier"
            };

            var content = JsonSerializer.Serialize(metadata);
            await WhenUserPublishesEvent(user, "0", content, Array.Empty<string[]>());
        }

        private async Task WhenUserPublishesEvent(string user, string kind, string content, string[][] tags)
        {
            var c = this.scenarioContext.Get<Clients>()[user];
            
            var e = new Event
            {
                Id = "",
                Signature = "",
                Content = content,
                CreatedAt = DateTimeOffset.UtcNow,
                PublicKey = c.Keys.PublicKey,
                Tags = tags,
                Kind = long.Parse(kind)
            };

            e = Helpers.FinalizeEvent(e, c.Keys.PrivateKey);
            
            await c.WebSocket.SendEventAsync(e);
            
            var start = DateTimeOffset.UtcNow;
            await c.WaitForMessageAsync(start, ["OK", e.Id]);
        }
    }
}