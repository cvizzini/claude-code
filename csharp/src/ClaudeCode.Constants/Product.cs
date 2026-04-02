namespace ClaudeCode.Constants;

public static class Product
{
    public const string ProductUrl = "https://claude.com/claude-code";
    public const string ClaudeAiBaseUrl = "https://claude.ai";
    public const string ClaudeAiStagingBaseUrl = "https://claude-ai.staging.ant.dev";
    public const string ClaudeAiLocalBaseUrl = "http://localhost:4000";

    public static bool IsRemoteSessionStaging(string? sessionId = null, string? ingressUrl = null)
        => sessionId?.Contains("_staging_") == true || ingressUrl?.Contains("staging") == true;

    public static bool IsRemoteSessionLocal(string? sessionId = null, string? ingressUrl = null)
        => sessionId?.Contains("_local_") == true || ingressUrl?.Contains("localhost") == true;

    public static string GetClaudeAiBaseUrl(string? sessionId = null, string? ingressUrl = null)
    {
        if (IsRemoteSessionLocal(sessionId, ingressUrl)) return ClaudeAiLocalBaseUrl;
        if (IsRemoteSessionStaging(sessionId, ingressUrl)) return ClaudeAiStagingBaseUrl;
        return ClaudeAiBaseUrl;
    }

    public static string GetRemoteSessionUrl(string sessionId, string? ingressUrl = null)
    {
        var baseUrl = GetClaudeAiBaseUrl(sessionId, ingressUrl);
        return $"{baseUrl}/code/{sessionId}";
    }
}
