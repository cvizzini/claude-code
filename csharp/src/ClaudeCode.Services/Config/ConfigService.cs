using ClaudeCode.Core.Config;
using ClaudeCode.Constants;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ClaudeCode.Services.Config;

public interface IConfigService
{
    AppConfig LoadConfig();
    void SaveConfig(AppConfig config);
}

public class ConfigService : IConfigService
{
    private static readonly string[] UnsupportedLegacyModelPrefixes = ["claude-"];

    private readonly ILogger<ConfigService>? _logger;
    private AppConfig? _config;

    public ConfigService(ILogger<ConfigService>? logger) { _logger = logger; }

    public AppConfig LoadConfig()
    {
        if (_config != null) return _config;

        var config = new AppConfig
        {
            ApiKey = Environment.GetEnvironmentVariable(ProductConstants.CopilotApiKeyEnvVar)
                     ?? Environment.GetEnvironmentVariable(ProductConstants.CopilotFallbackTokenEnvVar),
            Model = Environment.GetEnvironmentVariable(ProductConstants.CopilotModelEnvVar)
                    ?? Environment.GetEnvironmentVariable(ProductConstants.CopilotFallbackModelEnvVar)
                    ?? AppConfig.DefaultModel,
            Provider = ProductConstants.CopilotProvider
        };

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
                    if (!string.IsNullOrWhiteSpace(saved.OAuthToken)) config.OAuthToken = saved.OAuthToken;
                    if (!string.IsNullOrWhiteSpace(saved.SystemPrompt)) config.SystemPrompt = saved.SystemPrompt;
                    if (saved.MaxTokens > 0) config.MaxTokens = saved.MaxTokens;
                    config.Verbose = saved.Verbose;
                    config.Debug = saved.Debug;
                    config.BypassPermissions = saved.BypassPermissions;
                    config.DefaultPermissionMode = saved.DefaultPermissionMode;
                    config.AdditionalDirectories = saved.AdditionalDirectories ?? [];

                    if (!string.IsNullOrWhiteSpace(saved.Model) && !LooksLikeUnsupportedLegacyModel(saved.Model))
                    {
                        config.Model = saved.Model;
                    }
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
        config.Provider = ProductConstants.CopilotProvider;
        if (LooksLikeUnsupportedLegacyModel(config.Model))
        {
            config.Model = ProductConstants.DefaultModel;
        }

        _config = config;
        var configPath = GetConfigFilePath();
        var dir = Path.GetDirectoryName(configPath)!;
        Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, json);
    }

    private static bool LooksLikeUnsupportedLegacyModel(string? model)
        => !string.IsNullOrWhiteSpace(model)
           && UnsupportedLegacyModelPrefixes.Any(prefix => model.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    private static string GetConfigFilePath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ProductConstants.ConfigDir, ProductConstants.GlobalConfigFile);
    }
}
