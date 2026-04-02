namespace ClaudeCode.Constants;

public static class PromptConstants
{
    public const string ClaudeCodeDocsMapUrl = "https://code.claude.com/docs/en/claude_code_docs_map.md";
    public const string SystemPromptDynamicBoundary = "__SYSTEM_PROMPT_DYNAMIC_BOUNDARY__";

    public static string[] PrependBullets(IEnumerable<string> items)
        => items.Select(item => $" - {item}").ToArray();
}
