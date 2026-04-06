using ClaudeCode.Constants;
using ClaudeCode.Core.Services;
using ClaudeCode.Core.Tools;
using ClaudeCode.Core.Types;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ClaudeCode.Services.QueryEngine;

public interface IQueryEngine
{
    IAsyncEnumerable<QueryEvent> QueryAsync(QueryRequest request, CancellationToken cancellationToken = default);
}

public record QueryRequest(
    string Prompt,
    string Model,
    string? SystemPrompt = null,
    List<ConversationMessage>? History = null,
    List<ITool>? Tools = null,
    int MaxTokens = 8096
);

public abstract record QueryEvent;
public record TextEvent(string Text) : QueryEvent;
public record ToolUseEvent(string Id, string ToolName, Dictionary<string, object> Input) : QueryEvent;
public record ToolResultEvent(string ToolUseId, ToolResult Result) : QueryEvent;
public record HistoryEvent(ConversationMessage Message) : QueryEvent;
public record FinalMessageEvent(Message Message) : QueryEvent;
public record ErrorEvent(string Error) : QueryEvent;

public class QueryEngine : IQueryEngine
{
    private const int MaxToolRounds = 8;
    private const string ToolUseInstruction = "When tools are available, act like an agent: inspect files, directories, and code before answering; use Bash for shell or system commands; fetch external content when useful; and complete the task instead of only narrating intent. For multi-step work, keep progress updated with TodoWrite. After using tools, always provide a concise final answer that states what you found or changed.";

    private readonly IAgentClient _client;
    private readonly ILogger<QueryEngine> _logger;

    public QueryEngine(IAgentClient client, ILogger<QueryEngine> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async IAsyncEnumerable<QueryEvent> QueryAsync(
        QueryRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = new List<ConversationMessage>(request.History ?? new List<ConversationMessage>());
        var userMessage = new ConversationMessage(MessageRole.User, request.Prompt, DateTime.UtcNow);
        messages.Add(userMessage);
        yield return new HistoryEvent(userMessage);
        var systemPrompt = BuildSystemPrompt(request.SystemPrompt, request.Tools);

        var tools = request.Tools?.Where(t => t.IsEnabled).ToList();
        if (tools == null || tools.Count == 0)
        {
            var assistantText = string.Empty;
            var apiRequest = new CreateMessageRequest(
                request.Model,
                request.MaxTokens,
                messages,
                systemPrompt,
                null,
                Stream: true
            );

            await foreach (var evt in _client.StreamMessageAsync(apiRequest, cancellationToken))
            {
                if (evt.Type == "content_block_delta" && evt.Delta != null)
                {
                    assistantText += evt.Delta;
                    yield return new TextEvent(evt.Delta);
                }
                else if (evt.StopReason is "end_turn" or "stop_sequence" or "max_tokens")
                {
                    break;
                }
            }

            if (!string.IsNullOrWhiteSpace(assistantText))
            {
                yield return new HistoryEvent(new ConversationMessage(MessageRole.Assistant, assistantText, DateTime.UtcNow));
            }

            yield break;
        }

        var toolDefinitions = tools.Select(CreateToolDefinition).ToList();
        var toolsByName = tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        var promptedToUseTools = false;
        var promptedForFinalAnswer = false;
        var executedToolCount = 0;

        for (var round = 0; round < MaxToolRounds; round++)
        {
            var apiRequest = new CreateMessageRequest(
                request.Model,
                request.MaxTokens,
                messages,
                systemPrompt,
                toolDefinitions,
                Stream: false
            );

            var response = await _client.CreateMessageAsync(apiRequest, cancellationToken);
            var assistantText = string.Concat(response.Content.Where(c => c.Type == "text").Select(c => c.Text));
            var toolUses = response.Content
                .Where(c => c.Type == "tool_use" && !string.IsNullOrWhiteSpace(c.Id) && !string.IsNullOrWhiteSpace(c.Name))
                .ToList();

            if (!string.IsNullOrEmpty(assistantText))
            {
                yield return new TextEvent(assistantText);
            }

            if (toolUses.Count == 0)
            {
                if (!promptedToUseTools && ShouldContinueWithTools(request.Prompt, assistantText))
                {
                    promptedToUseTools = true;
                    var assistantMessage = new ConversationMessage(MessageRole.Assistant, assistantText, DateTime.UtcNow);
                    messages.Add(assistantMessage);
                    yield return new HistoryEvent(assistantMessage);

                    var followUpInstruction = new ConversationMessage(
                        MessageRole.User,
                        "Use the available tools now to complete the request. Do not just describe your next step; inspect, edit, or create what is needed and then give the result.",
                        DateTime.UtcNow);
                    messages.Add(followUpInstruction);
                    yield return new HistoryEvent(followUpInstruction);
                    continue;
                }

                if (!promptedForFinalAnswer && ShouldPromptForFinalAnswer(executedToolCount, assistantText))
                {
                    promptedForFinalAnswer = true;

                    if (!string.IsNullOrWhiteSpace(assistantText))
                    {
                        var assistantMessage = new ConversationMessage(MessageRole.Assistant, assistantText, DateTime.UtcNow);
                        messages.Add(assistantMessage);
                        yield return new HistoryEvent(assistantMessage);
                    }

                    var finalAnswerInstruction = new ConversationMessage(
                        MessageRole.User,
                        "Using the completed tool results, provide the final answer now. Summarize what you found or changed and do not ask for another step unless something failed.",
                        DateTime.UtcNow);
                    messages.Add(finalAnswerInstruction);
                    yield return new HistoryEvent(finalAnswerInstruction);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(assistantText))
                {
                    yield return new HistoryEvent(new ConversationMessage(MessageRole.Assistant, assistantText, DateTime.UtcNow));
                }

                yield return new FinalMessageEvent(response);
                yield break;
            }

            var assistantToolMessage = new ConversationMessage(
                MessageRole.Assistant,
                assistantText,
                DateTime.UtcNow,
                ToolCalls: toolUses.Select(toolUse => new ToolCall(toolUse.Id!, toolUse.Name!, toolUse.Input ?? new Dictionary<string, object>())).ToList());
            messages.Add(assistantToolMessage);
            yield return new HistoryEvent(assistantToolMessage);

            foreach (var toolUse in toolUses)
            {
                var toolInput = toolUse.Input ?? new Dictionary<string, object>();
                yield return new ToolUseEvent(toolUse.Id!, toolUse.Name!, toolInput);

                var result = await ExecuteToolAsync(toolUse.Name!, toolInput, toolsByName, cancellationToken);
                executedToolCount++;
                yield return new ToolResultEvent(toolUse.Id!, result);

                var toolResultMessage = new ConversationMessage(
                    MessageRole.Tool,
                    FormatToolResult(result),
                    DateTime.UtcNow,
                    ToolCallId: toolUse.Id);
                messages.Add(toolResultMessage);
                yield return new HistoryEvent(toolResultMessage);
            }
        }

        yield return new ErrorEvent("Tool call limit exceeded.");
    }

    private async Task<ToolResult> ExecuteToolAsync(
        string toolName,
        Dictionary<string, object> input,
        IReadOnlyDictionary<string, ITool> toolsByName,
        CancellationToken cancellationToken)
    {
        if (!toolsByName.TryGetValue(toolName, out var tool))
        {
            return new ToolResult(false, Error: $"Unknown tool: {toolName}");
        }

        var permission = await tool.CheckPermissionAsync(input);
        if (permission.Behavior != PermissionBehavior.Allow)
        {
            return new ToolResult(false, Error: permission.Message ?? $"Permission denied for tool: {toolName}");
        }

        return await tool.ExecuteAsync(input, cancellationToken);
    }

    private static string FormatToolResult(ToolResult result)
    {
        if (result.Success)
        {
            if (!string.IsNullOrEmpty(result.Output))
            {
                return result.Output;
            }

            if (result.ContentBlocks?.Count > 0)
            {
                return JsonSerializer.Serialize(result.ContentBlocks);
            }

            return "Tool completed successfully.";
        }

        return result.Error ?? "Tool execution failed.";
    }

    private static ToolDefinition CreateToolDefinition(ITool tool)
        => new(tool.Name, tool.Description, GetInputSchema(tool.Name));

    private static string? BuildSystemPrompt(string? systemPrompt, List<ITool>? tools)
    {
        if (tools == null || tools.Count == 0)
        {
            return systemPrompt;
        }

        return string.IsNullOrWhiteSpace(systemPrompt)
            ? ToolUseInstruction
            : $"{systemPrompt}\n\n{ToolUseInstruction}";
    }

    private static bool ShouldContinueWithTools(string prompt, string assistantText)
    {
        if (string.IsNullOrWhiteSpace(assistantText))
        {
            return false;
        }

        var normalizedPrompt = prompt.ToLowerInvariant();
        var normalizedText = assistantText.Trim().ToLowerInvariant();

        var promptNeedsAction = normalizedPrompt.Contains("read ")
            || normalizedPrompt.Contains("write ")
            || normalizedPrompt.Contains("edit ")
            || normalizedPrompt.Contains("create ")
            || normalizedPrompt.Contains("update ")
            || normalizedPrompt.Contains("modify ")
            || normalizedPrompt.Contains("run ")
            || normalizedPrompt.Contains("execute ")
            || normalizedPrompt.Contains("command")
            || normalizedPrompt.Contains("bash")
            || normalizedPrompt.Contains("shell")
            || normalizedPrompt.Contains("terminal")
            || normalizedPrompt.Contains("ipconfig")
            || normalizedPrompt.Contains("git ")
            || normalizedPrompt.Contains("dotnet ")
            || normalizedPrompt.Contains("build ")
            || normalizedPrompt.Contains("test ")
            || normalizedPrompt.Contains("search ")
            || normalizedPrompt.Contains("find ")
            || normalizedPrompt.Contains("directory")
            || normalizedPrompt.Contains("folder")
            || normalizedPrompt.Contains("file")
            || normalizedPrompt.Contains("\\")
            || normalizedPrompt.Contains("/");

        var responseDefersWork = normalizedText.StartsWith("let me")
            || normalizedText.StartsWith("i'll")
            || normalizedText.StartsWith("i will")
            || normalizedText.StartsWith("first,")
            || normalizedText.StartsWith("first ")
            || normalizedText.StartsWith("i'm going to")
            || normalizedText.StartsWith("i need to");

        return promptNeedsAction && responseDefersWork;
    }

    private static bool ShouldPromptForFinalAnswer(int executedToolCount, string assistantText)
    {
        if (executedToolCount == 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(assistantText))
        {
            return true;
        }

        var normalizedText = assistantText.Trim().ToLowerInvariant();
        return normalizedText.StartsWith("let me")
            || normalizedText.StartsWith("i'll")
            || normalizedText.StartsWith("i will")
            || normalizedText.StartsWith("first ")
            || normalizedText.StartsWith("i'm going to")
            || normalizedText.StartsWith("i need to");
    }

    private static Dictionary<string, object> GetInputSchema(string toolName) => toolName switch
    {
        var name when name.Equals(ToolNames.ApplyPatch, StringComparison.OrdinalIgnoreCase) => new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["changes"] = new Dictionary<string, object>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["file_path"] = new Dictionary<string, object> { ["type"] = "string" },
                            ["old_str"] = new Dictionary<string, object> { ["type"] = "string" },
                            ["new_str"] = new Dictionary<string, object> { ["type"] = "string" },
                            ["replace_all"] = new Dictionary<string, object> { ["type"] = "boolean" },
                            ["create_if_missing"] = new Dictionary<string, object> { ["type"] = "boolean" }
                        },
                        ["required"] = new[] { "file_path", "old_str", "new_str" }
                    }
                }
            },
            ["required"] = new[] { "changes" }
        },
        var name when name.Equals(ToolNames.Bash, StringComparison.OrdinalIgnoreCase) => new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["command"] = new Dictionary<string, object> { ["type"] = "string" },
                ["timeout"] = new Dictionary<string, object> { ["type"] = "integer" }
            },
            ["required"] = new[] { "command" }
        },
        var name when name.Equals(ToolNames.ListDirectory, StringComparison.OrdinalIgnoreCase) => new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["path"] = new Dictionary<string, object> { ["type"] = "string" },
                ["recursive"] = new Dictionary<string, object> { ["type"] = "boolean" },
                ["max_depth"] = new Dictionary<string, object> { ["type"] = "integer" }
            },
            ["required"] = new[] { "path" }
        },
        var name when name.Equals(ToolNames.Glob, StringComparison.OrdinalIgnoreCase) => new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["pattern"] = new Dictionary<string, object> { ["type"] = "string" },
                ["path"] = new Dictionary<string, object> { ["type"] = "string" }
            },
            ["required"] = new[] { "pattern" }
        },
        var name when name.Equals(ToolNames.Grep, StringComparison.OrdinalIgnoreCase) => new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["pattern"] = new Dictionary<string, object> { ["type"] = "string" },
                ["path"] = new Dictionary<string, object> { ["type"] = "string" },
                ["include"] = new Dictionary<string, object> { ["type"] = "string" },
                ["ignore_case"] = new Dictionary<string, object> { ["type"] = "boolean" }
            },
            ["required"] = new[] { "pattern" }
        },
        var name when name.Equals(ToolNames.WebFetch, StringComparison.OrdinalIgnoreCase) => new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["url"] = new Dictionary<string, object> { ["type"] = "string" }
            },
            ["required"] = new[] { "url" }
        },
        var name when name.Equals(ToolNames.TodoWrite, StringComparison.OrdinalIgnoreCase) => new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["todos"] = new Dictionary<string, object>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["id"] = new Dictionary<string, object> { ["type"] = "string" },
                            ["content"] = new Dictionary<string, object> { ["type"] = "string" },
                            ["status"] = new Dictionary<string, object> { ["type"] = "string" },
                            ["priority"] = new Dictionary<string, object> { ["type"] = "string" }
                        },
                        ["required"] = new[] { "id", "content", "status", "priority" }
                    }
                }
            },
            ["required"] = new[] { "todos" }
        },
        var name when name.Equals(ToolNames.FileRead, StringComparison.OrdinalIgnoreCase) => new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["file_path"] = new Dictionary<string, object> { ["type"] = "string" },
                ["start_line"] = new Dictionary<string, object> { ["type"] = "integer" },
                ["end_line"] = new Dictionary<string, object> { ["type"] = "integer" }
            },
            ["required"] = new[] { "file_path" }
        },
        var name when name.Equals(ToolNames.FileWrite, StringComparison.OrdinalIgnoreCase) => new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["file_path"] = new Dictionary<string, object> { ["type"] = "string" },
                ["content"] = new Dictionary<string, object> { ["type"] = "string" }
            },
            ["required"] = new[] { "file_path", "content" }
        },
        var name when name.Equals(ToolNames.FileEdit, StringComparison.OrdinalIgnoreCase) => new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["file_path"] = new Dictionary<string, object> { ["type"] = "string" },
                ["old_str"] = new Dictionary<string, object> { ["type"] = "string" },
                ["new_str"] = new Dictionary<string, object> { ["type"] = "string" }
            },
            ["required"] = new[] { "file_path", "old_str", "new_str" }
        },
        _ => new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>()
        }
    };
}
