using FluentAssertions;
using Netstr.Messaging;
using Netstr.Messaging.Events.Validators;
using Netstr.Messaging.Models;

namespace Netstr.Tests.Events
{
    public class SealEventValidatorTests
    {
        [Fact]
        public void RejectsKind13WithTags()
        {
            var validator = new SealEventValidator();
            var e = new Event
            {
                Id = "id",
                PublicKey = Alice,
                Signature = "sig",
                Content = "payload",
                Tags = [["p", Bob]],
                Kind = 13,
                CreatedAt = DateTimeOffset.UtcNow
            };

            validator.Validate(e, new ClientContext("client", "127.0.0.1"))
                .Should()
                .Be(Messages.InvalidEmptyTagsForKind13);
        }

        [Fact]
        public void AcceptsKind13WithoutTags()
        {
            var validator = new SealEventValidator();
            var e = new Event
            {
                Id = "id",
                PublicKey = Alice,
                Signature = "sig",
                Content = "payload",
                Tags = [],
                Kind = 13,
                CreatedAt = DateTimeOffset.UtcNow
            };

            validator.Validate(e, new ClientContext("client", "127.0.0.1"))
                .Should()
                .BeNull();
        }

        [Fact]
        public void IgnoresOtherKinds()
        {
            var validator = new SealEventValidator();
            var e = new Event
            {
                Id = "id",
                PublicKey = Alice,
                Signature = "sig",
                Content = "payload",
                Tags = [["p", Bob]],
                Kind = (long)EventKind.EncryptedDirectMessage,
                CreatedAt = DateTimeOffset.UtcNow
            };

            validator.Validate(e, new ClientContext("client", "127.0.0.1"))
                .Should()
                .BeNull();
        }

        private const string Alice = "5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75";
        private const string Bob = "79be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798";
    }
}
