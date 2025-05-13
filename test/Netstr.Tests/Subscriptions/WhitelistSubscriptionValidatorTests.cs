using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Netstr.Messaging;
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
        private readonly Mock<IOptions<WhitelistOptions>> optionsMock;
        private readonly WhitelistOptions options;
        private readonly WhitelistSubscriptionValidator validator;

        public WhitelistSubscriptionValidatorTests()
        {
            loggerMock = new Mock<ILogger<WhitelistSubscriptionValidator>>();
            optionsMock = new Mock<IOptions<WhitelistOptions>>();
            options = new WhitelistOptions
            {
                Enabled = true,
                AllowedPublicKeys = new[] { "allowed_pubkey1", "allowed_pubkey2" },
                RestrictPublishing = true,
                RestrictSubscribing = true
            };
            optionsMock.Setup(x => x.Value).Returns(options);
            validator = new WhitelistSubscriptionValidator(loggerMock.Object, optionsMock.Object);
        }

        [Fact]
        public void Validate_WhitelistDisabled_ReturnsNull()
        {
            // Arrange
            options.Enabled = false;
            var context = CreateAuthenticatedContext("not_allowed_pubkey");
            var filters = Array.Empty<SubscriptionFilter>();

            // Act
            var result = validator.Validate(context, filters);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Validate_RestrictSubscribingDisabled_ReturnsNull()
        {
            // Arrange
            options.RestrictSubscribing = false;
            var context = CreateAuthenticatedContext("not_allowed_pubkey");
            var filters = Array.Empty<SubscriptionFilter>();

            // Act
            var result = validator.Validate(context, filters);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Validate_NotAuthenticated_ReturnsAuthRequiredError()
        {
            // Arrange
            var context = new ClientContext("client1", "127.0.0.1");
            var filters = Array.Empty<SubscriptionFilter>();

            // Act
            var result = validator.Validate(context, filters);

            // Assert
            Assert.Equal("auth-required: authentication required for subscription", result);
        }

        [Fact]
        public void Validate_AllowedPublicKey_ReturnsNull()
        {
            // Arrange
            var context = CreateAuthenticatedContext("allowed_pubkey1");
            var filters = Array.Empty<SubscriptionFilter>();

            // Act
            var result = validator.Validate(context, filters);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Validate_NotAllowedPublicKey_ReturnsError()
        {
            // Arrange
            var context = CreateAuthenticatedContext("not_allowed_pubkey");
            var filters = Array.Empty<SubscriptionFilter>();

            // Act
            var result = validator.Validate(context, filters);

            // Assert
            Assert.Equal(Messages.WhitelistRestricted, result);
        }

        [Fact]
        public void Validate_CaseInsensitiveMatch_ReturnsNull()
        {
            // Arrange
            var context = CreateAuthenticatedContext("ALLOWED_PUBKEY1");
            var filters = Array.Empty<SubscriptionFilter>();

            // Act
            var result = validator.Validate(context, filters);

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
