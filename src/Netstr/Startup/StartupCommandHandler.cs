using Npgsql;
using Netstr.Options;
using System.Reflection;

namespace Netstr.Startup
{
    public record StartupCommandResult(bool ShouldExit, int ExitCode);

    public static class StartupCommandHandler
    {
        private static readonly string[] RelayMetadataKeys =
        [
            "RelayInformation:Name",
            "RelayInformation:Description",
            "RelayInformation:PublicKey",
            "RelayInformation:Contact",
            "RelayInformation:Version"
        ];

        public static StartupCommandResult TryHandle(string[] args, RuntimePathsOptions runtimePaths)
        {
            if (HasCommand(args, "--version"))
            {
                Console.Out.WriteLine(GetVersion());
                return new StartupCommandResult(ShouldExit: true, ExitCode: 0);
            }

            if (HasCommand(args, "--check-config"))
            {
                var configuration = BuildConfiguration(args, runtimePaths);
                var errors = ValidateConfiguration(configuration, runtimePaths).ToArray();

                if (errors.Length == 0)
                {
                    Console.Out.WriteLine("Configuration is valid.");
                    return new StartupCommandResult(ShouldExit: true, ExitCode: 0);
                }

                foreach (var error in errors)
                {
                    Console.Error.WriteLine($"ERROR: {error}");
                }

                return new StartupCommandResult(ShouldExit: true, ExitCode: 1);
            }

            return new StartupCommandResult(ShouldExit: false, ExitCode: 0);
        }

        public static IConfigurationRoot BuildConfiguration(string[] args, RuntimePathsOptions runtimePaths)
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder
                .AddInMemoryCollection(runtimePaths.ToConfigurationEntries())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            if (!string.IsNullOrWhiteSpace(environmentName))
            {
                configBuilder.AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false);
            }

            configBuilder
                .AddJsonFile(runtimePaths.GetConfigFilePath("appsettings.json"), optional: true, reloadOnChange: false)
                .AddJsonFile(runtimePaths.GetConfigFilePath("appsettings.local.json"), optional: true, reloadOnChange: false)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .AddCommandLine(args);

            return configBuilder.Build();
        }

        public static IEnumerable<string> ValidateConfiguration(IConfiguration configuration, RuntimePathsOptions runtimePaths)
        {
            if (string.IsNullOrWhiteSpace(runtimePaths.ConfigDirectory))
            {
                yield return "RuntimePaths:ConfigDirectory cannot be empty.";
            }

            if (string.IsNullOrWhiteSpace(runtimePaths.DataDirectory))
            {
                yield return "RuntimePaths:DataDirectory cannot be empty.";
            }

            if (string.IsNullOrWhiteSpace(runtimePaths.LogDirectory))
            {
                yield return "RuntimePaths:LogDirectory cannot be empty.";
            }

            var webSocketsPath = configuration["Connection:WebSocketsPath"];
            if (string.IsNullOrWhiteSpace(webSocketsPath))
            {
                yield return "Connection:WebSocketsPath is required.";
            }
            else if (!webSocketsPath.StartsWith('/'))
            {
                yield return "Connection:WebSocketsPath must start with '/'.";
            }

            foreach (var relayMetadataKey in RelayMetadataKeys)
            {
                if (string.IsNullOrWhiteSpace(configuration[relayMetadataKey]))
                {
                    yield return $"{relayMetadataKey} is required.";
                }
            }

            var connectionString = configuration.GetConnectionString("NetstrDatabase");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                yield return "ConnectionStrings:NetstrDatabase is required.";
                yield break;
            }

            NpgsqlConnectionStringBuilder? builder = null;
            var isValidConnectionString = true;
            try
            {
                builder = new NpgsqlConnectionStringBuilder(connectionString);
            }
            catch
            {
                isValidConnectionString = false;
            }

            if (!isValidConnectionString || builder == null)
            {
                yield return "ConnectionStrings:NetstrDatabase must be a valid PostgreSQL connection string.";
                yield break;
            }

            if (string.IsNullOrWhiteSpace(builder.Host))
            {
                yield return "ConnectionStrings:NetstrDatabase must include Host.";
            }

            if (string.IsNullOrWhiteSpace(builder.Database))
            {
                yield return "ConnectionStrings:NetstrDatabase must include Database.";
            }

            if (string.IsNullOrWhiteSpace(builder.Username))
            {
                yield return "ConnectionStrings:NetstrDatabase must include Username.";
            }
        }

        private static bool HasCommand(string[] args, string command)
        {
            return args.Any(x => string.Equals(x, command, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetVersion()
        {
            var informationalVersion = Assembly
                .GetEntryAssembly()
                ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            if (!string.IsNullOrWhiteSpace(informationalVersion))
            {
                return informationalVersion;
            }

            return Assembly
                .GetEntryAssembly()
                ?.GetName()
                ?.Version
                ?.ToString()
                ?? "unknown";
        }
    }
}
