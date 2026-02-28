using FluentAssertions;
using Netstr.Messaging;
using Netstr.Messaging.Models;
using Netstr.Tests.NIPs;

namespace Netstr.Tests
{
    public class Nip62ReplayHardeningTests
    {
        private readonly WebApplicationFactory factory;

        public Nip62ReplayHardeningTests()
        {
            this.factory = new WebApplicationFactory();
        }

        [Fact]
        public async Task NIP_62_VanishDeletedGiftWrapCannotBeRepublished()
        {
            using var aliceWs = await this.factory.ConnectWebSocketAsync();
            using var bobWs = await this.factory.ConnectWebSocketAsync();

            var giftWrap = Helpers.FinalizeEvent(new Event
            {
                Id = string.Empty,
                Signature = string.Empty,
                PublicKey = Bob.PublicKey,
                Kind = (long)EventKind.GiftWrap,
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(1728905459),
                Tags = [[EventTag.PublicKey, Alice.PublicKey]],
                Content = "encrypted"
            }, Bob.PrivateKey);

            var vanish = Helpers.FinalizeEvent(new Event
            {
                Id = string.Empty,
                Signature = string.Empty,
                PublicKey = Alice.PublicKey,
                Kind = (long)EventKind.RequestToVanish,
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(1728905470),
                Tags = [[EventTag.Relay, "ALL_RELAYS"]],
                Content = string.Empty
            }, Alice.PrivateKey);

            await bobWs.SendEventAsync(giftWrap);
            var firstGiftWrapAck = await bobWs.ReceiveOnceAsync();

            await aliceWs.SendEventAsync(vanish);
            var vanishAck = await aliceWs.ReceiveOnceAsync();

            await bobWs.SendEventAsync(giftWrap);
            var replayGiftWrapAck = await bobWs.ReceiveOnceAsync();

            firstGiftWrapAck[0].GetString().Should().Be(MessageType.Ok);
            firstGiftWrapAck[1].GetString().Should().Be(giftWrap.Id);
            firstGiftWrapAck[2].GetBoolean().Should().BeTrue();

            vanishAck[0].GetString().Should().Be(MessageType.Ok);
            vanishAck[1].GetString().Should().Be(vanish.Id);
            vanishAck[2].GetBoolean().Should().BeTrue();

            replayGiftWrapAck[0].GetString().Should().Be(MessageType.Ok);
            replayGiftWrapAck[1].GetString().Should().Be(giftWrap.Id);
            replayGiftWrapAck[2].GetBoolean().Should().BeFalse();
            replayGiftWrapAck[3].GetString().Should().Be(Messages.InvalidDeletedEvent);
        }
    }
}
