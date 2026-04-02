namespace ClaudeCode.Core.Types;

/// <summary>
/// Strongly-typed session ID to prevent mixing with agent IDs.
/// </summary>
public readonly record struct SessionId(string Value)
{
    public override string ToString() => Value;
    public static implicit operator string(SessionId id) => id.Value;
    public static explicit operator SessionId(string s) => new(s);
}

/// <summary>
/// Strongly-typed agent ID to prevent mixing with session IDs.
/// </summary>
public readonly record struct AgentId(string Value)
{
    private static readonly System.Text.RegularExpressions.Regex Pattern =
        new(@"^a(?:.+-)?[0-9a-f]{16}$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public override string ToString() => Value;
    public static implicit operator string(AgentId id) => id.Value;

    public static AgentId? TryParse(string s)
        => Pattern.IsMatch(s) ? new AgentId(s) : null;
}
