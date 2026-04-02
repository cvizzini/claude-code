using ClaudeCode.Core.Tools;
using ClaudeCode.Core.Types;
using ClaudeCode.Constants;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace ClaudeCode.Tools.Bash;

public class BashTool : ToolBase
{
    private readonly ILogger<BashTool> _logger;

    public BashTool(ILogger<BashTool> logger)
    {
        _logger = logger;
    }

    public override string Name => ToolNames.Bash;
    public override string Description => "Execute a bash command in a persistent shell session. Use for running shell commands, scripts, and system operations. Note: interactive commands or commands that need user input are not supported.";
    public override bool IsReadonly => false;

    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> input, CancellationToken cancellationToken = default)
    {
        var command = GetString(input, "command");
        if (string.IsNullOrEmpty(command))
            return new ToolResult(false, Error: "No command provided");

        var timeoutMs = input.TryGetValue("timeout", out var t) ? Convert.ToInt32(t) : ToolLimits.BashDefaultTimeoutMs;
        timeoutMs = Math.Min(timeoutMs, ToolLimits.BashMaxTimeoutMs);

        try
        {
            return await RunCommandAsync(command, timeoutMs, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return new ToolResult(false, Error: "Command was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing bash command: {Command}", command);
            return new ToolResult(false, Error: ex.Message);
        }
    }

    private async Task<ToolResult> RunCommandAsync(string command, int timeoutMs, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeoutMs);

        var psi = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        if (OperatingSystem.IsWindows())
        {
            psi.FileName = "cmd.exe";
            psi.Arguments = $"/c {command}";
        }
        else
        {
            psi.FileName = "/bin/bash";
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add(command);
        }

        using var process = new Process { StartInfo = psi };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            return new ToolResult(false, Error: $"Command timed out after {timeoutMs}ms");
        }

        var output = stdout.ToString();
        var errOutput = stderr.ToString();

        var combined = string.IsNullOrEmpty(errOutput)
            ? output
            : output + "\n" + errOutput;

        if (process.ExitCode != 0)
            return new ToolResult(false, Output: combined.TrimEnd(), Error: $"Command exited with code {process.ExitCode}");

        return new ToolResult(true, Output: combined.TrimEnd());
    }
}
