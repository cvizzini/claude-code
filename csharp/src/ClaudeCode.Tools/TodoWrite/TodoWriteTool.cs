using ClaudeCode.Core.Tools;
using ClaudeCode.Core.Types;
using ClaudeCode.Constants;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ClaudeCode.Tools.TodoWrite;

public record TodoItem(string Id, string Content, string Status, string Priority);

public class TodoWriteTool : ToolBase
{
    private readonly ILogger<TodoWriteTool> _logger;
    private readonly object _lock = new();
    private List<TodoItem> _todos = new();

    public TodoWriteTool(ILogger<TodoWriteTool> logger) { _logger = logger; }

    public override string Name => ToolNames.TodoWrite;
    public override string Description => "Write a list of todos to track task progress. Each todo has an id, content, status (pending/in_progress/completed), and priority (high/medium/low).";
    public override bool IsReadonly => false;

    public override Task<ToolResult> ExecuteAsync(Dictionary<string, object> input, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!input.TryGetValue("todos", out var todosObj))
                return Task.FromResult(new ToolResult(false, Error: "No todos provided"));

            var json = todosObj?.ToString() ?? "[]";
            var todos = JsonSerializer.Deserialize<List<TodoItem>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<TodoItem>();

            if (todos.Count > ToolLimits.TodoMaxItems)
                return Task.FromResult(new ToolResult(false, Error: $"Too many todos: {todos.Count} (max {ToolLimits.TodoMaxItems})"));

            lock (_lock) { _todos = todos; }
            return Task.FromResult(new ToolResult(true, Output: $"Todos updated: {todos.Count} item(s)"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating todos");
            return Task.FromResult(new ToolResult(false, Error: ex.Message));
        }
    }

    public IReadOnlyList<TodoItem> GetTodos() { lock (_lock) { return _todos.AsReadOnly(); } }
}
