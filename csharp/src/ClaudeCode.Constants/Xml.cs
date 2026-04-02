namespace ClaudeCode.Constants;

public static class XmlTags
{
    public const string CommandName = "command-name";
    public const string CommandMessage = "command-message";
    public const string CommandArgs = "command-args";

    public const string BashInput = "bash-input";
    public const string BashStdout = "bash-stdout";
    public const string BashStderr = "bash-stderr";
    public const string LocalCommandStdout = "local-command-stdout";
    public const string LocalCommandStderr = "local-command-stderr";
    public const string LocalCommandCaveat = "local-command-caveat";

    public static readonly string[] TerminalOutputTags =
    [
        BashInput, BashStdout, BashStderr,
        LocalCommandStdout, LocalCommandStderr, LocalCommandCaveat,
    ];

    public const string Tick = "tick";

    public const string TaskNotification = "task-notification";
    public const string TaskId = "task-id";
    public const string ToolUseId = "tool-use-id";
    public const string TaskType = "task-type";
    public const string OutputFile = "output-file";
    public const string Status = "status";
    public const string Summary = "summary";
    public const string Reason = "reason";
    public const string Worktree = "worktree";
    public const string WorktreePath = "worktreePath";
    public const string WorktreeBranch = "worktreeBranch";

    public const string Ultraplan = "ultraplan";
    public const string RemoteReview = "remote-review";
    public const string RemoteReviewProgress = "remote-review-progress";
    public const string TeammateMessage = "teammate-message";
    public const string ChannelMessage = "channel-message";
    public const string Channel = "channel";
    public const string CrossSessionMessage = "cross-session-message";
    public const string ForkBoilerplate = "fork-boilerplate";
    public const string ForkDirectivePrefix = "Your directive: ";

    public static readonly string[] CommonHelpArgs = ["help", "-h", "--help"];

    public static readonly string[] CommonInfoArgs =
    [
        "list", "show", "display", "current", "view", "get",
        "check", "describe", "print", "version", "about", "status", "?",
    ];
}
