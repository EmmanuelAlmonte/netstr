using FluentAssertions;
using Microsoft.Extensions.Options;
using Netstr.Messaging;
using Netstr.Messaging.Events.Validators;
using Netstr.Messaging.Models;
using NetstrOptions = Netstr.Options;

namespace Netstr.Tests.Events
{
    public class AuthCreatedAtValidatorTests
    {
        [Fact]
        public void AcceptsAuthEventWithinConfiguredWindow()
        {
            var validator = CreateValidator(600);
            var createdAt = DateTimeOffset.UtcNow;

            var result = validator.Validate(AuthEvent(createdAt), new ClientContext("client", "127.0.0.1"));

            result.Should().BeNull();
        }

        [Fact]
        public void RejectsAuthEventOlderThanConfiguredWindow()
        {
            var validator = CreateValidator(60);
            var createdAt = DateTimeOffset.UtcNow.AddMinutes(-2);

            var result = validator.Validate(AuthEvent(createdAt), new ClientContext("client", "127.0.0.1"));

            result.Should().Be(Messages.InvalidCreatedAt);
        }

        [Fact]
        public void RejectsAuthEventFurtherInFutureThanConfiguredWindow()
        {
            var validator = CreateValidator(60);
            var createdAt = DateTimeOffset.UtcNow.AddMinutes(2);

            var result = validator.Validate(AuthEvent(createdAt), new ClientContext("client", "127.0.0.1"));

            result.Should().Be(Messages.InvalidCreatedAt);
        }

        [Fact]
        public void SkipsAuthCreatedAtCheckWhenOptionDisabled()
        {
            var validator = CreateValidator(0);
            var createdAt = DateTimeOffset.UtcNow.AddMinutes(-30);

            var result = validator.Validate(AuthEvent(createdAt), new ClientContext("client", "127.0.0.1"));

            result.Should().BeNull();
        }

        private static AuthCreatedAtValidator CreateValidator(int tolerance)
        {
            return new AuthCreatedAtValidator(
                global::Microsoft.Extensions.Options.Options.Create(new NetstrOptions.AuthOptions
                {
                    AuthCreatedAtWindowSeconds = tolerance
                }));
        }

        private static Event AuthEvent(DateTimeOffset createdAt)
        {
            return new Event
            {
                Id = "id",
                PublicKey = Alice,
                Signature = "signature",
                Content = "",
                CreatedAt = createdAt,
                Tags = Array.Empty<string[]>(),
                Kind = (long)EventKind.Auth
            };
        }

        private const string Alice = "5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75";
    }
}
