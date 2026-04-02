namespace ClaudeCode.Constants;

public static class Common
{
    public static string GetLocalIsoDate()
    {
        var overrideDate = Environment.GetEnvironmentVariable("CLAUDE_CODE_OVERRIDE_DATE");
        if (!string.IsNullOrEmpty(overrideDate))
            return overrideDate;

        var now = DateTime.Now;
        return $"{now.Year:0000}-{now.Month:00}-{now.Day:00}";
    }

    private static readonly Lazy<string> _sessionStartDate = new(GetLocalIsoDate);
    public static string GetSessionStartDate() => _sessionStartDate.Value;

    public static string GetLocalMonthYear()
    {
        var overrideDate = Environment.GetEnvironmentVariable("CLAUDE_CODE_OVERRIDE_DATE");
        var date = !string.IsNullOrEmpty(overrideDate) ? DateTime.Parse(overrideDate) : DateTime.Now;
        return date.ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
    }
}
