using ClaudeCode.Core.Tools;
using ClaudeCode.Core.Types;
using ClaudeCode.Constants;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace ClaudeCode.Tools.Grep;

public class GrepTool : ToolBase
{
    private readonly ILogger<GrepTool> _logger;

    public GrepTool(ILogger<GrepTool> logger) { _logger = logger; }

    public override string Name => ToolNames.Grep;
    public override string Description => "Search for a pattern in files using regular expressions.";
    public override bool IsReadonly => true;

    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> input, CancellationToken cancellationToken = default)
    {
        var pattern = GetString(input, "pattern");
        var path = GetString(input, "path", Directory.GetCurrentDirectory());
        var include = GetString(input, "include", "");
        var ignoreCase = GetBool(input, "ignore_case");

        if (string.IsNullOrEmpty(pattern))
            return new ToolResult(false, Error: "No pattern provided");

        try
        {
            path = Path.GetFullPath(path);
            var regexOptions = RegexOptions.Multiline;
            if (ignoreCase) regexOptions |= RegexOptions.IgnoreCase;

            var regex = new Regex(pattern, regexOptions);
            var results = new List<string>();

            IEnumerable<string> files;
            if (File.Exists(path))
            {
                files = [path];
            }
            else if (Directory.Exists(path))
            {
                var searchPattern = string.IsNullOrEmpty(include) ? "*" : include;
                files = Directory.EnumerateFiles(path, searchPattern, SearchOption.AllDirectories);
            }
            else
            {
                return new ToolResult(false, Error: $"Path not found: {path}");
            }

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (results.Count >= ToolLimits.GrepMaxResults) break;
                try
                {
                    var content = await File.ReadAllTextAsync(file, cancellationToken);
                    var lines = content.Split('\n');
                    for (int i = 0; i < lines.Length && results.Count < ToolLimits.GrepMaxResults; i++)
                    {
                        if (regex.IsMatch(lines[i]))
                            results.Add($"{file}:{i + 1}:{lines[i]}");
                    }
                }
                catch (Exception) { /* skip unreadable files */ }
            }

            return new ToolResult(true, Output: string.Join('\n', results));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in grep: {Pattern}", pattern);
            return new ToolResult(false, Error: ex.Message);
        }
    }
}
