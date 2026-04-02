namespace ClaudeCode.Core.Types.Generated;

public record GrowthbookExperimentEvent(
    string? EventId = null,
    DateTime? Timestamp = null,
    string? ExperimentId = null,
    int? VariationId = null,
    string? Environment = null,
    string? UserAttributes = null,
    string? ExperimentMetadata = null,
    string? DeviceId = null,
    PublicApiAuth? Auth = null,
    string? SessionId = null,
    string? AnonymousId = null,
    string? EventMetadataVars = null
);
