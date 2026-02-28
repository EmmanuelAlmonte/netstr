using FluentAssertions;
using Netstr.Messaging.Models;
using TechTalk.SpecFlow;

namespace Netstr.Tests.NIPs.Steps
{
    public partial class Steps
    {
        private const string Nip17LastPublishedEventId = "NIP17.ListEvent.LastPublishedEventId:{0}";
        private const string Nip17LastPublishStarted = "NIP17.ListEvent.LastPublishStarted:{0}";

        [When(@"(.*) publishes a kind 10050 event without relay tags")]
        public async Task WhenUserPublishesKind10050EventWithoutRelayTags(string user)
        {
            await PublishDmRelayListEventAsync(user, []);
        }

        [When(@"(.*) publishes a kind 10050 event with a valid relay tag")]
        public async Task WhenUserPublishesKind10050EventWithAValidRelayTag(string user)
        {
            await PublishDmRelayListEventAsync(user, [new[] { "relay", "wss://relay.example.com" }]);
        }

        [Then(@"(.*) relay list publish should be rejected")]
        public async Task ThenUserRelayListPublishShouldBeRejected(string user)
        {
            await AssertDmRelayListAckAsync(user, expectedSuccess: false, expectedMessage: "invalid: list event missing required tags");
        }

        [Then(@"(.*) relay list publish should be accepted")]
        public async Task ThenUserRelayListPublishShouldBeAccepted(string user)
        {
            await AssertDmRelayListAckAsync(user, expectedSuccess: true);
        }

        private async Task PublishDmRelayListEventAsync(string user, string[][] tags)
        {
            var c = this.scenarioContext.Get<Clients>()[user];
            var started = DateTimeOffset.UtcNow;

            var e = new Event
            {
                Id = string.Empty,
                Signature = string.Empty,
                Content = string.Empty,
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(1722337838),
                PublicKey = c.Keys.PublicKey,
                Tags = tags,
                Kind = (long)EventKind.DmRelays
            };

            e = Helpers.FinalizeEvent(e, c.Keys.PrivateKey);

            await c.WebSocket.SendEventAsync(e);

            this.scenarioContext[string.Format(Nip17LastPublishedEventId, user)] = e.Id;
            this.scenarioContext[string.Format(Nip17LastPublishStarted, user)] = started;

            await c.WaitForMessageAsync(started, ["OK", e.Id]);
        }

        private async Task AssertDmRelayListAckAsync(string user, bool expectedSuccess, string? expectedMessage = null)
        {
            var c = this.scenarioContext.Get<Clients>()[user];
            var eventId = this.GetScenarioValue(string.Format(Nip17LastPublishedEventId, user), string.Empty);
            var started = this.GetScenarioValue(string.Format(Nip17LastPublishStarted, user), DateTimeOffset.UtcNow.AddMinutes(-1));

            eventId.Should().NotBeEmpty();

            await c.WaitForMessageAsync(started, ["OK", eventId]);

            var ack = c.GetReceivedMessages()
                .Where(m => m.Length >= 3 && m[0] as string == "OK" && string.Equals(m[1] as string, eventId))
                .Reverse()
                .FirstOrDefault();

            ack.Should().NotBeNull();
            ack![2].Should().Be(expectedSuccess);

            if (expectedMessage is not null)
            {
                ack[3]?.ToString().Should().Be(expectedMessage);
            }
        }
    }
}
