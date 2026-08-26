using AIUsageMonitor.Application.Handoffs;
using AIUsageMonitor.Application.Planning;

namespace AIUsageMonitor.Infrastructure.Persistence;

/// <summary>Persistence-only shape for one immutable structured handoff package.</summary>
internal sealed class HandoffPackageRecord
{
    public string RecordType { get; set; } = "handoff-package";
    public Guid ProjectId { get; set; }
    public Guid PackageId { get; set; }
    public int SchemaVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public HandoffTransition Transition { get; set; }
    public HandoffRole SourceRole { get; set; }
    public HandoffRole TargetRole { get; set; }
    public HandoffPlanningContractReferenceRecord? PlanningContractReference { get; set; }
    public PlanningWorkItemRecord? WorkItem { get; set; }
    public HandoffContextReferenceRecord? Context { get; set; }
    public PlanningRepositoryTargetRecord? RepositoryTarget { get; set; }
    public HandoffWorkGraphReferenceRecord? WorkGraphReference { get; set; }
    public Guid? WorkGraphNodeId { get; set; }
    public HandoffPackageReferenceRecord? PreviousPackageReference { get; set; }
    public HandoffExecutionScopeRecord? ExecutionScope { get; set; }
    public HandoffReviewScopeRecord? ReviewScope { get; set; }
    public HandoffRemediationScopeRecord? RemediationScope { get; set; }
    public HandoffAcceptanceScopeRecord? AcceptanceScope { get; set; }
    public List<HandoffEvidenceReferenceRecord>? EvidenceReferences { get; set; }
    public List<HandoffFindingReferenceRecord>? FindingReferences { get; set; }
    public List<HandoffChangedArtifactReferenceRecord>? ChangedArtifactReferences { get; set; }
    public HandoffOutcomeMetadataRecord? Outcome { get; set; }
    public List<string>? Limitations { get; set; }
    public string? NextAction { get; set; }
    public HandoffRedactionMetadataRecord? Redaction { get; set; }
    public HandoffPackageSizeMetadataRecord? Size { get; set; }
    public string? ContentHash { get; set; }

    public static HandoffPackageRecord FromApplication(HandoffPackage value) => new()
    {
        RecordType = "handoff-package",
        ProjectId = value.ProjectId,
        PackageId = value.PackageId,
        SchemaVersion = value.SchemaVersion,
        CreatedAt = value.CreatedAt,
        Transition = value.Transition,
        SourceRole = value.SourceRole,
        TargetRole = value.TargetRole,
        PlanningContractReference = HandoffPlanningContractReferenceRecord.FromApplication(value.PlanningContractReference),
        WorkItem = PlanningWorkItemRecord.FromApplication(value.WorkItem),
        Context = HandoffContextReferenceRecord.FromApplication(value.Context),
        RepositoryTarget = PlanningRepositoryTargetRecord.FromApplication(value.RepositoryTarget),
        WorkGraphReference = value.WorkGraphReference is null ? null : HandoffWorkGraphReferenceRecord.FromApplication(value.WorkGraphReference),
        WorkGraphNodeId = value.WorkGraphNodeId,
        PreviousPackageReference = value.PreviousPackageReference is null ? null : HandoffPackageReferenceRecord.FromApplication(value.PreviousPackageReference),
        ExecutionScope = value.ExecutionScope is null ? null : HandoffExecutionScopeRecord.FromApplication(value.ExecutionScope),
        ReviewScope = value.ReviewScope is null ? null : HandoffReviewScopeRecord.FromApplication(value.ReviewScope),
        RemediationScope = value.RemediationScope is null ? null : HandoffRemediationScopeRecord.FromApplication(value.RemediationScope),
        AcceptanceScope = value.AcceptanceScope is null ? null : HandoffAcceptanceScopeRecord.FromApplication(value.AcceptanceScope),
        EvidenceReferences = value.EvidenceReferences.Select(HandoffEvidenceReferenceRecord.FromApplication).ToList(),
        FindingReferences = value.FindingReferences.Select(HandoffFindingReferenceRecord.FromApplication).ToList(),
        ChangedArtifactReferences = value.ChangedArtifactReferences.Select(HandoffChangedArtifactReferenceRecord.FromApplication).ToList(),
        Outcome = value.Outcome is null ? null : HandoffOutcomeMetadataRecord.FromApplication(value.Outcome),
        Limitations = value.Limitations.ToList(),
        NextAction = value.NextAction,
        Redaction = HandoffRedactionMetadataRecord.FromApplication(value.Redaction),
        Size = HandoffPackageSizeMetadataRecord.FromApplication(value.Size),
        ContentHash = value.ContentHash
    };

    public HandoffPackage ToApplication() => new(
        ProjectId,
        PackageId,
        SchemaVersion,
        CreatedAt,
        Transition,
        SourceRole,
        TargetRole,
        (PlanningContractReference ?? throw new ArgumentException("Handoff planning contract reference is missing.")).ToApplication(),
        (WorkItem ?? throw new ArgumentException("Handoff work item is missing.")).ToApplication(),
        (Context ?? throw new ArgumentException("Handoff context is missing.")).ToApplication(),
        (RepositoryTarget ?? throw new ArgumentException("Handoff repository target is missing.")).ToApplication(),
        WorkGraphReference?.ToApplication(),
        WorkGraphNodeId,
        PreviousPackageReference?.ToApplication(),
        ExecutionScope?.ToApplication(),
        ReviewScope?.ToApplication(),
        RemediationScope?.ToApplication(),
        AcceptanceScope?.ToApplication(),
        (EvidenceReferences ?? throw new ArgumentException("Handoff evidence references are missing.")).Select(static value => value.ToApplication()).ToArray(),
        (FindingReferences ?? throw new ArgumentException("Handoff finding references are missing.")).Select(static value => value.ToApplication()).ToArray(),
        (ChangedArtifactReferences ?? throw new ArgumentException("Handoff changed artifacts are missing.")).Select(static value => value.ToApplication()).ToArray(),
        Outcome?.ToApplication(),
        Limitations ?? throw new ArgumentException("Handoff limitations are missing."),
        NextAction ?? throw new ArgumentException("Handoff next action is missing."),
        (Redaction ?? throw new ArgumentException("Handoff redaction metadata is missing.")).ToApplication(),
        (Size ?? throw new ArgumentException("Handoff size metadata is missing.")).ToApplication(),
        ContentHash);
}

internal sealed class HandoffPlanningContractReferenceRecord
{
    public Guid ContractId { get; set; }
    public int Revision { get; set; }
    public int SchemaVersion { get; set; }
    public string ContentHash { get; set; } = string.Empty;

    public static HandoffPlanningContractReferenceRecord FromApplication(PlanningExecutionContractReference value) => new()
    {
        ContractId = value.ContractId,
        Revision = value.Revision,
        SchemaVersion = value.SchemaVersion,
        ContentHash = value.ContentHash
    };

    public PlanningExecutionContractReference ToApplication() => new(ContractId, Revision, SchemaVersion, ContentHash);
}

internal sealed class HandoffPackageReferenceRecord
{
    public Guid PackageId { get; set; }
    public int SchemaVersion { get; set; }
    public string ContentHash { get; set; } = string.Empty;

    public static HandoffPackageReferenceRecord FromApplication(HandoffPackageReference value) => new()
    {
        PackageId = value.PackageId,
        SchemaVersion = value.SchemaVersion,
        ContentHash = value.ContentHash
    };

    public HandoffPackageReference ToApplication() => new(PackageId, SchemaVersion, ContentHash);
}

internal sealed class HandoffContextReferenceRecord
{
    public Guid ContextId { get; set; }
    public int ContextContractVersion { get; set; }
    public DateTimeOffset? CapturedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public static HandoffContextReferenceRecord FromApplication(HandoffContextReference value) => new()
    {
        ContextId = value.ContextId,
        ContextContractVersion = value.ContextContractVersion,
        CapturedAt = value.CapturedAt,
        UpdatedAt = value.UpdatedAt
    };

    public HandoffContextReference ToApplication() => new(ContextId, ContextContractVersion, CapturedAt, UpdatedAt);
}

internal sealed class HandoffWorkGraphReferenceRecord
{
    public Guid GraphId { get; set; }
    public int SchemaVersion { get; set; }
    public string ContentHash { get; set; } = string.Empty;

    public static HandoffWorkGraphReferenceRecord FromApplication(AIUsageMonitor.Application.Orchestration.WorkGraphReference value) => new()
    {
        GraphId = value.GraphId,
        SchemaVersion = value.SchemaVersion,
        ContentHash = value.ContentHash
    };

    public AIUsageMonitor.Application.Orchestration.WorkGraphReference ToApplication() => new(GraphId, SchemaVersion, ContentHash);
}

internal sealed class HandoffExecutionScopeRecord
{
    public List<PlanningScopeClauseRecord>? IncludedScope { get; set; }
    public List<PlanningScopeClauseRecord>? Constraints { get; set; }
    public List<PlanningScopeClauseRecord>? ForbiddenScope { get; set; }
    public List<PlanningDeliverableRecord>? Deliverables { get; set; }
    public List<PlanningValidationRequirementRecord>? ValidationRequirements { get; set; }
    public List<PlanningExecutionBudgetRecord>? ExecutionBudgets { get; set; }
    public List<PlanningStopConditionRecord>? StopConditions { get; set; }
    public List<string>? GovernanceReferences { get; set; }
    public string? RoutingPolicyReference { get; set; }
    public string? SafetyPolicyReference { get; set; }

    public static HandoffExecutionScopeRecord FromApplication(HandoffExecutionScope value) => new()
    {
        IncludedScope = value.IncludedScope.Select(PlanningScopeClauseRecord.FromApplication).ToList(),
        Constraints = value.Constraints.Select(PlanningScopeClauseRecord.FromApplication).ToList(),
        ForbiddenScope = value.ForbiddenScope.Select(PlanningScopeClauseRecord.FromApplication).ToList(),
        Deliverables = value.Deliverables.Select(PlanningDeliverableRecord.FromApplication).ToList(),
        ValidationRequirements = value.ValidationRequirements.Select(PlanningValidationRequirementRecord.FromApplication).ToList(),
        ExecutionBudgets = value.ExecutionBudgets.Select(PlanningExecutionBudgetRecord.FromApplication).ToList(),
        StopConditions = value.StopConditions.Select(PlanningStopConditionRecord.FromApplication).ToList(),
        GovernanceReferences = value.GovernanceReferences.ToList(),
        RoutingPolicyReference = value.RoutingPolicyReference,
        SafetyPolicyReference = value.SafetyPolicyReference
    };

    public HandoffExecutionScope ToApplication() => new(
        IncludedScope?.Select(static value => value.ToApplication()).ToArray() ?? throw new ArgumentException("Handoff included scope is missing."),
        Constraints?.Select(static value => value.ToApplication()).ToArray() ?? throw new ArgumentException("Handoff constraints are missing."),
        ForbiddenScope?.Select(static value => value.ToApplication()).ToArray() ?? throw new ArgumentException("Handoff forbidden scope is missing."),
        Deliverables?.Select(static value => value.ToApplication()).ToArray() ?? throw new ArgumentException("Handoff deliverables are missing."),
        ValidationRequirements?.Select(static value => value.ToApplication()).ToArray() ?? throw new ArgumentException("Handoff validations are missing."),
        ExecutionBudgets?.Select(static value => value.ToApplication()).ToArray() ?? throw new ArgumentException("Handoff budgets are missing."),
        StopConditions?.Select(static value => value.ToApplication()).ToArray() ?? throw new ArgumentException("Handoff stop conditions are missing."),
        GovernanceReferences ?? throw new ArgumentException("Handoff governance references are missing."),
        RoutingPolicyReference,
        SafetyPolicyReference);
}

internal sealed class HandoffReviewScopeRecord
{
    public List<PlanningScopeClauseRecord>? IncludedScope { get; set; }
    public List<PlanningScopeClauseRecord>? Constraints { get; set; }
    public List<PlanningScopeClauseRecord>? ForbiddenScope { get; set; }
    public List<PlanningAcceptanceCriterionRecord>? AcceptanceCriteria { get; set; }

    public static HandoffReviewScopeRecord FromApplication(HandoffReviewScope value) => new()
    {
        IncludedScope = value.IncludedScope.Select(PlanningScopeClauseRecord.FromApplication).ToList(),
        Constraints = value.Constraints.Select(PlanningScopeClauseRecord.FromApplication).ToList(),
        ForbiddenScope = value.ForbiddenScope.Select(PlanningScopeClauseRecord.FromApplication).ToList(),
        AcceptanceCriteria = value.AcceptanceCriteria.Select(PlanningAcceptanceCriterionRecord.FromApplication).ToList()
    };

    public HandoffReviewScope ToApplication() => new(
        IncludedScope?.Select(static value => value.ToApplication()).ToArray() ?? throw new ArgumentException("Handoff review included scope is missing."),
        Constraints?.Select(static value => value.ToApplication()).ToArray() ?? throw new ArgumentException("Handoff review constraints are missing."),
        ForbiddenScope?.Select(static value => value.ToApplication()).ToArray() ?? throw new ArgumentException("Handoff review forbidden scope is missing."),
        AcceptanceCriteria?.Select(static value => value.ToApplication()).ToArray() ?? throw new ArgumentException("Handoff review criteria are missing."));
}

internal sealed class HandoffRemediationScopeRecord
{
    public List<PlanningScopeClauseRecord>? IncludedScope { get; set; }
    public List<PlanningScopeClauseRecord>? Constraints { get; set; }
    public List<PlanningScopeClauseRecord>? ForbiddenScope { get; set; }
    public List<PlanningStopConditionRecord>? StopConditions { get; set; }

    public static HandoffRemediationScopeRecord FromApplication(HandoffRemediationScope value) => new()
    {
        IncludedScope = value.IncludedScope.Select(PlanningScopeClauseRecord.FromApplication).ToList(),
        Constraints = value.Constraints.Select(PlanningScopeClauseRecord.FromApplication).ToList(),
        ForbiddenScope = value.ForbiddenScope.Select(PlanningScopeClauseRecord.FromApplication).ToList(),
        StopConditions = value.StopConditions.Select(PlanningStopConditionRecord.FromApplication).ToList()
    };

    public HandoffRemediationScope ToApplication() => new(
        IncludedScope?.Select(static value => value.ToApplication()).ToArray() ?? throw new ArgumentException("Handoff remediation included scope is missing."),
        Constraints?.Select(static value => value.ToApplication()).ToArray() ?? throw new ArgumentException("Handoff remediation constraints are missing."),
        ForbiddenScope?.Select(static value => value.ToApplication()).ToArray() ?? throw new ArgumentException("Handoff remediation forbidden scope is missing."),
        StopConditions?.Select(static value => value.ToApplication()).ToArray() ?? throw new ArgumentException("Handoff remediation stop conditions are missing."));
}

internal sealed class HandoffAcceptanceScopeRecord
{
    public List<PlanningAcceptanceCriterionRecord>? AcceptanceCriteria { get; set; }

    public static HandoffAcceptanceScopeRecord FromApplication(HandoffAcceptanceScope value) => new()
    {
        AcceptanceCriteria = value.AcceptanceCriteria.Select(PlanningAcceptanceCriterionRecord.FromApplication).ToList()
    };

    public HandoffAcceptanceScope ToApplication() => new(
        AcceptanceCriteria?.Select(static value => value.ToApplication()).ToArray() ?? throw new ArgumentException("Handoff acceptance criteria are missing."));
}

internal sealed class HandoffEvidenceReferenceRecord
{
    public Guid EvidenceId { get; set; }
    public HandoffEvidenceKind Kind { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTimeOffset? CapturedAt { get; set; }
    public HandoffEvidenceFreshness Freshness { get; set; }
    public string? ContentHash { get; set; }

    public static HandoffEvidenceReferenceRecord FromApplication(HandoffEvidenceReference value) => new()
    {
        EvidenceId = value.EvidenceId,
        Kind = value.Kind,
        Reference = value.Reference,
        CapturedAt = value.CapturedAt,
        Freshness = value.Freshness,
        ContentHash = value.ContentHash
    };

    public HandoffEvidenceReference ToApplication() => new(EvidenceId, Kind, Reference, CapturedAt, Freshness, ContentHash);
}

internal sealed class HandoffFindingReferenceRecord
{
    public string FindingId { get; set; } = string.Empty;
    public HandoffFindingCategory Category { get; set; }
    public HandoffFindingSeverity Severity { get; set; }
    public HandoffFindingState State { get; set; }
    public string? Summary { get; set; }
    public string? SourceReference { get; set; }
    public List<Guid>? EvidenceIds { get; set; }

    public static HandoffFindingReferenceRecord FromApplication(HandoffFindingReference value) => new()
    {
        FindingId = value.FindingId,
        Category = value.Category,
        Severity = value.Severity,
        State = value.State,
        Summary = value.Summary,
        SourceReference = value.SourceReference,
        EvidenceIds = value.EvidenceIds.ToList()
    };

    public HandoffFindingReference ToApplication() => new(
        FindingId,
        Category,
        Severity,
        State,
        Summary,
        SourceReference,
        EvidenceIds ?? throw new ArgumentException("Handoff finding evidence ids are missing."));
}

internal sealed class HandoffChangedArtifactReferenceRecord
{
    public string? RepositoryRelativePath { get; set; }
    public string? CommitSha { get; set; }
    public string? ExternalReference { get; set; }

    public static HandoffChangedArtifactReferenceRecord FromApplication(HandoffChangedArtifactReference value) => new()
    {
        RepositoryRelativePath = value.RepositoryRelativePath,
        CommitSha = value.CommitSha,
        ExternalReference = value.ExternalReference
    };

    public HandoffChangedArtifactReference ToApplication() => new(RepositoryRelativePath, CommitSha, ExternalReference);
}

internal sealed class HandoffOutcomeMetadataRecord
{
    public HandoffOutcomeState State { get; set; }
    public string? Summary { get; set; }
    public string? ResultReference { get; set; }

    public static HandoffOutcomeMetadataRecord FromApplication(HandoffOutcomeMetadata value) => new()
    {
        State = value.State,
        Summary = value.Summary,
        ResultReference = value.ResultReference
    };

    public HandoffOutcomeMetadata ToApplication() => new(State, Summary, ResultReference);
}

internal sealed class HandoffRedactionMetadataRecord
{
    public bool RedactionApplied { get; set; }
    public int RedactionCount { get; set; }
    public List<HandoffRedactionCategory>? Categories { get; set; }

    public static HandoffRedactionMetadataRecord FromApplication(HandoffRedactionMetadata value) => new()
    {
        RedactionApplied = value.RedactionApplied,
        RedactionCount = value.RedactionCount,
        Categories = value.Categories.ToList()
    };

    public HandoffRedactionMetadata ToApplication() => new(
        RedactionApplied,
        RedactionCount,
        Categories ?? throw new ArgumentException("Handoff redaction categories are missing."));
}

internal sealed class HandoffPackageSizeMetadataRecord
{
    public int MaxPayloadBytes { get; set; }
    public int CanonicalPayloadBytes { get; set; }
    public int EvidenceReferenceCount { get; set; }
    public int FindingReferenceCount { get; set; }
    public int ChangedArtifactReferenceCount { get; set; }
    public int LimitationCount { get; set; }
    public int ScopeItemCount { get; set; }

    public static HandoffPackageSizeMetadataRecord FromApplication(HandoffPackageSizeMetadata value) => new()
    {
        MaxPayloadBytes = value.MaxPayloadBytes,
        CanonicalPayloadBytes = value.CanonicalPayloadBytes,
        EvidenceReferenceCount = value.EvidenceReferenceCount,
        FindingReferenceCount = value.FindingReferenceCount,
        ChangedArtifactReferenceCount = value.ChangedArtifactReferenceCount,
        LimitationCount = value.LimitationCount,
        ScopeItemCount = value.ScopeItemCount
    };

    public HandoffPackageSizeMetadata ToApplication() => new(
        MaxPayloadBytes,
        CanonicalPayloadBytes,
        EvidenceReferenceCount,
        FindingReferenceCount,
        ChangedArtifactReferenceCount,
        LimitationCount,
        ScopeItemCount);
}
