using ClaudeCode.Core.Tools;
using ClaudeCode.Core.Types;
using ClaudeCode.Constants;
using Microsoft.Extensions.Logging;

namespace ClaudeCode.Tools.WebFetch;

public class WebFetchTool : ToolBase
{
    private readonly ILogger<WebFetchTool> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public WebFetchTool(ILogger<WebFetchTool> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public override string Name => ToolNames.WebFetch;
    public override string Description => "Fetch content from a URL. Returns the page content as text.";
    public override bool IsReadonly => true;

    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> input, CancellationToken cancellationToken = default)
    {
        var url = GetString(input, "url");
        if (string.IsNullOrEmpty(url))
            return new ToolResult(false, Error: "No URL provided");

        try
        {
            using var httpClient = _httpClientFactory.CreateClient("WebFetch");
            using var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (bytes.Length > ToolLimits.WebFetchMaxBytes)
                return new ToolResult(false, Error: $"Response too large: {bytes.Length} bytes (max {ToolLimits.WebFetchMaxBytes})");

            var content = System.Text.Encoding.UTF8.GetString(bytes);
            return new ToolResult(true, Output: content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching URL: {Url}", url);
            return new ToolResult(false, Error: ex.Message);
        }
    }
}
