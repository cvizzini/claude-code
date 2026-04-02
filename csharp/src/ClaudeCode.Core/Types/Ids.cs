using System.Text.RegularExpressions;

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
public readonly partial record struct AgentId(string Value)
{
    [GeneratedRegex(@"^a(?:.+-)?[0-9a-f]{16}$")]
    private static partial Regex GetPattern();

    public override string ToString() => Value;
    public static implicit operator string(AgentId id) => id.Value;

    public static AgentId? TryParse(string s)
        => GetPattern().IsMatch(s) ? new AgentId(s) : null;
}
