using ClaudeCode.Core.Tools;
using ClaudeCode.Core.Types;
using ClaudeCode.Constants;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.Logging;

namespace ClaudeCode.Tools.Glob;

public class GlobTool : ToolBase
{
    private readonly ILogger<GlobTool> _logger;

    public GlobTool(ILogger<GlobTool> logger) { _logger = logger; }

    public override string Name => ToolNames.Glob;
    public override string Description => "Find files matching a glob pattern. Returns a list of file paths.";
    public override bool IsReadonly => true;

    public override Task<ToolResult> ExecuteAsync(Dictionary<string, object> input, CancellationToken cancellationToken = default)
    {
        var pattern = GetString(input, "pattern");
        var path = GetString(input, "path", Directory.GetCurrentDirectory());

        if (string.IsNullOrEmpty(pattern))
            return Task.FromResult(new ToolResult(false, Error: "No pattern provided"));

        try
        {
            path = Path.GetFullPath(path);
            if (!Directory.Exists(path))
                return Task.FromResult(new ToolResult(false, Error: $"Directory not found: {path}"));

            var matcher = new Matcher();
            matcher.AddInclude(pattern);

            var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(path)));
            var files = result.Files
                .Select(f => Path.Combine(path, f.Path))
                .OrderBy(f => f)
                .Take(ToolLimits.GlobMaxResults)
                .ToList();

            return Task.FromResult(new ToolResult(true, Output: string.Join('\n', files)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in glob: {Pattern}", pattern);
            return Task.FromResult(new ToolResult(false, Error: ex.Message));
        }
    }
}
