using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Netstr.Options;
using Netstr.Startup;
using Xunit;

namespace Netstr.Tests.Startup
{
    public class StartupCommandHandlerTests
    {
        [Fact]
        public void ValidateConfiguration_WhenConfigurationIsValid_ShouldReturnNoErrors()
        {
            var runtimePaths = new RuntimePathsOptions();
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Connection:WebSocketsPath"] = "/",
                ["ConnectionStrings:NetstrDatabase"] = "Host=localhost;Port=5432;Database=Netstr;Username=netstr;Password=secret",
                ["RelayInformation:Name"] = "netstr",
                ["RelayInformation:Description"] = "relay",
                ["RelayInformation:PublicKey"] = "npub123",
                ["RelayInformation:Contact"] = "ops@netstr.io",
                ["RelayInformation:Version"] = "v1.0.0"
            });

            var errors = StartupCommandHandler.ValidateConfiguration(configuration, runtimePaths).ToArray();

            errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateConfiguration_WhenConnectionStringIsMissing_ShouldReturnError()
        {
            var runtimePaths = new RuntimePathsOptions();
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Connection:WebSocketsPath"] = "/",
                ["RelayInformation:Name"] = "netstr",
                ["RelayInformation:Description"] = "relay",
                ["RelayInformation:PublicKey"] = "npub123",
                ["RelayInformation:Contact"] = "ops@netstr.io",
                ["RelayInformation:Version"] = "v1.0.0"
            });

            var errors = StartupCommandHandler.ValidateConfiguration(configuration, runtimePaths).ToArray();

            errors.Should().Contain(x => x.Contains("ConnectionStrings:NetstrDatabase", StringComparison.Ordinal));
        }

        [Fact]
        public void ValidateConfiguration_WhenWebSocketPathIsInvalid_ShouldReturnError()
        {
            var runtimePaths = new RuntimePathsOptions();
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Connection:WebSocketsPath"] = "relay",
                ["ConnectionStrings:NetstrDatabase"] = "Host=localhost;Port=5432;Database=Netstr;Username=netstr;Password=secret",
                ["RelayInformation:Name"] = "netstr",
                ["RelayInformation:Description"] = "relay",
                ["RelayInformation:PublicKey"] = "npub123",
                ["RelayInformation:Contact"] = "ops@netstr.io",
                ["RelayInformation:Version"] = "v1.0.0"
            });

            var errors = StartupCommandHandler.ValidateConfiguration(configuration, runtimePaths).ToArray();

            errors.Should().Contain("Connection:WebSocketsPath must start with '/'.");
        }

        private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
        }
    }
}
