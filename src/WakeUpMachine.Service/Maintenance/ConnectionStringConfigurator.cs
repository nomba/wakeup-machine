using System.Text.Json;
using System.Text.Json.Nodes;

namespace WakeUpMachine.Service.Maintenance;

internal class ConnectionStringConfigurator
{
    private readonly ILogger<ConnectionStringConfigurator> _logger;
    private readonly IConfiguration _configuration;

    public ConnectionStringConfigurator(ILogger<ConnectionStringConfigurator> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Configure(string connectionString, bool overwrite)
    {
        var appSettingsJsonPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

        var appSettingsJson = JsonNode.Parse(await File.ReadAllTextAsync(appSettingsJsonPath));
        if (appSettingsJson == null)
            throw new InvalidOperationException("Unable to parse appsettings.json.");

        // Check if connection string is put in mem configuration
        if (_configuration["ConnectionStrings:Default"] is not null)
        {
            _logger.LogWarning("Skip setting connection string. It is already put into memory configuration");
            return;
        }
            
        // Check if connection string is already set and skip if `overwrite` disabled
        if (!overwrite && appSettingsJson["ConnectionStrings"]?["Default"] is not null)
        {
            _logger.LogInformation("Skip setting connection string. It is already put into conf file. Overwrite option is disabled");
            return;
        }

        // Ensure connection strings section is created
        appSettingsJson["ConnectionStrings"] ??= new JsonObject();

        // Put connection string in Default nested section
        appSettingsJson["ConnectionStrings"]!["Default"] = connectionString;

        await File.WriteAllTextAsync(appSettingsJsonPath,
            appSettingsJson.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true,
                // Fix problem with "<>" chars, https://stackoverflow.com/a/58003397
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }));

        // Update already loaded configuration
        _configuration["ConnectionStrings:Default"] = connectionString;
        
        _logger.LogInformation("Connection string in {AppSettingsJsonPath} updated", appSettingsJsonPath);
    }
}