namespace ClaudeCode.Constants;

public static class ToolLimits
{
    public const int BashDefaultTimeoutMs = 120_000; // 2 minutes
    public const int BashMaxTimeoutMs = 600_000; // 10 minutes
    public const int FileReadMaxBytes = 10 * 1024 * 1024; // 10 MB
    public const int GrepMaxResults = 1000;
    public const int GlobMaxResults = 1000;
    public const int WebFetchMaxBytes = 5 * 1024 * 1024; // 5 MB
    public const int TodoMaxItems = 100;
}
