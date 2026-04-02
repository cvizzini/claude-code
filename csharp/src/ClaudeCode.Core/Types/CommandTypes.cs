namespace ClaudeCode.Core.Types;

public enum CommandAvailability { ClaudeAi, Console }

public enum ResumeEntrypoint { CliFFlag, SlashCommandPicker, SlashCommandSessionId, SlashCommandTitle, Fork }

public enum CommandResultDisplay { Skip, System, User }

public enum CommandLoadedFrom { CommandsDeprecated, Skills, Plugin, Managed, Bundled, Mcp }

public record CommandBase(
    string Name,
    string Description,
    string[]? Aliases = null,
    CommandAvailability[]? Availability = null,
    bool HasUserSpecifiedDescription = false,
    bool IsHidden = false,
    bool IsMcp = false,
    string? ArgumentHint = null,
    string? WhenToUse = null,
    string? Version = null,
    bool DisableModelInvocation = false,
    bool UserInvocable = false,
    CommandLoadedFrom? LoadedFrom = null,
    string? Kind = null,
    bool Immediate = false,
    bool IsSensitive = false
)
{
    public virtual string GetUserFacingName() => Name;
    public virtual bool GetIsEnabled() => true;
}

public record PromptCommand(
    string Name,
    string Description,
    string ProgressMessage,
    int ContentLength,
    string Source,
    string[]? ArgNames = null,
    string[]? AllowedTools = null,
    string? Model = null,
    bool DisableNonInteractive = false,
    string? Context = null,
    string? Agent = null,
    string[]? Paths = null
) : CommandBase(Name, Description);

public record LocalCommandResult;
public record TextCommandResult(string Value) : LocalCommandResult;
public record SkipCommandResult : LocalCommandResult;
