namespace ClaudeCode.Constants;

public static class ProductConstants
{
    public const string ProductName = "Claude Code";
    public const string ProductVersion = "1.0.0";
    public const string AnthropicBaseUrl = "https://api.anthropic.com";
    public const string ClaudeAiBaseUrl = "https://claude.ai";
    public const string DefaultModel = "github-copilot/gpt-5.4";
    public const string DefaultSmallModel = "claude-haiku-4-5";
    public const string ApiKeyEnvVar = "ANTHROPIC_API_KEY";
    public const string CopilotApiKeyEnvVar = "GITHUB_TOKEN";
    public const string ProviderEnvVar = "CLAUDE_CODE_PROVIDER";
    public const string AnthropicProvider = "anthropic";
    public const string CopilotProvider = "copilot";
    public const string ConfigDir = ".claude";
    public const string GlobalConfigFile = "claude.json";
    public const string ProjectConfigFile = ".claude.json";
}
