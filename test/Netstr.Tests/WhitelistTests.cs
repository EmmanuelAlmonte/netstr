using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Netstr.Messaging;
using Netstr.Messaging.Models;
using Netstr.Options;
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
    public class WhitelistTests : IClassFixture<WebApplicationFactory>
    {
        private readonly WebApplicationFactory factory;

        public WhitelistTests(WebApplicationFactory factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async Task WhitelistedPublicKey_CanPublishEvents()
        {
            // Arrange
            this.factory.WhitelistOptions = new WhitelistOptions
            {
                Enabled = true,
                AllowedPublicKeys = new[] { Alice.PublicKey },
                RestrictPublishing = true,
                RestrictSubscribing = false
            };

            using var client = this.factory.CreateClient();
            using var ws = await this.factory.ConnectWebSocketAsync();

            // Act
            var e = new Event { Kind = 1, Content = "Hello from whitelisted user", CreatedAt = DateTimeOffset.UtcNow, Id = "test", PublicKey = Alice.PublicKey, Signature = "test", Tags = [] };
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
            this.factory.WhitelistOptions = new WhitelistOptions
            {
                Enabled = true,
                AllowedPublicKeys = new[] { Alice.PublicKey },
                RestrictPublishing = true,
                RestrictSubscribing = false
            };

            using var client = this.factory.CreateClient();
            using var ws = await this.factory.ConnectWebSocketAsync();

            // Act
            var e = new Event
            {
                Id = "non_whitelisted_event_id",
                PublicKey = "non_whitelisted_pubkey",
                Kind = 1,
                Tags = Array.Empty<string[]>(),
                Content = "Hello from non-whitelisted user",
                Signature = "fake_signature",
                CreatedAt = DateTimeOffset.UtcNow
            };
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
            this.factory.WhitelistOptions = new WhitelistOptions
            {
                Enabled = false,
                AllowedPublicKeys = new[] { Alice.PublicKey },
                RestrictPublishing = true,
                RestrictSubscribing = false
            };

            using var client = this.factory.CreateClient();
            using var ws = await this.factory.ConnectWebSocketAsync();

            // Act
            var e = new Event
            {
                Id = "non_whitelisted_event_id",
                PublicKey = "non_whitelisted_pubkey",
                Kind = 1,
                Tags = Array.Empty<string[]>(),
                Content = "Hello with whitelist disabled",
                Signature = "fake_signature",
                CreatedAt = DateTimeOffset.UtcNow
            };
            await ws.SendEventAsync(e);

            // Assert
            var response = await ws.ReceiveOnceAsync();
            var okMessage = response;
            var messageType = okMessage[0].GetString();
            var eventId = okMessage[1].GetString();

            // Note: This might fail due to other validations like signature check
            // We're just checking that it doesn't fail with the whitelist error
            Assert.Equal("OK", messageType);
            Assert.Equal(e.Id, eventId);
            if (okMessage.Length > 3)
            {
                var message = okMessage[3].GetString();
                Assert.NotEqual(Messages.WhitelistRestricted, message);
            }
        }
    }
}
