using FluentAssertions;
using Netstr.Messaging.Subscriptions;

namespace Netstr.Tests
{
    public class SearchQueryParserTests
    {
        [Theory]
        [InlineData("foo include:spam", "foo", "include", "spam")]
        [InlineData("domain:example.com foo bar", "foo bar", "domain", "example.com")]
        public void Parse_SplitsBasicTermsAndExtensions(string input, string expectedBasic, string expectedKey, string expectedValue)
        {
            var parsed = SearchQueryParser.Parse(input);

            parsed.BasicTerms.Should().Be(expectedBasic);
            parsed.Extensions.Should().Contain((expectedKey, expectedValue));
        }

        [Fact]
        public void Parse_RemovesExtensionsFromBasicTerms()
        {
            var parsed = SearchQueryParser.Parse("foo unknown:ext bar");

            parsed.BasicTerms.Should().Be("foo bar");
            parsed.Extensions.Should().Contain(("unknown", "ext"));
        }
    }
}

