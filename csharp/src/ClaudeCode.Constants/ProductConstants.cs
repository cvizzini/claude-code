namespace ClaudeCode.Constants;

public static class ProductConstants
{
    public const string ProductName = "Claude Code";
    public const string ProductVersion = "1.0.0";
    public const string DefaultModel = "gpt-4o";
    public const string DefaultSmallModel = "gpt-4o-mini";
    public const string CopilotApiKeyEnvVar = "GITHUB_TOKEN";
    public const string CopilotFallbackTokenEnvVar = "COPILOT_API_KEY";
    public const string CopilotModelEnvVar = "GITHUB_COPILOT_MODEL";
    public const string CopilotFallbackModelEnvVar = "COPILOT_MODEL";
    public const string ProviderEnvVar = "CLAUDE_CODE_PROVIDER";
    public const string CopilotProvider = "copilot";
    public const string ConfigDir = ".claude";
    public const string GlobalConfigFile = "claude.json";
    public const string ProjectConfigFile = ".claude.json";
}
