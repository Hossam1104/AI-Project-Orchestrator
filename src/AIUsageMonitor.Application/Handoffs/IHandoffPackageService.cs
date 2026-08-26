using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Planning;

namespace AIUsageMonitor.Application.Handoffs;

/// <summary>Typed input for one bounded handoff transition. Canonical work and scope are not caller copies.</summary>
public sealed class HandoffPackageCreationRequest
{
    public HandoffPackageCreationRequest(
        Guid projectId,
        Guid packageId,
        HandoffTransition transition,
        PlanningExecutionContractReference? planningContractReference,
        DateTimeOffset createdAt,
        int schemaVersion = HandoffPackageSchema.CurrentVersion,
        WorkGraphReference? workGraphReference = null,
        Guid? workGraphNodeId = null,
        HandoffPackageReference? previousPackageReference = null,
        IReadOnlyList<HandoffEvidenceReference>? evidenceReferences = null,
        IReadOnlyList<HandoffFindingReference>? findingReferences = null,
        IReadOnlyList<HandoffChangedArtifactReference>? changedArtifactReferences = null,
        HandoffOutcomeMetadata? outcome = null,
        IReadOnlyList<string>? limitations = null,
        string? nextAction = null)
    {
        ProjectId = projectId;
        PackageId = packageId;
        Transition = transition;
        PlanningContractReference = planningContractReference;
        CreatedAt = createdAt;
        SchemaVersion = schemaVersion;
        WorkGraphReference = workGraphReference;
        WorkGraphNodeId = workGraphNodeId;
        PreviousPackageReference = previousPackageReference;
        EvidenceReferences = evidenceReferences ?? Array.Empty<HandoffEvidenceReference>();
        FindingReferences = findingReferences ?? Array.Empty<HandoffFindingReference>();
        ChangedArtifactReferences = changedArtifactReferences ?? Array.Empty<HandoffChangedArtifactReference>();
        Outcome = outcome;
        Limitations = limitations ?? Array.Empty<string>();
        NextAction = nextAction;
    }

    public Guid ProjectId { get; }
    public Guid PackageId { get; }
    public HandoffTransition Transition { get; }
    public PlanningExecutionContractReference? PlanningContractReference { get; }
    public DateTimeOffset CreatedAt { get; }
    public int SchemaVersion { get; }
    public WorkGraphReference? WorkGraphReference { get; }
    public Guid? WorkGraphNodeId { get; }
    public HandoffPackageReference? PreviousPackageReference { get; }
    public IReadOnlyList<HandoffEvidenceReference> EvidenceReferences { get; }
    public IReadOnlyList<HandoffFindingReference> FindingReferences { get; }
    public IReadOnlyList<HandoffChangedArtifactReference> ChangedArtifactReferences { get; }
    public HandoffOutcomeMetadata? Outcome { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string? NextAction { get; }
}

public enum HandoffPackageCreationStatus
{
    Created,
    ProjectNotFound,
    InvalidRequest,
    UnsupportedTransition,
    ContractMissing,
    ContractInvalid,
    ContractMismatch,
    GraphMissing,
    GraphInvalid,
    GraphNodeMismatch,
    PredecessorMissing,
    PredecessorInvalid,
    InvalidLineage,
    RequiredContextMissing,
    PackageTooLarge,
    RedactionRejected,
    PackageConflict,
    PersistenceUnavailable
}

public sealed record HandoffPackageCreationResult(
    HandoffPackageCreationStatus Status,
    HandoffPackage? Package = null,
    PlanningContractReadState? ContractState = null,
    WorkGraphReadState? GraphState = null,
    HandoffPackageReadState? PredecessorState = null,
    string? ErrorMessage = null)
{
    public bool Succeeded => Status == HandoffPackageCreationStatus.Created && Package is not null;
}

public interface IHandoffPackageService
{
    Task<HandoffPackageCreationResult> CreateAsync(
        HandoffPackageCreationRequest request,
        CancellationToken cancellationToken = default);
}
