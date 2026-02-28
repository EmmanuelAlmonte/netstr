namespace Netstr.Options
{
    public record RuntimePathsOptions
    {
        public const string DefaultConfigDirectory = "/etc/netstr";
        public const string DefaultDataDirectory = "/var/lib/netstr";
        public const string DefaultLogDirectory = "/var/log/netstr";

        public const string ConfigDirectoryEnvironmentVariable = "NETSTR_CONFIG_DIR";
        public const string DataDirectoryEnvironmentVariable = "NETSTR_DATA_DIR";
        public const string LogDirectoryEnvironmentVariable = "NETSTR_LOG_DIR";

        public string ConfigDirectory { get; init; } = DefaultConfigDirectory;
        public string DataDirectory { get; init; } = DefaultDataDirectory;
        public string LogDirectory { get; init; } = DefaultLogDirectory;

        public static RuntimePathsOptions FromEnvironment()
        {
            return new RuntimePathsOptions
            {
                ConfigDirectory = ResolvePath(
                    ConfigDirectoryEnvironmentVariable,
                    "RuntimePaths__ConfigDirectory",
                    DefaultConfigDirectory),
                DataDirectory = ResolvePath(
                    DataDirectoryEnvironmentVariable,
                    "RuntimePaths__DataDirectory",
                    DefaultDataDirectory),
                LogDirectory = ResolvePath(
                    LogDirectoryEnvironmentVariable,
                    "RuntimePaths__LogDirectory",
                    DefaultLogDirectory)
            };
        }

        public Dictionary<string, string?> ToConfigurationEntries()
        {
            return new Dictionary<string, string?>
            {
                ["RuntimePaths:ConfigDirectory"] = this.ConfigDirectory,
                ["RuntimePaths:DataDirectory"] = this.DataDirectory,
                ["RuntimePaths:LogDirectory"] = this.LogDirectory
            };
        }

        public string GetConfigFilePath(string fileName)
        {
            return Path.Combine(this.ConfigDirectory, fileName);
        }

        public string GetDefaultLogFilePath()
        {
            return Path.Combine(this.LogDirectory, "netstr-.log");
        }

        private static string ResolvePath(string preferredEnvironmentVariable, string fallbackEnvironmentVariable, string defaultValue)
        {
            var explicitValue = Environment.GetEnvironmentVariable(preferredEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(explicitValue))
            {
                return explicitValue.Trim();
            }

            var sectionValue = Environment.GetEnvironmentVariable(fallbackEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(sectionValue))
            {
                return sectionValue.Trim();
            }

            return defaultValue;
        }
    }
}
