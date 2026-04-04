using ClaudeCode.Core.Tools;
using ClaudeCode.Tools.Bash;
using ClaudeCode.Tools.FileEdit;
using ClaudeCode.Tools.FileRead;
using ClaudeCode.Tools.FileWrite;
using ClaudeCode.Tools.Glob;
using ClaudeCode.Tools.Grep;
using ClaudeCode.Tools.ListDirectory;
using ClaudeCode.Tools.TodoWrite;
using ClaudeCode.Tools.WebFetch;
using Microsoft.Extensions.DependencyInjection;

namespace ClaudeCode.Tools;

public interface IToolRegistry
{
    IEnumerable<ITool> GetAllTools();
    ITool? GetTool(string name);
    void RegisterTool(ITool tool);
}

public class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<ITool> GetAllTools() => _tools.Values;

    public ITool? GetTool(string name) => _tools.TryGetValue(name, out var tool) ? tool : null;

    public void RegisterTool(ITool tool) => _tools[tool.Name] = tool;
}

public static class ToolRegistryExtensions
{
    public static IServiceCollection AddClaudeTools(this IServiceCollection services)
    {
        services.AddSingleton<IToolRegistry, ToolRegistry>();
        services.AddSingleton<BashTool>();
      services.AddSingleton<ListDirectoryTool>();
        services.AddSingleton<FileReadTool>();
        services.AddSingleton<FileWriteTool>();
        services.AddSingleton<FileEditTool>();
        services.AddSingleton<GlobTool>();
        services.AddSingleton<GrepTool>();
        services.AddSingleton<WebFetchTool>();
        services.AddSingleton<TodoWriteTool>();
        return services;
    }

    public static IServiceProvider PopulateToolRegistry(this IServiceProvider serviceProvider)
    {
        var registry = serviceProvider.GetRequiredService<IToolRegistry>();
        registry.RegisterTool(serviceProvider.GetRequiredService<BashTool>());
      registry.RegisterTool(serviceProvider.GetRequiredService<ListDirectoryTool>());
        registry.RegisterTool(serviceProvider.GetRequiredService<FileReadTool>());
        registry.RegisterTool(serviceProvider.GetRequiredService<FileWriteTool>());
        registry.RegisterTool(serviceProvider.GetRequiredService<FileEditTool>());
        registry.RegisterTool(serviceProvider.GetRequiredService<GlobTool>());
        registry.RegisterTool(serviceProvider.GetRequiredService<GrepTool>());
        registry.RegisterTool(serviceProvider.GetRequiredService<WebFetchTool>());
        registry.RegisterTool(serviceProvider.GetRequiredService<TodoWriteTool>());
        return serviceProvider;
    }
}
