namespace ClaudeCode.Core.Types;

public enum HookOutcome { Success, Blocking, NonBlockingError, Cancelled }

public record HookBlockingError(string BlockingErrorMessage, string Command);

public record HookProgress(
    string HookEvent,
    string HookName,
    string Command,
    string? PromptText = null,
    string? StatusMessage = null
)
{
    public string Type => "hook_progress";
}

public record PromptRequestOption(string Key, string Label, string? Description = null);

public record PromptRequest(string PromptId, string MessageText, PromptRequestOption[] Options);

public record PromptResponse(string PromptResponseId, string Selected);

public record SyncHookResponse(
    bool? Continue = null,
    bool? SuppressOutput = null,
    string? StopReason = null,
    string? Decision = null,
    string? Reason = null,
    string? SystemMessage = null
);

public record HookResult(
    HookOutcome Outcome,
    string? StopReason = null,
    string? PermissionBehavior = null,
    string? HookPermissionDecisionReason = null,
    string? AdditionalContext = null,
    string? InitialUserMessage = null,
    Dictionary<string, object>? UpdatedInput = null,
    bool PreventContinuation = false,
    bool Retry = false
);

public record AggregatedHookResult(
    HookOutcome Outcome,
    HookBlockingError[]? BlockingErrors = null,
    bool PreventContinuation = false,
    string? StopReason = null,
    string? HookPermissionDecisionReason = null,
    string? PermissionBehavior = null,
    string[]? AdditionalContexts = null,
    string? InitialUserMessage = null,
    Dictionary<string, object>? UpdatedInput = null,
    bool Retry = false
);
