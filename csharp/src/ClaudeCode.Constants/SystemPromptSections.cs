namespace ClaudeCode.Constants;

public delegate Task<string?> SystemPromptComputeFn();

public record SystemPromptSection(string Name, SystemPromptComputeFn Compute, bool CacheBreak);

public static class SystemPromptSections
{
    private static readonly Dictionary<string, string?> _cache = new();

    public static SystemPromptSection CreateSection(string name, SystemPromptComputeFn compute)
        => new(name, compute, CacheBreak: false);

    public static SystemPromptSection CreateUncachedSection(string name, SystemPromptComputeFn compute)
        => new(name, compute, CacheBreak: true);

    public static async Task<IReadOnlyList<string?>> ResolveSections(IEnumerable<SystemPromptSection> sections)
    {
        var results = new List<string?>();
        foreach (var section in sections)
        {
            if (!section.CacheBreak && _cache.TryGetValue(section.Name, out var cached))
            {
                results.Add(cached);
                continue;
            }
            var value = await section.Compute();
            _cache[section.Name] = value;
            results.Add(value);
        }
        return results;
    }

    public static void ClearCache() => _cache.Clear();
}
