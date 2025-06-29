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
    }
}
