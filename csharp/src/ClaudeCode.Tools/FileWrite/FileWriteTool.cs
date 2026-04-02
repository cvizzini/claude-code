using ClaudeCode.Core.Tools;
using ClaudeCode.Core.Types;
using ClaudeCode.Constants;
using Microsoft.Extensions.Logging;

namespace ClaudeCode.Tools.FileWrite;

public class FileWriteTool : ToolBase
{
    private readonly ILogger<FileWriteTool> _logger;

    public FileWriteTool(ILogger<FileWriteTool> logger) { _logger = logger; }

    public override string Name => ToolNames.FileWrite;
    public override string Description => "Write content to a file. Creates the file if it doesn't exist, overwrites if it does.";
    public override bool IsReadonly => false;

    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> input, CancellationToken cancellationToken = default)
    {
        var path = GetString(input, "file_path");
        var content = GetString(input, "content");

        if (string.IsNullOrEmpty(path))
            return new ToolResult(false, Error: "No file_path provided");

        try
        {
            path = Path.GetFullPath(path);
            var dir = Path.GetDirectoryName(path);
            if (dir != null) Directory.CreateDirectory(dir);

            await File.WriteAllTextAsync(path, content, cancellationToken);
            return new ToolResult(true, Output: $"File written to {path}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing file: {Path}", path);
            return new ToolResult(false, Error: ex.Message);
        }
    }
}
