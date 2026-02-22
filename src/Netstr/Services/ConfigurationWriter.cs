using System.Text.Json;

namespace Netstr.Services
{
    public interface IConfigurationWriter
    {
        Task UpdateConfigurationAsync(string section, object value);
    }

    public class ConfigurationWriter : IConfigurationWriter
    {
        private readonly IHostEnvironment _environment;
        private readonly ILogger<ConfigurationWriter> _logger;

        public ConfigurationWriter(IHostEnvironment environment, ILogger<ConfigurationWriter> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task UpdateConfigurationAsync(string section, object value)
        {
            try
            {
                // Determine which settings file to update
                string configFile = _environment.IsDevelopment() 
                    ? "appsettings.Development.json" 
                    : "appsettings.json";
                
                string filePath = Path.Combine(_environment.ContentRootPath, configFile);
                
                // Read the current config
                string json = await File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions { WriteIndented = true };
                var config = JsonSerializer.Deserialize<JsonElement>(json);
                
                // Convert to dictionary for easier manipulation
                var configDict = JsonToDictionary(config);
                
                // Update the specified section
                UpdateSection(configDict, section, value);
                
                // Write back to file
                string updatedJson = JsonSerializer.Serialize(configDict, options);
                await File.WriteAllTextAsync(filePath, updatedJson);
                
                _logger.LogInformation("Updated configuration section {Section} in {File}", section, configFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update configuration section {Section}", section);
                throw;
            }
        }

        private Dictionary<string, object> JsonToDictionary(JsonElement element)
        {
            var dict = new Dictionary<string, object>();
            
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    dict[property.Name] = property.Value.ValueKind == JsonValueKind.Object
                        ? JsonToDictionary(property.Value)
                        : property.Value.ValueKind == JsonValueKind.Array
                            ? JsonToList(property.Value)
                            : GetValue(property.Value);
                }
            }
            
            return dict;
        }

        private List<object> JsonToList(JsonElement element)
        {
            var list = new List<object>();
            
            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(item.ValueKind == JsonValueKind.Object
                        ? JsonToDictionary(item)
                        : item.ValueKind == JsonValueKind.Array
                            ? JsonToList(item)
                            : GetValue(item));
                }
            }
            
            return list;
        }

        private object GetValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.TryGetInt64(out long l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.ToString()
            };
        }

        private void UpdateSection(Dictionary<string, object> config, string section, object value)
        {
            var parts = section.Split(':', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length == 1)
            {
                config[parts[0]] = value;
                return;
            }
            
            if (!config.ContainsKey(parts[0]))
            {
                config[parts[0]] = new Dictionary<string, object>();
            }
            
            if (config[parts[0]] is Dictionary<string, object> dict)
            {
                UpdateSection(dict, string.Join(':', parts.Skip(1)), value);
            }
        }
    }
}
