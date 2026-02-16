using Xunit;
using Netstr.Messaging.Models;
using Netstr.Messaging.Events.Validators;

namespace Netstr.Tests.Events
{
    public class ListEventValidatorTests
    {
        [Fact]
        public void ValidateListType_ShouldReturnNull_ForUnknownEventKind()
        {
            // Arrange
            var validator = new ListEventValidator();
            var unknownEvent = new Event { Kind = 99999, Content = string.Empty, CreatedAt = DateTimeOffset.UtcNow, Id = "test", PublicKey = "test", Signature = "test", Tags = [] }; // Unknown kind

            // Act
            var result = validator.Validate(unknownEvent, null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ValidateListType_ShouldValidateMuteList()
        {
            // Arrange
            var validator = new ListEventValidator();
            var muteListEvent = new Event { Kind = (int)EventKind.MuteList, Tags = new[] { new[] { "p" } }, Content = string.Empty, CreatedAt = DateTimeOffset.UtcNow, Id = "test", PublicKey = "test", Signature = "test" };

            // Act
            var result = validator.Validate(muteListEvent, null);

            // Assert
            Assert.Null(result); // Valid tags
        }

        [Fact]
        public void ValidateListType_ShouldReturnInvalidListTags_ForInvalidMuteList()
        {
            // Arrange
            var validator = new ListEventValidator();
            var invalidMuteListEvent = new Event { Kind = (int)EventKind.MuteList, Tags = new[] { new[] { "invalid" } }, Content = string.Empty, CreatedAt = DateTimeOffset.UtcNow, Id = "test", PublicKey = "test", Signature = "test" };

            // Act
            var result = validator.Validate(invalidMuteListEvent, null);

            // Assert
            Assert.Equal("invalid: list event missing required tags", result);
        }

        [Fact]
        public void ValidateSetEvents_ShouldRequireDTag_ForApplicationSpecificData()
        {
            var validator = new ListEventValidator();
            var missingDTag = new Event
            {
                Kind = (long)EventKind.ApplicationSpecificData,
                Tags = new[] { new[] { "foo", "bar" } },
                Content = string.Empty,
                CreatedAt = DateTimeOffset.UtcNow,
                Id = "test",
                PublicKey = "test",
                Signature = "test"
            };

            var missingResult = validator.Validate(missingDTag, null);
            Assert.Equal("invalid: set event missing 'd' tag identifier", missingResult);
        }

        [Fact]
        public void ValidateSetEvents_ShouldAllowAnyTags_WithDTag_ForApplicationSpecificData()
        {
            var validator = new ListEventValidator();
            var withDTag = new Event
            {
                Kind = (long)EventKind.ApplicationSpecificData,
                Tags = new[] { new[] { "d", "app" }, new[] { "foo", "bar" } },
                Content = string.Empty,
                CreatedAt = DateTimeOffset.UtcNow,
                Id = "test",
                PublicKey = "test",
                Signature = "test"
            };

            var result = validator.Validate(withDTag, null);
            Assert.Null(result);
        }
    }
}
