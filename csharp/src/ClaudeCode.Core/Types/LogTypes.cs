namespace ClaudeCode.Core.Types;

public record LogOption(
    string Date,
    string FirstPrompt,
    int MessageCount,
    int Value,
    DateTime Created,
    DateTime Modified,
    bool IsSidechain,
    string? FullPath = null,
    long? FileSize = null,
    bool IsLite = false,
    string? SessionId = null,
    string? TeamName = null,
    string? AgentName = null,
    string? AgentColor = null,
    string? AgentSetting = null,
    bool IsTeammate = false,
    string? Summary = null,
    string? CustomTitle = null,
    string? Tag = null,
    string? GitBranch = null,
    string? ProjectPath = null,
    int? PrNumber = null,
    string? PrUrl = null,
    string? PrRepository = null,
    string? Mode = null
);

public record SummaryMessage(string LeafUuid, string SummaryText)
{
    public string Type => "summary";
}

public record CustomTitleMessage(string SessionId, string CustomTitle)
{
    public string Type => "custom-title";
}

public record AiTitleMessage(string SessionId, string AiTitle)
{
    public string Type => "ai-title";
}

public record LastPromptMessage(string SessionId, string LastPrompt)
{
    public string Type => "last-prompt";
}

public record TaskSummaryMessage(string SessionId, string SummaryText, string Timestamp)
{
    public string Type => "task-summary";
}

public record TagMessage(string SessionId, string Tag)
{
    public string Type => "tag";
}
