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

        [Theory]
        [InlineData((long)EventKind.WalletResponse)]
        [InlineData((long)EventKind.CashuWalletToken)]
        [InlineData((long)EventKind.CashuWalletHistory)]
        [InlineData((long)EventKind.Nutzap)]
        [InlineData((long)EventKind.NutzapMintRecommendation)]
        [InlineData((long)EventKind.CashuWalletEvent)]
        public void Validate_ExemptCashuAndNutzapKinds_ReturnNull(long walletKind)
        {
            // Arrange
            var exemptKinds = new[]
            {
                (long)EventKind.WalletResponse,
                (long)EventKind.CashuWalletToken,
                (long)EventKind.CashuWalletHistory,
                (long)EventKind.Nutzap,
                (long)EventKind.NutzapMintRecommendation,
                (long)EventKind.CashuWalletEvent
            };

            options = new WhitelistOptions
            {
                Enabled = true,
                AllowedPublicKeys = [],
                RestrictPublishing = true,
                RestrictSubscribing = true,
                ExemptKinds = exemptKinds
            };
            optionsMock.Setup(x => x.CurrentValue).Returns(options);

            var e = CreateEvent("not_allowed_pubkey", walletKind);
            var context = new ClientContext("client1", "127.0.0.1");

            // Act
            var result = validator.Validate(e, context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Validate_NonExemptKindBlocked_WhileCashuAndNutzapExemptKindsAllowed()
        {
            // Arrange
            var exemptKinds = new[]
            {
                (long)EventKind.WalletResponse,
                (long)EventKind.CashuWalletToken,
                (long)EventKind.CashuWalletHistory,
                (long)EventKind.Nutzap,
                (long)EventKind.NutzapMintRecommendation,
                (long)EventKind.CashuWalletEvent
            };
            options = new WhitelistOptions
            {
                Enabled = true,
                AllowedPublicKeys = [],
                RestrictPublishing = true,
                RestrictSubscribing = true,
                ExemptKinds = exemptKinds
            };
            optionsMock.Setup(x => x.CurrentValue).Returns(options);
            var context = new ClientContext("client1", "127.0.0.1");

            // Act
            var blocked = validator.Validate(CreateEvent("not_allowed_pubkey", (long)EventKind.ShortTextNote), context);
            var exemptResults = exemptKinds
                .Select(kind => validator.Validate(CreateEvent("not_allowed_pubkey", kind), context))
                .ToArray();

            // Assert
            Assert.Equal(Messages.WhitelistRestricted, blocked);
            Assert.All(exemptResults, Assert.Null);
        }

        private Event CreateEvent(string publicKey, long kind = 1)
        {
            return new Event
            {
                Id = "event_id",
                PublicKey = publicKey,
                Kind = kind,
                Tags = Array.Empty<string[]>(),
                Content = "content",
                Signature = "signature",
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
    }
}
