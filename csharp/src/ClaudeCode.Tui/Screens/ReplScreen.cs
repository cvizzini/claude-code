using ClaudeCode.Constants;
using ClaudeCode.Core.Services;
using ClaudeCode.Core.Tools;
using ClaudeCode.Core.Types;
using ClaudeCode.Services.Config;
using ClaudeCode.Services.QueryEngine;
using ClaudeCode.Tools;
using ClaudeCode.Tui.Theme;
using Terminal.Gui;

namespace ClaudeCode.Tui.Screens;

/// <summary>
/// The main interactive chat screen. Occupies the full terminal with:
/// <list type="bullet">
///   <item>a scrollable, colour-coded message history area</item>
///   <item>a multi-line-capable text input bar</item>
///   <item>a bottom status bar showing model info and key hints</item>
/// </list>
/// </summary>
internal sealed class ReplScreen : Toplevel
{
    // ── dependencies ──────────────────────────────────────────────────────────
    private readonly IQueryEngine     _queryEngine;
    private readonly IConfigService   _configService;
    private readonly IToolRegistry    _toolRegistry;

    // ── child views ───────────────────────────────────────────────────────────
    private readonly MessageView  _messageView;
    private readonly TextField    _inputField;
    private readonly StatusBar    _statusBar;
    private readonly FrameView    _inputFrame;

    // ── state ─────────────────────────────────────────────────────────────────
    private readonly List<ConversationMessage> _history = new();
    private bool          _busy;
    private string        _streamingBuffer = string.Empty;
    private bool          _streamingStarted;
    private CancellationTokenSource? _queryCts;

    // ── constructor ───────────────────────────────────────────────────────────

    public ReplScreen(IQueryEngine queryEngine, IConfigService configService, IToolRegistry toolRegistry)
    {
        _queryEngine   = queryEngine;
        _configService = configService;
        _toolRegistry  = toolRegistry;

        ColorScheme = AppTheme.BaseScheme;
        CanFocus = true;

        // ── message area ──────────────────────────────────────────────────────
        _messageView = new MessageView
        {
            X           = 0,
            Y           = 0,
            Width       = Dim.Fill(),
            Height      = Dim.Fill() - 4,   // leave room for input + status
            ColorScheme = AppTheme.BaseScheme,
            CanFocus    = false,
        };
        Add(_messageView);

        // ── input frame ───────────────────────────────────────────────────────
        _inputFrame = new FrameView("Message")
        {
            X           = 0,
            Y           = Pos.Bottom(_messageView),
            Width       = Dim.Fill(),
            Height      = 3,
            ColorScheme = AppTheme.InputScheme,
        };

        _inputField = new TextField("")
        {
            X           = 0,
            Y           = 0,
            Width       = Dim.Fill(),
            Height      = 1,
            ColorScheme = AppTheme.InputScheme,
        };
        _inputFrame.Add(_inputField);
        Add(_inputFrame);

        // ── status bar ────────────────────────────────────────────────────────
        var config = _configService.LoadConfig();
        _statusBar = new StatusBar(new[]
        {
            new StatusItem(Key.F1,     "~F1~ Help",    ShowHelp),
            new StatusItem(Key.F2,     "~F2~ Doctor",  ShowDoctor),
            new StatusItem(Key.F5,     "~F5~ Theme",   ToggleTheme),
            new StatusItem(Key.CtrlMask | Key.C, "~Ctrl+C~ Quit", RequestStop),
        })
        {
            ColorScheme = AppTheme.StatusScheme,
        };
        Add(_statusBar);

        // Override Ctrl+C to stop a running query first; if idle then quit
        KeyDown += OnKeyDown;

        // Enter in the input field triggers a query
        _inputField.KeyDown += OnInputKeyDown;

        // Welcome message
        _messageView.AppendLine(ChatRole.Separator, string.Empty);
        _messageView.AppendLine(ChatRole.Assistant,
            $"Claude Code {ProductConstants.ProductVersion} – GitHub Copilot agent");
        _messageView.AppendLine(ChatRole.Assistant,
            $"Model: {config.Model}  |  Press F1 for help, Ctrl+C to quit");
        _messageView.AppendLine(ChatRole.Separator, string.Empty);
    }

    // ── lifecycle ─────────────────────────────────────────────────────────────

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();
        _messageView.SetNeedsDisplay();
    }

    // ── key handling ─────────────────────────────────────────────────────────

    private void OnKeyDown(View.KeyEventEventArgs e)
    {
        if (e.KeyEvent.Key == (Key.CtrlMask | Key.C))
        {
            if (_busy && _queryCts != null)
            {
                _queryCts.Cancel();
                e.Handled = true;
            }
        }
    }

    private void OnInputKeyDown(View.KeyEventEventArgs e)
    {
        if (e.KeyEvent.Key == Key.Enter)
        {
            var text = _inputField.Text?.ToString()?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(text) && !_busy)
            {
                _inputField.Text = string.Empty;
                e.Handled = true;
                HandleSlashCommandOrSend(text);
            }
        }
    }

    // ── message sending ───────────────────────────────────────────────────────

    private void HandleSlashCommandOrSend(string input)
    {
        if (input.StartsWith('/'))
        {
            HandleSlashCommand(input);
            return;
        }

        SendMessageAsync(input);
    }

    private void HandleSlashCommand(string input)
    {
        var parts   = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLowerInvariant();
        switch (command)
        {
            case "/exit":
            case "/quit":
                RequestStop();
                return;

            case "/clear":
                _history.Clear();
                _messageView.ClearMessages();
                _messageView.AppendLine(ChatRole.Assistant, "Conversation cleared.");
                return;

            case "/help":
                ShowHelp();
                return;

            case "/doctor":
                ShowDoctor();
                return;

            case "/theme":
                ToggleTheme();
                return;

            case "/model":
                ShowModelInfo();
                return;

            case "/resume":
                ShowResumeScreen();
                return;

            default:
                _messageView.AppendLine(ChatRole.Error, $"Unknown command: {command}. Type /help for available commands.");
                return;
        }
    }

    private void SendMessageAsync(string userInput)
    {
        _busy = true;
        _streamingBuffer  = string.Empty;
        _streamingStarted = false;

        // Echo the user message in the UI
        _messageView.AppendLine(ChatRole.User, userInput);
        _messageView.AppendLine(ChatRole.Separator, string.Empty);

        var config = _configService.LoadConfig();
        var tools  = GetTools(config.Provider);

        var request = new QueryRequest(
            userInput,
            config.Model,
            config.SystemPrompt,
            History: _history,
            Tools:   tools
        );

        _queryCts = new CancellationTokenSource();
        var ct = _queryCts.Token;

        Task.Run(async () =>
        {
            try
            {
                await foreach (var evt in _queryEngine.QueryAsync(request, ct))
                {
                    var captured = evt;
                    Application.MainLoop.Invoke(() => HandleQueryEvent(captured));
                }
            }
            catch (OperationCanceledException)
            {
                Application.MainLoop.Invoke(() =>
                    _messageView.AppendLine(ChatRole.ToolResult, "[cancelled]"));
            }
            catch (Exception ex)
            {
                Application.MainLoop.Invoke(() =>
                    _messageView.AppendLine(ChatRole.Error, ex.Message));
            }
            finally
            {
                Application.MainLoop.Invoke(() => OnQueryComplete());
            }
        }, ct);
    }

    private void HandleQueryEvent(QueryEvent evt)
    {
        switch (evt)
        {
            case TextEvent text:
                if (!_streamingStarted)
                {
                    // Start a new assistant line in the message view
                    _messageView.AppendLine(ChatRole.Assistant, string.Empty);
                    _streamingStarted = true;
                }
                _streamingBuffer += text.Text;
                _messageView.AppendToLastLine(text.Text);
                break;

            case HistoryEvent historyEvt:
                _history.Add(historyEvt.Message);
                break;

            case ToolUseEvent toolUse:
                FlushStreamingLine();
                var args = DescribeToolInput(toolUse);
                _messageView.AppendLine(ChatRole.ToolUse,
                    $"{toolUse.ToolName} {args}".Trim());
                break;

            case ToolResultEvent toolResult:
                if (toolResult.Result.Success)
                {
                    var summary = string.IsNullOrWhiteSpace(toolResult.Result.Output)
                        ? string.Empty
                        : Summarize(toolResult.Result.Output);
                    var display = string.IsNullOrEmpty(summary) ? "done" : $"done: {summary}";
                    _messageView.AppendLine(ChatRole.ToolResult, display);
                }
                else
                {
                    var err = toolResult.Result.Error ?? "Tool failed.";
                    _messageView.AppendLine(ChatRole.Error, $"error: {err}");
                }
                _streamingStarted = false;
                _streamingBuffer  = string.Empty;
                break;

            case ErrorEvent error:
                FlushStreamingLine();
                _messageView.AppendLine(ChatRole.Error, error.Error);
                break;
        }
    }

    private void OnQueryComplete()
    {
        FlushStreamingLine();
        _messageView.AppendLine(ChatRole.Separator, string.Empty);
        _busy = false;
        _queryCts?.Dispose();
        _queryCts = null;
        _inputField.SetFocus();
    }

    private void FlushStreamingLine()
    {
        _streamingStarted = false;
        _streamingBuffer  = string.Empty;
    }

    // ── slash-command handlers ────────────────────────────────────────────────

    private static void ShowHelp()
    {
        var d = new Dialog("Help", 60, 22);

        var lines = new[]
        {
            "Available commands:",
            string.Empty,
            "  /clear   – clear conversation history",
            "  /doctor  – show diagnostics",
            "  /model   – show current model",
            "  /resume  – browse saved sessions",
            "  /theme   – toggle light / dark theme",
            "  /help    – show this help",
            "  /exit    – quit Claude Code",
            string.Empty,
            "Keyboard shortcuts:",
            string.Empty,
            "  Enter      – send message",
            "  F1         – help",
            "  F2         – doctor",
            "  F5         – toggle theme",
            "  Ctrl+C     – cancel query / quit",
            "  ↑/↓/PgUp/PgDn – scroll messages",
        };

        var textView = new TextView
        {
            X        = 1,
            Y        = 1,
            Width    = Dim.Fill() - 1,
            Height   = lines.Length,
            ReadOnly = true,
            Text     = string.Join('\n', lines),
            ColorScheme = AppTheme.DialogScheme,
        };
        d.Add(textView);

        var ok = new Button("OK", is_default: true) { X = Pos.Center() };
        ok.Clicked += () => Application.RequestStop();
        d.AddButton(ok);

        Application.Run(d);
    }

    private void ShowDoctor()
    {
        var screen = new DoctorScreen(_configService);
        Application.Run(screen);
    }

    private void ToggleTheme()
    {
        AppTheme.Apply();
        // Re-apply the updated scheme refs to all child views
        ColorScheme          = AppTheme.BaseScheme;
        _messageView.ColorScheme = AppTheme.BaseScheme;
        _inputFrame.ColorScheme  = AppTheme.InputScheme;
        _inputField.ColorScheme  = AppTheme.InputScheme;
        _statusBar.ColorScheme   = AppTheme.StatusScheme;
        SetNeedsDisplay();
    }

    private void ShowModelInfo()
    {
        var config = _configService.LoadConfig();
        _messageView.AppendLine(ChatRole.Assistant,
            $"Active model: {config.Model}  Provider: {config.Provider}");
    }

    private static void ShowResumeScreen()
    {
        var screen = new ResumeConversationScreen();
        Application.Run(screen);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private List<ITool>? GetTools(string provider)
    {
        if (!string.Equals(provider, ProductConstants.CopilotProvider, StringComparison.OrdinalIgnoreCase))
            return null;

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
            _toolRegistry.GetTool(ToolNames.FileEdit),
        }
        .OfType<ITool>()
        .ToList();
    }

    private static string DescribeToolInput(ToolUseEvent toolUse)
    {
        if (toolUse.Input.TryGetValue("file_path", out var fp) && fp != null)
            return fp.ToString() ?? string.Empty;
        if (toolUse.Input.TryGetValue("path", out var p) && p != null)
            return p.ToString() ?? string.Empty;
        if (toolUse.Input.TryGetValue("command", out var cmd) && cmd != null)
            return cmd.ToString() ?? string.Empty;
        if (toolUse.Input.ContainsKey("changes"))
            return "patch changes";
        return string.Empty;
    }

    private static string Summarize(string text)
    {
        var first = text.Replace("\r", string.Empty)
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(first))
            return "completed";
        return first.Length <= 120 ? first : first[..117] + "...";
    }
}
