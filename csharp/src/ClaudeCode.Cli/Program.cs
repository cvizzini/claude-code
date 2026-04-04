using ClaudeCode.Commands;
using ClaudeCode.Constants;
using ClaudeCode.Core.Config;
using ClaudeCode.Core.Services;
using ClaudeCode.Services.AnthropicClient;
using ClaudeCode.Services.Config;
using ClaudeCode.Services.CopilotClient;
using ClaudeCode.Services.QueryEngine;
using ClaudeCode.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Invocation;

var apiKeyOption = new Option<string?>("--api-key", "Provider API key (overrides provider env var)");
var providerOption = new Option<string?>("--provider", "AI provider: anthropic or copilot");
var modelOption = new Option<string?>("--model", "Model to use");
var verboseOption = new Option<bool>(new[] { "--verbose", "-v" }, "Enable verbose output");
var debugOption = new Option<bool>("--debug", "Enable debug output");
var printOption = new Option<bool>(new[] { "-p", "--print" }, "Non-interactive mode: print response and exit");
var systemPromptOption = new Option<string?>("--system-prompt", "System prompt to use");
var promptArgument = new Argument<string?>("prompt", () => null, "Initial prompt (optional)");

var rootCommand = new RootCommand("Claude Code - AI coding assistant powered by Claude");
rootCommand.AddGlobalOption(apiKeyOption);
rootCommand.AddGlobalOption(providerOption);
rootCommand.AddGlobalOption(modelOption);
rootCommand.AddGlobalOption(verboseOption);
rootCommand.AddGlobalOption(debugOption);
rootCommand.AddArgument(promptArgument);
rootCommand.AddOption(printOption);
rootCommand.AddOption(systemPromptOption);

rootCommand.SetHandler(async (InvocationContext ctx) =>
{
    var apiKey = ctx.ParseResult.GetValueForOption(apiKeyOption);
    var provider = ctx.ParseResult.GetValueForOption(providerOption);
    var model = ctx.ParseResult.GetValueForOption(modelOption);
    var verbose = ctx.ParseResult.GetValueForOption(verboseOption);
    var debug = ctx.ParseResult.GetValueForOption(debugOption);
    var prompt = ctx.ParseResult.GetValueForArgument(promptArgument);
    var print = ctx.ParseResult.GetValueForOption(printOption);

    var services = await BuildServicesAsync(apiKey, provider, model, verbose, debug, ctx.GetCancellationToken());
    var chat = services.GetRequiredService<ChatCommand>();

    if (prompt != null || print)
    {
        var actualPrompt = prompt ?? ReadStdinPrompt() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(actualPrompt))
            await chat.RunNonInteractiveAsync(actualPrompt, ctx.GetCancellationToken());
        else
            AnsiConsole.MarkupLine("[yellow]No prompt provided.[/]");
    }
    else
    {
        await chat.RunInteractiveAsync(ctx.GetCancellationToken());
    }
});

// Doctor command
var doctorCommand = new Command("doctor", "Check Claude Code configuration and connectivity");
doctorCommand.SetHandler(async (InvocationContext ctx) =>
{
    var provider = ctx.ParseResult.GetValueForOption(providerOption);
    var configService = new ConfigService(null);
    await RunDoctorAsync(configService, provider);
});
rootCommand.AddCommand(doctorCommand);

// Config command
var configCommand = new Command("config", "Manage Claude Code configuration");
var configListCmd = new Command("list", "List current configuration values");
configListCmd.SetHandler((InvocationContext ctx) =>
{
    var configService = new ConfigService(null);
    var config = configService.LoadConfig();

    var table = new Table().AddColumn("Setting").AddColumn("Value");
    table.AddRow("Provider", config.Provider);
    table.AddRow("Model", config.Model);
    table.AddRow("Max Tokens", config.MaxTokens.ToString());
    table.AddRow("API Key", config.ApiKey != null ? "***" + (config.ApiKey.Length > 4 ? config.ApiKey[^4..] : "****") : "(not set)");
    table.AddRow("OAuth Token", config.OAuthToken != null ? "(set)" : "(not set)");
    table.AddRow("Verbose", config.Verbose.ToString());
    table.AddRow("Debug", config.Debug.ToString());
    AnsiConsole.Write(table);
});
configCommand.AddCommand(configListCmd);
rootCommand.AddCommand(configCommand);

return await rootCommand.InvokeAsync(args);

static async Task<IServiceProvider> BuildServicesAsync(string? apiKey, string? provider, string? model, bool verbose, bool debug, CancellationToken cancellationToken)
{
    var bootstrapConfig = new ConfigService(null).LoadConfig();
    var services = CreateBaseServices(verbose, debug);

    var resolvedProvider = ResolveProvider(provider, bootstrapConfig.Provider);
    var resolvedModel = ResolveModel(model, resolvedProvider, bootstrapConfig);

    if (resolvedProvider == ProductConstants.CopilotProvider)
    {
        using var bootstrapProvider = services.BuildServiceProvider();
        var accessToken = await ResolveCopilotAccessTokenAsync(apiKey, bootstrapConfig, bootstrapProvider, cancellationToken);
        services.AddCopilotClient(accessToken);
    }
    else
    {
        var resolvedApiKey = ResolveAnthropicApiKey(apiKey, bootstrapConfig.ApiKey);
        if (!string.IsNullOrEmpty(resolvedApiKey))
            services.AddAnthropicClient(resolvedApiKey);
        else
            services.AddHttpClient<IAnthropicClient, ClaudeCode.Services.AnthropicClient.AnthropicClient>();
    }

    services.AddSingleton<IQueryEngine, QueryEngine>();
    services.AddClaudeTools();
    services.AddTransient<ChatCommand>();

    var serviceProvider = services.BuildServiceProvider();
    ApplyRuntimeConfig(serviceProvider, resolvedProvider, resolvedModel, verbose, debug);
    serviceProvider.PopulateToolRegistry();
    return serviceProvider;
}

static ServiceCollection CreateBaseServices(bool verbose, bool debug)
{
    var services = new ServiceCollection();

    var logLevel = debug ? LogLevel.Debug : verbose ? LogLevel.Information : LogLevel.Warning;
    services.AddLogging(builder => builder
        .SetMinimumLevel(logLevel)
        .AddConsole());

    services.AddHttpClient();
    services.AddHttpClient<CopilotAuthService>();
    services.AddSingleton<IConfigService>(sp => new ConfigService(sp.GetService<ILogger<ConfigService>>()));

    return services;
}

static string ResolveProvider(string? cliProvider, string? configuredProvider)
{
    var envProvider = Environment.GetEnvironmentVariable(ProductConstants.ProviderEnvVar);
    var value = (cliProvider ?? envProvider ?? configuredProvider ?? ProductConstants.AnthropicProvider).Trim().ToLowerInvariant();

    return value == ProductConstants.CopilotProvider
        ? ProductConstants.CopilotProvider
        : ProductConstants.AnthropicProvider;
}

static string ResolveModel(string? cliModel, string provider, AppConfig configuredConfig)
{
    if (!string.IsNullOrWhiteSpace(cliModel))
    {
        return cliModel;
    }

    var providerMatchesConfigured = string.Equals(provider, configuredConfig.Provider, StringComparison.OrdinalIgnoreCase);
    if (providerMatchesConfigured && !string.IsNullOrWhiteSpace(configuredConfig.Model))
    {
        return configuredConfig.Model;
    }

    return provider == ProductConstants.CopilotProvider
        ? ProductConstants.DefaultModel
        : AppConfig.DefaultModel;
}

static void ApplyRuntimeConfig(IServiceProvider serviceProvider, string provider, string model, bool verbose, bool debug)
{
    var configService = serviceProvider.GetRequiredService<IConfigService>();
    var config = configService.LoadConfig();
    config.Provider = provider;
    config.Model = model;
    config.Verbose = verbose;
    config.Debug = debug;
}

static string ResolveAnthropicApiKey(string? cliApiKey, string? configuredApiKey)
{
    if (!string.IsNullOrWhiteSpace(cliApiKey)) return cliApiKey;

    return Environment.GetEnvironmentVariable(ProductConstants.ApiKeyEnvVar)
        ?? configuredApiKey
        ?? string.Empty;
}

static async Task<string> ResolveCopilotAccessTokenAsync(string? cliToken, AppConfig config, IServiceProvider services, CancellationToken cancellationToken)
{
    var configuredToken = !string.IsNullOrWhiteSpace(cliToken)
        ? cliToken
        : Environment.GetEnvironmentVariable(ProductConstants.CopilotApiKeyEnvVar)
            ?? Environment.GetEnvironmentVariable("COPILOT_API_KEY")
            ?? config.OAuthToken;

    var authService = services.GetRequiredService<CopilotAuthService>();
    var authResult = await authService.CreateSessionAsync(configuredToken, cancellationToken);

    if (authResult.IsInteractiveSignIn)
    {
        var configService = services.GetRequiredService<IConfigService>();
        var savedConfig = configService.LoadConfig();
        savedConfig.OAuthToken = authResult.GitHubToken;
        configService.SaveConfig(savedConfig);
    }

    return authResult.CopilotToken;
}

static async Task RunDoctorAsync(IConfigService configService, string? providerOverride)
{
    AnsiConsole.MarkupLine("[bold]Claude Code Doctor[/]");
    AnsiConsole.WriteLine();

    var config = configService.LoadConfig();
    var provider = ResolveProvider(providerOverride, config.Provider);

    var anthropicApiKey = Environment.GetEnvironmentVariable(ProductConstants.ApiKeyEnvVar);
    var copilotToken = Environment.GetEnvironmentVariable(ProductConstants.CopilotApiKeyEnvVar)
                       ?? Environment.GetEnvironmentVariable("COPILOT_API_KEY")
                       ?? config.OAuthToken;
    var configPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ProductConstants.ConfigDir,
        ProductConstants.GlobalConfigFile);

    var copilotCredentialSet = !string.IsNullOrEmpty(copilotToken);
    var copilotDetail = copilotCredentialSet
        ? LooksLikePersonalAccessToken(copilotToken!)
            ? "PAT detected; Copilot requires GitHub OAuth sign-in"
            : "GitHub credential available"
        : $"Run with --provider {ProductConstants.CopilotProvider} to sign in";

    var providerKeySet = provider == ProductConstants.CopilotProvider
        ? copilotCredentialSet && !LooksLikePersonalAccessToken(copilotToken!)
        : !string.IsNullOrEmpty(anthropicApiKey);

    var checks = new (string Name, bool Passed, string Detail)[]
    {
        ("Provider", true, provider),
        ($"Credential ({provider})", providerKeySet, provider == ProductConstants.CopilotProvider ? copilotDetail : $"Set {ProductConstants.ApiKeyEnvVar}"),
        ("Config file", File.Exists(configPath), configPath),
        (".NET Runtime", true, System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription),
    };

    foreach (var (name, passed, detail) in checks)
    {
        var icon = passed ? "[green]✓[/]" : "[red]✗[/]";
        AnsiConsole.MarkupLine($"  {icon} {name}: [dim]{Markup.Escape(detail)}[/]");
    }

    AnsiConsole.WriteLine();
}

static bool LooksLikePersonalAccessToken(string token)
    => token.StartsWith("github_pat_", StringComparison.OrdinalIgnoreCase)
       || token.StartsWith("ghp_", StringComparison.OrdinalIgnoreCase);

static string? ReadStdinPrompt()
{
    if (Console.IsInputRedirected)
        return Console.In.ReadToEnd().Trim();
    return null;
}
