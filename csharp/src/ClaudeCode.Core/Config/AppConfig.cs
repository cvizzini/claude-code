using ClaudeCode.Core.Types;

namespace ClaudeCode.Core.Config;

public class AppConfig
{
    // Defined here because ClaudeCode.Core cannot depend on ClaudeCode.Constants
    public const string DefaultModel = "claude-opus-4-5";

    public string? ApiKey { get; set; }
    public string Model { get; set; } = DefaultModel;
    public string? OAuthToken { get; set; }
    public bool BypassPermissions { get; set; }
    public PermissionMode DefaultPermissionMode { get; set; } = PermissionMode.Default;
    public List<string> AdditionalDirectories { get; set; } = new();
    public bool Verbose { get; set; }
    public bool Debug { get; set; }
    public string? SystemPrompt { get; set; }
    public int MaxTokens { get; set; } = 8096;
}
