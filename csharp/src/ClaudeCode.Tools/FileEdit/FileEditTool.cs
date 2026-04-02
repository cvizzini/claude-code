using ClaudeCode.Core.Tools;
using ClaudeCode.Core.Types;
using ClaudeCode.Constants;
using Microsoft.Extensions.Logging;

namespace ClaudeCode.Tools.FileEdit;

public class FileEditTool : ToolBase
{
    private readonly ILogger<FileEditTool> _logger;

    public FileEditTool(ILogger<FileEditTool> logger) { _logger = logger; }

    public override string Name => ToolNames.FileEdit;
    public override string Description => "Edit a file by replacing a specific string with another. The old_str must appear exactly once in the file.";
    public override bool IsReadonly => false;

    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> input, CancellationToken cancellationToken = default)
    {
        var path = GetString(input, "file_path");
        var oldStr = GetString(input, "old_str");
        var newStr = GetString(input, "new_str");

        if (string.IsNullOrEmpty(path))
            return new ToolResult(false, Error: "No file_path provided");

        try
        {
            path = Path.GetFullPath(path);
            if (!File.Exists(path))
                return new ToolResult(false, Error: $"File not found: {path}");

            var content = await File.ReadAllTextAsync(path, cancellationToken);

            var count = CountOccurrences(content, oldStr);
            if (count == 0)
                return new ToolResult(false, Error: $"old_str not found in file: {path}");
            if (count > 1)
                return new ToolResult(false, Error: $"old_str appears {count} times in file. It must appear exactly once.");

            var newContent = content.Replace(oldStr, newStr);
            await File.WriteAllTextAsync(path, newContent, cancellationToken);

            return new ToolResult(true, Output: $"File edited successfully: {path}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing file: {Path}", path);
            return new ToolResult(false, Error: ex.Message);
        }
    }

    private static int CountOccurrences(string text, string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return 0;
        int count = 0, index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
}
