using ClaudeCode.Core.Services;
using ClaudeCode.Core.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClaudeCode.Services.AnthropicClient;

public class AnthropicClient : IAnthropicClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AnthropicClient> _logger;
    private const string BaseUrl = "https://api.anthropic.com";

    public AnthropicClient(HttpClient httpClient, ILogger<AnthropicClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Message> CreateMessageAsync(CreateMessageRequest request, CancellationToken cancellationToken = default)
    {
        var payload = BuildPayload(request, stream: false);
        var json = JsonSerializer.Serialize(payload);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync($"{BaseUrl}/v1/messages", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<AnthropicMessageResponse>(responseJson);

        return new Message(
            result?.Id ?? "",
            MessageRole.Assistant,
            result?.Content?.Select(c => new ContentBlock(c.Type, c.Text)).ToList() ?? new List<ContentBlock>()
        );
    }

    public async IAsyncEnumerable<MessageStreamEvent> StreamMessageAsync(
        CreateMessageRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var payload = BuildPayload(request, stream: true);
        var json = JsonSerializer.Serialize(payload);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v1/messages") { Content = content };

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        
        Console.WriteLine(httpRequest);
        Console.WriteLine(response);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break;

            if (!line.StartsWith("data: ")) continue;

            var eventData = line[6..];
            if (eventData == "[DONE]") break;

            MessageStreamEvent? evt = null;
            try
            {
                var parsed = JsonSerializer.Deserialize<StreamEventData>(eventData);
                if (parsed != null)
                    evt = new MessageStreamEvent(parsed.Type, parsed.Delta?.Text, parsed.StopReason);
            }
            catch (JsonException ex)
            {
                _logger.LogDebug(ex, "Failed to parse stream event: {Data}", eventData);
            }

            if (evt != null)
                yield return evt;
        }
    }

    private static object BuildPayload(CreateMessageRequest request, bool stream)
    {
        return new
        {
            model = request.Model,
            max_tokens = request.MaxTokens,
            messages = request.Messages.Select(m => new
            {
                role = m.Role.ToString().ToLowerInvariant(),
                content = m.Content
            }),
            system = request.System,
            tools = request.Tools?.Select(t => new
            {
                name = t.Name,
                description = t.Description,
                input_schema = t.InputSchema
            }),
            stream
        };
    }

    private record AnthropicMessageResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("content")] List<ContentBlockResponse>? Content
    );

    private record ContentBlockResponse(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string? Text
    );

    private record StreamEventData(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("delta")] DeltaData? Delta,
        [property: JsonPropertyName("stop_reason")] string? StopReason
    );

    private record DeltaData(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string? Text
    );
}

public static class AnthropicClientExtensions
{
    public static IServiceCollection AddAnthropicClient(this IServiceCollection services, string apiKey)
    {
        services.AddHttpClient<IAnthropicClient, AnthropicClient>(client =>
        {
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        });
        return services;
    }
}
