using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Netstr.Messaging;
using Netstr.Messaging.MessageHandlers;
using Netstr.Messaging.Models;
using Netstr.Messaging.Subscriptions.Validators;
using Netstr.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Netstr.Tests.Subscriptions
{
    public class WhitelistSubscriptionValidatorTests
    {
        private readonly Mock<ILogger<WhitelistSubscriptionValidator>> loggerMock;
        private readonly Mock<IOptionsMonitor<WhitelistOptions>> optionsMock;
        private WhitelistOptions options;
        private readonly WhitelistSubscriptionValidator validator;

        public WhitelistSubscriptionValidatorTests()
        {
            loggerMock = new Mock<ILogger<WhitelistSubscriptionValidator>>();
            optionsMock = new Mock<IOptionsMonitor<WhitelistOptions>>();
            options = new WhitelistOptions
            {
                Enabled = true,
                AllowedPublicKeys = new[] { "allowed_pubkey1", "allowed_pubkey2" },
                RestrictPublishing = true,
                RestrictSubscribing = true
            };
            optionsMock.Setup(x => x.CurrentValue).Returns(options);
            validator = new WhitelistSubscriptionValidator(loggerMock.Object, optionsMock.Object);
        }

        [Fact]
        public void IsApplicable_AlwaysReturnsTrue()
        {
            // Arrange
            var handlerMock = new Mock<FilterMessageHandlerBase>();

            // Act
            var result = validator.IsApplicable(handlerMock.Object);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanSubscribe_WhitelistDisabled_ReturnsNull()
        {
            // Arrange
            options = new WhitelistOptions { Enabled = false };
            optionsMock.Setup(x => x.CurrentValue).Returns(options);
            var context = CreateAuthenticatedContext("not_allowed_pubkey");
            var filters = Array.Empty<SubscriptionFilter>();

            // Act
            var result = validator.CanSubscribe("test_id", context, filters);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void CanSubscribe_RestrictSubscribingDisabled_ReturnsNull()
        {
            // Arrange
            options = new WhitelistOptions { RestrictSubscribing = false };
            optionsMock.Setup(x => x.CurrentValue).Returns(options);
            var context = CreateAuthenticatedContext("not_allowed_pubkey");
            var filters = Array.Empty<SubscriptionFilter>();

            // Act
            var result = validator.CanSubscribe("test_id", context, filters);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void CanSubscribe_NotAuthenticated_ReturnsAuthRequiredError()
        {
            // Arrange
            var context = new ClientContext("client1", "127.0.0.1");
            var filters = Array.Empty<SubscriptionFilter>();

            // Act
            var result = validator.CanSubscribe("test_id", context, filters);

            // Assert
            Assert.Equal("auth-required: authentication required for subscription", result);
        }

        [Fact]
        public void CanSubscribe_AllowedPublicKey_ReturnsNull()
        {
            // Arrange
            var context = CreateAuthenticatedContext("allowed_pubkey1");
            var filters = Array.Empty<SubscriptionFilter>();

            // Act
            var result = validator.CanSubscribe("test_id", context, filters);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void CanSubscribe_NotAllowedPublicKey_ReturnsError()
        {
            // Arrange
            var context = CreateAuthenticatedContext("not_allowed_pubkey");
            var filters = Array.Empty<SubscriptionFilter>();

            // Act
            var result = validator.CanSubscribe("test_id", context, filters);

            // Assert
            Assert.Equal(Messages.WhitelistRestricted, result);
        }

        [Fact]
        public void CanSubscribe_CaseInsensitiveMatch_ReturnsNull()
        {
            // Arrange
            var context = CreateAuthenticatedContext("ALLOWED_PUBKEY1");
            var filters = Array.Empty<SubscriptionFilter>();

            // Act
            var result = validator.CanSubscribe("test_id", context, filters);

            // Assert
            Assert.Null(result);
        }

        private ClientContext CreateAuthenticatedContext(string publicKey)
        {
            var context = new ClientContext("client1", "127.0.0.1");
            context.Authenticate(publicKey);
            return context;
        }
    }
}
