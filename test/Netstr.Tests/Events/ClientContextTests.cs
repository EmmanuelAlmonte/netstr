using Netstr.Messaging.Models;
using Xunit;

namespace Netstr.Tests.Events
{
    public class ClientContextTests
    {
        private const string Alice = "5758137ec7f38f3d6c3ef103e28cd9312652285dab3497fe5e5f6c5c0ef45e75";
        private const string Bob = "79be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798";

        [Fact]
        public void AuthenticateSupportsMultiplePubKeys()
        {
            var context = new ClientContext("client1", "127.0.0.1");

            context.Authenticate(Alice);
            context.Authenticate(Bob);

            Assert.True(context.IsAuthenticated());
            Assert.True(context.IsAuthenticated(Alice));
            Assert.True(context.IsAuthenticated(Bob));
            Assert.Contains(Alice, context.AuthenticatedPublicKeys);
            Assert.Contains(Bob, context.AuthenticatedPublicKeys);
            Assert.True(context.IsAuthenticatedForAny([Alice]));
            Assert.True(context.IsAuthenticatedForAny([Bob]));
            Assert.True(context.IsAuthenticatedForAny(new[] { "abc", Bob }));
            Assert.False(context.IsAuthenticatedForAny("abc", "def"));
        }

        [Fact]
        public void AuthenticateDeduplicatesAndSkipsWhitespace()
        {
            var context = new ClientContext("client1", "127.0.0.1");

            context.Authenticate(Alice);
            context.Authenticate(Alice);

            Assert.Single(context.AuthenticatedPublicKeys);
            Assert.Equal(Alice, context.PublicKey);
            Assert.Throws<ArgumentException>(() => context.Authenticate("  "));
        }
    }
}
