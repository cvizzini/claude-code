namespace ClaudeCode.Core.Types;

public enum PermissionBehavior { Allow, Deny, Ask, Passthrough }

public enum PermissionMode { AcceptEdits, BypassPermissions, Default, DontAsk, Plan, Auto, Bubble }

public enum PermissionRuleSource
{
    UserSettings, ProjectSettings, LocalSettings, FlagSettings,
    PolicySettings, CliArg, Command, Session
}

public record PermissionRuleValue(string ToolName, string? RuleContent = null);

public record PermissionRule(PermissionRuleSource Source, PermissionBehavior RuleBehavior, PermissionRuleValue RuleValue);

public enum PermissionUpdateDestination { UserSettings, ProjectSettings, LocalSettings, Session, CliArg }

public abstract record PermissionUpdate;
public record AddRulesUpdate(PermissionUpdateDestination Destination, List<PermissionRuleValue> Rules, PermissionBehavior Behavior) : PermissionUpdate;
public record ReplaceRulesUpdate(PermissionUpdateDestination Destination, List<PermissionRuleValue> Rules, PermissionBehavior Behavior) : PermissionUpdate;
public record RemoveRulesUpdate(PermissionUpdateDestination Destination, List<PermissionRuleValue> Rules, PermissionBehavior Behavior) : PermissionUpdate;
public record SetModeUpdate(PermissionUpdateDestination Destination, PermissionMode Mode) : PermissionUpdate;
public record AddDirectoriesUpdate(PermissionUpdateDestination Destination, List<string> Directories) : PermissionUpdate;
public record RemoveDirectoriesUpdate(PermissionUpdateDestination Destination, List<string> Directories) : PermissionUpdate;

public record AdditionalWorkingDirectory(string Path, PermissionRuleSource Source);

public abstract record PermissionDecision;
public record AllowDecision(Dictionary<string, object>? UpdatedInput = null, bool UserModified = false) : PermissionDecision;
public record DenyDecision(string Message) : PermissionDecision;
public record AskDecision(string Message, Dictionary<string, object>? UpdatedInput = null) : PermissionDecision;

public record PermissionResult(PermissionBehavior Behavior, string? Message = null);

public enum RiskLevel { Low, Medium, High }

public record ClassifierResult(bool Matches, string? MatchedDescription, string Confidence, string Reason);
