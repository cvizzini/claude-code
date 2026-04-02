namespace ClaudeCode.Core.Types;

public record InlineGhostText(string Text, string FullCommand, int InsertPosition);

public enum VimMode { Insert, Normal }

public enum QueuePriority { Now, Next, Later }

public enum PromptInputMode { Bash, Prompt, OrphanedPermission, TaskNotification }

public record QueuedCommand(
    string Value,
    PromptInputMode Mode,
    QueuePriority? Priority = null,
    string? Uuid = null,
    bool SkipSlashCommands = false,
    bool BridgeOrigin = false,
    bool IsMeta = false,
    string? Origin = null,
    string? Workload = null,
    string? AgentId = null
);
