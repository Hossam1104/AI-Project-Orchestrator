using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Routing;

namespace AIUsageMonitor.Infrastructure.Persistence;

/// <summary>Persistence-only DTOs for one immutable routing decision.</summary>
public sealed class RoutingDecisionRecord
{
    public int SchemaVersion { get; set; }
    public string RecordType { get; set; } = "routing-decision";
    public Guid ProjectId { get; set; }
    public Guid DecisionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public RoutingInputRecord Input { get; set; } = new();
    public RoutingDecisionOutcome Outcome { get; set; }
    public List<RoutingCandidateAssessmentRecord> CandidateAssessments { get; set; } = [];
    public RoutingRecommendationRecord? OriginalRecommendation { get; set; }
    public RoutingRecommendationRecord? Recommendation { get; set; }
    public Guid? SelectedAgentId { get; set; }
    public RoutingOverrideDisposition OwnerOverrideDisposition { get; set; }
    public RoutingConfidence Confidence { get; set; }
    public List<string> Limitations { get; set; } = [];
    public List<RoutingReasonCode> ReasonCodes { get; set; } = [];
    public string InputFingerprint { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;

    public static RoutingDecisionRecord FromApplication(RoutingDecision value) => new()
    {
        SchemaVersion = value.SchemaVersion,
        ProjectId = value.ProjectId,
        DecisionId = value.DecisionId,
        CreatedAt = value.CreatedAt,
        Input = RoutingInputRecord.FromApplication(value.Input),
        Outcome = value.Outcome,
        CandidateAssessments = value.CandidateAssessments.Select(RoutingCandidateAssessmentRecord.FromApplication).ToList(),
        OriginalRecommendation = value.OriginalRecommendation is null ? null : RoutingRecommendationRecord.FromApplication(value.OriginalRecommendation),
        Recommendation = value.Recommendation is null ? null : RoutingRecommendationRecord.FromApplication(value.Recommendation),
        SelectedAgentId = value.SelectedAgentId,
        OwnerOverrideDisposition = value.OwnerOverrideDisposition,
        Confidence = value.Confidence,
        Limitations = value.Limitations.ToList(),
        ReasonCodes = value.ReasonCodes.ToList(),
        InputFingerprint = value.InputFingerprint,
        ContentHash = value.ContentHash
    };

    public RoutingDecision ToApplicationForIntegrityValidation()
    {
        var input = Input.ToApplication();
        var evaluation = new RoutingEvaluation(
            input,
            Outcome,
            CandidateAssessments.Select(value => value.ToApplication()).ToArray(),
            OriginalRecommendation?.ToApplication(),
            Recommendation?.ToApplication(),
            OwnerOverrideDisposition,
            Confidence,
            Limitations,
            ReasonCodes);
        return new(ProjectId, DecisionId, SchemaVersion, CreatedAt, evaluation);
    }

    public RoutingDecision ToApplication()
    {
        var input = Input.ToApplication();
        var evaluation = new RoutingEvaluation(
            input,
            Outcome,
            CandidateAssessments.Select(value => value.ToApplication()).ToArray(),
            OriginalRecommendation?.ToApplication(),
            Recommendation?.ToApplication(),
            OwnerOverrideDisposition,
            Confidence,
            Limitations,
            ReasonCodes);
        return new(ProjectId, DecisionId, SchemaVersion, CreatedAt, evaluation, ContentHash);
    }
}

public sealed class RoutingInputRecord
{
    public Guid ProjectId { get; set; }
    public PlanningExecutionContractReferenceRecord PlanningContractReference { get; set; } = new();
    public RoutingContextReferenceRecord Context { get; set; } = new();
    public RoutingTaskClassificationRecord Classification { get; set; } = new();
    public RoutingPolicySnapshotRecord Policy { get; set; } = new();
    public List<RoutingAgentSnapshotRecord> Candidates { get; set; } = [];
    public List<RoutingCapacityEvidenceRecord> CapacityEvidence { get; set; } = [];
    public RoutingOwnerOverrideRecord? OwnerOverride { get; set; }
    public DateTimeOffset EvaluatedAt { get; set; }
    public string InputFingerprint { get; set; } = string.Empty;

    public static RoutingInputRecord FromApplication(RoutingInputSnapshot value) => new()
    {
        ProjectId = value.ProjectId,
        PlanningContractReference = PlanningExecutionContractReferenceRecord.FromApplication(value.PlanningContractReference),
        Context = RoutingContextReferenceRecord.FromApplication(value.Context),
        Classification = RoutingTaskClassificationRecord.FromApplication(value.Classification),
        Policy = RoutingPolicySnapshotRecord.FromApplication(value.Policy),
        Candidates = value.Candidates.Select(RoutingAgentSnapshotRecord.FromApplication).ToList(),
        CapacityEvidence = value.CapacityEvidence.Select(RoutingCapacityEvidenceRecord.FromApplication).ToList(),
        OwnerOverride = value.OwnerOverride is null ? null : RoutingOwnerOverrideRecord.FromApplication(value.OwnerOverride),
        EvaluatedAt = value.EvaluatedAt,
        InputFingerprint = value.InputFingerprint
    };

    public RoutingInputSnapshot ToApplication() => new(
        ProjectId,
        PlanningContractReference.ToApplication(),
        Context.ToApplication(),
        Classification.ToApplication(),
        Policy.ToApplication(),
        Candidates.Select(static value => value.ToApplication()).ToArray(),
        CapacityEvidence.Select(static value => value.ToApplication()).ToArray(),
        OwnerOverride?.ToApplication(),
        EvaluatedAt,
        InputFingerprint);
}

public sealed class PlanningExecutionContractReferenceRecord
{
    public Guid ContractId { get; set; }
    public int Revision { get; set; }
    public int SchemaVersion { get; set; }
    public string ContentHash { get; set; } = string.Empty;

    public static PlanningExecutionContractReferenceRecord FromApplication(PlanningExecutionContractReference value) => new()
    {
        ContractId = value.ContractId,
        Revision = value.Revision,
        SchemaVersion = value.SchemaVersion,
        ContentHash = value.ContentHash
    };

    public PlanningExecutionContractReference ToApplication() =>
        new(ContractId, Revision, SchemaVersion, ContentHash);
}

public sealed class RoutingContextReferenceRecord
{
    public Guid ContextId { get; set; }
    public int ContextContractVersion { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static RoutingContextReferenceRecord FromApplication(RoutingContextReference value) => new()
    {
        ContextId = value.ContextId,
        ContextContractVersion = value.ContextContractVersion,
        UpdatedAt = value.UpdatedAt
    };

    public RoutingContextReference ToApplication() => new(ContextId, ContextContractVersion, UpdatedAt);
}

public sealed class RoutingTaskClassificationRecord
{
    public RoutingScopeScale ScopeScale { get; set; }
    public RoutingTaskRisk Risk { get; set; }
    public RoutingBlastRadius BlastRadius { get; set; }
    public RoutingValidationCost ValidationCost { get; set; }
    public AgentRole RequiredRole { get; set; }
    public List<string> RequiredCapabilities { get; set; } = [];
    public List<string> PolicyTags { get; set; } = [];
    public RoutingCapacityRequirement CapacityRequirement { get; set; }
    public bool IndependentReviewRequired { get; set; }
    public bool SecurityReviewRequired { get; set; }
    public bool OwnerApprovalRequired { get; set; }
    public bool RequiresSupportedConnection { get; set; }
    public bool RequiresVerifiedAvailability { get; set; }
    public bool RequiresAuthenticatedAccess { get; set; }
    public bool RequiresVerifiedEntitlement { get; set; }

    public static RoutingTaskClassificationRecord FromApplication(RoutingTaskClassification value) => new()
    {
        ScopeScale = value.ScopeScale,
        Risk = value.Risk,
        BlastRadius = value.BlastRadius,
        ValidationCost = value.ValidationCost,
        RequiredRole = value.RequiredRole,
        RequiredCapabilities = value.RequiredCapabilities.ToList(),
        PolicyTags = value.PolicyTags.ToList(),
        CapacityRequirement = value.CapacityRequirement,
        IndependentReviewRequired = value.IndependentReviewRequired,
        SecurityReviewRequired = value.SecurityReviewRequired,
        OwnerApprovalRequired = value.OwnerApprovalRequired,
        RequiresSupportedConnection = value.RequiresSupportedConnection,
        RequiresVerifiedAvailability = value.RequiresVerifiedAvailability,
        RequiresAuthenticatedAccess = value.RequiresAuthenticatedAccess,
        RequiresVerifiedEntitlement = value.RequiresVerifiedEntitlement
    };

    public RoutingTaskClassification ToApplication() => new(
        ScopeScale, Risk, BlastRadius, ValidationCost, RequiredRole, RequiredCapabilities, PolicyTags,
        CapacityRequirement, IndependentReviewRequired, SecurityReviewRequired, OwnerApprovalRequired,
        RequiresSupportedConnection, RequiresVerifiedAvailability, RequiresAuthenticatedAccess, RequiresVerifiedEntitlement);
}

public sealed class RoutingPolicySnapshotRecord
{
    public string PolicyId { get; set; } = string.Empty;
    public AgentRole RequiredRole { get; set; }
    public List<Guid> PreferredAgentIds { get; set; } = [];
    public List<Guid> ProhibitedAgentIds { get; set; } = [];
    public RoutingCapacityRequirement CapacityRequirement { get; set; }
    public RoutingCapacityState? MinimumCapacityState { get; set; }
    public bool IndependentReviewRequired { get; set; }
    public bool SecurityReviewRequired { get; set; }
    public bool OwnerApprovalRequired { get; set; }
    public bool RequireSupportedConnection { get; set; }
    public bool RequireVerifiedAvailability { get; set; }
    public bool RequireAuthenticatedAccess { get; set; }
    public bool RequireVerifiedEntitlement { get; set; }
    public string? PolicyReference { get; set; }
    public string? Reason { get; set; }

    public static RoutingPolicySnapshotRecord FromApplication(RoutingPolicySnapshot value) => new()
    {
        PolicyId = value.PolicyId,
        RequiredRole = value.RequiredRole,
        PreferredAgentIds = value.PreferredAgentIds.ToList(),
        ProhibitedAgentIds = value.ProhibitedAgentIds.ToList(),
        CapacityRequirement = value.CapacityRequirement,
        MinimumCapacityState = value.MinimumCapacityState,
        IndependentReviewRequired = value.IndependentReviewRequired,
        SecurityReviewRequired = value.SecurityReviewRequired,
        OwnerApprovalRequired = value.OwnerApprovalRequired,
        RequireSupportedConnection = value.RequireSupportedConnection,
        RequireVerifiedAvailability = value.RequireVerifiedAvailability,
        RequireAuthenticatedAccess = value.RequireAuthenticatedAccess,
        RequireVerifiedEntitlement = value.RequireVerifiedEntitlement,
        PolicyReference = value.PolicyReference,
        Reason = value.Reason
    };

    public RoutingPolicySnapshot ToApplication() => new(
        PolicyId, RequiredRole, PreferredAgentIds, ProhibitedAgentIds, CapacityRequirement, MinimumCapacityState,
        IndependentReviewRequired, SecurityReviewRequired, OwnerApprovalRequired, RequireSupportedConnection,
        RequireVerifiedAvailability, RequireAuthenticatedAccess, RequireVerifiedEntitlement, PolicyReference, Reason);
}

public sealed class RoutingAgentSnapshotRecord
{
    public Guid ProjectId { get; set; }
    public Guid AgentId { get; set; }
    public AgentIdentityRecord Identity { get; set; } = new();
    public DateTimeOffset RegistryUpdatedAt { get; set; }
    public bool Enabled { get; set; }
    public List<AgentRole> RoleCapabilities { get; set; } = [];
    public List<string> Capabilities { get; set; } = [];
    public List<string> Limitations { get; set; } = [];
    public AgentConnectionMode ConnectionMode { get; set; }
    public List<AgentConnectionMode> SupportedConnectionModes { get; set; } = [];
    public AgentAvailability Availability { get; set; }
    public AgentAuthenticationState AuthenticationState { get; set; }
    public AgentEntitlementState EntitlementState { get; set; }

    public static RoutingAgentSnapshotRecord FromApplication(RoutingAgentSnapshot value) => new()
    {
        ProjectId = value.ProjectId,
        AgentId = value.AgentId,
        Identity = AgentIdentityRecord.FromApplication(value.Identity),
        RegistryUpdatedAt = value.RegistryUpdatedAt,
        Enabled = value.Enabled,
        RoleCapabilities = value.RoleCapabilities.ToList(),
        Capabilities = value.Capabilities.ToList(),
        Limitations = value.Limitations.ToList(),
        ConnectionMode = value.ConnectionMode,
        SupportedConnectionModes = value.SupportedConnectionModes.ToList(),
        Availability = value.Availability,
        AuthenticationState = value.AuthenticationState,
        EntitlementState = value.EntitlementState
    };

    public RoutingAgentSnapshot ToApplication() => new(
        ProjectId, AgentId, Identity.ToApplication(), RegistryUpdatedAt, Enabled, RoleCapabilities, Capabilities,
        Limitations, ConnectionMode, SupportedConnectionModes, Availability, AuthenticationState, EntitlementState);
}

public sealed class AgentIdentityRecord
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public string? ModelIdentifier { get; set; }

    public static AgentIdentityRecord FromApplication(AgentIdentity value) => new()
    {
        Id = value.Id,
        DisplayName = value.DisplayName,
        Provider = value.Provider,
        ModelIdentifier = value.ModelIdentifier
    };

    public AgentIdentity ToApplication() => new(Id, DisplayName, Provider, ModelIdentifier);
}

public sealed class RoutingCapacityEvidenceRecord
{
    public Guid AgentId { get; set; }
    public RoutingCapacityState CapacityState { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public DateTimeOffset? ValidUntil { get; set; }
    public string? EvidenceReference { get; set; }
    public Guid? ProviderId { get; set; }
    public Guid? QuotaDefinitionId { get; set; }
    public double? RemainingFraction { get; set; }
    public RoutingCapacityEvidenceSource Source { get; set; }

    public static RoutingCapacityEvidenceRecord FromApplication(RoutingCapacityEvidence value) => new()
    {
        AgentId = value.AgentId,
        CapacityState = value.CapacityState,
        ObservedAt = value.ObservedAt,
        ValidUntil = value.ValidUntil,
        EvidenceReference = value.EvidenceReference,
        ProviderId = value.ProviderId,
        QuotaDefinitionId = value.QuotaDefinitionId,
        RemainingFraction = value.RemainingFraction,
        Source = value.Source
    };

    public RoutingCapacityEvidence ToApplication() => new(
        AgentId, CapacityState, ObservedAt, ValidUntil, EvidenceReference, ProviderId,
        QuotaDefinitionId, RemainingFraction, Source);
}

public sealed class RoutingOwnerOverrideRecord
{
    public Guid RequestedAgentId { get; set; }
    public string ActorReference { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset RequestedAt { get; set; }

    public static RoutingOwnerOverrideRecord FromApplication(RoutingOwnerOverrideRequest value) => new()
    {
        RequestedAgentId = value.RequestedAgentId,
        ActorReference = value.ActorReference,
        Reason = value.Reason,
        RequestedAt = value.RequestedAt
    };

    public RoutingOwnerOverrideRequest ToApplication() =>
        new(RequestedAgentId, ActorReference, Reason, RequestedAt);
}

public sealed class RoutingCandidateAssessmentRecord
{
    public RoutingAgentSnapshotRecord Candidate { get; set; } = new();
    public RoutingCapacityState CapacityState { get; set; }
    public int? PreferencePosition { get; set; }
    public bool IsHardEligible { get; set; }
    public bool IsEligible { get; set; }
    public List<string> MissingCapabilities { get; set; } = [];
    public List<RoutingReasonCode> Reasons { get; set; } = [];

    public static RoutingCandidateAssessmentRecord FromApplication(RoutingCandidateAssessment value) => new()
    {
        Candidate = RoutingAgentSnapshotRecord.FromApplication(value.Candidate),
        CapacityState = value.CapacityState,
        PreferencePosition = value.PreferencePosition,
        IsHardEligible = value.IsHardEligible,
        IsEligible = value.IsEligible,
        MissingCapabilities = value.MissingCapabilities.ToList(),
        Reasons = value.Reasons.ToList()
    };

    public RoutingCandidateAssessment ToApplication() => new(
        Candidate.ToApplication(), CapacityState, PreferencePosition, IsHardEligible, IsEligible,
        MissingCapabilities, Reasons);
}

public sealed class RoutingRecommendationRecord
{
    public Guid SelectedAgentId { get; set; }
    public AgentIdentityRecord SelectedAgentIdentity { get; set; } = new();
    public AgentRole RequestedRole { get; set; }
    public List<RoutingReasonCode> Reasons { get; set; } = [];
    public int? PreferencePosition { get; set; }
    public RoutingCapacityState CapacityState { get; set; }
    public bool IndependentReviewRequired { get; set; }
    public bool SecurityReviewRequired { get; set; }
    public bool OwnerApprovalRequired { get; set; }
    public RoutingConfidence Confidence { get; set; }
    public List<string> Limitations { get; set; } = [];
    public string NextSafeAction { get; set; } = string.Empty;

    public static RoutingRecommendationRecord FromApplication(RoutingRecommendation value) => new()
    {
        SelectedAgentId = value.SelectedAgentId,
        SelectedAgentIdentity = AgentIdentityRecord.FromApplication(value.SelectedAgentIdentity),
        RequestedRole = value.RequestedRole,
        Reasons = value.Reasons.ToList(),
        PreferencePosition = value.PreferencePosition,
        CapacityState = value.CapacityState,
        IndependentReviewRequired = value.IndependentReviewRequired,
        SecurityReviewRequired = value.SecurityReviewRequired,
        OwnerApprovalRequired = value.OwnerApprovalRequired,
        Confidence = value.Confidence,
        Limitations = value.Limitations.ToList(),
        NextSafeAction = value.NextSafeAction
    };

    public RoutingRecommendation ToApplication() => new(
        SelectedAgentId, SelectedAgentIdentity.ToApplication(), RequestedRole, Reasons, PreferencePosition,
        CapacityState, IndependentReviewRequired, SecurityReviewRequired, OwnerApprovalRequired, Confidence,
        Limitations, NextSafeAction);
}
