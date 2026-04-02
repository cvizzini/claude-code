namespace ClaudeCode.Constants;

public static class Betas
{
    public const string ClaudeCode20250219 = "claude-code-20250219";
    public const string InterleavedThinking = "interleaved-thinking-2025-05-14";
    public const string Context1M = "context-1m-2025-08-07";
    public const string ContextManagement = "context-management-2025-06-27";
    public const string StructuredOutputs = "structured-outputs-2025-12-15";
    public const string WebSearch = "web-search-2025-03-05";
    public const string ToolSearchBeta1P = "advanced-tool-use-2025-11-20";
    public const string ToolSearchBeta3P = "tool-search-tool-2025-10-19";
    public const string Effort = "effort-2025-11-24";
    public const string TaskBudgets = "task-budgets-2026-03-13";
    public const string PromptCachingScope = "prompt-caching-scope-2026-01-05";
    public const string FastMode = "fast-mode-2026-02-01";
    public const string RedactThinking = "redact-thinking-2026-02-12";
    public const string TokenEfficientTools = "token-efficient-tools-2026-03-28";
    public const string Advisor = "advisor-tool-2026-03-01";

    public static readonly IReadOnlySet<string> BedrockExtraParamsHeaders = new HashSet<string>
    {
        InterleavedThinking,
        Context1M,
        ToolSearchBeta3P,
    };

    public static readonly IReadOnlySet<string> VertexCountTokensAllowedBetas = new HashSet<string>
    {
        ClaudeCode20250219,
        InterleavedThinking,
        ContextManagement,
    };
}
