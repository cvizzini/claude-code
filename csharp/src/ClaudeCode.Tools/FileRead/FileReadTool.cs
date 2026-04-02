using ClaudeCode.Core.Tools;
using ClaudeCode.Core.Types;
using ClaudeCode.Constants;
using Microsoft.Extensions.Logging;

namespace ClaudeCode.Tools.FileRead;

public class FileReadTool : ToolBase
{
    private readonly ILogger<FileReadTool> _logger;

    public FileReadTool(ILogger<FileReadTool> logger) { _logger = logger; }

    public override string Name => ToolNames.FileRead;
    public override string Description => "Read the contents of a file at the given path. Use this to understand what's in a file before modifying it.";
    public override bool IsReadonly => true;

    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> input, CancellationToken cancellationToken = default)
    {
        var path = GetString(input, "file_path");
        if (string.IsNullOrEmpty(path))
            return new ToolResult(false, Error: "No file_path provided");

        try
        {
            path = Path.GetFullPath(path);
            if (!File.Exists(path))
                return new ToolResult(false, Error: $"File not found: {path}");

            var fileInfo = new FileInfo(path);
            if (fileInfo.Length > ToolLimits.FileReadMaxBytes)
                return new ToolResult(false, Error: $"File too large: {fileInfo.Length} bytes (max {ToolLimits.FileReadMaxBytes})");

            var content = await File.ReadAllTextAsync(path, cancellationToken);

            if (input.TryGetValue("start_line", out var startLineObj) && input.TryGetValue("end_line", out var endLineObj))
            {
                var lines = content.Split('\n');
                var startLine = Math.Max(0, Convert.ToInt32(startLineObj) - 1);
                var endLine = Math.Min(lines.Length, Convert.ToInt32(endLineObj));
                content = string.Join('\n', lines[startLine..endLine]);
            }

            return new ToolResult(true, Output: content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file: {Path}", path);
            return new ToolResult(false, Error: ex.Message);
        }
    }
}
