namespace ClaudeCode.Constants;

public static class SystemConstants
{
    public const string DefaultPrefix = "You are Claude Code, Anthropic's official CLI for Claude.";
    public const string AgentSdkClaudeCodePresetPrefix = "You are Claude Code, Anthropic's official CLI for Claude, running within the Claude Agent SDK.";
    public const string AgentSdkPrefix = "You are a Claude agent, built on Anthropic's Claude Agent SDK.";

    public static readonly IReadOnlySet<string> CliSyspromptPrefixes = new HashSet<string>
    {
        DefaultPrefix,
        AgentSdkClaudeCodePresetPrefix,
        AgentSdkPrefix,
    };

    public static string GetAttributionHeader(string fingerprint, string version)
    {
        var entrypoint = Environment.GetEnvironmentVariable("CLAUDE_CODE_ENTRYPOINT") ?? "unknown";
        return $"x-anthropic-billing-header: cc_version={version}.{fingerprint}; cc_entrypoint={entrypoint};";
    }
}
