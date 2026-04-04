namespace ClaudeCode.Core.Types;

public enum MessageRole { User, Assistant, System, Tool }

public record ContentBlock(
    string Type,
    string? Text = null,
    string? Id = null,
    string? Name = null,
    Dictionary<string, object>? Input = null
);

public record Message(string Id, MessageRole Role, List<ContentBlock> Content, string Model = "");

public record ToolCall(string Id, string Name, Dictionary<string, object> Input);

public record ConversationMessage(
    MessageRole Role,
    string Content,
    DateTime Timestamp,
    string? ToolCallId = null,
    List<ToolCall>? ToolCalls = null
);
