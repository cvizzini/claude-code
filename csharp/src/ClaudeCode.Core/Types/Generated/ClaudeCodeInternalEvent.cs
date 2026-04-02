using ClaudeCode.Core.Types.Generated;

namespace ClaudeCode.Core.Types.Generated;

public record GitHubActionsMetadata(
    string? ActorId = null,
    string? RepositoryId = null,
    string? RepositoryOwnerId = null
);

public record EnvironmentMetadata(
    string? Platform = null,
    string? NodeVersion = null,
    string? Terminal = null,
    string? PackageManagers = null,
    string? Runtimes = null,
    bool? IsRunningWithBun = null,
    bool? IsCi = null,
    bool? IsClaubbit = null,
    bool? IsGithubAction = null,
    bool? IsClaudeCodeAction = null,
    bool? IsClaudeAiAuth = null,
    string? Version = null,
    string? GithubEventName = null,
    string? GithubActionsRunnerEnvironment = null,
    string? GithubActionsRunnerOs = null,
    string? GithubActionRef = null,
    string? WslVersion = null,
    GitHubActionsMetadata? GithubActionsMetadata = null,
    string? Arch = null,
    bool? IsClaudeCodeRemote = null,
    string? RemoteEnvironmentType = null,
    string? ClaudeCodeContainerId = null,
    string? ClaudeCodeRemoteSessionId = null,
    string[]? Tags = null,
    string? DeploymentEnvironment = null,
    bool? IsConductor = null,
    string? VersionBase = null,
    string? CoworkerType = null,
    string? BuildTime = null,
    bool? IsLocalAgentMode = null,
    string? LinuxDistroId = null,
    string? LinuxDistroVersion = null,
    string? LinuxKernel = null,
    string? Vcs = null,
    string? PlatformRaw = null
);

public record SlackContext(
    string? SlackTeamId = null,
    bool? IsEnterpriseInstall = null,
    string? Trigger = null,
    string? CreationMethod = null
);

public record ClaudeCodeInternalEvent(
    string? EventName = null,
    DateTime? ClientTimestamp = null,
    string? Model = null,
    string? SessionId = null,
    string? UserType = null,
    string? Betas = null,
    EnvironmentMetadata? Env = null,
    string? Entrypoint = null,
    string? AgentSdkVersion = null,
    bool? IsInteractive = null,
    string? ClientType = null,
    string? Process = null
);
