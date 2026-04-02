namespace ClaudeCode.Constants;

public record OAuthConfig(
    string BaseApiUrl,
    string ConsoleAuthorizeUrl,
    string ClaudeAiAuthorizeUrl,
    string ClaudeAiOrigin,
    string TokenUrl,
    string ApiKeyUrl,
    string RolesUrl,
    string ConsoleSuccessUrl,
    string ClaudeAiSuccessUrl,
    string ManualRedirectUrl,
    string ClientId,
    string OAuthFileSuffix,
    string McpProxyUrl,
    string McpProxyPath
);

public static class OAuthConstants
{
    public const string ClaudeAiInferenceScope = "user:inference";
    public const string ClaudeAiProfileScope = "user:profile";
    public const string ConsoleScope = "org:create_api_key";
    public const string OAuthBetaHeader = "oauth-2025-04-20";
    public const string McpClientMetadataUrl = "https://claude.ai/oauth/claude-code-client-metadata";

    public static readonly string[] ConsoleOAuthScopes = [ConsoleScope, ClaudeAiProfileScope];

    public static readonly string[] ClaudeAiOAuthScopes =
    [
        ClaudeAiProfileScope,
        ClaudeAiInferenceScope,
        "user:sessions:claude_code",
        "user:mcp_servers",
        "user:file_upload",
    ];

    public static readonly string[] AllOAuthScopes =
        ConsoleOAuthScopes.Union(ClaudeAiOAuthScopes).Distinct().ToArray();

    public static readonly OAuthConfig ProdConfig = new(
        BaseApiUrl: "https://api.anthropic.com",
        ConsoleAuthorizeUrl: "https://platform.claude.com/oauth/authorize",
        ClaudeAiAuthorizeUrl: "https://claude.com/cai/oauth/authorize",
        ClaudeAiOrigin: "https://claude.ai",
        TokenUrl: "https://platform.claude.com/v1/oauth/token",
        ApiKeyUrl: "https://api.anthropic.com/api/oauth/claude_cli/create_api_key",
        RolesUrl: "https://api.anthropic.com/api/oauth/claude_cli/roles",
        ConsoleSuccessUrl: "https://platform.claude.com/buy_credits?returnUrl=/oauth/code/success%3Fapp%3Dclaude-code",
        ClaudeAiSuccessUrl: "https://platform.claude.com/oauth/code/success?app=claude-code",
        ManualRedirectUrl: "https://platform.claude.com/oauth/code/callback",
        ClientId: "9d1c250a-e61b-44d9-88ed-5944d1962f5e",
        OAuthFileSuffix: "",
        McpProxyUrl: "https://mcp-proxy.anthropic.com",
        McpProxyPath: "/v1/mcp/{server_id}"
    );

    private static readonly string[] AllowedOAuthBaseUrls =
    [
        "https://beacon.claude-ai.staging.ant.dev",
        "https://claude.fedstart.com",
        "https://claude-staging.fedstart.com",
    ];

    public static string GetFileSuffixForOAuthConfig()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CLAUDE_CODE_CUSTOM_OAUTH_URL")))
            return "-custom-oauth";

        var userType = Environment.GetEnvironmentVariable("USER_TYPE");
        if (userType == "ant")
        {
            if (IsEnvTruthy(Environment.GetEnvironmentVariable("USE_LOCAL_OAUTH")))
                return "-local-oauth";
            if (IsEnvTruthy(Environment.GetEnvironmentVariable("USE_STAGING_OAUTH")))
                return "-staging-oauth";
        }
        return "";
    }

    private static bool IsEnvTruthy(string? value)
        => value is "1" or "true" or "yes" or "on";
}
