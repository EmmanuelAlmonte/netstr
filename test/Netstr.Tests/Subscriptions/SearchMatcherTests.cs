using FluentAssertions;
using Netstr.Messaging.Models;
using Netstr.Messaging.Subscriptions;

namespace Netstr.Tests.Subscriptions
{
    public class SearchMatcherTests
    {
        [Fact]
        public void IncludeSpam_IsNoOp_AndDoesNotForceNoMatches()
        {
            var e = new Event
            {
                Id = "id",
                Content = "foo bar",
                CreatedAt = DateTimeOffset.UtcNow,
                Kind = 1,
                PublicKey = "pubkey",
                Signature = "sig",
                Tags = []
            };

            SearchMatcher.MatchesSearch(e, "foo include:spam").Should().BeTrue();
            SearchMatcher.MatchesSearch(e, "include:spam").Should().BeTrue();
        }

        [Fact]
        public void UnsupportedExtensions_AreIgnored()
        {
            var e = new Event
            {
                Id = "id",
                Content = "foo",
                CreatedAt = DateTimeOffset.UtcNow,
                Kind = 1,
                PublicKey = "pubkey",
                Signature = "sig",
                Tags = []
            };

            SearchMatcher.MatchesSearch(e, "domain:example.com foo").Should().BeTrue();
        }

        [Fact]
        public void BasicTerms_MustMatchContent()
        {
            var e = new Event
            {
                Id = "id",
                Content = "bar",
                CreatedAt = DateTimeOffset.UtcNow,
                Kind = 1,
                PublicKey = "pubkey",
                Signature = "sig",
                Tags = []
            };

            SearchMatcher.MatchesSearch(e, "foo include:spam").Should().BeFalse();
        }
    }
}

