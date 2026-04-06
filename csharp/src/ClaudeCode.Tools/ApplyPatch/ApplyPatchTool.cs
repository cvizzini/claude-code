using ClaudeCode.Constants;
using ClaudeCode.Core.Tools;
using ClaudeCode.Core.Types;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ClaudeCode.Tools.ApplyPatch;

public sealed record PatchChange(string FilePath, string OldStr, string NewStr, bool ReplaceAll = false, bool CreateIfMissing = false);

public class ApplyPatchTool : ToolBase
{
    private readonly ILogger<ApplyPatchTool> _logger;

    public ApplyPatchTool(ILogger<ApplyPatchTool> logger)
    {
        _logger = logger;
    }

    public override string Name => ToolNames.ApplyPatch;
    public override string Description => "Apply one or more targeted text replacements across files. Use this for structured multi-edit code changes.";
    public override bool IsReadonly => false;

    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> input, CancellationToken cancellationToken = default)
    {
        try
        {
            var changes = ParseChanges(input);
            if (changes.Count == 0)
            {
                return new ToolResult(false, Error: "No changes provided");
            }

            if (changes.Count > ToolLimits.ApplyPatchMaxChanges)
            {
                return new ToolResult(false, Error: $"Too many changes: {changes.Count} (max {ToolLimits.ApplyPatchMaxChanges})");
            }

            var outputs = new List<string>();

            foreach (var fileGroup in changes.GroupBy(change => Path.GetFullPath(change.FilePath), StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var path = fileGroup.Key;
                var fileExists = File.Exists(path);
                var content = fileExists
                    ? await File.ReadAllTextAsync(path, cancellationToken)
                    : string.Empty;

                foreach (var change in fileGroup)
                {
                    if (!fileExists && !change.CreateIfMissing)
                    {
                        return new ToolResult(false, Error: $"File not found: {path}");
                    }

                    if (!fileExists && change.CreateIfMissing)
                    {
                        content = change.NewStr;
                        fileExists = true;
                        outputs.Add($"Created {path}");
                        continue;
                    }

                    var occurrences = CountOccurrences(content, change.OldStr);
                    if (occurrences == 0)
                    {
                        return new ToolResult(false, Error: $"old_str not found in file: {path}");
                    }

                    if (!change.ReplaceAll && occurrences > 1)
                    {
                        return new ToolResult(false, Error: $"old_str appears {occurrences} times in file. It must appear exactly once unless replace_all is true.");
                    }

                    content = change.ReplaceAll
                        ? content.Replace(change.OldStr, change.NewStr, StringComparison.Ordinal)
                        : ReplaceOnce(content, change.OldStr, change.NewStr);

                    outputs.Add(change.ReplaceAll
                        ? $"Updated {path} ({occurrences} replacements)"
                        : $"Updated {path}");
                }

                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(path, content, cancellationToken);
            }

            return new ToolResult(true, Output: string.Join('\n', outputs.Distinct()));
        }
        catch (OperationCanceledException)
        {
            return new ToolResult(false, Error: "Patch application was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying patch");
            return new ToolResult(false, Error: ex.Message);
        }
    }

    private static List<PatchChange> ParseChanges(Dictionary<string, object> input)
    {
        if (!input.TryGetValue("changes", out var changesValue) || changesValue == null)
        {
            return [];
        }

        var json = changesValue switch
        {
            string text => text,
            _ => JsonSerializer.Serialize(changesValue)
        };

        return JsonSerializer.Deserialize<List<PatchChange>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];
    }

    private static int CountOccurrences(string text, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return 0;
        }

        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }

    private static string ReplaceOnce(string text, string oldStr, string newStr)
    {
        var index = text.IndexOf(oldStr, StringComparison.Ordinal);
        if (index < 0)
        {
            return text;
        }

        return string.Concat(text.AsSpan(0, index), newStr, text.AsSpan(index + oldStr.Length));
    }
}
