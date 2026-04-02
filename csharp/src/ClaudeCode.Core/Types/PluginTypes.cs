namespace ClaudeCode.Core.Types;

public record PluginRepository(
    string Url,
    string Branch,
    string? LastUpdated = null,
    string? CommitSha = null
);

public record PluginConfig(Dictionary<string, PluginRepository> Repositories);

public record LoadedPlugin(
    string Name,
    string Path,
    string Source,
    string Repository,
    bool? Enabled = null,
    bool IsBuiltin = false,
    string? Sha = null,
    string? CommandsPath = null,
    string? AgentsPath = null,
    string? SkillsPath = null,
    string? OutputStylesPath = null
);

public enum PluginComponent { Commands, Agents, Skills, Hooks, OutputStyles }

public abstract record PluginError(string Kind);
public record GenericPluginError(string Message) : PluginError("generic-error");
public record PluginNotFoundError(string PluginName) : PluginError("plugin-not-found");

public record BuiltinPluginDefinition(
    string Name,
    string Description,
    string? Version = null,
    bool DefaultEnabled = true
);
