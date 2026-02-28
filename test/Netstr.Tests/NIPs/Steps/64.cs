using FluentAssertions;
using Netstr.Messaging.Models;
using System;
using System.Linq;
using TechTalk.SpecFlow;

namespace Netstr.Tests.NIPs.Steps
{
    public partial class Steps
    {
        private const string DefaultAliceUser = "Alice";
        private const string LastPublishedEventIdFormat = "NIP64.LastPublishedEventId:{0}";
        private const string LastPublishStartedFormat = "NIP64.LastPublishStarted:{0}";
        private const string SubscriptionIdFormat = "NIP64.SubscriptionId:{0}";
        private const string SubscribeStartedFormat = "NIP64.SubscribeStarted:{0}";
        private const string ReceivedEventsFormat = "NIP64.ReceivedEvents:{0}";
        private const string DraftKindFormat = "NIP64.DraftKind";
        private const string DraftTagsFormat = "NIP64.DraftTags";
        private const string DraftUserFormat = "NIP64.DraftUser";
        private const string UserKeysFormat = "NIP64.UserKeys:{0}";

        [Given(@"a relay at ""(.*)""")]
        public void GivenARelayIsRunningAt(string _)
        {
            GivenARelayIsRunning();
        }

        [Given(@"a user (.*)")]
        public void GivenAUser(string user)
        {
            this.scenarioContext.Set(GetDefaultUserKeys(user), string.Format(UserKeysFormat, user));
        }

        [Given(@"(.*) is connected to the relay")]
        public Task GivenUserIsConnectedToTheRelay(string user)
        {
            return GivenAliceIsConnectedToRelay(user, GetUserKeys(user));
        }

        [When(@"(.*) publishes an event with kind (.*) and content ""(.*)""")]
        public Task WhenUserPublishesAnEventWithKindAndContent(string user, long kind, string content)
        {
            return PublishKind64EventAsync(user, kind, content, Array.Empty<string[]>());
        }

        [When(@"(.*) publishes an event with kind (.*) and content:")]
        public Task WhenUserPublishesAnEventWithKindAndContentMultiline(string user, long kind, string content)
        {
            return PublishKind64EventAsync(user, kind, content, Array.Empty<string[]>());
        }

        [When(@"(.*) publishes an event with kind (.*) and tags:")]
        public void WhenUserPublishesAnEventWithKindAndTags(string user, long kind, Table table)
        {
            var tags = ExtractTagsFromTable(table);

            this.scenarioContext[DraftKindFormat] = kind;
            this.scenarioContext[DraftTagsFormat] = tags;
            this.scenarioContext[DraftUserFormat] = user;
        }

        [When(@"content ""(.*)""")]
        public Task WhenUserPublishesDraftContent(string content)
        {
            var user = GetScenarioValue(DraftUserFormat, string.Empty);
            user.Should().NotBeNullOrWhiteSpace("a tags-first publish step must be followed by content");

            var kind = GetScenarioValue(DraftKindFormat, 0L);
            var tags = GetScenarioValue(DraftTagsFormat, Array.Empty<string[]>());

            return PublishKind64EventAsync(user, kind, content, tags);
        }

        [When(@"(.*) subscribes to events with kind (.*)")]
        public async Task WhenUserSubscribesToEventsWithKind(string user, long kind)
        {
            var c = this.scenarioContext.Get<Clients>()[user];
            var now = DateTimeOffset.UtcNow;
            var subscriptionId = BuildSubscriptionId(user);
            await c.WebSocket.SendReqAsync(subscriptionId, [new SubscriptionFilterRequest { Kinds = [kind] }]);
            await c.WaitForMessageAsync(now, [MessageType.EndOfStoredEvents, subscriptionId], [MessageType.Closed, subscriptionId]);

            this.scenarioContext[string.Format(SubscriptionIdFormat, user)] = subscriptionId;
            this.scenarioContext[string.Format(SubscribeStartedFormat, user)] = now;
        }

        [Then(@"the relay accepts the event")]
        public async Task ThenTheRelayAcceptsTheEvent()
        {
            await AssertLastEventAck(expectedSuccess: true);
        }

        [Then(@"the relay rejects the event with ""(.*)""")]
        public async Task ThenTheRelayRejectsTheEventWith(string expectedMessage)
        {
            await AssertLastEventAck(expectedSuccess: false, expectedMessage: expectedMessage);
        }

        [Then(@"(.*) receives (.*) event")]
        public Task ThenUserReceivesEvent(string user, int expectedCount)
        {
            return ThenUserReceivesEventsAsync(user, expectedCount);
        }

        [Then(@"the event content is ""(.*)""")]
        public void ThenTheEventContentIs(string expectedContent)
        {
            var user = DefaultAliceUser;
            var received = GetLatestReceivedEvents(user);

            received.Should().ContainSingle();
            received[0].Content.Should().Be(expectedContent);
        }

        [Then(@"the event has tag ""(.*)"" with value ""(.*)""")]
        public void ThenTheEventHasTagWithValue(string tag, string expectedValue)
        {
            var user = DefaultAliceUser;
            var received = GetLatestReceivedEvents(user);

            received.Should().ContainSingle();
            received[0].GetTagValue(tag).Should().Be(expectedValue);
        }

        private async Task AssertLastEventAck(bool expectedSuccess, string? expectedMessage = null)
        {
            var user = DefaultAliceUser;
            var c = this.scenarioContext.Get<Clients>()[user];
            var eventId = GetScenarioValue(string.Format(LastPublishedEventIdFormat, user), string.Empty);
            var started = GetScenarioValue(string.Format(LastPublishStartedFormat, user), DateTimeOffset.UtcNow.AddMinutes(-1));

            await c.WaitForMessageAsync(started, [MessageType.Ok, eventId]);

            var ack = c.GetReceivedMessages()
                .Reverse()
                .FirstOrDefault(x => x[0] as string == MessageType.Ok && string.Equals(x[1], eventId));

            ack.Should().NotBeNull();
            ack![2].Should().Be(expectedSuccess);
            if (expectedMessage is not null)
            {
                ack[3]?.ToString().Should().Be(expectedMessage);
            }
        }

        private Task ThenUserReceivesEventsAsync(string user, int expectedCount)
        {
            var subscriptionId = GetScenarioValue(string.Format(SubscriptionIdFormat, user), string.Empty);
            subscriptionId.Should().NotBeNullOrWhiteSpace("subscription must be created before checking received events");

            var c = this.scenarioContext.Get<Clients>()[user];
            var received = c.GetReceivedMessages().ToList();
            var messageEvents = received
                .Where(x => IsMatchingSubscriptionEvent(x, subscriptionId))
                .ToList();

            if (expectedCount == 0)
            {
                messageEvents.Should().BeEmpty();
                this.scenarioContext[string.Format(ReceivedEventsFormat, user)] = new List<Event>();
                return Task.CompletedTask;
            }

            messageEvents.Should().HaveCount(expectedCount);

            var ids = messageEvents
                .Where(x => x.Length > 2)
                .Select(x => x[2]?.ToString())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();

            var events = c.GetReceivedEvents()
                .Where(x => ids.Contains(x.Id))
                .ToList();

            events.Should().HaveCount(expectedCount);
            this.scenarioContext[string.Format(ReceivedEventsFormat, user)] = events;

            return Task.CompletedTask;
        }

        private async Task PublishKind64EventAsync(string user, long kind, string content, string[][] tags)
        {
            var c = this.scenarioContext.Get<Clients>()[user];
            var started = DateTimeOffset.UtcNow;
            var e = new Event
            {
                Id = "",
                Signature = "",
                Content = content,
                CreatedAt = DateTimeOffset.UtcNow,
                PublicKey = c.Keys.PublicKey,
                Tags = tags,
                Kind = kind
            };

            e = Helpers.FinalizeEvent(e, c.Keys.PrivateKey);
            await c.WebSocket.SendEventAsync(e);

            this.scenarioContext[string.Format(LastPublishedEventIdFormat, user)] = e.Id;
            this.scenarioContext[string.Format(LastPublishStartedFormat, user)] = started;
            this.scenarioContext.Remove(DraftKindFormat);
            this.scenarioContext.Remove(DraftTagsFormat);
            this.scenarioContext.Remove(DraftUserFormat);
        }

        private static string[][] ExtractTagsFromTable(Table table)
        {
            var rows = table.Rows
                .Where(r => r.Values.Count >= 2)
                .Select(r => r.Values.Take(2).ToArray())
                .ToArray();

            if (rows.Length > 0)
            {
                return rows;
            }

            // Support header-only one-row shorthand tables written as:
            // | key | value |
            if (table.Header.Count == 2)
            {
                var header = table.Header.ToArray();
                return new[]
                {
                    new[] { header[0], header[1] }
                };
            }

            return Array.Empty<string[]>();
        }

        private static bool IsMatchingSubscriptionEvent(object[] message, string subscriptionId)
        {
            return message.Length > 1 && (string)message[0] == MessageType.Event && (string)message[1] == subscriptionId;
        }

        private static string BuildSubscriptionId(string user) => $"{user}-64";

        private static List<Event> GetLatestReceivedEvents(string user, ScenarioContext scenarioContext)
        {
            return scenarioContext.TryGetValue(string.Format(ReceivedEventsFormat, user), out var events)
                ? (List<Event>)events
                : new List<Event>();
        }

        private List<Event> GetLatestReceivedEvents(string user)
        {
            return GetLatestReceivedEvents(user, this.scenarioContext);
        }

        private T GetScenarioValue<T>(string key, T defaultValue)
        {
            if (this.scenarioContext.ContainsKey(key))
            {
                if (this.scenarioContext[key] is T value)
                {
                    return value;
                }

                throw new InvalidOperationException($"Context value '{key}' is not the expected type {typeof(T).Name}.");
            }

            return defaultValue;
        }

        private static Keys GetDefaultUserKeys(string user)
        {
            if (user == DefaultAliceUser)
            {
                return new Keys(
                    "5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75",
                    "512a14752ed58380496920da432f1c0cdad952cd4afda3d9bfa51c2051f91b02"
                );
            }

            throw new InvalidOperationException($"No default keys configured for user '{user}'.");
        }

        private Keys GetUserKeys(string user)
        {
            if (this.scenarioContext.ContainsKey(string.Format(UserKeysFormat, user)))
            {
                return (Keys)this.scenarioContext[string.Format(UserKeysFormat, user)];
            }

            return GetDefaultUserKeys(user);
        }
    }
}
