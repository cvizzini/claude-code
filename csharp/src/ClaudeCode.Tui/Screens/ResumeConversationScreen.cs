using ClaudeCode.Tui.Theme;
using Terminal.Gui;

namespace ClaudeCode.Tui.Screens;

/// <summary>
/// Session browser screen.  Allows the user to pick up a prior conversation.
/// <para>
/// Session persistence is handled by Gap 13; at this stage the screen is scaffolded
/// and shows a placeholder message when no saved sessions are found.
/// </para>
/// </summary>
internal sealed class ResumeConversationScreen : Toplevel
{
    private readonly ListView? _sessionList;

    public ResumeConversationScreen()
    {
        ColorScheme = AppTheme.BaseScheme;

        var sessions = LoadSessions();

        var dialog = new Dialog("Resume Conversation", 70, 20)
        {
            ColorScheme = AppTheme.DialogScheme,
        };

        if (sessions.Count == 0)
        {
            var msg = new Label("No saved sessions found.\nSession persistence will be available in a future update.")
            {
                X           = Pos.Center(),
                Y           = Pos.Center(),
                ColorScheme = AppTheme.DialogScheme,
            };
            dialog.Add(msg);
        }
        else
        {
            var header = new Label("Select a session to resume:")
            {
                X           = 1,
                Y           = 1,
                ColorScheme = AppTheme.DialogScheme,
            };
            dialog.Add(header);

            _sessionList = new ListView(sessions.Select(s => s.DisplayLabel).ToList())
            {
                X           = 1,
                Y           = 3,
                Width       = Dim.Fill() - 2,
                Height      = Dim.Fill() - 4,
                ColorScheme = AppTheme.BaseScheme,
                AllowsMarking = false,
            };
            dialog.Add(_sessionList);
        }

        var cancel = new Button("Cancel") { X = Pos.Center() };
        cancel.Clicked += () => Application.RequestStop();
        dialog.AddButton(cancel);

        if (sessions.Count > 0)
        {
            var open = new Button("Open", is_default: true);
            open.Clicked += () =>
            {
                // Gap 13 will implement actual session loading;
                // for now simply close the dialog.
                Application.RequestStop();
            };
            dialog.AddButton(open);
        }

        Add(dialog);
    }

    // ── session discovery ─────────────────────────────────────────────────────

    private static List<SessionEntry> LoadSessions()
    {
        var sessionsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".claude",
            "sessions");

        if (!Directory.Exists(sessionsDir))
            return new List<SessionEntry>();

        return Directory.EnumerateFiles(sessionsDir, "*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Take(50)
            .Select(path =>
            {
                var lastWrite = File.GetLastWriteTimeUtc(path).ToLocalTime();
                var name      = Path.GetFileNameWithoutExtension(path);
                return new SessionEntry(name, path, $"{lastWrite:yyyy-MM-dd HH:mm}  {name}");
            })
            .ToList();
    }

    private record SessionEntry(string Id, string FilePath, string DisplayLabel);
}
