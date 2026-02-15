using FluentAssertions;
using Netstr.Messaging.Models;
using Netstr.Messaging.Subscriptions;

namespace Netstr.Tests.Subscriptions
{
    public class SubscriptionFilterMatcherTests
    {
        [Fact]
        public void OrTags_DoesNotThrow_OnSingleElementEventTag_AndDoesNotMatch()
        {
            var e = new Event
            {
                Id = "id",
                Content = "content",
                CreatedAt = DateTimeOffset.UtcNow,
                Kind = 1,
                PublicKey = "pubkey",
                Signature = "sig",
                Tags = [["p"]]
            };

            var filter = new SubscriptionFilter(
                [],
                [],
                [],
                null,
                null,
                null,
                null,
                new Dictionary<string, string[]> { ["p"] = ["someone"] },
                new());

            var act = () => SubscriptionFilterMatcher.IsMatch(filter, e);

            act.Should().NotThrow();
            SubscriptionFilterMatcher.IsMatch(filter, e).Should().BeFalse();
        }

        [Fact]
        public void AndTags_DoesNotThrow_OnSingleElementEventTag_AndDoesNotMatch()
        {
            var e = new Event
            {
                Id = "id",
                Content = "content",
                CreatedAt = DateTimeOffset.UtcNow,
                Kind = 1,
                PublicKey = "pubkey",
                Signature = "sig",
                Tags = [["p"]]
            };

            var filter = new SubscriptionFilter(
                [],
                [],
                [],
                null,
                null,
                null,
                null,
                new(),
                new Dictionary<string, string[]> { ["p"] = ["someone"] });

            var act = () => SubscriptionFilterMatcher.IsMatch(filter, e);

            act.Should().NotThrow();
            SubscriptionFilterMatcher.IsMatch(filter, e).Should().BeFalse();
        }
    }
}

