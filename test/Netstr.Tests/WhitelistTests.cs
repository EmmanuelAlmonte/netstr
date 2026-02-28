using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Netstr.Messaging;
using Netstr.Messaging.Models;
using Netstr.Options;
using Netstr.Tests.NIPs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Netstr.Tests
{
    public class WhitelistTests
    {
        [Fact]
        public async Task WhitelistedPublicKey_CanPublishEvents()
        {
            // Arrange
            using var factory = new WebApplicationFactory();
            factory.WhitelistOptions = new WhitelistOptions
            {
                Enabled = true,
                AllowedPublicKeys = new[] { Alice.PublicKey },
                RestrictPublishing = true,
                RestrictSubscribing = false
            };

            using var client = factory.CreateClient();
            using var ws = await factory.ConnectWebSocketAsync();

            // Act
            var e = new Event { Kind = 1, Content = "Hello from whitelisted user", CreatedAt = DateTimeOffset.UtcNow, Id = "", PublicKey = Alice.PublicKey, Signature = "", Tags = [] };
            e = NIPs.Helpers.FinalizeEvent(e, Alice.PrivateKey);
            await ws.SendEventAsync(e);

            // Assert
            var response = await ws.ReceiveOnceAsync();
            var okMessage = response;
            var messageType = okMessage[0].GetString();
            var eventId = okMessage[1].GetString();
            var success = okMessage[2].GetBoolean();

            Assert.Equal("OK", messageType);
            Assert.Equal(e.Id, eventId);
            Assert.True(success);
        }

        [Fact]
        public async Task NonWhitelistedPublicKey_CannotPublishEvents()
        {
            // Arrange
            using var factory = new WebApplicationFactory();
            factory.WhitelistOptions = new WhitelistOptions
            {
                Enabled = true,
                AllowedPublicKeys = new[] { Alice.PublicKey },
                RestrictPublishing = true,
                RestrictSubscribing = false
            };

            using var client = factory.CreateClient();
            using var ws = await factory.ConnectWebSocketAsync();

            // Act
            var e = new Event { Kind = 1, Content = "Hello from non-whitelisted user", CreatedAt = DateTimeOffset.UtcNow, Id = "", PublicKey = Bob.PublicKey, Signature = "", Tags = [] };
            e = NIPs.Helpers.FinalizeEvent(e, Bob.PrivateKey);
            await ws.SendEventAsync(e);

            // Assert
            var response = await ws.ReceiveOnceAsync();
            var okMessage = response;
            var messageType = okMessage[0].GetString();
            var eventId = okMessage[1].GetString();
            var success = okMessage[2].GetBoolean();
            var message = okMessage[3].GetString();

            Assert.Equal("OK", messageType);
            Assert.Equal(e.Id, eventId);
            Assert.False(success);
            Assert.Equal(Messages.WhitelistRestricted, message);
        }

        [Fact]
        public async Task WhitelistDisabled_AllowsAnyPublicKey()
        {
            // Arrange
            using var factory = new WebApplicationFactory();
            factory.WhitelistOptions = new WhitelistOptions
            {
                Enabled = false,
                AllowedPublicKeys = new[] { Alice.PublicKey },
                RestrictPublishing = true,
                RestrictSubscribing = false
            };

            using var client = factory.CreateClient();
            using var ws = await factory.ConnectWebSocketAsync();

            // Act
            var e = new Event { Kind = 1, Content = "Hello with whitelist disabled", CreatedAt = DateTimeOffset.UtcNow, Id = "", PublicKey = Bob.PublicKey, Signature = "", Tags = [] };
            e = NIPs.Helpers.FinalizeEvent(e, Bob.PrivateKey);
            await ws.SendEventAsync(e);

            // Assert
            var response = await ws.ReceiveOnceAsync();
            var okMessage = response;
            var messageType = okMessage[0].GetString();
            var eventId = okMessage[1].GetString();
            var success = okMessage[2].GetBoolean();
            var message = okMessage.Length > 3 ? okMessage[3].GetString() : null;

            // Note: This might fail due to other validations like signature check
            // We're just checking that it doesn't fail with the whitelist error
            Assert.Equal("OK", messageType);
            Assert.Equal(e.Id, eventId);
            Assert.True(success, $"Publish rejected: {message ?? "<no message>"}");
            if (okMessage.Length > 3)
            {
                Assert.NotEqual(Messages.WhitelistRestricted, message);
            }
        }
    }
}
