using FluentAssertions;
using Netstr.Messaging.Models;
using System.IO;
using System.Text.Json;
using System.Linq;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Netstr.Tests.NIPs.Steps
{
    public partial class Steps
    {
        [When(@"(.*) sends a subscription request (.*)")]
        public async Task WhenAliceSubscribesToEvents(string client, string subscriptionId, IEnumerable<SubscriptionFilterRequest> filters)
        {
            var now = DateTimeOffset.UtcNow;
            var c = this.scenarioContext.Get<Clients>()[client];

            await c.WebSocket.SendReqAsync(subscriptionId, filters);
            await c.WaitForMessageAsync(now, ["EOSE", subscriptionId], ["CLOSED", subscriptionId]);
        }

        [When(@"(.*) publishes an event")]
        [When(@"(.*) publishes events")]
        public async Task WhenBobPublishesAnEvent(string client, Table table)
        {
            var start = DateTimeOffset.UtcNow;
            var c = this.scenarioContext.Get<Clients>()[client];
            var events = Transforms.CreateEvents(table, c);

            foreach (var e in events)
            {
                await c.WebSocket.SendEventAsync(e);
            }

            foreach (var e in events)
            {
                await c.WaitForMessageAsync(start, ["OK", e.Id]);
            }
        }

        [When(@"(.*) closes a subscription (.*)")]
        public async Task WhenAliceClosesASubscriptionAbcd(string client, string subscriptionId)
        {
            var c = this.scenarioContext.Get<Clients>()[client];

            await c.WebSocket.SendCloseAsync(subscriptionId);
            await Task.Delay(500);
        }

        [Then(@"(.*) receives a message")]
        [Then(@"(.*) receives messages")]
        public Task ThenBobReceivesAReply(string client, IEnumerable<object[]> messages)
        {
            var debugFile = Environment.GetEnvironmentVariable("NETSTR_TEST_DEBUG_FILE");
            if (!string.IsNullOrWhiteSpace(debugFile))
            {
                try
                {
                    var debugReceived = this.scenarioContext.Get<Clients>()[client].GetReceivedMessages().ToList();
                    var debugExpected = messages.Select(x => x.ToArray()).ToList();
                    var receivedText = string.Join(
                        Environment.NewLine,
                        debugReceived.Select(message => string.Join(" | ", message.Select(item => item?.ToString() ?? "<null>")))
                    );
                    var expectedText = string.Join(
                        Environment.NewLine,
                        debugExpected.Select(message => string.Join(" | ", message.Select(item => item?.ToString() ?? "<null>")))
                    );

                    File.AppendAllText(
                        debugFile,
                        $"Scenario messages for {client}:{Environment.NewLine}Expected:{Environment.NewLine}{expectedText}{Environment.NewLine}Actual:{Environment.NewLine}{receivedText}{Environment.NewLine}---{Environment.NewLine}"
                    );
                }
                catch
                {
                }
            }

            if (Environment.GetEnvironmentVariable("NETSTR_TEST_DEBUG_MESSAGES") == "1")
            {
                var debugReceived = this.scenarioContext.Get<Clients>()[client].GetReceivedMessages().ToList();
                var debugLines = debugReceived.Select(message =>
                    string.Join(" | ", message.Select(item => item?.ToString() ?? "<null>")));
                Console.WriteLine($"Actual messages for {client}:{Environment.NewLine}{string.Join(Environment.NewLine, debugLines)}");
            }

            return Helpers.VerifyWithDelayAsync(() =>
            {
                var expected = messages.Select(x => x.ToArray()).ToList();
                var received = this.scenarioContext.Get<Clients>()[client].GetReceivedMessages().ToList();

                received.Should().HaveSameCount(expected, "same number of messages should be received");

                for (var i = 0; i < expected.Count; i++)
                {
                    var expectedMessage = expected[i];
                    var actualMessage = received[i];
                    var messageType = GetMessageType(expectedMessage);
                    expectedMessage.Should().HaveSameCount(actualMessage, $"message payload at index {i} should match");

                    for (var j = 0; j < expectedMessage.Length; j++)
                    {
                        var expectedItem = expectedMessage[j];
                        var actualItem = actualMessage[j];

                        if (expectedItem is string expectedText && actualItem is string actualText)
                        {
                            if (expectedText == "*" || IsSyntheticPlaceholder(expectedText))
                            {
                                continue;
                            }

                            if (ShouldIgnoreExpectedValue(messageType, j, expectedText))
                            {
                                continue;
                            }

                            actualText.Should().Be(expectedText);
                        }

                        if (expectedItem is null && actualItem is null)
                        {
                            continue;
                        }

                        actualItem.Should().Be(expectedItem);
                    }
                }
            });
        }

        private static string? GetMessageType(object[] message)
        {
            if (message.Length == 0)
            {
                return null;
            }

            return message[0] as string;
        }

        private static bool ShouldIgnoreExpectedValue(string? messageType, int index, string expectedText)
        {
            return messageType == MessageType.Ok && index == 3 && string.IsNullOrWhiteSpace(expectedText)
                || messageType == MessageType.Event && index == 2 && string.IsNullOrWhiteSpace(expectedText)
                || messageType == MessageType.Notice && index == 1 && string.IsNullOrWhiteSpace(expectedText)
                || messageType == MessageType.Closed && index == 2 && string.IsNullOrWhiteSpace(expectedText);
        }

        private static bool IsSyntheticPlaceholder(string expectedText)
        {
            const string hexChars = "0123456789abcdefABCDEF";
            return expectedText.Length >= 16
                && expectedText.All(c => c == expectedText[0])
                && hexChars.Contains(expectedText[0]);
        }
    }
}
