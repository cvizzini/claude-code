using ClaudeCode.Core.Config;
using ClaudeCode.Constants;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ClaudeCode.Services.Config;

public interface IConfigService
{
    AppConfig LoadConfig();
    void SaveConfig(AppConfig config);
    string GetApiKey();
}

public class ConfigService : IConfigService
{
    private readonly ILogger<ConfigService>? _logger;
    private AppConfig? _config;

    public ConfigService(ILogger<ConfigService>? logger) { _logger = logger; }

    public AppConfig LoadConfig()
    {
        if (_config != null) return _config;

        var config = new AppConfig();

        config.ApiKey = Environment.GetEnvironmentVariable(ProductConstants.ApiKeyEnvVar);
        config.Model = Environment.GetEnvironmentVariable("ANTHROPIC_MODEL") ?? config.Model;

        var configPath = GetConfigFilePath();
        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                var saved = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (saved != null)
                {
                    config.ApiKey ??= saved.ApiKey;
                    if (!string.IsNullOrEmpty(saved.Model)) config.Model = saved.Model;
                    config.OAuthToken ??= saved.OAuthToken;
                    config.MaxTokens = saved.MaxTokens;
                    config.SystemPrompt = saved.SystemPrompt;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load config file: {Path}", configPath);
            }
        }

        _config = config;
        return config;
    }

    public void SaveConfig(AppConfig config)
    {
        _config = config;
        var configPath = GetConfigFilePath();
        var dir = Path.GetDirectoryName(configPath)!;
        Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, json);
    }

    public string GetApiKey()
    {
        var config = LoadConfig();
        return config.ApiKey
            ?? config.OAuthToken
            ?? throw new InvalidOperationException(
                $"No API key configured. Set the {ProductConstants.ApiKeyEnvVar} environment variable.");
    }

    private static string GetConfigFilePath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ProductConstants.ConfigDir, ProductConstants.GlobalConfigFile);
    }
}
