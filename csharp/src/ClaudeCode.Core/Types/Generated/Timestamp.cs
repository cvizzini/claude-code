namespace ClaudeCode.Core.Types.Generated;

/// <summary>
/// A Timestamp represents a point in time independent of any time zone or local calendar,
/// encoded as a count of seconds and fractions of seconds at nanosecond resolution.
/// </summary>
public record ProtoTimestamp(long? Seconds = null, int? Nanos = null)
{
    public DateTime ToDateTime()
    {
        var seconds = Seconds ?? 0;
        var nanos = Nanos ?? 0;
        return DateTimeOffset.FromUnixTimeSeconds(seconds)
            .AddTicks(nanos / 100)
            .UtcDateTime;
    }

    public static ProtoTimestamp FromDateTime(DateTime dt)
    {
        var offset = new DateTimeOffset(dt, TimeSpan.Zero);
        var seconds = offset.ToUnixTimeSeconds();
        var nanos = (int)((offset.ToUnixTimeMilliseconds() % 1000) * 1_000_000);
        return new ProtoTimestamp(seconds, nanos);
    }
}
