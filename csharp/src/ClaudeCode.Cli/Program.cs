using ClaudeCode.Commands;
using ClaudeCode.Constants;
using ClaudeCode.Core.Services;
using ClaudeCode.Services.AnthropicClient;
using ClaudeCode.Services.Config;
using ClaudeCode.Services.QueryEngine;
using ClaudeCode.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Invocation;

var apiKeyOption = new Option<string?>("--api-key", "Anthropic API key (overrides ANTHROPIC_API_KEY env var)");
var modelOption = new Option<string>("--model", () => ProductConstants.DefaultModel, "Model to use");
var verboseOption = new Option<bool>(new[] { "--verbose", "-v" }, "Enable verbose output");
var debugOption = new Option<bool>("--debug", "Enable debug output");
var printOption = new Option<bool>(new[] { "-p", "--print" }, "Non-interactive mode: print response and exit");
var systemPromptOption = new Option<string?>("--system-prompt", "System prompt to use");
var promptArgument = new Argument<string?>("prompt", () => null, "Initial prompt (optional)");

var rootCommand = new RootCommand("Claude Code - AI coding assistant powered by Claude");
rootCommand.AddGlobalOption(apiKeyOption);
rootCommand.AddGlobalOption(modelOption);
rootCommand.AddGlobalOption(verboseOption);
rootCommand.AddGlobalOption(debugOption);
rootCommand.AddArgument(promptArgument);
rootCommand.AddOption(printOption);
rootCommand.AddOption(systemPromptOption);

rootCommand.SetHandler(async (InvocationContext ctx) =>
{
    var apiKey = ctx.ParseResult.GetValueForOption(apiKeyOption);
    var model = ctx.ParseResult.GetValueForOption(modelOption) ?? ProductConstants.DefaultModel;
    var verbose = ctx.ParseResult.GetValueForOption(verboseOption);
    var debug = ctx.ParseResult.GetValueForOption(debugOption);
    var prompt = ctx.ParseResult.GetValueForArgument(promptArgument);
    var print = ctx.ParseResult.GetValueForOption(printOption);

    var services = BuildServices(apiKey, model, verbose, debug);
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
    var apiKey = ctx.ParseResult.GetValueForOption(apiKeyOption);
    var model = ctx.ParseResult.GetValueForOption(modelOption) ?? ProductConstants.DefaultModel;
    var verbose = ctx.ParseResult.GetValueForOption(verboseOption);
    var debug = ctx.ParseResult.GetValueForOption(debugOption);
    var services = BuildServices(apiKey, model, verbose, debug);
    var configService = services.GetRequiredService<IConfigService>();
    await RunDoctorAsync(configService);
});
rootCommand.AddCommand(doctorCommand);

// Config command
var configCommand = new Command("config", "Manage Claude Code configuration");
var configListCmd = new Command("list", "List current configuration values");
configListCmd.SetHandler((InvocationContext ctx) =>
{
    var apiKey = ctx.ParseResult.GetValueForOption(apiKeyOption);
    var model = ctx.ParseResult.GetValueForOption(modelOption) ?? ProductConstants.DefaultModel;
    var verbose = ctx.ParseResult.GetValueForOption(verboseOption);
    var debug = ctx.ParseResult.GetValueForOption(debugOption);
    var services = BuildServices(apiKey, model, verbose, debug);
    var configService = services.GetRequiredService<IConfigService>();
    var config = configService.LoadConfig();

    var table = new Table().AddColumn("Setting").AddColumn("Value");
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

static IServiceProvider BuildServices(string? apiKey, string model, bool verbose, bool debug)
{
    var services = new ServiceCollection();

    var logLevel = debug ? LogLevel.Debug : verbose ? LogLevel.Information : LogLevel.Warning;
    services.AddLogging(builder => builder
        .SetMinimumLevel(logLevel)
        .AddConsole());

    services.AddHttpClient();
    services.AddSingleton<IConfigService>(sp => new ConfigService(sp.GetService<ILogger<ConfigService>>()));

    var resolvedApiKey = apiKey ?? Environment.GetEnvironmentVariable(ProductConstants.ApiKeyEnvVar) ?? string.Empty;

    if (!string.IsNullOrEmpty(resolvedApiKey))
        services.AddAnthropicClient(resolvedApiKey);
    else
        services.AddHttpClient<IAnthropicClient, ClaudeCode.Services.AnthropicClient.AnthropicClient>();

    services.AddSingleton<IQueryEngine, QueryEngine>();
    services.AddClaudeTools();
    services.AddTransient<ChatCommand>();

    var provider = services.BuildServiceProvider();
    provider.PopulateToolRegistry();
    return provider;
}

static string? ReadStdinPrompt()
{
    if (Console.IsInputRedirected)
        return Console.In.ReadToEnd().Trim();
    return null;
}

static async Task RunDoctorAsync(IConfigService configService)
{
    AnsiConsole.MarkupLine("[bold]Claude Code Doctor[/]");
    AnsiConsole.WriteLine();

    var apiKey = Environment.GetEnvironmentVariable(ProductConstants.ApiKeyEnvVar);
    var configPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ProductConstants.ConfigDir,
        ProductConstants.GlobalConfigFile);

    var checks = new (string Name, bool Passed, string Detail)[]
    {
        ("API Key (env var)", !string.IsNullOrEmpty(apiKey), apiKey != null ? "Set" : $"Set {ProductConstants.ApiKeyEnvVar} environment variable"),
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
