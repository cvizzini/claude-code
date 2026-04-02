namespace ClaudeCode.Constants;

public static class Figures
{
    public const string BlackCircle = "⏺";  // darwin; use ● on Linux/Win
    public const string BulletOperator = "∙";
    public const string TeardropAsterisk = "✻";
    public const string UpArrow = "\u2191";
    public const string DownArrow = "\u2193";
    public const string LightningBolt = "↯";
    public const string EffortLow = "○";
    public const string EffortMedium = "◐";
    public const string EffortHigh = "●";
    public const string EffortMax = "◉";

    public const string PlayIcon = "\u25b6";
    public const string PauseIcon = "\u23f8";

    public const string RefreshArrow = "\u21bb";
    public const string ChannelArrow = "\u2190";
    public const string InjectedArrow = "\u2192";
    public const string ForkGlyph = "\u2442";

    public const string DiamondOpen = "\u25c7";
    public const string DiamondFilled = "\u25c6";
    public const string ReferenceMark = "\u203b";

    public const string FlagIcon = "\u2691";

    public const string BlockquoteBar = "\u258e";
    public const string HeavyHorizontal = "\u2501";

    public static readonly string[] BridgeSpinnerFrames =
    [
        "\u00b7|\u00b7",
        "\u00b7/\u00b7",
        "\u00b7\u2014\u00b7",
        "\u00b7\\\u00b7",
    ];

    public const string BridgeReadyIndicator = "\u00b7\u2714\ufe0e\u00b7";
    public const string BridgeFailedIndicator = "\u00d7";

    public static string GetBlackCircle()
        => Environment.OSVersion.Platform == PlatformID.MacOSX ? "⏺" : "●";
}
