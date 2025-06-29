using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Netstr.Messaging;
using Netstr.Messaging.Events.Validators;
using Netstr.Messaging.Models;
using Netstr.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Netstr.Tests.Events
{
    public class WhitelistValidatorTests
    {
        private readonly Mock<ILogger<WhitelistValidator>> loggerMock;
        private readonly Mock<IOptionsMonitor<WhitelistOptions>> optionsMock;
        private WhitelistOptions options;
        private readonly WhitelistValidator validator;

        public WhitelistValidatorTests()
        {
            loggerMock = new Mock<ILogger<WhitelistValidator>>();
            optionsMock = new Mock<IOptionsMonitor<WhitelistOptions>>();
            options = new WhitelistOptions
            {
                Enabled = true,
                AllowedPublicKeys = new[] { "allowed_pubkey1", "allowed_pubkey2" },
                RestrictPublishing = true,
                RestrictSubscribing = true
            };
            optionsMock.Setup(x => x.CurrentValue).Returns(options);
            validator = new WhitelistValidator(loggerMock.Object, optionsMock.Object);
        }

        [Fact]
        public void Validate_WhitelistDisabled_ReturnsNull()
        {
            // Arrange
            options = new WhitelistOptions { Enabled = false };
            optionsMock.Setup(x => x.CurrentValue).Returns(options);
            var e = CreateEvent("not_allowed_pubkey");
            var context = new ClientContext("client1", "127.0.0.1");

            // Act
            var result = validator.Validate(e, context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Validate_RestrictPublishingDisabled_ReturnsNull()
        {
            // Arrange
            options = new WhitelistOptions { RestrictPublishing = false };
            optionsMock.Setup(x => x.CurrentValue).Returns(options);
            var e = CreateEvent("not_allowed_pubkey");
            var context = new ClientContext("client1", "127.0.0.1");

            // Act
            var result = validator.Validate(e, context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Validate_AllowedPublicKey_ReturnsNull()
        {
            // Arrange
            var e = CreateEvent("allowed_pubkey1");
            var context = new ClientContext("client1", "127.0.0.1");

            // Act
            var result = validator.Validate(e, context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Validate_NotAllowedPublicKey_ReturnsError()
        {
            // Arrange
            var e = CreateEvent("not_allowed_pubkey");
            var context = new ClientContext("client1", "127.0.0.1");

            // Act
            var result = validator.Validate(e, context);

            // Assert
            Assert.Equal(Messages.WhitelistRestricted, result);
        }

        [Fact]
        public void Validate_CaseInsensitiveMatch_ReturnsNull()
        {
            // Arrange
            var e = CreateEvent("ALLOWED_PUBKEY1");
            var context = new ClientContext("client1", "127.0.0.1");

            // Act
            var result = validator.Validate(e, context);

            // Assert
            Assert.Null(result);
        }

        private Event CreateEvent(string publicKey)
        {
            return new Event
            {
                Id = "event_id",
                PublicKey = publicKey,
                Kind = 1,
                Tags = Array.Empty<string[]>(),
                Content = "content",
                Signature = "signature",
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
    }
}
