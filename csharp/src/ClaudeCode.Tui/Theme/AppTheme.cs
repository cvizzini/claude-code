using Terminal.Gui;

namespace ClaudeCode.Tui.Theme;

/// <summary>Selectable colour theme for the TUI.</summary>
public enum AppThemeKind { Dark, Light }

/// <summary>
/// Colour tokens used throughout every TUI screen.
/// Call <see cref="Apply"/> after <c>Application.Init()</c> to install the theme.
/// </summary>
public static class AppTheme
{
    // ── public state ──────────────────────────────────────────────────────────
    public static AppThemeKind Current { get; private set; } = AppThemeKind.Dark;

    // ── colour tokens ─────────────────────────────────────────────────────────
    public static Terminal.Gui.Attribute UserLabel      { get; private set; }
    public static Terminal.Gui.Attribute AssistantLabel { get; private set; }
    public static Terminal.Gui.Attribute ToolUse        { get; private set; }
    public static Terminal.Gui.Attribute ToolResult     { get; private set; }
    public static Terminal.Gui.Attribute ErrorText      { get; private set; }
    public static Terminal.Gui.Attribute StatusBar      { get; private set; }
    public static Terminal.Gui.Attribute InputArea      { get; private set; }
    public static Terminal.Gui.Attribute NormalText     { get; private set; }
    public static Terminal.Gui.Attribute DimText        { get; private set; }

    // ── ColorSchemes for views ────────────────────────────────────────────────
    public static ColorScheme BaseScheme     { get; private set; } = new();
    public static ColorScheme InputScheme    { get; private set; } = new();
    public static ColorScheme StatusScheme   { get; private set; } = new();
    public static ColorScheme DialogScheme   { get; private set; } = new();

    // ── API ───────────────────────────────────────────────────────────────────

    /// <summary>Install the given theme, or toggle between dark and light.</summary>
    public static void Apply(AppThemeKind? kind = null)
    {
        Current = kind ?? (Current == AppThemeKind.Dark ? AppThemeKind.Light : AppThemeKind.Dark);

        if (Current == AppThemeKind.Dark)
            ApplyDark();
        else
            ApplyLight();
    }

    // ── private ───────────────────────────────────────────────────────────────

    private static Terminal.Gui.Attribute Make(Color fg, Color bg)
        => Application.Driver.MakeAttribute(fg, bg);

    private static void ApplyDark()
    {
        // Raw colour attributes
        UserLabel      = Make(Color.BrightCyan,   Color.Black);
        AssistantLabel = Make(Color.BrightGreen,  Color.Black);
        ToolUse        = Make(Color.Gray,          Color.Black);
        ToolResult     = Make(Color.Gray,          Color.Black);
        ErrorText      = Make(Color.BrightRed,    Color.Black);
        StatusBar      = Make(Color.White,         Color.DarkGray);
        InputArea      = Make(Color.White,         Color.Black);
        NormalText     = Make(Color.White,         Color.Black);
        DimText        = Make(Color.Gray,          Color.Black);

        // View colour schemes
        BaseScheme = new ColorScheme
        {
            Normal    = Make(Color.White,    Color.Black),
            Focus     = Make(Color.White,    Color.Blue),
            HotNormal = Make(Color.BrightYellow, Color.Black),
            HotFocus  = Make(Color.BrightYellow, Color.Blue),
        };

        InputScheme = new ColorScheme
        {
            Normal    = Make(Color.White,    Color.Black),
            Focus     = Make(Color.White,    Color.DarkGray),
            HotNormal = Make(Color.White,    Color.Black),
            HotFocus  = Make(Color.White,    Color.DarkGray),
        };

        StatusScheme = new ColorScheme
        {
            Normal    = Make(Color.White,    Color.DarkGray),
            Focus     = Make(Color.White,    Color.DarkGray),
            HotNormal = Make(Color.BrightYellow, Color.DarkGray),
            HotFocus  = Make(Color.BrightYellow, Color.DarkGray),
        };

        DialogScheme = new ColorScheme
        {
            Normal    = Make(Color.White,    Color.DarkGray),
            Focus     = Make(Color.Black,    Color.Gray),
            HotNormal = Make(Color.BrightYellow, Color.DarkGray),
            HotFocus  = Make(Color.BrightYellow, Color.Gray),
        };
    }

    private static void ApplyLight()
    {
        UserLabel      = Make(Color.Blue,         Color.White);
        AssistantLabel = Make(Color.Green,         Color.White);
        ToolUse        = Make(Color.DarkGray,      Color.White);
        ToolResult     = Make(Color.DarkGray,      Color.White);
        ErrorText      = Make(Color.Red,           Color.White);
        StatusBar      = Make(Color.Black,         Color.Gray);
        InputArea      = Make(Color.Black,         Color.White);
        NormalText     = Make(Color.Black,         Color.White);
        DimText        = Make(Color.DarkGray,      Color.White);

        BaseScheme = new ColorScheme
        {
            Normal    = Make(Color.Black,    Color.White),
            Focus     = Make(Color.White,    Color.Blue),
            HotNormal = Make(Color.DarkGray, Color.White),
            HotFocus  = Make(Color.White,    Color.Blue),
        };

        InputScheme = new ColorScheme
        {
            Normal    = Make(Color.Black,    Color.White),
            Focus     = Make(Color.Black,    Color.Gray),
            HotNormal = Make(Color.Black,    Color.White),
            HotFocus  = Make(Color.Black,    Color.Gray),
        };

        StatusScheme = new ColorScheme
        {
            Normal    = Make(Color.Black,    Color.Gray),
            Focus     = Make(Color.Black,    Color.Gray),
            HotNormal = Make(Color.DarkGray, Color.Gray),
            HotFocus  = Make(Color.DarkGray, Color.Gray),
        };

        DialogScheme = new ColorScheme
        {
            Normal    = Make(Color.Black,    Color.Gray),
            Focus     = Make(Color.White,    Color.Blue),
            HotNormal = Make(Color.DarkGray, Color.Gray),
            HotFocus  = Make(Color.White,    Color.Blue),
        };
    }
}
