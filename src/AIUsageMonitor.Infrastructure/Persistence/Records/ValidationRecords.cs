using AIUsageMonitor.Application.Handoffs;
using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Validation;

namespace AIUsageMonitor.Infrastructure.Persistence;

internal sealed class ValidationReferenceRecord
{
    public Guid Id { get; set; }
    public int SchemaVersion { get; set; }
    public string ContentHash { get; set; } = string.Empty;

    public ValidationReferenceRecord() { }
    public ValidationReferenceRecord(Guid id, int schemaVersion, string contentHash) => (Id, SchemaVersion, ContentHash) = (id, schemaVersion, contentHash);
}

internal sealed class ValidationPlanReferenceRecord
{
    public Guid ProjectId { get; set; }
    public Guid PlanId { get; set; }
    public int Revision { get; set; }
    public int SchemaVersion { get; set; }
    public string ContentHash { get; set; } = string.Empty;

    public static ValidationPlanReferenceRecord From(ValidationPlanReference value) => new()
    {
        ProjectId = value.ProjectId, PlanId = value.PlanId, Revision = value.Revision,
        SchemaVersion = value.SchemaVersion, ContentHash = value.ContentHash
    };

    public ValidationPlanReference ToApplication() => new(ProjectId, PlanId, Revision, SchemaVersion, ContentHash);
}

internal sealed class ExecutionRunAuthorityReferenceRecord
{
    public Guid RunId { get; set; }
    public int SchemaVersion { get; set; }
    public string ContentHash { get; set; } = string.Empty;

    public static ExecutionRunAuthorityReferenceRecord From(ExecutionRunAuthorityReference value) => new()
    {
        RunId = value.RunId, SchemaVersion = value.SchemaVersion, ContentHash = value.ContentHash
    };

    public ExecutionRunAuthorityReference ToApplication() => new(RunId, SchemaVersion, ContentHash);
}

internal sealed class PlanningContractReferenceRecord
{
    public Guid ContractId { get; set; }
    public int Revision { get; set; }
    public int SchemaVersion { get; set; }
    public string ContentHash { get; set; } = string.Empty;

    public static PlanningContractReferenceRecord From(PlanningExecutionContractReference value) => new()
    {
        ContractId = value.ContractId, Revision = value.Revision, SchemaVersion = value.SchemaVersion, ContentHash = value.ContentHash
    };

    public PlanningExecutionContractReference ToApplication() => new(ContractId, Revision, SchemaVersion, ContentHash);
}

internal sealed class WorkGraphReferenceRecord
{
    public Guid GraphId { get; set; }
    public int SchemaVersion { get; set; }
    public string ContentHash { get; set; } = string.Empty;

    public static WorkGraphReferenceRecord? From(WorkGraphReference? value) => value is null ? null : new()
    {
        GraphId = value.GraphId, SchemaVersion = value.SchemaVersion, ContentHash = value.ContentHash
    };

    public WorkGraphReference ToApplication() => new(GraphId, SchemaVersion, ContentHash);
}

internal sealed class ValidationRequirementRecord
{
    public string RequirementId { get; set; } = string.Empty;
    public ValidationEvidenceKind EvidenceKind { get; set; }
    public bool Required { get; set; }
    public ValidationCoverageScope Coverage { get; set; }
    public ValidationBaselineRelation BaselineRelation { get; set; }
    public string CollectorIdentifier { get; set; } = string.Empty;
    public double? MaxAgeSeconds { get; set; }
    public bool AllowFullEvidenceForTargeted { get; set; }
    public string? TargetPath { get; set; }
    public string? TestFilter { get; set; }
    public double? TimeoutSeconds { get; set; }
    public string? ExpectedLocalHeadCommitSha { get; set; }
    public string? ExpectedBranchName { get; set; }
    public bool? RequireCleanWorktree { get; set; }
    public string? ExpectedRepositoryIdentity { get; set; }
    public string? ExpectedRemoteCommitId { get; set; }
    public string? ExpectedTrackerProjectId { get; set; }
    public string? ExpectedTrackerWorkItemKey { get; set; }
    public string? ExpectedTrackerStatus { get; set; }
    public ValidationEvidenceState? ExpectedState { get; set; }
    public ValidationOutcome? ExpectedOutcome { get; set; }
    public string? RequestedBranch { get; set; }
    public int? PullRequestNumber { get; set; }
    public string? ValidationDefinitionId { get; set; }
    public ValidationBaselineBindingRecord? BaselineBinding { get; set; }

    public static ValidationRequirementRecord From(ValidationRequirement value) => new()
    {
        RequirementId = value.RequirementId, EvidenceKind = value.EvidenceKind, Required = value.Required,
        Coverage = value.Coverage, BaselineRelation = value.BaselineRelation, CollectorIdentifier = value.CollectorIdentifier,
        MaxAgeSeconds = value.MaxAge?.TotalSeconds, AllowFullEvidenceForTargeted = value.AllowFullEvidenceForTargeted,
        TargetPath = value.TargetPath, TestFilter = value.TestFilter, TimeoutSeconds = value.Timeout?.TotalSeconds,
        ExpectedLocalHeadCommitSha = value.ExpectedLocalHeadCommitSha, ExpectedBranchName = value.ExpectedBranchName,
        RequireCleanWorktree = value.RequireCleanWorktree, ExpectedRepositoryIdentity = value.ExpectedRepositoryIdentity,
        ExpectedRemoteCommitId = value.ExpectedRemoteCommitId, ExpectedTrackerProjectId = value.ExpectedTrackerProjectId,
        ExpectedTrackerWorkItemKey = value.ExpectedTrackerWorkItemKey, ExpectedTrackerStatus = value.ExpectedTrackerStatus,
        ExpectedState = value.ExpectedState, ExpectedOutcome = value.ExpectedOutcome, RequestedBranch = value.RequestedBranch,
        PullRequestNumber = value.PullRequestNumber, ValidationDefinitionId = value.ValidationDefinitionId,
        BaselineBinding = value.BaselineBinding is null ? null : ValidationBaselineBindingRecord.From(value.BaselineBinding)
    };

    public ValidationRequirement ToApplication() => new(
        RequirementId, EvidenceKind, Required, Coverage, BaselineRelation, CollectorIdentifier,
        MaxAgeSeconds is null ? null : TimeSpan.FromSeconds(MaxAgeSeconds.Value), AllowFullEvidenceForTargeted,
        TargetPath, TestFilter, TimeoutSeconds is null ? null : TimeSpan.FromSeconds(TimeoutSeconds.Value),
        ExpectedLocalHeadCommitSha, ExpectedBranchName, RequireCleanWorktree, ExpectedRepositoryIdentity,
        ExpectedRemoteCommitId, ExpectedTrackerProjectId, ExpectedTrackerWorkItemKey, ExpectedTrackerStatus,
        ExpectedState, ExpectedOutcome, RequestedBranch, PullRequestNumber, ValidationDefinitionId,
        BaselineBinding?.ToApplication());
}

internal sealed class ValidationBaselineBindingRecord
{
    public ValidationPlanReferenceRecord PlanReference { get; set; } = new();
    public ValidationEvidenceReferenceRecord EvidenceReference { get; set; } = new();
    public string ValidationDefinitionId { get; set; } = string.Empty;

    public static ValidationBaselineBindingRecord From(ValidationBaselineBinding value) => new()
    {
        PlanReference = ValidationPlanReferenceRecord.From(value.PlanReference),
        EvidenceReference = ValidationEvidenceReferenceRecord.From(value.EvidenceReference),
        ValidationDefinitionId = value.ValidationDefinitionId
    };

    public ValidationBaselineBinding ToApplication() => new(PlanReference.ToApplication(), EvidenceReference.ToApplication(), ValidationDefinitionId);
}

internal sealed class ValidationPlanRecord
{
    public string RecordType { get; set; } = "validation-plan";
    public int SchemaVersion { get; set; }
    public Guid ProjectId { get; set; }
    public Guid PlanId { get; set; }
    public int Revision { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset EvidenceNotBefore { get; set; }
    public ExecutionRunAuthorityReferenceRecord ExecutionRunAuthorityReference { get; set; } = new();
    public PlanningContractReferenceRecord PlanningContractReference { get; set; } = new();
    public WorkGraphReferenceRecord? WorkGraphReference { get; set; }
    public Guid? WorkGraphNodeId { get; set; }
    public HandoffPackageReferenceRecord? HandoffPackageReference { get; set; }
    public Guid WorkspaceId { get; set; }
    public string WorkspacePath { get; set; } = string.Empty;
    public string WorkspaceReceiptContentHash { get; set; } = string.Empty;
    public RecoveryCheckpointReferenceRecord CurrentRecoveryCheckpointReference { get; set; } = new();
    public List<ValidationRequirementRecord> Requirements { get; set; } = [];
    public string ContentHash { get; set; } = string.Empty;

    public static ValidationPlanRecord FromApplication(ValidationPlan value) => new()
    {
        SchemaVersion = value.SchemaVersion, ProjectId = value.ProjectId, PlanId = value.PlanId, Revision = value.Revision,
        CreatedAt = value.CreatedAt, EvidenceNotBefore = value.EvidenceNotBefore, ExecutionRunAuthorityReference = ExecutionRunAuthorityReferenceRecord.From(value.ExecutionRunAuthorityReference),
        PlanningContractReference = PlanningContractReferenceRecord.From(value.PlanningContractReference), WorkGraphReference = WorkGraphReferenceRecord.From(value.WorkGraphReference),
        WorkGraphNodeId = value.WorkGraphNodeId, HandoffPackageReference = value.HandoffPackageReference is null ? null : HandoffPackageReferenceRecord.FromApplication(value.HandoffPackageReference),
        WorkspaceId = value.WorkspaceId, WorkspacePath = value.WorkspacePath, WorkspaceReceiptContentHash = value.WorkspaceReceiptContentHash,
        CurrentRecoveryCheckpointReference = RecoveryCheckpointReferenceRecord.FromApplication(value.CurrentRecoveryCheckpointReference),
        Requirements = value.Requirements.Select(ValidationRequirementRecord.From).ToList(), ContentHash = value.ContentHash
    };

    public ValidationPlan ToApplication() => new(
        ProjectId, PlanId, Revision, CreatedAt, ExecutionRunAuthorityReference.ToApplication(), PlanningContractReference.ToApplication(),
        WorkGraphReference?.ToApplication() ?? throw new ArgumentException("Validation-plan work graph reference is missing."), WorkGraphNodeId ?? Guid.Empty, WorkspaceId, WorkspacePath, WorkspaceReceiptContentHash,
        CurrentRecoveryCheckpointReference.ToApplication(), Requirements.Select(static value => value.ToApplication()).ToArray(),
        HandoffPackageReference?.ToApplication(), SchemaVersion, ContentHash, EvidenceNotBefore == default ? CreatedAt : EvidenceNotBefore);
}

internal sealed class ValidationEvidenceReferenceRecord
{
    public Guid EvidenceId { get; set; }
    public int SchemaVersion { get; set; }
    public string ContentHash { get; set; } = string.Empty;

    public static ValidationEvidenceReferenceRecord From(ValidationEvidenceReference value) => new()
    {
        EvidenceId = value.EvidenceId, SchemaVersion = value.SchemaVersion, ContentHash = value.ContentHash
    };

    public ValidationEvidenceReference ToApplication() => new(EvidenceId, SchemaVersion, ContentHash);
}

internal sealed class ValidationEvidenceRecord
{
    public string RecordType { get; set; } = "validation-evidence";
    public int SchemaVersion { get; set; }
    public Guid ProjectId { get; set; }
    public Guid EvidenceId { get; set; }
    public ValidationPlanReferenceRecord PlanReference { get; set; } = new();
    public string RequirementId { get; set; } = string.Empty;
    public string? ValidationDefinitionId { get; set; }
    public Guid RunId { get; set; }
    public ExecutionRunAuthorityReferenceRecord ExecutionRunAuthorityReference { get; set; } = new();
    public PlanningContractReferenceRecord PlanningContractReference { get; set; } = new();
    public WorkGraphReferenceRecord? WorkGraphReference { get; set; }
    public Guid? WorkGraphNodeId { get; set; }
    public RecoveryCheckpointReferenceRecord CurrentRecoveryCheckpointReference { get; set; } = new();
    public Guid WorkspaceId { get; set; }
    public string WorkspacePath { get; set; } = string.Empty;
    public string WorkspaceReceiptContentHash { get; set; } = string.Empty;
    public string CollectorIdentifier { get; set; } = string.Empty;
    public ValidationEvidenceKind Kind { get; set; }
    public ValidationEvidenceState State { get; set; }
    public ValidationOutcome Outcome { get; set; }
    public ValidationCoverageScope Coverage { get; set; }
    public ValidationBaselineRelation BaselineRelation { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
    public bool IndependentlyCaptured { get; set; }
    public bool SecurityBoundaryValid { get; set; }
    public ValidationEvidenceReferenceRecord? BaselineEvidenceReference { get; set; }
    public string? TargetIdentity { get; set; }
    public string? LocalHeadCommitSha { get; set; }
    public string? BranchName { get; set; }
    public bool? LocalIsClean { get; set; }
    public string? RepositoryIdentity { get; set; }
    public string? RemoteCommitId { get; set; }
    public string? TrackerProjectId { get; set; }
    public string? TrackerWorkItemKey { get; set; }
    public string? TrackerStatus { get; set; }
    public int StdoutBytes { get; set; }
    public int StderrBytes { get; set; }
    public bool OutputTruncated { get; set; }
    public string? DiagnosticSummary { get; set; }
    public string? ReasonCode { get; set; }
    public string? ValidatedEvidenceSetHash { get; set; }
    public List<ValidationEvidenceReferenceRecord> ValidatedEvidenceReferences { get; set; } = [];
    public string ContentHash { get; set; } = string.Empty;

    public static ValidationEvidenceRecord FromApplication(ValidationEvidence value) => new()
    {
        SchemaVersion = value.SchemaVersion, ProjectId = value.ProjectId, EvidenceId = value.EvidenceId,
        PlanReference = ValidationPlanReferenceRecord.From(value.PlanReference), RequirementId = value.RequirementId, ValidationDefinitionId = value.ValidationDefinitionId, RunId = value.RunId,
        ExecutionRunAuthorityReference = ExecutionRunAuthorityReferenceRecord.From(value.ExecutionRunAuthorityReference),
        PlanningContractReference = PlanningContractReferenceRecord.From(value.PlanningContractReference), WorkGraphReference = WorkGraphReferenceRecord.From(value.WorkGraphReference),
        WorkGraphNodeId = value.WorkGraphNodeId, CurrentRecoveryCheckpointReference = RecoveryCheckpointReferenceRecord.FromApplication(value.CurrentRecoveryCheckpointReference),
        WorkspaceId = value.WorkspaceId, WorkspacePath = value.WorkspacePath, WorkspaceReceiptContentHash = value.WorkspaceReceiptContentHash,
        CollectorIdentifier = value.CollectorIdentifier, Kind = value.Kind, State = value.State, Outcome = value.Outcome, Coverage = value.Coverage,
        BaselineRelation = value.BaselineRelation, CapturedAt = value.CapturedAt, IndependentlyCaptured = value.IndependentlyCaptured,
        SecurityBoundaryValid = value.SecurityBoundaryValid, BaselineEvidenceReference = value.BaselineEvidenceReference is null ? null : ValidationEvidenceReferenceRecord.From(value.BaselineEvidenceReference),
        TargetIdentity = value.TargetIdentity, LocalHeadCommitSha = value.LocalHeadCommitSha, BranchName = value.BranchName, LocalIsClean = value.LocalIsClean,
        RepositoryIdentity = value.RepositoryIdentity, RemoteCommitId = value.RemoteCommitId, TrackerProjectId = value.TrackerProjectId,
        TrackerWorkItemKey = value.TrackerWorkItemKey, TrackerStatus = value.TrackerStatus, StdoutBytes = value.StdoutBytes, StderrBytes = value.StderrBytes,
        OutputTruncated = value.OutputTruncated, DiagnosticSummary = value.DiagnosticSummary, ReasonCode = value.ReasonCode,
        ValidatedEvidenceSetHash = value.ValidatedEvidenceSetHash, ValidatedEvidenceReferences = value.ValidatedEvidenceReferences.Select(ValidationEvidenceReferenceRecord.From).ToList(), ContentHash = value.ContentHash
    };

    public ValidationEvidence ToApplication() => new(
        ProjectId, EvidenceId, PlanReference.ToApplication(), RequirementId, RunId, ExecutionRunAuthorityReference.ToApplication(),
        PlanningContractReference.ToApplication(), WorkGraphReference?.ToApplication() ?? throw new ArgumentException("Validation-evidence work graph reference is missing."), WorkGraphNodeId ?? Guid.Empty, CurrentRecoveryCheckpointReference.ToApplication(),
        WorkspaceId, WorkspacePath, WorkspaceReceiptContentHash, CollectorIdentifier, Kind, State, Outcome, Coverage, BaselineRelation, CapturedAt,
        IndependentlyCaptured, SecurityBoundaryValid, BaselineEvidenceReference?.ToApplication(), TargetIdentity, LocalHeadCommitSha, BranchName,
        LocalIsClean, RepositoryIdentity, RemoteCommitId, TrackerProjectId, TrackerWorkItemKey, TrackerStatus, StdoutBytes, StderrBytes,
        OutputTruncated, DiagnosticSummary, ReasonCode, SchemaVersion, ContentHash, ValidationDefinitionId, ValidatedEvidenceSetHash,
        ValidatedEvidenceReferences.Select(static value => value.ToApplication()).ToArray());
}

internal sealed class ValidationRequirementDecisionRecord
{
    public string RequirementId { get; set; } = string.Empty;
    public ValidationRequirementDecisionState State { get; set; }
    public List<ValidationEvidenceReferenceRecord> SupportingEvidence { get; set; } = [];
    public ValidationEvidenceState? ObservedState { get; set; }
    public ValidationOutcome? ObservedOutcome { get; set; }
    public bool? Fresh { get; set; }
    public string? ReasonCode { get; set; }
    public string? Explanation { get; set; }

    public static ValidationRequirementDecisionRecord From(ValidationRequirementDecision value) => new()
    {
        RequirementId = value.RequirementId, State = value.State, SupportingEvidence = value.SupportingEvidence.Select(ValidationEvidenceReferenceRecord.From).ToList(),
        ObservedState = value.ObservedState, ObservedOutcome = value.ObservedOutcome, Fresh = value.Fresh, ReasonCode = value.ReasonCode, Explanation = value.Explanation
    };

    public ValidationRequirementDecision ToApplication() => new(
        RequirementId, State, SupportingEvidence.Select(static value => value.ToApplication()).ToArray(), ObservedState, ObservedOutcome, Fresh,
        ReasonCode ?? ValidationReasonCodes.EvidenceNotUsable, Explanation ?? "Validation requirement decision has no explanation.");
}

internal sealed class ValidationGateDecisionRecord
{
    public string RecordType { get; set; } = "validation-gate-decision";
    public int SchemaVersion { get; set; }
    public Guid ProjectId { get; set; }
    public Guid DecisionId { get; set; }
    public ValidationPlanReferenceRecord PlanReference { get; set; } = new();
    public ExecutionRunAuthorityReferenceRecord ExecutionRunAuthorityReference { get; set; } = new();
    public RecoveryCheckpointReferenceRecord CurrentRecoveryCheckpointReference { get; set; } = new();
    public DateTimeOffset DecidedAt { get; set; }
    public ValidationGateDecisionState State { get; set; }
    public List<ValidationRequirementDecisionRecord> RequirementDecisions { get; set; } = [];
    public List<ValidationEvidenceReferenceRecord> SupportingEvidence { get; set; } = [];
    public string? ReasonCode { get; set; }
    public string? Explanation { get; set; }
    public string ContentHash { get; set; } = string.Empty;

    public static ValidationGateDecisionRecord FromApplication(ValidationGateDecision value) => new()
    {
        SchemaVersion = value.SchemaVersion, ProjectId = value.ProjectId, DecisionId = value.DecisionId,
        PlanReference = ValidationPlanReferenceRecord.From(value.PlanReference), ExecutionRunAuthorityReference = ExecutionRunAuthorityReferenceRecord.From(value.ExecutionRunAuthorityReference),
        CurrentRecoveryCheckpointReference = RecoveryCheckpointReferenceRecord.FromApplication(value.CurrentRecoveryCheckpointReference), DecidedAt = value.DecidedAt, State = value.State,
        RequirementDecisions = value.RequirementDecisions.Select(ValidationRequirementDecisionRecord.From).ToList(), SupportingEvidence = value.SupportingEvidence.Select(ValidationEvidenceReferenceRecord.From).ToList(),
        ReasonCode = value.ReasonCode, Explanation = value.Explanation, ContentHash = value.ContentHash
    };

    public ValidationGateDecision ToApplication() => new(
        ProjectId, DecisionId, PlanReference.ToApplication(), ExecutionRunAuthorityReference.ToApplication(), CurrentRecoveryCheckpointReference.ToApplication(), DecidedAt,
        State, RequirementDecisions.Select(static value => value.ToApplication()).ToArray(), SupportingEvidence.Select(static value => value.ToApplication()).ToArray(),
        ReasonCode, Explanation, SchemaVersion, ContentHash);
}
