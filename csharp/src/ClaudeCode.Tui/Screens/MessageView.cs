using ClaudeCode.Core.Types;
using ClaudeCode.Tui.Theme;
using Terminal.Gui;

namespace ClaudeCode.Tui.Screens;

/// <summary>Role-tagged line that the <see cref="MessageView"/> renders.</summary>
internal record MessageLine(ChatRole Role, string Text);

/// <summary>
/// Custom <see cref="View"/> that renders a colour-coded, scrollable list of chat
/// messages.  New lines are appended with <see cref="AppendLine"/> and the view
/// scrolls automatically to the bottom when <see cref="AutoScroll"/> is true.
/// </summary>
internal sealed class MessageView : View
{
    // ── state ─────────────────────────────────────────────────────────────────
    private readonly List<MessageLine> _lines = new();
    private int _scrollTop;

    // ── properties ────────────────────────────────────────────────────────────

    /// <summary>When true, the view scrolls to the bottom when new lines are added.</summary>
    public bool AutoScroll { get; set; } = true;

    /// <summary>Total number of rendered lines.</summary>
    public int LineCount => _lines.Count;

    // ── public API ────────────────────────────────────────────────────────────

    /// <summary>Add a fully-formed message line.</summary>
    public void AppendLine(ChatRole role, string text)
    {
        // Word-wrap long lines to the view width so horizontal scrolling is not
        // needed and the wrapped segments render under the same prefix indent.
        var maxWidth = Math.Max(Frame.Width - 2, 10);
        foreach (var wrapped in WordWrap(text, maxWidth))
            _lines.Add(new MessageLine(role, wrapped));

        if (AutoScroll)
            ScrollToBottom();

        SetNeedsDisplay();
    }

    /// <summary>Amend the last line (used for streaming tokens).</summary>
    public void AppendToLastLine(string text)
    {
        if (_lines.Count == 0)
        {
            AppendLine(ChatRole.Assistant, text);
            return;
        }

        var last = _lines[^1];
        _lines[^1] = last with { Text = last.Text + text };

        // If the updated last line is now too wide, re-flow it
        var maxWidth = Math.Max(Frame.Width - 2, 10);
        var combined = _lines[^1].Text;
        if (combined.Length > maxWidth)
        {
            _lines.RemoveAt(_lines.Count - 1);
            var role = last.Role;
            foreach (var wrapped in WordWrap(combined, maxWidth))
                _lines.Add(new MessageLine(role, wrapped));
        }

        if (AutoScroll)
            ScrollToBottom();

        SetNeedsDisplay();
    }

    /// <summary>Remove all lines.</summary>
    public void ClearMessages()
    {
        _lines.Clear();
        _scrollTop = 0;
        SetNeedsDisplay();
    }

    /// <summary>Scroll to show the last line.</summary>
    public void ScrollToBottom()
    {
        var visible = Frame.Height;
        _scrollTop = Math.Max(0, _lines.Count - visible);
    }

    // ── keyboard handling ─────────────────────────────────────────────────────

    public override bool ProcessKey(KeyEvent keyEvent)
    {
        switch (keyEvent.Key)
        {
            case Key.CursorUp:
                ScrollUp(1);
                return true;
            case Key.CursorDown:
                ScrollDown(1);
                return true;
            case Key.PageUp:
                ScrollUp(Frame.Height - 1);
                return true;
            case Key.PageDown:
                ScrollDown(Frame.Height - 1);
                return true;
            case Key.Home:
                _scrollTop = 0;
                SetNeedsDisplay();
                return true;
            case Key.End:
                ScrollToBottom();
                SetNeedsDisplay();
                return true;
        }

        return base.ProcessKey(keyEvent);
    }

    private void ScrollUp(int amount)
    {
        _scrollTop = Math.Max(0, _scrollTop - amount);
        SetNeedsDisplay();
    }

    private void ScrollDown(int amount)
    {
        var max = Math.Max(0, _lines.Count - Frame.Height);
        _scrollTop = Math.Min(max, _scrollTop + amount);
        SetNeedsDisplay();
    }

    // ── rendering ─────────────────────────────────────────────────────────────

    public override void Redraw(Rect bounds)
    {
        // Fill background
        Driver.SetAttribute(AppTheme.NormalText);
        Clear(bounds);

        var viewWidth  = bounds.Width;
        var viewHeight = bounds.Height;

        for (var row = 0; row < viewHeight; row++)
        {
            var lineIndex = _scrollTop + row;
            if (lineIndex >= _lines.Count)
                break;

            var line = _lines[lineIndex];
            DrawLine(bounds.X, bounds.Y + row, viewWidth, line);
        }

        // Scroll-bar indicator on the right edge (if content overflows)
        if (_lines.Count > viewHeight)
            DrawScrollBar(bounds, viewHeight);
    }

    private void DrawLine(int startX, int y, int maxWidth, MessageLine line)
    {
        Move(startX, y);
        Driver.SetAttribute(AppTheme.NormalText);

        // Render the role prefix (coloured), then the text
        switch (line.Role)
        {
            case ChatRole.User:
                Driver.SetAttribute(AppTheme.UserLabel);
                Driver.AddStr("You: ");
                Driver.SetAttribute(AppTheme.NormalText);
                break;
            case ChatRole.Assistant:
                Driver.SetAttribute(AppTheme.AssistantLabel);
                Driver.AddStr("Claude: ");
                Driver.SetAttribute(AppTheme.NormalText);
                break;
            case ChatRole.ToolUse:
                Driver.SetAttribute(AppTheme.ToolUse);
                Driver.AddStr("> ");
                break;
            case ChatRole.ToolResult:
                Driver.SetAttribute(AppTheme.ToolResult);
                Driver.AddStr("  ");
                break;
            case ChatRole.Error:
                Driver.SetAttribute(AppTheme.ErrorText);
                Driver.AddStr("Error: ");
                break;
            case ChatRole.Separator:
                Driver.SetAttribute(AppTheme.DimText);
                Driver.AddStr(new string('─', maxWidth));
                return;
            default:
                // Continuation / plain text
                Driver.AddStr("  ");
                break;
        }

        // Clip text to available width
        var text = line.Text;
        if (!string.IsNullOrEmpty(text))
            Driver.AddStr(text);
    }

    private void DrawScrollBar(Rect bounds, int viewHeight)
    {
        var totalLines   = _lines.Count;
        var scrollBarHeight = Math.Max(1, (int)Math.Round((double)viewHeight / totalLines * viewHeight));
        var scrollBarTop = (int)Math.Round((double)_scrollTop / totalLines * viewHeight);

        Driver.SetAttribute(AppTheme.DimText);
        var x = bounds.X + bounds.Width - 1;

        for (var row = 0; row < viewHeight; row++)
        {
            Move(x, bounds.Y + row);
            var inThumb = row >= scrollBarTop && row < scrollBarTop + scrollBarHeight;
            Driver.AddRune(inThumb ? '\u2588' : '\u2591'); // █ / ░
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static IEnumerable<string> WordWrap(string text, int maxWidth)
    {
        if (maxWidth <= 0 || text.Length <= maxWidth)
        {
            yield return text;
            yield break;
        }

        var remaining = text;
        while (remaining.Length > maxWidth)
        {
            var slice = remaining[..maxWidth];
            var cut   = slice.LastIndexOf(' ');
            if (cut <= 0)
                cut = maxWidth;

            yield return remaining[..cut];
            remaining = remaining[cut..].TrimStart();
        }

        if (!string.IsNullOrEmpty(remaining))
            yield return remaining;
    }
}

/// <summary>Extended role enum used only by the TUI to distinguish tool-use lines.</summary>
internal enum ChatRole
{
    User,
    Assistant,
    Continuation,
    ToolUse,
    ToolResult,
    Error,
    Separator,
}
