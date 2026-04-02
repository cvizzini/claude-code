using ClaudeCode.Core.Types;

namespace ClaudeCode.Core.Services;

public interface IAnthropicClient
{
    Task<Message> CreateMessageAsync(CreateMessageRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<MessageStreamEvent> StreamMessageAsync(CreateMessageRequest request, CancellationToken cancellationToken = default);
}

public record CreateMessageRequest(
    string Model,
    int MaxTokens,
    List<ConversationMessage> Messages,
    string? System = null,
    List<ToolDefinition>? Tools = null,
    bool Stream = false
);

public record ToolDefinition(string Name, string Description, Dictionary<string, object> InputSchema);

public record MessageStreamEvent(string Type, string? Delta = null, string? StopReason = null);
