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
            var options = new WhitelistOptions
            {
                Enabled = true,
                AllowedPublicKeys = new[] { Alice.PublicKey },
                RestrictPublishing = true,
                RestrictSubscribing = false
            };

            using var client = factory.CreateClient();
            using var ws = await client.ConnectWebSocketAsync();

            // Override the whitelist options for this test
            factory.Services.GetRequiredService<IOptions<WhitelistOptions>>().Value.Enabled = options.Enabled;
            factory.Services.GetRequiredService<IOptions<WhitelistOptions>>().Value.AllowedPublicKeys = options.AllowedPublicKeys;
            factory.Services.GetRequiredService<IOptions<WhitelistOptions>>().Value.RestrictPublishing = options.RestrictPublishing;
            factory.Services.GetRequiredService<IOptions<WhitelistOptions>>().Value.RestrictSubscribing = options.RestrictSubscribing;

            // Act
            var e = Alice.CreateEvent(1, "Hello from whitelisted user");
            await ws.SendEventAsync(e);

            // Assert
            var response = await ws.ReceiveMessageAsync();
            var okMessage = JsonDocument.Parse(response);
            var messageType = okMessage.RootElement[0].GetString();
            var eventId = okMessage.RootElement[1].GetString();
            var success = okMessage.RootElement[2].GetBoolean();

            Assert.Equal("OK", messageType);
            Assert.Equal(e.Id, eventId);
            Assert.True(success);
        }

        [Fact]
        public async Task NonWhitelistedPublicKey_CannotPublishEvents()
        {
            // Arrange
            var options = new WhitelistOptions
            {
                Enabled = true,
                AllowedPublicKeys = new[] { Alice.PublicKey },
                RestrictPublishing = true,
                RestrictSubscribing = false
            };

            using var client = factory.CreateClient();
            using var ws = await client.ConnectWebSocketAsync();

            // Override the whitelist options for this test
            factory.Services.GetRequiredService<IOptions<WhitelistOptions>>().Value.Enabled = options.Enabled;
            factory.Services.GetRequiredService<IOptions<WhitelistOptions>>().Value.AllowedPublicKeys = options.AllowedPublicKeys;
            factory.Services.GetRequiredService<IOptions<WhitelistOptions>>().Value.RestrictPublishing = options.RestrictPublishing;
            factory.Services.GetRequiredService<IOptions<WhitelistOptions>>().Value.RestrictSubscribing = options.RestrictSubscribing;

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
            var response = await ws.ReceiveMessageAsync();
            var okMessage = JsonDocument.Parse(response);
            var messageType = okMessage.RootElement[0].GetString();
            var eventId = okMessage.RootElement[1].GetString();
            var success = okMessage.RootElement[2].GetBoolean();
            var message = okMessage.RootElement[3].GetString();

            Assert.Equal("OK", messageType);
            Assert.Equal(e.Id, eventId);
            Assert.False(success);
            Assert.Equal(Messages.WhitelistRestricted, message);
        }

        [Fact]
        public async Task WhitelistDisabled_AllowsAnyPublicKey()
        {
            // Arrange
            var options = new WhitelistOptions
            {
                Enabled = false,
                AllowedPublicKeys = new[] { Alice.PublicKey },
                RestrictPublishing = true,
                RestrictSubscribing = false
            };

            using var client = factory.CreateClient();
            using var ws = await client.ConnectWebSocketAsync();

            // Override the whitelist options for this test
            factory.Services.GetRequiredService<IOptions<WhitelistOptions>>().Value.Enabled = options.Enabled;
            factory.Services.GetRequiredService<IOptions<WhitelistOptions>>().Value.AllowedPublicKeys = options.AllowedPublicKeys;
            factory.Services.GetRequiredService<IOptions<WhitelistOptions>>().Value.RestrictPublishing = options.RestrictPublishing;
            factory.Services.GetRequiredService<IOptions<WhitelistOptions>>().Value.RestrictSubscribing = options.RestrictSubscribing;

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
            var response = await ws.ReceiveMessageAsync();
            var okMessage = JsonDocument.Parse(response);
            var messageType = okMessage.RootElement[0].GetString();
            var eventId = okMessage.RootElement[1].GetString();

            // Note: This might fail due to other validations like signature check
            // We're just checking that it doesn't fail with the whitelist error
            Assert.Equal("OK", messageType);
            Assert.Equal(e.Id, eventId);
            if (okMessage.RootElement.GetArrayLength() > 3)
            {
                var message = okMessage.RootElement[3].GetString();
                Assert.NotEqual(Messages.WhitelistRestricted, message);
            }
        }
    }
}
