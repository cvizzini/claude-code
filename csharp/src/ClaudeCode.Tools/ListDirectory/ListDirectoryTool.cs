using ClaudeCode.Constants;
using ClaudeCode.Core.Tools;
using ClaudeCode.Core.Types;
using Microsoft.Extensions.Logging;

namespace ClaudeCode.Tools.ListDirectory;

public class ListDirectoryTool : ToolBase
{
    private readonly ILogger<ListDirectoryTool> _logger;

    public ListDirectoryTool(ILogger<ListDirectoryTool> logger)
    {
        _logger = logger;
    }

    public override string Name => ToolNames.ListDirectory;
    public override string Description => "List files and directories at a path. Use this to inspect a folder before reading or writing files.";
    public override bool IsReadonly => true;

    public override Task<ToolResult> ExecuteAsync(Dictionary<string, object> input, CancellationToken cancellationToken = default)
    {
        var path = GetString(input, "path", Directory.GetCurrentDirectory());
        var recursive = GetBool(input, "recursive");
        var maxDepth = input.TryGetValue("max_depth", out var maxDepthValue)
            ? Math.Max(1, Convert.ToInt32(maxDepthValue))
            : recursive ? 3 : 1;

        try
        {
            path = Path.GetFullPath(path);

            if (File.Exists(path))
            {
                return Task.FromResult(new ToolResult(true, Output: $"[F] {path}"));
            }

            if (!Directory.Exists(path))
            {
                return Task.FromResult(new ToolResult(false, Error: $"Path not found: {path}"));
            }

            var entries = new List<string>();
            Traverse(path, path, 0, recursive, maxDepth, entries, cancellationToken);

            if (entries.Count == 0)
            {
                entries.Add("(empty)");
            }

            return Task.FromResult(new ToolResult(true, Output: string.Join('\n', entries)));
        }
        catch (OperationCanceledException)
        {
            return Task.FromResult(new ToolResult(false, Error: "Directory listing was cancelled"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing directory: {Path}", path);
            return Task.FromResult(new ToolResult(false, Error: ex.Message));
        }
    }

    private static void Traverse(
        string rootPath,
        string currentPath,
        int depth,
        bool recursive,
        int maxDepth,
        List<string> entries,
        CancellationToken cancellationToken)
    {
        if (entries.Count >= ToolLimits.GlobMaxResults)
        {
            return;
        }

        foreach (var directory in Directory.EnumerateDirectories(currentPath).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            entries.Add($"[D] {FormatPath(rootPath, directory)}");

            if (entries.Count >= ToolLimits.GlobMaxResults)
            {
                return;
            }

            if (recursive && depth + 1 < maxDepth)
            {
                Traverse(rootPath, directory, depth + 1, recursive, maxDepth, entries, cancellationToken);
                if (entries.Count >= ToolLimits.GlobMaxResults)
                {
                    return;
                }
            }
        }

        foreach (var file in Directory.EnumerateFiles(currentPath).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            entries.Add($"[F] {FormatPath(rootPath, file)}");
            if (entries.Count >= ToolLimits.GlobMaxResults)
            {
                return;
            }
        }
    }

    private static string FormatPath(string rootPath, string path)
    {
        if (string.Equals(rootPath, path, StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        var relativePath = Path.GetRelativePath(rootPath, path);
        return relativePath == "." ? path : relativePath;
    }
}
