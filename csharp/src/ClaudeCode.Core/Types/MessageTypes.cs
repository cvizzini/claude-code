namespace ClaudeCode.Core.Types;

public enum MessageRole { User, Assistant, System }

public record ContentBlock(string Type, string? Text = null);

public record Message(string Id, MessageRole Role, List<ContentBlock> Content, string Model = "");

public record ConversationMessage(MessageRole Role, string Content, DateTime Timestamp);
