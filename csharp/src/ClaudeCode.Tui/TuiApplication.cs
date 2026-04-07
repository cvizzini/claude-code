using ClaudeCode.Core.Services;
using ClaudeCode.Core.Tools;
using ClaudeCode.Services.Config;
using ClaudeCode.Services.QueryEngine;
using ClaudeCode.Tools;
using ClaudeCode.Tui.Screens;
using ClaudeCode.Tui.Theme;
using Terminal.Gui;

namespace ClaudeCode.Tui;

/// <summary>
/// Bootstraps and runs the Terminal.Gui TUI.
/// Call <see cref="RunAsync"/> from <c>Program.cs</c> for the interactive path.
/// </summary>
public sealed class TuiApplication
{
    private readonly IQueryEngine   _queryEngine;
    private readonly IConfigService _configService;
    private readonly IToolRegistry  _toolRegistry;

    public TuiApplication(IQueryEngine queryEngine, IConfigService configService, IToolRegistry toolRegistry)
    {
        _queryEngine   = queryEngine;
        _configService = configService;
        _toolRegistry  = toolRegistry;
    }

    /// <summary>
    /// Initialise Terminal.Gui, apply the default dark theme and run the REPL screen.
    /// Blocks until the user quits.  Safe to <c>await</c> from an async context because
    /// it wraps the synchronous Terminal.Gui event loop in a <see cref="Task"/>.
    /// </summary>
    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            Application.Init();
            try
            {
                AppTheme.Apply(AppThemeKind.Dark);

                cancellationToken.Register(() => Application.MainLoop?.Invoke(() => Application.RequestStop()));

                var screen = new ReplScreen(_queryEngine, _configService, _toolRegistry);
                Application.Run(screen);
            }
            finally
            {
                Application.Shutdown();
            }
        }, CancellationToken.None);
    }
}
