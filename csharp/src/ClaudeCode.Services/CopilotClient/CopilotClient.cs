using ClaudeCode.Core.Services;
using ClaudeCode.Core.Types;
using ClaudeCode.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClaudeCode.Services.CopilotClient;

public class CopilotClient : IAgentClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CopilotClient> _logger;
    private const string BaseUrl = "https://api.githubcopilot.com";

    public CopilotClient(HttpClient httpClient, ILogger<CopilotClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Message> CreateMessageAsync(CreateMessageRequest request, CancellationToken cancellationToken = default)
    {
        var payload = BuildPayload(request, stream: false);
        var json = JsonSerializer.Serialize(payload);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync($"{BaseUrl}/chat/completions", content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<OpenAiChatCompletionResponse>(responseJson);
        var choice = result?.Choices?.FirstOrDefault();
        var message = choice?.Message;

        var contentBlocks = new List<ContentBlock>();
        if (!string.IsNullOrEmpty(message?.Content))
        {
            contentBlocks.Add(new ContentBlock("text", message.Content));
        }

        if (message?.ToolCalls != null)
        {
            foreach (var toolCall in message.ToolCalls)
            {
                if (string.IsNullOrWhiteSpace(toolCall.Id) || string.IsNullOrWhiteSpace(toolCall.Function?.Name))
                {
                    continue;
                }

                contentBlocks.Add(new ContentBlock(
                    "tool_use",
                    Id: toolCall.Id,
                    Name: toolCall.Function.Name,
                    Input: DeserializeArguments(toolCall.Function.Arguments)));
            }
        }

        return new Message(
            result?.Id ?? string.Empty,
            MessageRole.Assistant,
            contentBlocks,
            result?.Model ?? string.Empty
        );
    }

    public async IAsyncEnumerable<MessageStreamEvent> StreamMessageAsync(
        CreateMessageRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var payload = BuildPayload(request, stream: true);
        var json = JsonSerializer.Serialize(payload);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/chat/completions") { Content = content };

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null || !line.StartsWith("data: ")) continue;

            var eventData = line[6..].Trim();
            if (eventData == "[DONE]") break;

            OpenAiStreamResponse? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<OpenAiStreamResponse>(eventData);
            }
            catch (JsonException ex)
            {
                _logger.LogDebug(ex, "Failed to parse Copilot stream event: {Data}", eventData);
                continue;
            }

            if (parsed?.Choices == null) continue;

            foreach (var choice in parsed.Choices)
            {
                if (!string.IsNullOrEmpty(choice.Delta?.Content))
                {
                    yield return new MessageStreamEvent("content_block_delta", choice.Delta.Content, null);
                }

                if (!string.IsNullOrEmpty(choice.FinishReason))
                {
                    yield return new MessageStreamEvent("message_stop", null, MapStopReason(choice.FinishReason));
                }
            }
        }
    }

    private static string MapStopReason(string finishReason) => finishReason switch
    {
        "stop" => "end_turn",
        "length" => "max_tokens",
        _ => "stop_sequence"
    };

    private static object BuildPayload(CreateMessageRequest request, bool stream)
    {
        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(request.System))
        {
            messages.Add(new { role = "system", content = request.System });
        }

        messages.AddRange(request.Messages.Select(MapMessage));

        return new
        {
            model = request.Model,
            max_tokens = request.MaxTokens,
            messages,
            tools = request.Tools?.Select(t => new
            {
                type = "function",
                function = new
                {
                    name = t.Name,
                    description = t.Description,
                    parameters = t.InputSchema
                }
            }),
            stream
        };
    }

    private static object MapMessage(ConversationMessage message)
    {
        if (message.Role == MessageRole.Assistant && message.ToolCalls?.Count > 0)
        {
            return new
            {
                role = "assistant",
                content = string.IsNullOrWhiteSpace(message.Content) ? null : message.Content,
                tool_calls = message.ToolCalls.Select(toolCall => new
                {
                    id = toolCall.Id,
                    type = "function",
                    function = new
                    {
                        name = toolCall.Name,
                        arguments = JsonSerializer.Serialize(toolCall.Input)
                    }
                })
            };
        }

        if (message.Role == MessageRole.Tool)
        {
            return new
            {
                role = "tool",
                content = message.Content,
                tool_call_id = message.ToolCallId
            };
        }

        return new
        {
            role = message.Role.ToString().ToLowerInvariant(),
            content = message.Content
        };
    }

    private static Dictionary<string, object> DeserializeArguments(string? arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return new Dictionary<string, object>();
        }

        using var document = JsonDocument.Parse(arguments);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, object>();
        }

        return document.RootElement.EnumerateObject()
            .ToDictionary(property => property.Name, property => ConvertJsonValue(property.Value));
    }

    private static object? ConvertJsonValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject().ToDictionary(property => property.Name, property => ConvertJsonValue(property.Value)),
        JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonValue).ToList(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt64(out var longValue)
            ? longValue
            : element.TryGetDouble(out var doubleValue)
                ? doubleValue
                : element.GetRawText(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null
    };

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var detail = string.IsNullOrWhiteSpace(body) ? string.Empty : $": {body.Trim()}";
        throw new HttpRequestException($"Copilot request failed with {(int)response.StatusCode} ({response.ReasonPhrase}){detail}", null, response.StatusCode);
    }

    private record OpenAiChatCompletionResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("model")] string? Model,
        [property: JsonPropertyName("choices")] List<OpenAiChoice>? Choices
    );

    private record OpenAiChoice(
        [property: JsonPropertyName("message")] OpenAiMessage? Message,
        [property: JsonPropertyName("finish_reason")] string? FinishReason
    );

    private record OpenAiMessage(
        [property: JsonPropertyName("role")] string? Role,
        [property: JsonPropertyName("content")] string? Content,
        [property: JsonPropertyName("tool_calls")] List<OpenAiToolCall>? ToolCalls
    );

    private record OpenAiToolCall(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("function")] OpenAiFunctionCall? Function
    );

    private record OpenAiFunctionCall(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("arguments")] string? Arguments
    );

    private record OpenAiStreamResponse(
        [property: JsonPropertyName("choices")] List<OpenAiStreamChoice>? Choices
    );

    private record OpenAiStreamChoice(
        [property: JsonPropertyName("delta")] OpenAiDelta? Delta,
        [property: JsonPropertyName("finish_reason")] string? FinishReason
    );

    private record OpenAiDelta(
        [property: JsonPropertyName("content")] string? Content
    );
}

public static class CopilotClientExtensions
{
    public static IServiceCollection AddCopilotClient(this IServiceCollection services, string accessToken)
    {
        services.AddHttpClient<IAgentClient, CopilotClient>(client =>
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Editor-Version", "vscode/1.85.1");
            client.DefaultRequestHeaders.Add("Copilot-Integration-Id", "vscode-chat");
        });
        return services;
    }
}
