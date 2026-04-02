namespace ClaudeCode.Core.Types.Generated;

/// <summary>PublicApiAuth contains authentication context automatically injected by the API.</summary>
public record PublicApiAuth(
    long? AccountId = null,
    string? OrganizationUuid = null,
    string? AccountUuid = null
);
