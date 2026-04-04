using ClaudeCode.Constants;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClaudeCode.Services.CopilotClient;

public sealed class CopilotAuthService
{
    private const string GitHubDeviceCodeUrl = "https://github.com/login/device/code";
    private const string GitHubAccessTokenUrl = "https://github.com/login/oauth/access_token";
    private const string CopilotTokenUrl = "https://api.github.com/copilot_internal/v2/token";
    private const string ClientId = "Iv1.b507a08c87ecfe98";

    private readonly HttpClient _httpClient;
    private readonly ILogger<CopilotAuthService> _logger;

    public CopilotAuthService(HttpClient httpClient, ILogger<CopilotAuthService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CopilotAuthenticationResult> CreateSessionAsync(string? githubToken, CancellationToken cancellationToken = default)
    {
        var isInteractiveSignIn = string.IsNullOrWhiteSpace(githubToken);
        var resolvedGitHubToken = githubToken;

        if (isInteractiveSignIn)
        {
            resolvedGitHubToken = await SignInWithDeviceFlowAsync(cancellationToken);
        }

        ValidateGitHubToken(resolvedGitHubToken!);
        var copilotToken = await ExchangeForCopilotTokenAsync(resolvedGitHubToken!, cancellationToken);

        return new CopilotAuthenticationResult(resolvedGitHubToken!, copilotToken, isInteractiveSignIn);
    }

    private async Task<string> SignInWithDeviceFlowAsync(CancellationToken cancellationToken)
    {
        using var deviceCodeRequest = new HttpRequestMessage(HttpMethod.Post, GitHubDeviceCodeUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["scope"] = "read:user"
            })
        };
        deviceCodeRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var deviceCodeResponse = await _httpClient.SendAsync(deviceCodeRequest, cancellationToken);
        await EnsureSuccessAsync(deviceCodeResponse, cancellationToken, "GitHub device authorization");

        var deviceCodePayload = await deviceCodeResponse.Content.ReadAsStringAsync(cancellationToken);
        var deviceCode = JsonSerializer.Deserialize<DeviceCodeResponse>(deviceCodePayload);

        if (deviceCode == null || string.IsNullOrWhiteSpace(deviceCode.DeviceCode) || string.IsNullOrWhiteSpace(deviceCode.UserCode))
        {
            throw new InvalidOperationException("GitHub device authorization returned an invalid response.");
        }

        Console.WriteLine();
        Console.WriteLine("GitHub Copilot sign-in required.");
        Console.WriteLine($"1. Open {deviceCode.VerificationUri}");
        Console.WriteLine($"2. Enter code: {deviceCode.UserCode}");
        Console.WriteLine();

        var pollDelay = TimeSpan.FromSeconds(deviceCode.Interval <= 0 ? 5 : deviceCode.Interval);
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(deviceCode.ExpiresIn <= 0 ? 900 : deviceCode.ExpiresIn);

        while (DateTimeOffset.UtcNow < expiresAt)
        {
            await Task.Delay(pollDelay, cancellationToken);

            using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, GitHubAccessTokenUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = ClientId,
                    ["device_code"] = deviceCode.DeviceCode,
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
                })
            };
            tokenRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var tokenResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken);
            await EnsureSuccessAsync(tokenResponse, cancellationToken, "GitHub OAuth token");

            var tokenPayload = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            var token = JsonSerializer.Deserialize<DeviceTokenResponse>(tokenPayload);

            if (!string.IsNullOrWhiteSpace(token?.AccessToken))
            {
                return token.AccessToken;
            }

            switch (token?.Error)
            {
                case "authorization_pending":
                    _logger.LogDebug("Waiting for GitHub device authorization approval.");
                    continue;
                case "slow_down":
                    pollDelay += TimeSpan.FromSeconds(5);
                    continue;
                case "access_denied":
                    throw new InvalidOperationException("GitHub Copilot sign-in was denied.");
                case "expired_token":
                    throw new InvalidOperationException("GitHub device authorization expired before sign-in completed.");
                default:
                    if (!string.IsNullOrWhiteSpace(token?.Error))
                    {
                        throw new InvalidOperationException($"GitHub OAuth token request failed: {token.Error}.");
                    }
                    break;
            }
        }

        throw new TimeoutException("GitHub device authorization expired before sign-in completed.");
    }

    private async Task<string> ExchangeForCopilotTokenAsync(string githubToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, CopilotTokenUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.ParseAdd($"ClaudeCode/{ProductConstants.ProductVersion}");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken, "GitHub Copilot token exchange");

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var token = JsonSerializer.Deserialize<CopilotTokenResponse>(payload);

        if (string.IsNullOrWhiteSpace(token?.Token))
        {
            throw new InvalidOperationException("GitHub Copilot token exchange returned an invalid response.");
        }

        return token.Token;
    }

    private static void ValidateGitHubToken(string githubToken)
    {
        if (LooksLikePersonalAccessToken(githubToken))
        {
            throw new InvalidOperationException(
                "GitHub Copilot does not accept Personal Access Tokens. Remove the PAT from GITHUB_TOKEN/COPILOT_API_KEY and sign in with GitHub OAuth instead.");
        }
    }

    private static bool LooksLikePersonalAccessToken(string token)
        => token.StartsWith("github_pat_", StringComparison.OrdinalIgnoreCase)
           || token.StartsWith("ghp_", StringComparison.OrdinalIgnoreCase);

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken, string operation)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var detail = string.IsNullOrWhiteSpace(body) ? string.Empty : $": {body.Trim()}";
        throw new HttpRequestException($"{operation} failed with {(int)response.StatusCode} ({response.ReasonPhrase}){detail}", null, response.StatusCode);
    }

    private sealed record DeviceCodeResponse(
        [property: JsonPropertyName("device_code")] string DeviceCode,
        [property: JsonPropertyName("user_code")] string UserCode,
        [property: JsonPropertyName("verification_uri")] string VerificationUri,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("interval")] int Interval
    );

    private sealed record DeviceTokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("error")] string? Error
    );

    private sealed record CopilotTokenResponse(
        [property: JsonPropertyName("token")] string? Token
    );
}

public sealed record CopilotAuthenticationResult(string GitHubToken, string CopilotToken, bool IsInteractiveSignIn);
