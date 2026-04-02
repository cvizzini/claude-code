using ClaudeCode.Core.Services;
using ClaudeCode.Core.Tools;
using ClaudeCode.Core.Types;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

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
public record FinalMessageEvent(Message Message) : QueryEvent;
public record ErrorEvent(string Error) : QueryEvent;

public class QueryEngine : IQueryEngine
{
    private readonly IAnthropicClient _client;
    private readonly ILogger<QueryEngine> _logger;

    public QueryEngine(IAnthropicClient client, ILogger<QueryEngine> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async IAsyncEnumerable<QueryEvent> QueryAsync(
        QueryRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = new List<ConversationMessage>(request.History ?? new List<ConversationMessage>());
        messages.Add(new ConversationMessage(MessageRole.User, request.Prompt, DateTime.UtcNow));

        var toolDefs = request.Tools?.Select(t => new ToolDefinition(
            t.Name,
            t.Description,
            new Dictionary<string, object> { ["type"] = "object", ["properties"] = new Dictionary<string, object>() }
        )).ToList();

        var apiRequest = new CreateMessageRequest(
            request.Model,
            request.MaxTokens,
            messages,
            request.SystemPrompt,
            toolDefs,
            Stream: true
        );

        await foreach (var evt in _client.StreamMessageAsync(apiRequest, cancellationToken))
        {
            if (evt.Type == "content_block_delta" && evt.Delta != null)
            {
                yield return new TextEvent(evt.Delta);
            }
            else if (evt.StopReason is "end_turn" or "stop_sequence" or "max_tokens")
            {
                break;
            }
        }
    }
}
