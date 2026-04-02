using ClaudeCode.Core.Types;

namespace ClaudeCode.Core.Tools;

public interface ITool
{
    string Name { get; }
    string Description { get; }
    bool IsEnabled { get; }
    bool IsReadonly { get; }
    Task<ToolResult> ExecuteAsync(Dictionary<string, object> input, CancellationToken cancellationToken = default);
    Task<PermissionResult> CheckPermissionAsync(Dictionary<string, object> input);
}

public record ToolResult(bool Success, string? Output = null, string? Error = null, List<ContentBlock>? ContentBlocks = null);

public abstract class ToolBase : ITool
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual bool IsEnabled => true;
    public virtual bool IsReadonly => false;

    public abstract Task<ToolResult> ExecuteAsync(Dictionary<string, object> input, CancellationToken cancellationToken = default);

    public virtual Task<PermissionResult> CheckPermissionAsync(Dictionary<string, object> input)
        => Task.FromResult(new PermissionResult(PermissionBehavior.Allow));

    protected string GetString(Dictionary<string, object> input, string key, string defaultValue = "")
        => input.TryGetValue(key, out var val) ? val?.ToString() ?? defaultValue : defaultValue;

    protected bool GetBool(Dictionary<string, object> input, string key, bool defaultValue = false)
        => input.TryGetValue(key, out var val) ? val is bool b ? b : defaultValue : defaultValue;
}
