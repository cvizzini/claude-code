using ClaudeCode.Core.Types;
using ClaudeCode.Services.Config;
using ClaudeCode.Services.QueryEngine;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace ClaudeCode.Commands;

public class ChatCommand
{
    private readonly IQueryEngine _queryEngine;
    private readonly IConfigService _configService;
    private readonly ILogger<ChatCommand> _logger;

    public ChatCommand(IQueryEngine queryEngine, IConfigService configService, ILogger<ChatCommand> logger)
    {
        _queryEngine = queryEngine;
        _configService = configService;
        _logger = logger;
    }

    public async Task RunInteractiveAsync(CancellationToken cancellationToken = default)
    {
        var config = _configService.LoadConfig();
        var history = new List<ConversationMessage>();

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
                History: history
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
            history.Add(new ConversationMessage(MessageRole.User, input, DateTime.UtcNow));
        }
    }

    public async Task RunNonInteractiveAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var config = _configService.LoadConfig();

        var request = new QueryRequest(prompt, config.Model, config.SystemPrompt);

        try
        {
            await foreach (var evt in _queryEngine.QueryAsync(request, cancellationToken))
            {
                switch (evt)
                {
                    case TextEvent text:
                        Console.Write(text.Text);
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
}
