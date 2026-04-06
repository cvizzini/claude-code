using ClaudeCode.Constants;
using ClaudeCode.Core.Tools;
using ClaudeCode.Core.Types;
using ClaudeCode.Services.Config;
using ClaudeCode.Services.QueryEngine;
using ClaudeCode.Tools;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Text;

namespace ClaudeCode.Commands;

public class ChatCommand
{
    private readonly IQueryEngine _queryEngine;
    private readonly IConfigService _configService;
    private readonly IToolRegistry _toolRegistry;
    private readonly ILogger<ChatCommand> _logger;

    public ChatCommand(IQueryEngine queryEngine, IConfigService configService, IToolRegistry toolRegistry, ILogger<ChatCommand> logger)
    {
        _queryEngine = queryEngine;
        _configService = configService;
        _toolRegistry = toolRegistry;
        _logger = logger;
    }

    public async Task RunInteractiveAsync(CancellationToken cancellationToken = default)
    {
        var config = _configService.LoadConfig();
        var history = new List<ConversationMessage>();
        var tools = GetToolsForProvider(config.Provider);

        AnsiConsole.MarkupLine("[bold green]Claude Code[/] - Type your message or 'exit' to quit");
        AnsiConsole.WriteLine();

        while (!cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.Markup("[bold blue]You:[/] ");
            var input = Console.ReadLine();

            if (input == null || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            if (string.IsNullOrWhiteSpace(input))
                continue;

            AnsiConsole.Markup("[bold green]Claude:[/] ");

            var request = new QueryRequest(
                input,
                config.Model,
                config.SystemPrompt,
                History: history,
                Tools: tools
            );

            try
            {
                await foreach (var evt in _queryEngine.QueryAsync(request, cancellationToken))
                {
                    switch (evt)
                    {
                        case TextEvent text:
                            Console.Write(text.Text);
                            break;
                        case HistoryEvent historyEvent:
                            history.Add(historyEvent.Message);
                            break;
                        case ToolUseEvent toolUse:
                            WriteToolUse(toolUse);
                            break;
                        case ToolResultEvent toolResult:
                            WriteToolResult(toolResult);
                            break;
                        case ErrorEvent error:
                            AnsiConsole.MarkupLine($"[bold red]Error:[/] {Markup.Escape(error.Error)}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Query failed");
                AnsiConsole.MarkupLine($"[bold red]Error:[/] {Markup.Escape(ex.Message)}");
            }

            Console.WriteLine();
        }
    }

    public async Task RunNonInteractiveAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var config = _configService.LoadConfig();
        var tools = GetToolsForProvider(config.Provider);

        var request = new QueryRequest(prompt, config.Model, config.SystemPrompt, Tools: tools);

        try
        {
            await foreach (var evt in _queryEngine.QueryAsync(request, cancellationToken))
            {
                switch (evt)
                {
                    case TextEvent text:
                        Console.Write(text.Text);
                        break;
                    case HistoryEvent:
                        break;
                    case ToolUseEvent toolUse:
                        WriteToolUse(toolUse);
                        break;
                    case ToolResultEvent toolResult:
                        WriteToolResult(toolResult);
                        break;
                    case ErrorEvent error:
                        Console.Error.WriteLine($"Error: {error.Error}");
                        break;
                }
            }
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query failed");
            Console.Error.WriteLine($"Error: {ex.Message}");
        }
    }

    private List<ITool>? GetToolsForProvider(string provider)
    {
        if (!string.Equals(provider, ProductConstants.CopilotProvider, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new ITool?[]
        {
            _toolRegistry.GetTool(ToolNames.ApplyPatch),
            _toolRegistry.GetTool(ToolNames.Bash),
            _toolRegistry.GetTool(ToolNames.TodoWrite),
            _toolRegistry.GetTool(ToolNames.ListDirectory),
            _toolRegistry.GetTool(ToolNames.Glob),
            _toolRegistry.GetTool(ToolNames.Grep),
            _toolRegistry.GetTool(ToolNames.WebFetch),
            _toolRegistry.GetTool(ToolNames.FileRead),
            _toolRegistry.GetTool(ToolNames.FileWrite),
            _toolRegistry.GetTool(ToolNames.FileEdit)
        }
        .OfType<ITool>()
        .ToList();
    }

    private static void WriteToolUse(ToolUseEvent toolUse)
    {
        AnsiConsole.MarkupLine($"\n[dim]> {Markup.Escape(toolUse.ToolName)} {Markup.Escape(DescribeToolInput(toolUse))}[/]");
    }

    private static void WriteToolResult(ToolResultEvent toolResult)
    {
        if (toolResult.Result.Success)
        {
            var summary = string.IsNullOrWhiteSpace(toolResult.Result.Output)
                ? "completed"
                : Summarize(toolResult.Result.Output);
            AnsiConsole.MarkupLine($"[dim]done: {Markup.Escape(summary)}[/]");
            return;
        }

        var error = toolResult.Result.Error ?? "Tool failed.";
        AnsiConsole.MarkupLine($"[dim red]error: {Markup.Escape(error)}[/]");
    }

    private static string DescribeToolInput(ToolUseEvent toolUse)
    {
        if (toolUse.Input.TryGetValue("file_path", out var filePath) && filePath != null)
        {
            return filePath.ToString() ?? string.Empty;
        }

        if (toolUse.Input.TryGetValue("path", out var path) && path != null)
        {
            return path.ToString() ?? string.Empty;
        }

        if (toolUse.Input.TryGetValue("command", out var command) && command != null)
        {
            return command.ToString() ?? string.Empty;
        }

        if (toolUse.Input.TryGetValue("changes", out var changes) && changes != null)
        {
            return "patch changes";
        }

        return string.Empty;
    }

    private static string Summarize(string text)
    {
        var normalized = text.Replace("\r", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "completed";
        }

        return normalized.Length <= 120 ? normalized : normalized[..117] + "...";
    }
}
