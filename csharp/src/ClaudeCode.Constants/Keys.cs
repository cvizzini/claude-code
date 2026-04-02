namespace ClaudeCode.Constants;

public static class GrowthBookKeys
{
    public static string GetGrowthBookClientKey()
    {
        var userType = Environment.GetEnvironmentVariable("USER_TYPE");
        if (userType == "ant")
        {
            var enableDev = Environment.GetEnvironmentVariable("ENABLE_GROWTHBOOK_DEV");
            return IsEnvTruthy(enableDev) ? "sdk-yZQvlplybuXjYh6L" : "sdk-xRVcrliHIlrg4og4";
        }
        return "sdk-zAZezfDKGoZuXXKe";
    }

    private static bool IsEnvTruthy(string? value)
        => value is "1" or "true" or "yes" or "on";
}
