using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIUsageMonitor.Application.Handoffs;
using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Projects;
using AIUsageMonitor.Application.Routing;
using AIUsageMonitor.Application.Time;

namespace AIUsageMonitor.Application.Workspaces;

public static class WorkspacePreparationPlanSchema
{
    public const int CurrentVersion = 1;
}

public static class WorkspacePreparationReceiptSchema
{
    public const int CurrentVersion = 1;
}

public static class WorkspacePreparationApprovalEvidenceSchema
{
    public const int CurrentVersion = 1;
}

public static class WorkspacePreparationLimits
{
    public const int MaxCanonicalPayloadBytes = 128 * 1024;
    public const int MaxBranchLength = 255;
    public const int MaxPathLength = 2_000;
    public const int MaxExplanationLength = 4_000;
    public const int MaxCorrelationLength = 200;
    public const int MaxWorktrees = 128;
    public const int MaxChangedFiles = 128;
    public const int MaxDivergenceReferenceLength = 512;
}

public enum WorkspacePreparationPolicy
{
    RequireCleanSource = 0,
    AllowDirtySourceWithWarning = 1
}

public enum WorkspaceRepositoryDiscoveryStatus
{
    Available,
    RepositoryMissing,
    NotGitRepository,
    EvidenceOverflow,
    Unavailable
}

public enum WorkspacePreparationPlanningStatus
{
    Planned,
    ProjectNotFound,
    RepositoryMissing,
    NotGitRepository,
    RepositoryUnavailable,
    ContractMissing,
    ContractMismatch,
    ContextMismatch,
    InvalidBranchName,
    BaseCommitMissing,
    BranchConflict,
    WorktreeConflict,
    UnsafePath,
    PolicyBlocked,
    RedactionRejected,
    PersistenceUnavailable,
    PlanConflict,
    EvidenceOverflow
}

public enum WorkspacePreparationStatus
{
    Prepared,
    AlreadyPrepared,
    ApprovalRequired,
    ApprovalMismatch,
    PlanMissing,
    PlanIntegrityFailure,
    PlanStale,
    BranchConflict,
    PathConflict,
    WorktreeConflict,
    RepositoryUnavailable,
    GitCommandFailed,
    VerificationFailed,
    ReceiptPersistenceFailed,
    RedactionRejected,
    PolicyBlocked,
    PersistenceUnavailable,
    PreparedWithoutReceipt,
    ForeignWorkspace,
    UnsafePath,
    ReceiptConflict,
    ApprovalEvidencePersistenceFailed,
    ApprovalEvidenceConflict
}

public enum WorkspaceRecoveryState
{
    NotPrepared,
    PreparedAndRecorded,
    PreparedWithoutReceipt,
    ForeignWorkspace,
    Conflict,
    UnsafePath,
    IntegrityFailure,
    Unavailable
}

public enum WorkspacePreparationPlanReadState
{
    Missing,
    Valid,
    UnsupportedFutureVersion,
    MigrationRequired,
    Invalid,
    IntegrityFailure,
    Unavailable
}

public enum WorkspacePreparationReceiptReadState
{
    Missing,
    Valid,
    UnsupportedFutureVersion,
    MigrationRequired,
    Invalid,
    IntegrityFailure,
    Unavailable
}

public enum WorkspaceCleanupOwner
{
    APO,
    Owner,
    External
}

public enum WorkspaceCleanupPolicy
{
    ExplicitActionRequired
}

public enum WorkspacePreparationApprovalEvidenceReadState
{
    Missing,
    Valid,
    UnsupportedFutureVersion,
    MigrationRequired,
    Invalid,
    IntegrityFailure,
    Unavailable
}

public enum WorkspacePreparationApprovalEvidenceWriteStatus
{
    Created,
    ApprovalEvidenceConflict,
    Unavailable
}

public sealed record WorkspacePreparationApprovalEvidenceWriteResult(
    WorkspacePreparationApprovalEvidenceWriteStatus Status,
    string? ErrorMessage = null)
{
    public bool Succeeded => Status == WorkspacePreparationApprovalEvidenceWriteStatus.Created;
}

public sealed record WorkspacePreparationApprovalEvidenceReadResult(
    WorkspacePreparationApprovalEvidenceReadState State,
    WorkspacePreparationApprovalEvidence? Evidence = null,
    string? ErrorMessage = null)
{
    public bool IsValid => State == WorkspacePreparationApprovalEvidenceReadState.Valid && Evidence is not null;
}

public sealed class WorkspacePreparationPlanReference
{
    public WorkspacePreparationPlanReference(Guid planId, int schemaVersion, string contentHash, Guid projectId = default)
    {
        if (planId == Guid.Empty) throw new ArgumentException("Plan id cannot be empty.", nameof(planId));
        if (schemaVersion <= 0) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        if (!WorkspacePreparationIntegrity.IsSha256(contentHash))
        {
            throw new ArgumentException("Plan content hash must be SHA-256 hexadecimal evidence.", nameof(contentHash));
        }

        PlanId = planId;
        SchemaVersion = schemaVersion;
        ContentHash = contentHash.ToLowerInvariant();
        ProjectId = projectId;
    }

    public Guid ProjectId { get; }
    public Guid PlanId { get; }
    public int SchemaVersion { get; }
    public string ContentHash { get; }
    public string PlanFingerprint => ContentHash;
}

/// <summary>
/// Durable, immutable evidence that a specific actor approved a specific immutable plan. The
/// application persists this value before it asks Git to create a worktree.
/// </summary>
public sealed class WorkspacePreparationApprovalEvidence
{
    public WorkspacePreparationApprovalEvidence(
        Guid projectId,
        Guid workspaceId,
        Guid approvalId,
        WorkspacePreparationPlanReference planReference,
        string actorReference,
        DateTimeOffset approvedAt,
        DateTimeOffset recordedAt,
        string? sanitizedReason = null,
        string? contentHash = null)
    {
        if (projectId == Guid.Empty || workspaceId == Guid.Empty || approvalId == Guid.Empty)
        {
            throw new ArgumentException("Project, workspace, and approval identifiers are required.");
        }

        PlanReference = planReference ?? throw new ArgumentNullException(nameof(planReference));
        if (planReference.ProjectId != projectId)
        {
            throw new ArgumentException("Approval plan reference belongs to another project.", nameof(planReference));
        }

        ProjectId = projectId;
        WorkspaceId = workspaceId;
        ApprovalId = approvalId;
        ActorReference = Required(actorReference, nameof(actorReference), 300);
        ApprovedAt = approvedAt == default
            ? throw new ArgumentException("Approval time is required.", nameof(approvedAt))
            : approvedAt;
        RecordedAt = recordedAt == default
            ? throw new ArgumentException("Evidence record time is required.", nameof(recordedAt))
            : recordedAt;
        SanitizedReason = Optional(sanitizedReason, nameof(sanitizedReason), WorkspacePreparationLimits.MaxExplanationLength);

        var calculatedHash = WorkspacePreparationIntegrity.ComputeContentHash(this);
        if (contentHash is not null &&
            (!WorkspacePreparationIntegrity.IsSha256(contentHash) ||
             !string.Equals(contentHash, calculatedHash, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Approval evidence content hash does not match the authority payload.", nameof(contentHash));
        }

        ContentHash = calculatedHash;
    }

    public Guid ProjectId { get; }
    public Guid WorkspaceId { get; }
    public Guid ApprovalId { get; }
    public WorkspacePreparationPlanReference PlanReference { get; }
    public string ActorReference { get; }
    public DateTimeOffset ApprovedAt { get; }
    public DateTimeOffset RecordedAt { get; }
    public string? SanitizedReason { get; }
    public string? Reason => SanitizedReason;
    public string ContentHash { get; }

    private static string Required(string value, string name, int max) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A value is required.", name) :
        value.Trim().Length > max ? throw new ArgumentException($"The value cannot exceed {max} characters.", name) : value.Trim();

    private static string? Optional(string? value, string name, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : Required(value, name, max);
}

public sealed class WorkspacePreparationApprovalReference
{
    public WorkspacePreparationApprovalReference(Guid approvalId, int schemaVersion, string contentHash)
    {
        if (approvalId == Guid.Empty)
        {
            throw new ArgumentException("Approval id cannot be empty.", nameof(approvalId));
        }

        if (schemaVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        }

        if (!WorkspacePreparationIntegrity.IsSha256(contentHash))
        {
            throw new ArgumentException("Approval evidence content hash must be SHA-256 hexadecimal evidence.", nameof(contentHash));
        }

        ApprovalId = approvalId;
        SchemaVersion = schemaVersion;
        ContentHash = contentHash.ToLowerInvariant();
    }

    public Guid ApprovalId { get; }
    public int SchemaVersion { get; }
    public string ContentHash { get; }
}

public sealed class WorkspacePreparationApproval
{
    public WorkspacePreparationApproval(
        Guid approvalId,
        WorkspacePreparationPlanReference planReference,
        string actorReference,
        DateTimeOffset approvedAt,
        string? reason = null)
    {
        if (approvalId == Guid.Empty) throw new ArgumentException("Approval id cannot be empty.", nameof(approvalId));
        PlanReference = planReference ?? throw new ArgumentNullException(nameof(planReference));
        ActorReference = Required(actorReference, nameof(actorReference), 300);
        if (approvedAt == default) throw new ArgumentException("Approval time is required.", nameof(approvedAt));
        ApprovedAt = approvedAt;
        Reason = Optional(reason, nameof(reason), WorkspacePreparationLimits.MaxExplanationLength);
        ApprovalId = approvalId;
    }

    public Guid ApprovalId { get; }
    public WorkspacePreparationPlanReference PlanReference { get; }
    public string ActorReference { get; }
    public DateTimeOffset ApprovedAt { get; }
    public string? Reason { get; }

    private static string Required(string value, string name, int max) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A value is required.", name) :
        value.Trim().Length > max ? throw new ArgumentException($"The value cannot exceed {max} characters.", name) : value.Trim();

    private static string? Optional(string? value, string name, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : Required(value, name, max);
}

public sealed class WorkspaceWorktreeEvidence
{
    public WorkspaceWorktreeEvidence(
        string path,
        string headCommitSha,
        string? branchName,
        bool isDetached,
        bool isLocked,
        bool isPrunable)
    {
        Path = Required(path, nameof(path), WorkspacePreparationLimits.MaxPathLength);
        HeadCommitSha = Required(headCommitSha, nameof(headCommitSha), 64);
        BranchName = Optional(branchName, nameof(branchName), WorkspacePreparationLimits.MaxBranchLength);
        IsDetached = isDetached;
        IsLocked = isLocked;
        IsPrunable = isPrunable;
    }

    public string Path { get; }
    public string HeadCommitSha { get; }
    public string? BranchName { get; }
    public bool IsDetached { get; }
    public bool IsLocked { get; }
    public bool IsPrunable { get; }

    private static string Required(string value, string name, int max) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A value is required.", name) :
        value.Trim().Length > max ? throw new ArgumentException($"The value cannot exceed {max} characters.", name) : value.Trim();

    private static string? Optional(string? value, string name, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : Required(value, name, max);
}

public enum WorkspaceDivergenceState
{
    NotConfigured,
    PointInTime,
    Unknown,
    Unavailable
}

/// <summary>
/// Bounded local-ref divergence evidence. This is point-in-time evidence only; it never fetches
/// and makes no claim about the current remote state.
/// </summary>
public sealed class WorkspaceRepositoryDivergence
{
    public WorkspaceRepositoryDivergence(
        WorkspaceDivergenceState state,
        string? localUpstreamReference = null,
        int? aheadCount = null,
        int? behindCount = null)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentException("Divergence state is undefined.", nameof(state));
        }

        if (state == WorkspaceDivergenceState.PointInTime &&
            (string.IsNullOrWhiteSpace(localUpstreamReference) || aheadCount is null || behindCount is null || aheadCount < 0 || behindCount < 0))
        {
            throw new ArgumentException("Point-in-time divergence requires a local upstream and non-negative counts.", nameof(state));
        }

        State = state;
        LocalUpstreamReference = Optional(localUpstreamReference, nameof(localUpstreamReference), WorkspacePreparationLimits.MaxDivergenceReferenceLength);
        AheadCount = aheadCount;
        BehindCount = behindCount;
    }

    public WorkspaceDivergenceState State { get; }
    public string? LocalUpstreamReference { get; }
    public int? AheadCount { get; }
    public int? BehindCount { get; }

    private static string? Optional(string? value, string name, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length > max
            ? throw new ArgumentException($"The value cannot exceed {max} characters.", name)
            : value.Trim();
}

public sealed class WorkspaceRepositoryDiscovery
{
    public WorkspaceRepositoryDiscovery(
        WorkspaceRepositoryDiscoveryStatus status,
        string registeredPath,
        string? repositoryRoot = null,
        string? commonDirectory = null,
        bool isBareRepository = false,
        string? headCommitSha = null,
        string? branchName = null,
        bool isDetached = false,
        bool isClean = false,
        int changedFileCount = 0,
        IReadOnlyList<WorkspaceWorktreeEvidence>? worktrees = null,
        IReadOnlyList<string>? localBranches = null,
        string? errorMessage = null,
        string? workingTreeStateFingerprint = null,
        WorkspaceRepositoryDivergence? divergence = null,
        bool? worktreeEvidenceOverflow = null,
        bool? localBranchEvidenceOverflow = null)
    {
        Status = status;
        RegisteredPath = Required(registeredPath, nameof(registeredPath), WorkspacePreparationLimits.MaxPathLength);
        RepositoryRoot = Optional(repositoryRoot, nameof(repositoryRoot), WorkspacePreparationLimits.MaxPathLength);
        CommonDirectory = Optional(commonDirectory, nameof(commonDirectory), WorkspacePreparationLimits.MaxPathLength);
        IsBareRepository = isBareRepository;
        HeadCommitSha = Optional(headCommitSha, nameof(headCommitSha), 64);
        BranchName = Optional(branchName, nameof(branchName), WorkspacePreparationLimits.MaxBranchLength);
        IsDetached = isDetached;
        IsClean = isClean;
        ChangedFileCount = Math.Max(0, changedFileCount);
        var suppliedWorktrees = (worktrees ?? Array.Empty<WorkspaceWorktreeEvidence>()).ToArray();
        var suppliedBranches = (localBranches ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToArray();
        WorktreeEvidenceOverflow = worktreeEvidenceOverflow ?? suppliedWorktrees.Length > WorkspacePreparationLimits.MaxWorktrees;
        LocalBranchEvidenceOverflow = localBranchEvidenceOverflow ?? suppliedBranches.Length > WorkspacePreparationLimits.MaxWorktrees;
        Worktrees = suppliedWorktrees.Take(WorkspacePreparationLimits.MaxWorktrees).ToArray();
        LocalBranches = suppliedBranches.Take(WorkspacePreparationLimits.MaxWorktrees).ToArray();
        ErrorMessage = Optional(errorMessage, nameof(errorMessage), WorkspacePreparationLimits.MaxExplanationLength);
        if (workingTreeStateFingerprint is not null && !WorkspacePreparationIntegrity.IsSha256(workingTreeStateFingerprint))
            throw new ArgumentException("Working-tree state fingerprint must be SHA-256 evidence.", nameof(workingTreeStateFingerprint));
        WorkingTreeStateFingerprint = workingTreeStateFingerprint?.ToLowerInvariant();
        Divergence = divergence ?? new WorkspaceRepositoryDivergence(WorkspaceDivergenceState.Unknown);
    }

    public WorkspaceRepositoryDiscoveryStatus Status { get; }
    public string RegisteredPath { get; }
    public string? RepositoryRoot { get; }
    public string? CommonDirectory { get; }
    public bool IsBareRepository { get; }
    public string? HeadCommitSha { get; }
    public string? BranchName { get; }
    public bool IsDetached { get; }
    public bool IsClean { get; }
    public int ChangedFileCount { get; }
    public IReadOnlyList<WorkspaceWorktreeEvidence> Worktrees { get; }
    public IReadOnlyList<string> LocalBranches { get; }
    public string? ErrorMessage { get; }
    public string? WorkingTreeStateFingerprint { get; }
    public WorkspaceRepositoryDivergence Divergence { get; }
    public bool WorktreeEvidenceOverflow { get; }
    public bool LocalBranchEvidenceOverflow { get; }
    public bool IsAvailable => Status == WorkspaceRepositoryDiscoveryStatus.Available;

    private static string Required(string value, string name, int max) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A value is required.", name) :
        value.Trim().Length > max ? throw new ArgumentException($"The value cannot exceed {max} characters.", name) : value.Trim();

    private static string? Optional(string? value, string name, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : Required(value, name, max);
}

public sealed class WorkspaceContextIdentity
{
    public WorkspaceContextIdentity(Guid projectId, Guid contextId, int contractVersion, DateTimeOffset updatedAt)
    {
        if (projectId == Guid.Empty || contextId == Guid.Empty) throw new ArgumentException("Project and context identifiers are required.");
        if (contractVersion <= 0) throw new ArgumentOutOfRangeException(nameof(contractVersion));
        ProjectId = projectId; ContextId = contextId; ContractVersion = contractVersion;
        UpdatedAt = updatedAt == default ? throw new ArgumentException("Context update time is required.", nameof(updatedAt)) : updatedAt;
    }

    public Guid ProjectId { get; }
    public Guid ContextId { get; }
    public int ContractVersion { get; }
    public DateTimeOffset UpdatedAt { get; }
}

public sealed class WorkspacePreparationPlan
{
    public WorkspacePreparationPlan(
        Guid projectId,
        Guid workspaceId,
        Guid planId,
        Guid correlationId,
        DateTimeOffset createdAt,
        WorkspaceContextIdentity context,
        PlanningExecutionContractReference contractReference,
        WorkGraphReference? workGraphReference,
        Guid? workGraphNodeId,
        RoutingDecisionReference? routingDecisionReference,
        WorkspaceRepositoryDiscovery repository,
        string baseCommitSha,
        string workspaceBranch,
        string proposedWorkspacePath,
        WorkspacePreparationPolicy policy,
        bool approvalRequired,
        string? explanation,
        IReadOnlyList<string>? limitations = null,
        string? contentHash = null)
    {
        if (projectId == Guid.Empty || workspaceId == Guid.Empty || planId == Guid.Empty || correlationId == Guid.Empty)
            throw new ArgumentException("Project, workspace, plan, and correlation identifiers are required.");
        Context = context ?? throw new ArgumentNullException(nameof(context));
        if (context.ProjectId != projectId) throw new ArgumentException("Workspace context belongs to another project.", nameof(context));
        ContractReference = contractReference ?? throw new ArgumentNullException(nameof(contractReference));
        if (workGraphNodeId is not null && workGraphReference is null)
            throw new ArgumentException("A work-graph node requires a work-graph reference.", nameof(workGraphNodeId));
        if (!WorkspacePreparationIntegrity.IsGitObjectId(baseCommitSha))
            throw new ArgumentException("The base commit must be a full Git object id.", nameof(baseCommitSha));
        BaseCommitSha = baseCommitSha.ToLowerInvariant();
        WorkspaceBranch = Required(workspaceBranch, nameof(workspaceBranch), WorkspacePreparationLimits.MaxBranchLength);
        ProposedWorkspacePath = Required(proposedWorkspacePath, nameof(proposedWorkspacePath), WorkspacePreparationLimits.MaxPathLength);
        if (!Enum.IsDefined(policy)) throw new ArgumentException("Workspace policy is undefined.", nameof(policy));
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        ProjectId = projectId;
        WorkspaceId = workspaceId;
        PlanId = planId;
        CorrelationId = correlationId;
        CreatedAt = createdAt == default ? throw new ArgumentException("Creation time is required.", nameof(createdAt)) : createdAt;
        WorkGraphReference = workGraphReference;
        WorkGraphNodeId = workGraphNodeId;
        RoutingDecisionReference = routingDecisionReference;
        Policy = policy;
        ApprovalRequired = approvalRequired;
        Explanation = Optional(explanation, nameof(explanation), WorkspacePreparationLimits.MaxExplanationLength);
        Limitations = (limitations ?? Array.Empty<string>()).Select(value => Required(value, nameof(limitations), WorkspacePreparationLimits.MaxExplanationLength)).ToArray();
        ContentHash = contentHash is null ? WorkspacePreparationIntegrity.ComputeContentHash(this) :
            WorkspacePreparationIntegrity.IsSha256(contentHash) && string.Equals(contentHash, WorkspacePreparationIntegrity.ComputeContentHash(this), StringComparison.OrdinalIgnoreCase)
                ? contentHash.ToLowerInvariant()
                : throw new ArgumentException("Plan content hash does not match the authority payload.", nameof(contentHash));
        Reference = new WorkspacePreparationPlanReference(planId, WorkspacePreparationPlanSchema.CurrentVersion, ContentHash, projectId);
    }

    public Guid ProjectId { get; }
    public Guid WorkspaceId { get; }
    public Guid PlanId { get; }
    public Guid CorrelationId { get; }
    public DateTimeOffset CreatedAt { get; }
    public WorkspaceContextIdentity Context { get; }
    public PlanningExecutionContractReference ContractReference { get; }
    public WorkGraphReference? WorkGraphReference { get; }
    public Guid? WorkGraphNodeId { get; }
    public RoutingDecisionReference? RoutingDecisionReference { get; }
    public WorkspaceRepositoryDiscovery Repository { get; }
    public string BaseCommitSha { get; }
    public string WorkspaceBranch { get; }
    public string ProposedWorkspacePath { get; }
    public WorkspacePreparationPolicy Policy { get; }
    public bool ApprovalRequired { get; }
    public string? Explanation { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string ContentHash { get; }
    public string ReceiptFingerprint => ContentHash;
    public WorkspacePreparationPlanReference Reference { get; }

    private static string Required(string value, string name, int max) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A value is required.", name) :
        value.Trim().Length > max ? throw new ArgumentException($"The value cannot exceed {max} characters.", name) : value.Trim();

    private static string? Optional(string? value, string name, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : Required(value, name, max);
}

public sealed class WorkspacePreparationReceipt
{
    public WorkspacePreparationReceipt(
        Guid projectId,
        Guid workspaceId,
        Guid correlationId,
        DateTimeOffset preparedAt,
        WorkspacePreparationPlanReference planReference,
        string workspacePath,
        string workspaceBranch,
        string baseCommitSha,
        string actualHeadCommitSha,
        string repositoryIdentity,
        string cleanupOwnerReference,
        string? limitation = null,
        string? contentHash = null,
        WorkspaceCleanupOwner cleanupOwner = WorkspaceCleanupOwner.APO,
        WorkspaceCleanupPolicy cleanupPolicy = WorkspaceCleanupPolicy.ExplicitActionRequired,
        bool automaticCleanupAllowed = false,
        WorkspacePreparationApprovalReference? approvalReference = null)
    {
        if (projectId == Guid.Empty || workspaceId == Guid.Empty || correlationId == Guid.Empty)
            throw new ArgumentException("Project, workspace, and correlation identifiers are required.");
        PlanReference = planReference ?? throw new ArgumentNullException(nameof(planReference));
        if (PlanReference.ProjectId != projectId)
            throw new ArgumentException("Receipt plan reference belongs to another project.", nameof(planReference));
        ProjectId = projectId;
        WorkspaceId = workspaceId;
        CorrelationId = correlationId;
        PreparedAt = preparedAt == default ? throw new ArgumentException("Preparation time is required.", nameof(preparedAt)) : preparedAt;
        WorkspacePath = Required(workspacePath, nameof(workspacePath), WorkspacePreparationLimits.MaxPathLength);
        WorkspaceBranch = Required(workspaceBranch, nameof(workspaceBranch), WorkspacePreparationLimits.MaxBranchLength);
        if (!WorkspacePreparationIntegrity.IsGitObjectId(baseCommitSha) || !WorkspacePreparationIntegrity.IsGitObjectId(actualHeadCommitSha))
            throw new ArgumentException("Receipt commit ids must be full Git object ids.");
        BaseCommitSha = baseCommitSha.ToLowerInvariant();
        ActualHeadCommitSha = actualHeadCommitSha.ToLowerInvariant();
        RepositoryIdentity = Required(repositoryIdentity, nameof(repositoryIdentity), WorkspacePreparationLimits.MaxPathLength);
        CleanupOwnerReference = Required(cleanupOwnerReference, nameof(cleanupOwnerReference), 300);
        if (!Enum.IsDefined(cleanupOwner)) throw new ArgumentException("Cleanup owner is undefined.", nameof(cleanupOwner));
        if (!Enum.IsDefined(cleanupPolicy)) throw new ArgumentException("Cleanup policy is undefined.", nameof(cleanupPolicy));
        if (automaticCleanupAllowed) throw new ArgumentException("Automatic cleanup is not permitted for APO-46 receipts.", nameof(automaticCleanupAllowed));
        CleanupOwner = cleanupOwner;
        CleanupPolicy = cleanupPolicy;
        AutomaticCleanupAllowed = automaticCleanupAllowed;
        ApprovalReference = approvalReference;
        Limitation = Optional(limitation, nameof(limitation), WorkspacePreparationLimits.MaxExplanationLength);
        ContentHash = contentHash is null ? WorkspacePreparationIntegrity.ComputeContentHash(this) :
            WorkspacePreparationIntegrity.IsSha256(contentHash) && string.Equals(contentHash, WorkspacePreparationIntegrity.ComputeContentHash(this), StringComparison.OrdinalIgnoreCase)
                ? contentHash.ToLowerInvariant()
                : throw new ArgumentException("Receipt content hash does not match the authority payload.", nameof(contentHash));
    }

    public Guid ProjectId { get; }
    public Guid WorkspaceId { get; }
    public Guid CorrelationId { get; }
    public DateTimeOffset PreparedAt { get; }
    public WorkspacePreparationPlanReference PlanReference { get; }
    public string WorkspacePath { get; }
    public string WorkspaceBranch { get; }
    public string BaseCommitSha { get; }
    public string ActualHeadCommitSha { get; }
    public string RepositoryIdentity { get; }
    public string CleanupOwnerReference { get; }
    public WorkspaceCleanupOwner CleanupOwner { get; }
    public WorkspaceCleanupPolicy CleanupPolicy { get; }
    public bool AutomaticCleanupAllowed { get; }
    public WorkspacePreparationApprovalReference? ApprovalReference { get; }
    public string? Limitation { get; }
    public string ContentHash { get; }

    public RecoveryEvidenceReference ToRecoveryEvidenceReference(DateTimeOffset observedAt, RecoveryEvidenceFreshness freshness) =>
        new(Guid.NewGuid(), RecoveryEvidenceKind.Repository, $"workspace-receipt:{ProjectId:D}/{WorkspaceId:D}/{ContentHash}", observedAt, freshness, contentHash: ContentHash);

    private static string Required(string value, string name, int max) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A value is required.", name) :
        value.Trim().Length > max ? throw new ArgumentException($"The value cannot exceed {max} characters.", name) : value.Trim();

    private static string? Optional(string? value, string name, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : Required(value, name, max);
}

public sealed record WorkspacePreparationRequest(
    Guid ProjectId,
    Guid WorkspaceId,
    Guid PlanId,
    Guid CorrelationId,
    PlanningExecutionContractReference ContractReference,
    string BaseCommitSha,
    string WorkspaceBranch,
    WorkspacePreparationPolicy Policy = WorkspacePreparationPolicy.RequireCleanSource,
    WorkGraphReference? WorkGraphReference = null,
    Guid? WorkGraphNodeId = null,
    RoutingDecisionReference? RoutingDecisionReference = null);

public sealed record WorkspacePreparationPlanningResult(
    WorkspacePreparationPlanningStatus Status,
    WorkspacePreparationPlan? Plan = null,
    string? ErrorMessage = null)
{
    public bool Succeeded => Status == WorkspacePreparationPlanningStatus.Planned && Plan is not null;
}

public sealed record WorkspacePreparationResult(
    WorkspacePreparationStatus Status,
    WorkspacePreparationReceipt? Receipt = null,
    WorkspaceRecoveryInspectionResult? Recovery = null,
    string? ErrorMessage = null)
{
    public bool Succeeded => Status is WorkspacePreparationStatus.Prepared or WorkspacePreparationStatus.AlreadyPrepared;
}

public sealed record WorkspaceRecoveryInspectionResult(
    WorkspaceRecoveryState State,
    WorkspacePreparationReceipt? Receipt = null,
    string? NextSafeAction = null,
    string? ErrorMessage = null);

public sealed record WorkspacePreparationPlanReadResult(
    WorkspacePreparationPlanReadState State,
    WorkspacePreparationPlan? Plan = null,
    string? ErrorMessage = null);

public sealed record WorkspacePreparationReceiptReadResult(
    WorkspacePreparationReceiptReadState State,
    WorkspacePreparationReceipt? Receipt = null,
    string? ErrorMessage = null);

public enum WorkspacePreparationPlanWriteStatus { Created, PlanConflict, Unavailable }
public sealed record WorkspacePreparationPlanWriteResult(WorkspacePreparationPlanWriteStatus Status, string? ErrorMessage = null)
{
    public bool Succeeded => Status == WorkspacePreparationPlanWriteStatus.Created;
}

public enum WorkspacePreparationReceiptWriteStatus { Created, ReceiptConflict, Unavailable }
public sealed record WorkspacePreparationReceiptWriteResult(WorkspacePreparationReceiptWriteStatus Status, string? ErrorMessage = null)
{
    public bool Succeeded => Status == WorkspacePreparationReceiptWriteStatus.Created;
}

public interface IWorkspacePreparationPlanRepository
{
    Task<WorkspacePreparationPlanWriteResult> CreateAsync(WorkspacePreparationPlan plan, CancellationToken cancellationToken = default);
    Task<WorkspacePreparationPlanReadResult> GetAsync(Guid projectId, Guid planId, CancellationToken cancellationToken = default);
}

public interface IWorkspacePreparationReceiptRepository
{
    Task<WorkspacePreparationReceiptWriteResult> CreateAsync(WorkspacePreparationReceipt receipt, CancellationToken cancellationToken = default);
    Task<WorkspacePreparationReceiptReadResult> GetAsync(Guid projectId, Guid workspaceId, CancellationToken cancellationToken = default);
}

public interface IWorkspacePreparationApprovalEvidenceRepository
{
    Task<WorkspacePreparationApprovalEvidenceWriteResult> CreateAsync(
        WorkspacePreparationApprovalEvidence evidence,
        CancellationToken cancellationToken = default);

    Task<WorkspacePreparationApprovalEvidenceReadResult> GetAsync(
        Guid projectId,
        Guid workspaceId,
        Guid approvalId,
        CancellationToken cancellationToken = default);

    /// <summary>Exact plan-keyed lookup used by recovery; it never scans for a latest approval.</summary>
    Task<WorkspacePreparationApprovalEvidenceReadResult> GetForPlanAsync(
        Guid projectId,
        Guid workspaceId,
        Guid planId,
        CancellationToken cancellationToken = default);
}

public sealed record WorkspaceRepositoryMutationResult(bool Succeeded, bool CommandFailed = false, string? ErrorMessage = null);

public interface IWorkspaceRepository
{
    Task<WorkspaceRepositoryDiscovery> DiscoverAsync(string registeredPath, CancellationToken cancellationToken = default);

    /// <summary>The sole permitted Git mutation for APO-46: add a new worktree from an exact commit.</summary>
    Task<WorkspaceRepositoryMutationResult> AddExactWorktreeAsync(
        string commonDirectory,
        string workspaceBranch,
        string managedWorkspacePath,
        string exactBaseCommitSha,
        CancellationToken cancellationToken = default);
}

public enum WorkspaceBranchValidationStatus
{
    Valid,
    Invalid,
    Unavailable
}

public sealed record WorkspaceBranchValidationResult(
    WorkspaceBranchValidationStatus Status,
    string? ErrorMessage = null);

public enum WorkspaceBranchExistenceStatus
{
    Exists,
    NotFound,
    Unavailable
}

public sealed record WorkspaceBranchExistenceResult(
    WorkspaceBranchExistenceStatus Status,
    string? ErrorMessage = null);

/// <summary>Optional Git-authoritative branch checks implemented by the fixed Git adapter.</summary>
public interface IWorkspaceBranchSafety
{
    Task<WorkspaceBranchValidationResult> ValidateBranchNameAsync(
        string commonDirectory,
        string branchName,
        CancellationToken cancellationToken = default);

    Task<WorkspaceBranchExistenceResult> QueryLocalBranchAsync(
        string commonDirectory,
        string branchName,
        CancellationToken cancellationToken = default);
}

public interface IRepositoryPreparationLock
{
    Task<IAsyncDisposable> AcquireAsync(string repositoryIdentity, CancellationToken cancellationToken = default);
}

public interface IWorkspacePreparationPlanningService
{
    Task<WorkspacePreparationPlanningResult> CreatePlanAsync(WorkspacePreparationRequest request, CancellationToken cancellationToken = default);
}

public interface IWorkspacePreparationService
{
    Task<WorkspacePreparationResult> PrepareAsync(
        WorkspacePreparationPlanReference planReference,
        WorkspacePreparationApproval? approval,
        CancellationToken cancellationToken = default);

    Task<WorkspacePreparationResult> FinalizeReceiptAsync(
        WorkspacePreparationPlanReference planReference,
        WorkspacePreparationApproval approval,
        CancellationToken cancellationToken = default);
}

public interface IWorkspaceRecoveryInspectionService
{
    Task<WorkspaceRecoveryInspectionResult> InspectAsync(
        WorkspacePreparationPlanReference planReference,
        CancellationToken cancellationToken = default);
}

public static class WorkspacePreparationIntegrity
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string ComputeContentHash(WorkspacePreparationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return Hash(CanonicalPlanPayload(plan));
    }

    public static string ComputeContentHash(WorkspacePreparationReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        return Hash(CanonicalReceiptPayload(receipt));
    }

    public static string ComputeContentHash(WorkspacePreparationApprovalEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        return Hash(CanonicalApprovalEvidencePayload(evidence));
    }

    public static string ComputeWorkingTreeStateFingerprint(string statusPorcelainOutput) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(statusPorcelainOutput ?? string.Empty))).ToLowerInvariant();

    public static bool IsSha256(string? value) => value is { Length: 64 } && value.All(Uri.IsHexDigit);

    public static bool IsGitObjectId(string? value) => value is { Length: 40 or 64 } && value.All(Uri.IsHexDigit);

    internal static object CanonicalPlanPayload(WorkspacePreparationPlan plan) => new
    {
        schemaVersion = WorkspacePreparationPlanSchema.CurrentVersion,
        projectId = plan.ProjectId,
        workspaceId = plan.WorkspaceId,
        planId = plan.PlanId,
        correlationId = plan.CorrelationId,
        createdAt = plan.CreatedAt,
        context = new { plan.Context.ProjectId, plan.Context.ContextId, plan.Context.ContractVersion, updatedAt = plan.Context.UpdatedAt },
        contractReference = new { plan.ContractReference.ContractId, plan.ContractReference.Revision, plan.ContractReference.SchemaVersion, plan.ContractReference.ContentHash },
        workGraphReference = plan.WorkGraphReference is null ? null : new { plan.WorkGraphReference.GraphId, plan.WorkGraphReference.SchemaVersion, plan.WorkGraphReference.ContentHash },
        workGraphNodeId = plan.WorkGraphNodeId,
        routingDecisionReference = plan.RoutingDecisionReference is null ? null : new { plan.RoutingDecisionReference.DecisionId, plan.RoutingDecisionReference.SchemaVersion, plan.RoutingDecisionReference.ContentHash },
        repository = new
        {
            plan.Repository.Status, plan.Repository.RegisteredPath, plan.Repository.RepositoryRoot, plan.Repository.CommonDirectory,
            plan.Repository.IsBareRepository, plan.Repository.HeadCommitSha, plan.Repository.BranchName, plan.Repository.IsDetached,
            plan.Repository.IsClean, plan.Repository.ChangedFileCount,
            worktrees = plan.Repository.Worktrees.Select(WorktreePayload).ToArray(),
            localBranches = plan.Repository.LocalBranches.ToArray(),
            plan.Repository.WorkingTreeStateFingerprint,
            divergence = new
            {
                plan.Repository.Divergence.State,
                plan.Repository.Divergence.LocalUpstreamReference,
                plan.Repository.Divergence.AheadCount,
                plan.Repository.Divergence.BehindCount
            },
            plan.Repository.WorktreeEvidenceOverflow,
            plan.Repository.LocalBranchEvidenceOverflow
        },
        plan.BaseCommitSha,
        plan.WorkspaceBranch,
        plan.ProposedWorkspacePath,
        policy = plan.Policy.ToString(),
        plan.ApprovalRequired,
        plan.Explanation,
        limitations = plan.Limitations.ToArray()
    };

    internal static object CanonicalReceiptPayload(WorkspacePreparationReceipt receipt) => new
    {
        schemaVersion = WorkspacePreparationReceiptSchema.CurrentVersion,
        projectId = receipt.ProjectId,
        workspaceId = receipt.WorkspaceId,
        correlationId = receipt.CorrelationId,
        preparedAt = receipt.PreparedAt,
        planReference = new { receipt.PlanReference.ProjectId, receipt.PlanReference.PlanId, receipt.PlanReference.SchemaVersion, receipt.PlanReference.ContentHash },
        receipt.WorkspacePath,
        receipt.WorkspaceBranch,
        receipt.BaseCommitSha,
        receipt.ActualHeadCommitSha,
        receipt.RepositoryIdentity,
        receipt.CleanupOwnerReference,
        receipt.CleanupOwner,
        receipt.CleanupPolicy,
        receipt.AutomaticCleanupAllowed,
        approvalReference = receipt.ApprovalReference is null ? null : new
        {
            receipt.ApprovalReference.ApprovalId,
            receipt.ApprovalReference.SchemaVersion,
            receipt.ApprovalReference.ContentHash
        },
        receipt.Limitation
    };

    internal static object CanonicalApprovalEvidencePayload(WorkspacePreparationApprovalEvidence evidence) => new
    {
        schemaVersion = WorkspacePreparationApprovalEvidenceSchema.CurrentVersion,
        evidence.ProjectId,
        evidence.WorkspaceId,
        evidence.ApprovalId,
        planReference = new
        {
            evidence.PlanReference.ProjectId,
            evidence.PlanReference.PlanId,
            evidence.PlanReference.SchemaVersion,
            evidence.PlanReference.ContentHash
        },
        evidence.ActorReference,
        evidence.ApprovedAt,
        evidence.RecordedAt,
        evidence.SanitizedReason
    };

    private static object WorktreePayload(WorkspaceWorktreeEvidence value) => new
    {
        value.Path,
        value.HeadCommitSha,
        value.BranchName,
        value.IsDetached,
        value.IsLocked,
        value.IsPrunable
    };

    private static string Hash(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, Options)))).ToLowerInvariant();
}

/// <summary>Application orchestration for explain-before-write workspace preparation.</summary>
public sealed class WorkspacePreparationPlanningService : IWorkspacePreparationPlanningService
{
    private readonly IProjectRepository _projects;
    private readonly IProjectContextResolver _contexts;
    private readonly IPlanningExecutionContractRepository _contracts;
    private readonly IRoutingDecisionRepository _routing;
    private readonly IWorkspaceRepository _repositories;
    private readonly IWorkspacePreparationPlanRepository _plans;
    private readonly IWorkGraphRepository? _graphs;
    private readonly IHandoffRedactionService _redaction;
    private readonly IClock _clock;
    private readonly IManagedWorkspacePathProvider _paths;

    public WorkspacePreparationPlanningService(
        IProjectRepository projects,
        IProjectContextResolver contexts,
        IPlanningExecutionContractRepository contracts,
        IRoutingDecisionRepository routing,
        IWorkspaceRepository repositories,
        IWorkspacePreparationPlanRepository plans,
        IHandoffRedactionService redaction,
        IClock clock,
        IManagedWorkspacePathProvider paths,
        IWorkGraphRepository? graphs = null)
    {
        _projects = projects; _contexts = contexts; _contracts = contracts; _routing = routing; _repositories = repositories;
        _plans = plans; _redaction = redaction; _clock = clock; _paths = paths; _graphs = graphs;
    }

    public async Task<WorkspacePreparationPlanningResult> CreatePlanAsync(WorkspacePreparationRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ValidateRequest(request, out var validationStatus, out var validationMessage)) return new(validationStatus, ErrorMessage: validationMessage);
        if (_redaction.ValidateIdentityText(request.WorkspaceBranch).RequiresRedaction) return new(WorkspacePreparationPlanningStatus.RedactionRejected, ErrorMessage: "Branch authority crossed the redaction boundary.");

        var project = await _projects.GetByIdAsync(request.ProjectId, cancellationToken).ConfigureAwait(false);
        if (project is null) return new(WorkspacePreparationPlanningStatus.ProjectNotFound, ErrorMessage: "Project was not found.");
        var contextResolution = await _contexts.ResolveAsync(request.ProjectId, cancellationToken).ConfigureAwait(false);
        if (contextResolution.State != ProjectContextResolutionState.Ready || contextResolution.View is null)
            return new(WorkspacePreparationPlanningStatus.ContextMismatch, ErrorMessage: $"Project context is not ready ({contextResolution.State}).");
        var context = contextResolution.View.Context;
        var contractRead = await _contracts.GetAsync(request.ProjectId, request.ContractReference.ContractId, request.ContractReference.Revision, cancellationToken).ConfigureAwait(false);
        if (!contractRead.IsValid || contractRead.Contract is null) return new(WorkspacePreparationPlanningStatus.ContractMissing, ErrorMessage: contractRead.ErrorMessage ?? "Exact planning contract is unavailable.");
        var contract = contractRead.Contract;
        if (!SameContract(contract.Reference, request.ContractReference) || contract.ProjectId != request.ProjectId ||
            contract.Context.ProjectContextId != context.ContextId || contract.Context.ProjectContextContractVersion != context.ContractVersion ||
            contract.RepositoryTarget.Mode != PlanningRepositoryMode.LocalGit ||
            !SamePath(contract.RepositoryTarget.RegisteredLocalPath, project.LocalPath) ||
            !string.Equals(contract.RepositoryTarget.ExpectedHeadCommit, request.BaseCommitSha, StringComparison.OrdinalIgnoreCase) ||
            context.Repository.Selection != RepositorySelectionState.Inspect)
            return new(WorkspacePreparationPlanningStatus.ContractMismatch, ErrorMessage: "Planning contract does not bind the selected project context and repository.");
        if (request.WorkGraphReference is not null)
        {
            var graphRead = await ValidateWorkGraphAsync(request, cancellationToken).ConfigureAwait(false);
            if (!graphRead) return new(WorkspacePreparationPlanningStatus.ContractMismatch, ErrorMessage: "Work-graph reference is not project-bound or valid.");
        }
        if (request.RoutingDecisionReference is not null)
        {
            var routingRead = await _routing.GetAsync(request.ProjectId, request.RoutingDecisionReference.DecisionId, cancellationToken).ConfigureAwait(false);
            if (!routingRead.IsValid || routingRead.Decision is null ||
                !IsUsableRoutingDecision(routingRead.Decision, request.ProjectId, request.ContractReference,
                    new WorkspaceContextIdentity(context.ProjectId, context.ContextId, context.ContractVersion, context.UpdatedAt)) ||
                !SameRouting(routingRead.Decision.Reference, request.RoutingDecisionReference))
                return new(WorkspacePreparationPlanningStatus.ContractMismatch, ErrorMessage: "Routing reference is not an exact project-bound decision.");
        }

        WorkspaceRepositoryDiscovery repository;
        try { repository = await _repositories.DiscoverAsync(project.LocalPath, cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { return new(WorkspacePreparationPlanningStatus.RepositoryUnavailable, ErrorMessage: exception.Message); }
        var mapped = MapDiscovery(repository);
        if (mapped is not null) return new(mapped.Value.status, ErrorMessage: mapped.Value.message);
        if (!SamePath(repository.RepositoryRoot, project.LocalPath) || !SamePath(repository.RepositoryRoot, context.Repository.RepositoryRoot) ||
            !string.Equals(repository.BranchName, contract.RepositoryTarget.ExpectedBranch, StringComparison.Ordinal))
            return new(WorkspacePreparationPlanningStatus.ContextMismatch, ErrorMessage: "Discovered repository identity differs from the project context.");
        if (!string.Equals(repository.HeadCommitSha, request.BaseCommitSha, StringComparison.OrdinalIgnoreCase))
            return new(WorkspacePreparationPlanningStatus.ContextMismatch, ErrorMessage: "The requested base SHA differs from the observed source HEAD.");
        if (string.IsNullOrWhiteSpace(repository.CommonDirectory) || string.IsNullOrWhiteSpace(repository.RepositoryRoot))
            return new(WorkspacePreparationPlanningStatus.RepositoryUnavailable, ErrorMessage: "Repository identity evidence is incomplete.");
        if (_redaction.ValidateIdentityText(repository.CommonDirectory!).RequiresRedaction || _redaction.ValidateIdentityText(repository.RepositoryRoot!).RequiresRedaction)
            return new(WorkspacePreparationPlanningStatus.RedactionRejected, ErrorMessage: "Repository identity crossed the redaction boundary.");

        if (_repositories is IWorkspaceBranchSafety branchSafety)
        {
            var branchValidation = await branchSafety.ValidateBranchNameAsync(repository.CommonDirectory!, request.WorkspaceBranch, cancellationToken).ConfigureAwait(false);
            if (branchValidation.Status == WorkspaceBranchValidationStatus.Invalid)
                return new(WorkspacePreparationPlanningStatus.InvalidBranchName, ErrorMessage: branchValidation.ErrorMessage ?? "Git rejected the workspace branch name.");
            if (branchValidation.Status == WorkspaceBranchValidationStatus.Unavailable)
                return new(WorkspacePreparationPlanningStatus.RepositoryUnavailable, ErrorMessage: branchValidation.ErrorMessage ?? "Git branch validation was unavailable.");

            var branchQuery = await branchSafety.QueryLocalBranchAsync(repository.CommonDirectory!, request.WorkspaceBranch, cancellationToken).ConfigureAwait(false);
            if (branchQuery.Status == WorkspaceBranchExistenceStatus.Unavailable)
                return new(WorkspacePreparationPlanningStatus.RepositoryUnavailable, ErrorMessage: branchQuery.ErrorMessage ?? "Git branch evidence was unavailable.");
            if (branchQuery.Status == WorkspaceBranchExistenceStatus.Exists)
                return new(WorkspacePreparationPlanningStatus.BranchConflict, ErrorMessage: "The requested branch already exists.");
        }
        else if (repository.LocalBranchEvidenceOverflow)
        {
            return new(WorkspacePreparationPlanningStatus.EvidenceOverflow, ErrorMessage: "Complete local-branch evidence is unavailable.");
        }
        else if (repository.LocalBranches.Any(branch => string.Equals(branch, request.WorkspaceBranch, StringComparison.Ordinal)))
        {
            return new(WorkspacePreparationPlanningStatus.BranchConflict, ErrorMessage: "The requested branch already exists.");
        }

        if (repository.Worktrees.Any(worktree => string.Equals(worktree.BranchName, request.WorkspaceBranch, StringComparison.Ordinal)))
            return new(WorkspacePreparationPlanningStatus.BranchConflict, ErrorMessage: "The requested branch already appears in a worktree.");
        if (repository.Worktrees.Any(worktree => SamePath(worktree.Path, _paths.GetWorkspacePath(request.ProjectId, request.WorkspaceId))))
            return new(WorkspacePreparationPlanningStatus.WorktreeConflict, ErrorMessage: "The deterministic workspace path is already a worktree.");
        if (!_paths.IsSafeManagedWorkspacePath(request.ProjectId, request.WorkspaceId, out var workspacePath, out var pathError))
            return new(WorkspacePreparationPlanningStatus.UnsafePath, ErrorMessage: pathError);

        if (repository.Divergence.LocalUpstreamReference is not null && _redaction.ValidateIdentityText(repository.Divergence.LocalUpstreamReference).RequiresRedaction)
            return new(WorkspacePreparationPlanningStatus.RedactionRejected, ErrorMessage: "Local upstream reference crossed the redaction boundary.");

        var limitations = repository.IsClean ? Array.Empty<string>() : new[] { "Source was dirty at planning time; no source cleanup or staging is permitted." };
        var explanation = repository.IsClean ? "Observed source HEAD and repository identity are bound to this immutable plan." : "The source is dirty; preparation is allowed only because the explicit plan policy preserves this warning.";
        WorkspacePreparationPlan plan;
        try
        {
            plan = new(request.ProjectId, request.WorkspaceId, request.PlanId, request.CorrelationId, _clock.UtcNow,
                new WorkspaceContextIdentity(context.ProjectId, context.ContextId, context.ContractVersion, context.UpdatedAt), request.ContractReference,
                request.WorkGraphReference, request.WorkGraphNodeId, request.RoutingDecisionReference, repository, request.BaseCommitSha,
                request.WorkspaceBranch, workspacePath, request.Policy, approvalRequired: true, explanation, limitations);
        }
        catch (ArgumentException exception) { return new(WorkspacePreparationPlanningStatus.PersistenceUnavailable, ErrorMessage: exception.Message); }
        var write = await _plans.CreateAsync(plan, cancellationToken).ConfigureAwait(false);
        return write.Status switch
        {
            WorkspacePreparationPlanWriteStatus.Created => new(WorkspacePreparationPlanningStatus.Planned, plan),
            WorkspacePreparationPlanWriteStatus.PlanConflict => new(WorkspacePreparationPlanningStatus.PlanConflict, ErrorMessage: write.ErrorMessage ?? "Plan id already exists."),
            _ => new(WorkspacePreparationPlanningStatus.PersistenceUnavailable, ErrorMessage: write.ErrorMessage)
        };
    }

    private async Task<bool> ValidateWorkGraphAsync(WorkspacePreparationRequest request, CancellationToken cancellationToken)
    {
        if (request.WorkGraphReference is null || _graphs is null) return false;
        var graphRead = await _graphs.GetAsync(request.ProjectId, request.WorkGraphReference.GraphId, cancellationToken).ConfigureAwait(false);
        if (!graphRead.IsValid || graphRead.Graph is null || graphRead.Graph.ProjectId != request.ProjectId || graphRead.Graph.Reference.SchemaVersion != request.WorkGraphReference.SchemaVersion || !string.Equals(graphRead.Graph.ContentHash, request.WorkGraphReference.ContentHash, StringComparison.OrdinalIgnoreCase)) return false;
        if (request.WorkGraphNodeId is null) return true;
        var node = graphRead.Graph.Nodes.FirstOrDefault(value => value.NodeId == request.WorkGraphNodeId.Value);
        return node is not null && WorkspacePreparationPlanningService.SameContract(node.ContractReference, request.ContractReference);
    }

    private static (WorkspacePreparationPlanningStatus status, string message)? MapDiscovery(WorkspaceRepositoryDiscovery discovery)
    {
        if (discovery.Status == WorkspaceRepositoryDiscoveryStatus.Available &&
            (discovery.WorktreeEvidenceOverflow || discovery.LocalBranchEvidenceOverflow))
            return (WorkspacePreparationPlanningStatus.EvidenceOverflow, "Repository evidence exceeded the supported bound.");

        return discovery.Status switch
        {
            WorkspaceRepositoryDiscoveryStatus.Available => null,
            WorkspaceRepositoryDiscoveryStatus.RepositoryMissing => (WorkspacePreparationPlanningStatus.RepositoryMissing, discovery.ErrorMessage ?? "Repository path is missing."),
            WorkspaceRepositoryDiscoveryStatus.NotGitRepository => (WorkspacePreparationPlanningStatus.NotGitRepository, discovery.ErrorMessage ?? "Path is not a Git repository."),
            WorkspaceRepositoryDiscoveryStatus.EvidenceOverflow => (WorkspacePreparationPlanningStatus.EvidenceOverflow, discovery.ErrorMessage ?? "Repository evidence exceeded the supported bound."),
            _ => (WorkspacePreparationPlanningStatus.RepositoryUnavailable, discovery.ErrorMessage ?? "Repository discovery failed.")
        };
    }

    private static bool ValidateRequest(WorkspacePreparationRequest request, out WorkspacePreparationPlanningStatus status, out string? message)
    {
        status = WorkspacePreparationPlanningStatus.PersistenceUnavailable; message = null;
        if (request.ProjectId == Guid.Empty || request.WorkspaceId == Guid.Empty || request.PlanId == Guid.Empty || request.CorrelationId == Guid.Empty || request.ContractReference is null)
        { message = "Project, workspace, plan, correlation, and contract identifiers are required."; return false; }
        if (!WorkspacePreparationIntegrity.IsGitObjectId(request.BaseCommitSha)) { message = "Base commit must be a full SHA-1 or SHA-256."; return false; }
        if (!IsSafeBranchName(request.WorkspaceBranch))
        { status = WorkspacePreparationPlanningStatus.InvalidBranchName; message = "Workspace branch is empty or too long."; return false; }
        if (!Enum.IsDefined(request.Policy)) { message = "Workspace policy is undefined."; return false; }
        if (request.WorkGraphNodeId is not null && request.WorkGraphReference is null) { message = "Work-graph node requires a graph reference."; return false; }
        return true;
    }

    internal static bool IsSafeBranchName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > WorkspacePreparationLimits.MaxBranchLength || value[0] == '-' || value[^1] == '.' || value.EndsWith(".lock", StringComparison.OrdinalIgnoreCase) || value.Contains("..", StringComparison.Ordinal) || value.Contains("@{", StringComparison.Ordinal)) return false;
        return value[0] != '/' && value[^1] != '/' && !value.Contains("//", StringComparison.Ordinal) && value.All(character => char.IsLetterOrDigit(character) || character is '/' or '-' or '_' or '.');
    }

    internal static bool SameContract(PlanningExecutionContractReference left, PlanningExecutionContractReference right) => left.ContractId == right.ContractId && left.Revision == right.Revision && left.SchemaVersion == right.SchemaVersion && string.Equals(left.ContentHash, right.ContentHash, StringComparison.OrdinalIgnoreCase);
    internal static bool SameRouting(RoutingDecisionReference left, RoutingDecisionReference right) => left.DecisionId == right.DecisionId && left.SchemaVersion == right.SchemaVersion && string.Equals(left.ContentHash, right.ContentHash, StringComparison.OrdinalIgnoreCase);
    internal static bool SameContext(WorkspaceContextIdentity left, WorkspaceContextIdentity right) =>
        left.ProjectId == right.ProjectId && left.ContextId == right.ContextId &&
        left.ContractVersion == right.ContractVersion && left.UpdatedAt == right.UpdatedAt;

    internal static bool IsUsableRoutingDecision(
        RoutingDecision decision,
        Guid projectId,
        PlanningExecutionContractReference contractReference,
        WorkspaceContextIdentity context)
    {
        if (decision.ProjectId != projectId || decision.Input.ProjectId != projectId ||
            !SameContract(decision.Input.PlanningContractReference, contractReference) ||
            decision.Input.Context.ContextId != context.ContextId ||
            decision.Input.Context.ContextContractVersion != context.ContractVersion ||
            decision.Input.Context.UpdatedAt != context.UpdatedAt ||
            decision.Recommendation is null || decision.SelectedAgentId is null ||
            decision.Recommendation.SelectedAgentId != decision.SelectedAgentId ||
            decision.Input.Candidates.All(candidate => candidate.AgentId != decision.SelectedAgentId.Value) ||
            decision.CandidateAssessments.All(assessment => assessment.AgentId != decision.SelectedAgentId.Value) ||
            decision.Outcome is not (RoutingDecisionOutcome.Recommended or RoutingDecisionOutcome.OwnerOverrideApplied))
        {
            return false;
        }

        var selectedAssessment = decision.CandidateAssessments.FirstOrDefault(value => value.AgentId == decision.SelectedAgentId.Value);
        return selectedAssessment is not null && selectedAssessment.IsEligible &&
            selectedAssessment.Candidate.Identity.Id == decision.Recommendation.SelectedAgentIdentity.Id &&
            string.Equals(selectedAssessment.Candidate.Identity.DisplayName, decision.Recommendation.SelectedAgentIdentity.DisplayName, StringComparison.Ordinal) &&
            string.Equals(selectedAssessment.Candidate.Identity.Provider, decision.Recommendation.SelectedAgentIdentity.Provider, StringComparison.Ordinal) &&
            string.Equals(selectedAssessment.Candidate.Identity.ModelIdentifier, decision.Recommendation.SelectedAgentIdentity.ModelIdentifier, StringComparison.Ordinal);
    }

    /// <summary>
    /// Central, exact comparison used by preparation and recovery. A receipt is evidence only
    /// when every project, plan, approval, repository, path, branch, and observed worktree fact
    /// still agrees; callers must not substitute a latest or merely similar record.
    /// </summary>
    public static bool ReceiptMatchesExactPlan(
        WorkspacePreparationPlan plan,
        WorkspacePreparationReceipt receipt,
        WorkspacePreparationApprovalEvidence? approvalEvidence,
        string managedWorkspacePath,
        WorkspaceRepositoryDiscovery discovery,
        out string? errorMessage)
    {
        errorMessage = null;
        if (plan is null || receipt is null || discovery is null)
        {
            errorMessage = "Plan, receipt, and discovery evidence are required.";
            return false;
        }

        var worktree = discovery.Worktrees.FirstOrDefault(value => SamePath(value.Path, managedWorkspacePath));
        var approvalMatches = receipt.ApprovalReference is not null && approvalEvidence is not null &&
            approvalEvidence.ProjectId == plan.ProjectId &&
            approvalEvidence.WorkspaceId == plan.WorkspaceId &&
            approvalEvidence.ApprovalId == receipt.ApprovalReference.ApprovalId &&
            approvalEvidence.PlanReference.ProjectId == plan.ProjectId &&
            approvalEvidence.PlanReference.PlanId == plan.PlanId &&
            approvalEvidence.PlanReference.SchemaVersion == plan.Reference.SchemaVersion &&
            string.Equals(approvalEvidence.PlanReference.ContentHash, plan.ContentHash, StringComparison.OrdinalIgnoreCase) &&
            approvalEvidence.ContentHash.Equals(receipt.ApprovalReference.ContentHash, StringComparison.OrdinalIgnoreCase) &&
            approvalEvidence.PlanReference.SchemaVersion == receipt.ApprovalReference.SchemaVersion;
        var exact = plan.ProjectId == receipt.ProjectId &&
            plan.WorkspaceId == receipt.WorkspaceId &&
            plan.CorrelationId == receipt.CorrelationId &&
            receipt.PlanReference.ProjectId == plan.ProjectId &&
            receipt.PlanReference.PlanId == plan.PlanId &&
            receipt.PlanReference.SchemaVersion == plan.Reference.SchemaVersion &&
            string.Equals(receipt.PlanReference.ContentHash, plan.ContentHash, StringComparison.OrdinalIgnoreCase) &&
            SamePath(receipt.WorkspacePath, plan.ProposedWorkspacePath) &&
            SamePath(receipt.WorkspacePath, managedWorkspacePath) &&
            string.Equals(receipt.WorkspaceBranch, plan.WorkspaceBranch, StringComparison.Ordinal) &&
            string.Equals(receipt.BaseCommitSha, plan.BaseCommitSha, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(receipt.RepositoryIdentity, plan.Repository.CommonDirectory, StringComparison.OrdinalIgnoreCase) &&
            receipt.CleanupOwner == WorkspaceCleanupOwner.APO &&
            receipt.CleanupPolicy == WorkspaceCleanupPolicy.ExplicitActionRequired &&
            !receipt.AutomaticCleanupAllowed &&
            approvalMatches &&
            discovery.Status == WorkspaceRepositoryDiscoveryStatus.Available &&
            !discovery.WorktreeEvidenceOverflow &&
            !discovery.LocalBranchEvidenceOverflow &&
            SamePath(discovery.RegisteredPath, plan.Repository.RegisteredPath) &&
            SamePath(discovery.RepositoryRoot, plan.Repository.RepositoryRoot) &&
            SamePath(discovery.CommonDirectory, plan.Repository.CommonDirectory) &&
            discovery.IsBareRepository == plan.Repository.IsBareRepository &&
            string.Equals(discovery.HeadCommitSha, plan.Repository.HeadCommitSha, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(discovery.BranchName, plan.Repository.BranchName, StringComparison.Ordinal) &&
            discovery.IsDetached == plan.Repository.IsDetached &&
            discovery.IsClean == plan.Repository.IsClean &&
            discovery.ChangedFileCount == plan.Repository.ChangedFileCount &&
            string.Equals(discovery.WorkingTreeStateFingerprint, plan.Repository.WorkingTreeStateFingerprint, StringComparison.OrdinalIgnoreCase) &&
            discovery.Divergence.State == plan.Repository.Divergence.State &&
            string.Equals(discovery.Divergence.LocalUpstreamReference, plan.Repository.Divergence.LocalUpstreamReference, StringComparison.Ordinal) &&
            discovery.Divergence.AheadCount == plan.Repository.Divergence.AheadCount &&
            discovery.Divergence.BehindCount == plan.Repository.Divergence.BehindCount &&
            worktree is not null &&
            string.Equals(worktree.BranchName, plan.WorkspaceBranch, StringComparison.Ordinal) &&
            !worktree.IsDetached &&
            string.Equals(worktree.HeadCommitSha, plan.BaseCommitSha, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(receipt.ActualHeadCommitSha, worktree.HeadCommitSha, StringComparison.OrdinalIgnoreCase);

        if (!exact) errorMessage = "Receipt, approval, repository, or worktree evidence does not match the exact plan.";
        return exact;
    }

    internal static bool SamePath(string? left, string? right)
    {
        if (left is null || right is null) return false;
        static string Normalize(string value) => Path.GetFullPath(value.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>Preparation service with a single mutation gate and read-only revalidation.</summary>
public sealed class WorkspacePreparationService : IWorkspacePreparationService, IWorkspaceRecoveryInspectionService
{
    private readonly IWorkspacePreparationPlanRepository _plans;
    private readonly IWorkspacePreparationReceiptRepository _receipts;
    private readonly IWorkspaceRepository _repositories;
    private readonly IRepositoryPreparationLock _locks;
    private readonly IManagedWorkspacePathProvider _paths;
    private readonly IHandoffRedactionService _redaction;
    private readonly IClock _clock;
    private readonly IProjectContextResolver? _contexts;
    private readonly IPlanningExecutionContractRepository? _contracts;
    private readonly IRoutingDecisionRepository? _routing;
    private readonly IWorkspacePreparationApprovalEvidenceRepository? _approvalEvidence;

    public WorkspacePreparationService(
        IWorkspacePreparationPlanRepository plans,
        IWorkspacePreparationReceiptRepository receipts,
        IWorkspaceRepository repositories,
        IRepositoryPreparationLock locks,
        IManagedWorkspacePathProvider paths,
        IHandoffRedactionService redaction,
        IClock clock,
        IProjectContextResolver? contexts = null,
        IPlanningExecutionContractRepository? contracts = null,
        IRoutingDecisionRepository? routing = null,
        IWorkspacePreparationApprovalEvidenceRepository? approvalEvidence = null)
    {
        _plans = plans; _receipts = receipts; _repositories = repositories; _locks = locks; _paths = paths;
        _redaction = redaction; _clock = clock; _contexts = contexts; _contracts = contracts; _routing = routing;
        _approvalEvidence = approvalEvidence;
    }

    public async Task<WorkspacePreparationResult> PrepareAsync(WorkspacePreparationPlanReference planReference, WorkspacePreparationApproval? approval, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var loaded = await LoadPlanAsync(planReference, cancellationToken).ConfigureAwait(false);
        if (loaded.result is not null) return loaded.result;
        var plan = loaded.plan!;
        if (!plan.ApprovalRequired) return new(WorkspacePreparationStatus.ApprovalMismatch, ErrorMessage: "The plan does not carry a valid approval gate.");
        if (approval is null) return new(WorkspacePreparationStatus.ApprovalRequired, ErrorMessage: "An explicit approval bound to the exact plan is required.");
        var approvalResult = ValidateApproval(plan, approval);
        if (approvalResult is not null) return approvalResult;
        var evidenceResult = await EnsureApprovalEvidenceAsync(plan, approval, cancellationToken).ConfigureAwait(false);
        if (evidenceResult.result is not null) return evidenceResult.result;
        return await PrepareUnderLockAsync(plan, approval, evidenceResult.evidence!, cancellationToken).ConfigureAwait(false);
    }

    public async Task<WorkspacePreparationResult> FinalizeReceiptAsync(WorkspacePreparationPlanReference planReference, WorkspacePreparationApproval approval, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var loaded = await LoadPlanAsync(planReference, cancellationToken).ConfigureAwait(false);
        if (loaded.result is not null) return loaded.result;
        var plan = loaded.plan!;
        var approvalResult = ValidateApproval(plan, approval);
        if (approvalResult is not null) return approvalResult;
        var evidenceResult = await EnsureApprovalEvidenceAsync(plan, approval, cancellationToken).ConfigureAwait(false);
        if (evidenceResult.result is not null) return evidenceResult.result;
        if (!_paths.IsSafeManagedWorkspacePath(plan.ProjectId, plan.WorkspaceId, out var managedPath, out var pathError))
            return new(WorkspacePreparationStatus.UnsafePath, ErrorMessage: pathError);
        if (!WorkspacePreparationPlanningService.SamePath(plan.ProposedWorkspacePath, managedPath))
            return new(WorkspacePreparationStatus.UnsafePath, ErrorMessage: "Plan workspace path is not the deterministic managed destination.");
        if (!await BoundAuthorityStillMatchesAsync(plan, cancellationToken).ConfigureAwait(false))
            return new(WorkspacePreparationStatus.PlanStale, ErrorMessage: "The exact planning contract or context binding changed after planning.");

        IAsyncDisposable repositoryLock;
        try { repositoryLock = await _locks.AcquireAsync(plan.Repository.CommonDirectory!, cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { throw; }
        catch (IOException exception) { return new(WorkspacePreparationStatus.RepositoryUnavailable, ErrorMessage: exception.Message); }
        await using (repositoryLock)
        {
            var receiptRead = await _receipts.GetAsync(plan.ProjectId, plan.WorkspaceId, cancellationToken).ConfigureAwait(false);
            if (receiptRead.State is not (WorkspacePreparationReceiptReadState.Missing or WorkspacePreparationReceiptReadState.Valid))
                return new(WorkspacePreparationStatus.PersistenceUnavailable, ErrorMessage: receiptRead.ErrorMessage ?? "Receipt cannot be read safely.");
            var current = await _repositories.DiscoverAsync(plan.Repository.RepositoryRoot!, cancellationToken).ConfigureAwait(false);
            if (current.Status != WorkspaceRepositoryDiscoveryStatus.Available)
                return new(WorkspacePreparationStatus.RepositoryUnavailable, ErrorMessage: current.ErrorMessage ?? "Repository evidence is unavailable.");
            if (!SourceStateMatchesPlan(plan, current, out var sourceError))
                return new(WorkspacePreparationStatus.PlanStale, ErrorMessage: sourceError);
            if (receiptRead.Receipt is not null)
            {
                var existingEvidence = await ReadApprovalEvidenceForReceiptAsync(plan, receiptRead.Receipt, cancellationToken).ConfigureAwait(false);
                if (_approvalEvidence is not null && existingEvidence is null)
                    return new(WorkspacePreparationStatus.ReceiptConflict, receiptRead.Receipt, ErrorMessage: "Receipt approval evidence is unavailable or invalid.");
                if (!WorkspacePreparationPlanningService.ReceiptMatchesExactPlan(plan, receiptRead.Receipt, existingEvidence, managedPath, current, out var associationError))
                    return new(WorkspacePreparationStatus.ReceiptConflict, receiptRead.Receipt, ErrorMessage: associationError);
                return new(WorkspacePreparationStatus.AlreadyPrepared, receiptRead.Receipt);
            }

            if (!MatchesPreparedWorkspace(plan, current, out var found))
                return new(WorkspacePreparationStatus.VerificationFailed, ErrorMessage: "The existing worktree does not exactly match the plan.");
            var newReceipt = CreateReceipt(plan, found!, evidenceResult.evidence!);
            var write = await _receipts.CreateAsync(newReceipt, cancellationToken).ConfigureAwait(false);
            return write.Succeeded
                ? new(WorkspacePreparationStatus.Prepared, newReceipt)
                : new(WorkspacePreparationStatus.ReceiptPersistenceFailed, ErrorMessage: write.ErrorMessage);
        }
    }

    public async Task<WorkspaceRecoveryInspectionResult> InspectAsync(WorkspacePreparationPlanReference planReference, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var loaded = await LoadPlanAsync(planReference, cancellationToken).ConfigureAwait(false);
        if (loaded.result is not null) return new(loaded.result.Status == WorkspacePreparationStatus.PlanIntegrityFailure ? WorkspaceRecoveryState.IntegrityFailure : WorkspaceRecoveryState.Unavailable, ErrorMessage: loaded.result.ErrorMessage);
        var plan = loaded.plan!;
        if (!_paths.IsSafeManagedWorkspacePath(plan.ProjectId, plan.WorkspaceId, out var path, out var pathError)) return new(WorkspaceRecoveryState.UnsafePath, ErrorMessage: pathError);
        var receiptRead = await _receipts.GetAsync(plan.ProjectId, plan.WorkspaceId, cancellationToken).ConfigureAwait(false);
        if (receiptRead.State == WorkspacePreparationReceiptReadState.IntegrityFailure) return new(WorkspaceRecoveryState.IntegrityFailure, ErrorMessage: receiptRead.ErrorMessage);
        if (receiptRead.State is not (WorkspacePreparationReceiptReadState.Missing or WorkspacePreparationReceiptReadState.Valid)) return new(WorkspaceRecoveryState.Unavailable, ErrorMessage: receiptRead.ErrorMessage ?? "Receipt cannot be read safely.");
        var current = await _repositories.DiscoverAsync(plan.Repository.RepositoryRoot!, cancellationToken).ConfigureAwait(false);
        if (current.Status != WorkspaceRepositoryDiscoveryStatus.Available)
            return new(WorkspaceRecoveryState.Unavailable, ErrorMessage: current.ErrorMessage ?? "Repository evidence is unavailable.");
        if (!SourceStateMatchesPlan(plan, current, out var sourceError))
            return new(WorkspaceRecoveryState.Conflict, receiptRead.Receipt, ErrorMessage: sourceError);
        WorkspacePreparationApprovalEvidence? approvalEvidence = null;
        if (receiptRead.Receipt?.ApprovalReference is not null && _approvalEvidence is not null)
        {
            var evidenceRead = await _approvalEvidence.GetAsync(plan.ProjectId, plan.WorkspaceId, receiptRead.Receipt.ApprovalReference.ApprovalId, cancellationToken).ConfigureAwait(false);
            if (!evidenceRead.IsValid) return new(WorkspaceRecoveryState.Unavailable, ErrorMessage: evidenceRead.ErrorMessage ?? "Approval evidence is unavailable.");
            approvalEvidence = evidenceRead.Evidence;
        }

        if (receiptRead.Receipt is not null)
        {
            var matches = WorkspacePreparationPlanningService.ReceiptMatchesExactPlan(plan, receiptRead.Receipt, approvalEvidence, path, current, out var associationError);
            return matches
                ? new(WorkspaceRecoveryState.PreparedAndRecorded, receiptRead.Receipt)
                : new(WorkspaceRecoveryState.Conflict, receiptRead.Receipt, ErrorMessage: associationError);
        }

        if (_approvalEvidence is not null)
        {
            var evidenceRead = await _approvalEvidence.GetForPlanAsync(plan.ProjectId, plan.WorkspaceId, plan.PlanId, cancellationToken).ConfigureAwait(false);
            if (!evidenceRead.IsValid) return new(WorkspaceRecoveryState.Unavailable, ErrorMessage: evidenceRead.ErrorMessage ?? "Approval evidence is unavailable.");
            approvalEvidence = evidenceRead.Evidence;
        }

        var matchesWithoutReceipt = MatchesPreparedWorkspace(plan, current, out _);
        if (matchesWithoutReceipt && approvalEvidence is not null)
            return new(WorkspaceRecoveryState.PreparedWithoutReceipt, NextSafeAction: "FinalizeReceipt");
        if (Directory.Exists(path) || File.Exists(path)) return new(WorkspaceRecoveryState.ForeignWorkspace, NextSafeAction: "Owner inspection required.");
        return new(WorkspaceRecoveryState.NotPrepared);
    }

    private async Task<WorkspacePreparationResult> PrepareUnderLockAsync(WorkspacePreparationPlan plan, WorkspacePreparationApproval approval, WorkspacePreparationApprovalEvidence evidence, CancellationToken cancellationToken)
    {
        IAsyncDisposable repositoryLock;
        try { repositoryLock = await _locks.AcquireAsync(plan.Repository.CommonDirectory!, cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { throw; }
        catch (IOException exception) { return new(WorkspacePreparationStatus.RepositoryUnavailable, ErrorMessage: exception.Message); }
        await using (repositoryLock)
        {
            var existingReceipt = await _receipts.GetAsync(plan.ProjectId, plan.WorkspaceId, cancellationToken).ConfigureAwait(false);
            if (existingReceipt.State is not (WorkspacePreparationReceiptReadState.Missing or WorkspacePreparationReceiptReadState.Valid))
                return new(WorkspacePreparationStatus.PersistenceUnavailable, ErrorMessage: existingReceipt.ErrorMessage ?? "Receipt cannot be read safely.");
            if (!_paths.IsSafeManagedWorkspacePath(plan.ProjectId, plan.WorkspaceId, out var managedPath, out var pathError))
                return new(WorkspacePreparationStatus.UnsafePath, ErrorMessage: pathError);
            if (!WorkspacePreparationPlanningService.SamePath(plan.ProposedWorkspacePath, managedPath))
                return new(WorkspacePreparationStatus.UnsafePath, ErrorMessage: "Plan workspace path is not the deterministic managed destination.");
            if (!await BoundAuthorityStillMatchesAsync(plan, cancellationToken).ConfigureAwait(false))
                return new(WorkspacePreparationStatus.PlanStale, ErrorMessage: "The exact planning contract or context binding changed after planning.");

            var current = await _repositories.DiscoverAsync(plan.Repository.RepositoryRoot!, cancellationToken).ConfigureAwait(false);
            if (current.Status == WorkspaceRepositoryDiscoveryStatus.EvidenceOverflow)
                return new(WorkspacePreparationStatus.RepositoryUnavailable, ErrorMessage: current.ErrorMessage ?? "Complete repository evidence is unavailable.");
            if (current.Status != WorkspaceRepositoryDiscoveryStatus.Available)
                return new(WorkspacePreparationStatus.RepositoryUnavailable, ErrorMessage: current.ErrorMessage);
            if (!SourceStateMatchesPlan(plan, current, out var sourceError))
                return new(WorkspacePreparationStatus.PlanStale, ErrorMessage: sourceError);
            if (plan.Policy == WorkspacePreparationPolicy.RequireCleanSource)
            {
                if (!current.IsClean)
                    return new(WorkspacePreparationStatus.PolicyBlocked, ErrorMessage: "Source is dirty and the approved plan requires a clean source.");
                if (!string.Equals(current.WorkingTreeStateFingerprint,
                    WorkspacePreparationIntegrity.ComputeWorkingTreeStateFingerprint(string.Empty), StringComparison.OrdinalIgnoreCase))
                    return new(WorkspacePreparationStatus.PlanStale, ErrorMessage: "Clean-source evidence does not match the canonical clean working-tree fingerprint.");
            }

            if (existingReceipt.Receipt is not null)
            {
                var existingEvidence = await ReadApprovalEvidenceForReceiptAsync(plan, existingReceipt.Receipt, cancellationToken).ConfigureAwait(false);
                if (_approvalEvidence is not null && existingEvidence is null)
                    return new(WorkspacePreparationStatus.ReceiptConflict, existingReceipt.Receipt, ErrorMessage: "Receipt approval evidence is unavailable or invalid.");
                if (!WorkspacePreparationPlanningService.ReceiptMatchesExactPlan(plan, existingReceipt.Receipt, existingEvidence, managedPath, current, out var associationError))
                    return new(WorkspacePreparationStatus.ReceiptConflict, existingReceipt.Receipt, ErrorMessage: associationError);
                return new(WorkspacePreparationStatus.AlreadyPrepared, existingReceipt.Receipt);
            }

            if (_repositories is IWorkspaceBranchSafety branchSafety)
            {
                var branchQuery = await branchSafety.QueryLocalBranchAsync(plan.Repository.CommonDirectory!, plan.WorkspaceBranch, cancellationToken).ConfigureAwait(false);
                if (branchQuery.Status == WorkspaceBranchExistenceStatus.Unavailable)
                    return new(WorkspacePreparationStatus.RepositoryUnavailable, ErrorMessage: branchQuery.ErrorMessage ?? "Git branch evidence was unavailable.");
                if (branchQuery.Status == WorkspaceBranchExistenceStatus.Exists)
                    return new(WorkspacePreparationStatus.BranchConflict, ErrorMessage: "Workspace branch is already present.");
            }
            else if (current.LocalBranchEvidenceOverflow)
            {
                return new(WorkspacePreparationStatus.RepositoryUnavailable, ErrorMessage: "Complete local-branch evidence is unavailable.");
            }
            else if (current.LocalBranches.Any(branch => string.Equals(branch, plan.WorkspaceBranch, StringComparison.Ordinal)))
            {
                return new(WorkspacePreparationStatus.BranchConflict, ErrorMessage: "Workspace branch is already present.");
            }

            if (current.Worktrees.Any(worktree => string.Equals(worktree.BranchName, plan.WorkspaceBranch, StringComparison.Ordinal)))
                return new(WorkspacePreparationStatus.BranchConflict, ErrorMessage: "Workspace branch is already present.");
            if (current.Worktrees.Any(worktree => WorkspacePreparationPlanningService.SamePath(worktree.Path, managedPath)))
                return new(WorkspacePreparationStatus.WorktreeConflict, ErrorMessage: "Workspace path is already registered as a worktree.");
            if (Directory.Exists(managedPath) || File.Exists(managedPath))
                return new(WorkspacePreparationStatus.PathConflict, ErrorMessage: "Managed workspace target already exists and will not be overwritten.");
            if (_redaction.ValidateIdentityText(plan.WorkspaceBranch).RequiresRedaction || _redaction.ValidateIdentityText(plan.Repository.CommonDirectory!).RequiresRedaction)
                return new(WorkspacePreparationStatus.RedactionRejected, ErrorMessage: "Repository or branch authority crossed the redaction boundary.");

            cancellationToken.ThrowIfCancellationRequested();
            var mutation = await _repositories.AddExactWorktreeAsync(plan.Repository.CommonDirectory!, plan.WorkspaceBranch, managedPath, plan.BaseCommitSha, cancellationToken).ConfigureAwait(false);
            if (!mutation.Succeeded) return new(WorkspacePreparationStatus.GitCommandFailed, ErrorMessage: mutation.ErrorMessage ?? "Git worktree creation failed.");
            // Verification is read-only. No rollback command exists by design if this or receipt write fails.
            var verified = await _repositories.DiscoverAsync(plan.Repository.RepositoryRoot!, cancellationToken).ConfigureAwait(false);
            if (!SourceStateMatchesPlan(plan, verified, out var verifiedSourceError))
                return new(WorkspacePreparationStatus.VerificationFailed, ErrorMessage: $"Git succeeded but source evidence changed: {verifiedSourceError}");
            if (!MatchesPreparedWorkspace(plan, verified, out var worktree))
                return new(WorkspacePreparationStatus.VerificationFailed, ErrorMessage: "Git succeeded but the exact workspace evidence could not be verified.");
            var receipt = CreateReceipt(plan, worktree!, evidence);
            var write = await _receipts.CreateAsync(receipt, cancellationToken).ConfigureAwait(false);
            return write.Succeeded ? new(WorkspacePreparationStatus.Prepared, receipt) : new(WorkspacePreparationStatus.ReceiptPersistenceFailed, ErrorMessage: write.ErrorMessage);
        }
    }

    private WorkspacePreparationReceipt CreateReceipt(WorkspacePreparationPlan plan, WorkspaceWorktreeEvidence worktree, WorkspacePreparationApprovalEvidence evidence) =>
        new(plan.ProjectId, plan.WorkspaceId, plan.CorrelationId, _clock.UtcNow, plan.Reference, worktree.Path, plan.WorkspaceBranch,
            plan.BaseCommitSha, worktree.HeadCommitSha, plan.Repository.CommonDirectory!, "APO",
            limitation: plan.Limitations.FirstOrDefault(), cleanupOwner: WorkspaceCleanupOwner.APO,
            cleanupPolicy: WorkspaceCleanupPolicy.ExplicitActionRequired, automaticCleanupAllowed: false,
            approvalReference: new WorkspacePreparationApprovalReference(evidence.ApprovalId,
                WorkspacePreparationApprovalEvidenceSchema.CurrentVersion, evidence.ContentHash));

    private static bool MatchesPreparedWorkspace(WorkspacePreparationPlan plan, WorkspaceRepositoryDiscovery discovery, out WorkspaceWorktreeEvidence? worktree)
    {
        worktree = discovery.Worktrees.FirstOrDefault(value => WorkspacePreparationPlanningService.SamePath(value.Path, plan.ProposedWorkspacePath));
        return discovery.Status == WorkspaceRepositoryDiscoveryStatus.Available && !discovery.WorktreeEvidenceOverflow && !discovery.LocalBranchEvidenceOverflow &&
            worktree is not null && !worktree.IsDetached && string.Equals(worktree.BranchName, plan.WorkspaceBranch, StringComparison.Ordinal) &&
            string.Equals(worktree.HeadCommitSha, plan.BaseCommitSha, StringComparison.OrdinalIgnoreCase);
    }

    private static bool SourceStateMatchesPlan(WorkspacePreparationPlan plan, WorkspaceRepositoryDiscovery current, out string? errorMessage)
    {
        errorMessage = null;
        if (current.Status != WorkspaceRepositoryDiscoveryStatus.Available || current.WorktreeEvidenceOverflow || current.LocalBranchEvidenceOverflow)
        {
            errorMessage = "Complete repository evidence is unavailable.";
            return false;
        }

        var planned = plan.Repository;
        var matches = WorkspacePreparationPlanningService.SamePath(current.RegisteredPath, planned.RegisteredPath) &&
            WorkspacePreparationPlanningService.SamePath(current.RepositoryRoot, planned.RepositoryRoot) &&
            WorkspacePreparationPlanningService.SamePath(current.CommonDirectory, planned.CommonDirectory) &&
            current.IsBareRepository == planned.IsBareRepository &&
            string.Equals(current.HeadCommitSha, planned.HeadCommitSha, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(current.BranchName, planned.BranchName, StringComparison.Ordinal) &&
            current.IsDetached == planned.IsDetached &&
            current.IsClean == planned.IsClean &&
            current.ChangedFileCount == planned.ChangedFileCount &&
            string.Equals(current.WorkingTreeStateFingerprint, planned.WorkingTreeStateFingerprint, StringComparison.OrdinalIgnoreCase) &&
            current.Divergence.State == planned.Divergence.State &&
            string.Equals(current.Divergence.LocalUpstreamReference, planned.Divergence.LocalUpstreamReference, StringComparison.Ordinal) &&
            current.Divergence.AheadCount == planned.Divergence.AheadCount &&
            current.Divergence.BehindCount == planned.Divergence.BehindCount;

        if (!matches) errorMessage = "A plan-critical repository or source-state fact changed after planning.";
        return matches;
    }

    private async Task<WorkspacePreparationApprovalEvidence?> ReadApprovalEvidenceForReceiptAsync(
        WorkspacePreparationPlan plan,
        WorkspacePreparationReceipt receipt,
        CancellationToken cancellationToken)
    {
        if (_approvalEvidence is null || receipt.ApprovalReference is null) return null;
        var read = await _approvalEvidence.GetAsync(plan.ProjectId, plan.WorkspaceId, receipt.ApprovalReference.ApprovalId, cancellationToken).ConfigureAwait(false);
        return read.IsValid ? read.Evidence : null;
    }

    private async Task<(WorkspacePreparationApprovalEvidence? evidence, WorkspacePreparationResult? result)> EnsureApprovalEvidenceAsync(
        WorkspacePreparationPlan plan,
        WorkspacePreparationApproval approval,
        CancellationToken cancellationToken)
    {
        var sanitizedReason = approval.Reason is null ? null : _redaction.Redact(approval.Reason).Value;
        WorkspacePreparationApprovalEvidence evidence;
        try
        {
            evidence = new WorkspacePreparationApprovalEvidence(plan.ProjectId, plan.WorkspaceId, approval.ApprovalId,
                new WorkspacePreparationPlanReference(plan.PlanId, plan.Reference.SchemaVersion, plan.ContentHash, plan.ProjectId),
                approval.ActorReference, approval.ApprovedAt, _clock.UtcNow, sanitizedReason);
        }
        catch (ArgumentException exception)
        {
            return (null, new(WorkspacePreparationStatus.ApprovalMismatch, ErrorMessage: exception.Message));
        }

        if (_approvalEvidence is null)
            return (null, new(WorkspacePreparationStatus.ApprovalEvidencePersistenceFailed, ErrorMessage: "Approval evidence persistence is not configured."));

        // A retry/finalize operation must reuse the original immutable evidence. Re-recording the
        // same approval would change RecordedAt and therefore manufacture a different authority
        // hash even though the owner supplied the same approval.
        var existingRead = await _approvalEvidence.GetAsync(plan.ProjectId, plan.WorkspaceId, approval.ApprovalId, cancellationToken).ConfigureAwait(false);
        if (existingRead.IsValid)
        {
            return SameApprovalEvidence(existingRead.Evidence!, evidence)
                ? (existingRead.Evidence, null)
                : (null, new(WorkspacePreparationStatus.ApprovalEvidenceConflict, ErrorMessage: "Approval id is already bound to different evidence."));
        }
        if (existingRead.State is not WorkspacePreparationApprovalEvidenceReadState.Missing)
        {
            return (null, new(WorkspacePreparationStatus.ApprovalEvidenceConflict, ErrorMessage: existingRead.ErrorMessage ?? "Existing approval evidence cannot be read safely."));
        }

        var write = await _approvalEvidence.CreateAsync(evidence, cancellationToken).ConfigureAwait(false);
        if (write.Status == WorkspacePreparationApprovalEvidenceWriteStatus.Created) return (evidence, null);
        if (write.Status == WorkspacePreparationApprovalEvidenceWriteStatus.Unavailable)
            return (null, new(WorkspacePreparationStatus.ApprovalEvidencePersistenceFailed, ErrorMessage: write.ErrorMessage ?? "Approval evidence could not be durably recorded."));

        var existing = await _approvalEvidence.GetAsync(plan.ProjectId, plan.WorkspaceId, approval.ApprovalId, cancellationToken).ConfigureAwait(false);
        if (existing.IsValid && SameApprovalEvidence(existing.Evidence!, evidence)) return (existing.Evidence, null);
        return (null, new(WorkspacePreparationStatus.ApprovalEvidenceConflict, ErrorMessage: write.ErrorMessage ?? "Approval id is already bound to different evidence."));
    }

    private static bool SameApprovalEvidence(WorkspacePreparationApprovalEvidence left, WorkspacePreparationApprovalEvidence right) =>
        left.ProjectId == right.ProjectId && left.WorkspaceId == right.WorkspaceId && left.ApprovalId == right.ApprovalId &&
        left.PlanReference.ProjectId == right.PlanReference.ProjectId && left.PlanReference.PlanId == right.PlanReference.PlanId &&
        left.PlanReference.SchemaVersion == right.PlanReference.SchemaVersion &&
        string.Equals(left.PlanReference.ContentHash, right.PlanReference.ContentHash, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.ActorReference, right.ActorReference, StringComparison.Ordinal) && left.ApprovedAt == right.ApprovedAt &&
        string.Equals(left.SanitizedReason, right.SanitizedReason, StringComparison.Ordinal);

    private async Task<bool> BoundAuthorityStillMatchesAsync(WorkspacePreparationPlan plan, CancellationToken cancellationToken)
    {
        if (_contexts is null || _contracts is null)
            return _contexts is null && _contracts is null && _routing is null && plan.RoutingDecisionReference is null;
        var contextResult = await _contexts.ResolveAsync(plan.ProjectId, cancellationToken).ConfigureAwait(false);
        if (contextResult.State != ProjectContextResolutionState.Ready || contextResult.View is null) return false;
        var context = contextResult.View.Context;
        var currentContext = new WorkspaceContextIdentity(context.ProjectId, context.ContextId, context.ContractVersion, context.UpdatedAt);
        if (!WorkspacePreparationPlanningService.SameContext(currentContext, plan.Context)) return false;
        var contractResult = await _contracts.GetAsync(plan.ProjectId, plan.ContractReference.ContractId, plan.ContractReference.Revision, cancellationToken).ConfigureAwait(false);
        if (!contractResult.IsValid || contractResult.Contract is null || contractResult.Contract.ProjectId != plan.ProjectId || !WorkspacePreparationPlanningService.SameContract(contractResult.Contract.Reference, plan.ContractReference) || contractResult.Contract.Context.ProjectContextId != plan.Context.ContextId || contractResult.Contract.Context.ProjectContextContractVersion != plan.Context.ContractVersion) return false;
        if (plan.RoutingDecisionReference is not null)
        {
            if (_routing is null) return false;
            var routingResult = await _routing.GetAsync(plan.ProjectId, plan.RoutingDecisionReference.DecisionId, cancellationToken).ConfigureAwait(false);
            if (!routingResult.IsValid || routingResult.Decision is null || !WorkspacePreparationPlanningService.IsUsableRoutingDecision(routingResult.Decision, plan.ProjectId, plan.ContractReference, plan.Context) || !WorkspacePreparationPlanningService.SameRouting(routingResult.Decision.Reference, plan.RoutingDecisionReference)) return false;
        }
        return true;
    }

    private WorkspacePreparationResult? ValidateApproval(WorkspacePreparationPlan plan, WorkspacePreparationApproval approval)
    {
        if (approval is null || approval.PlanReference.ProjectId != plan.ProjectId || approval.PlanReference.PlanId != plan.PlanId || approval.PlanReference.SchemaVersion != plan.Reference.SchemaVersion || !string.Equals(approval.PlanReference.ContentHash, plan.ContentHash, StringComparison.OrdinalIgnoreCase)) return new(WorkspacePreparationStatus.ApprovalMismatch, ErrorMessage: "Approval does not bind the exact immutable plan.");
        if (_redaction.ValidateIdentityText(approval.ActorReference).RequiresRedaction) return new(WorkspacePreparationStatus.RedactionRejected, ErrorMessage: "Approval actor authority crossed the redaction boundary.");
        if (approval.ApprovedAt < plan.CreatedAt || approval.ApprovedAt > _clock.UtcNow) return new(WorkspacePreparationStatus.ApprovalMismatch, ErrorMessage: "Approval time is outside the plan approval window.");
        return null;
    }

    private async Task<(WorkspacePreparationPlan? plan, WorkspacePreparationResult? result)> LoadPlanAsync(WorkspacePreparationPlanReference reference, CancellationToken cancellationToken)
    {
        if (reference is null) return (null, new(WorkspacePreparationStatus.PlanMissing, ErrorMessage: "Plan reference is required."));
        if (reference.ProjectId == Guid.Empty) return (null, new(WorkspacePreparationStatus.PlanMissing, ErrorMessage: "Project-bound plan reference is required."));
        var read = await _plans.GetAsync(reference.ProjectId, reference.PlanId, cancellationToken).ConfigureAwait(false);
        if (read.State == WorkspacePreparationPlanReadState.Valid && read.Plan is not null &&
            (read.Plan.Reference.SchemaVersion != reference.SchemaVersion || !string.Equals(read.Plan.ContentHash, reference.ContentHash, StringComparison.OrdinalIgnoreCase)))
            return (null, new(WorkspacePreparationStatus.PlanIntegrityFailure, ErrorMessage: "Supplied plan reference does not match the immutable plan content."));
        return read.State switch
        {
            WorkspacePreparationPlanReadState.Valid when read.Plan is not null && WorkspacePreparationIntegrity.IsSha256(read.Plan.ContentHash) => (read.Plan, null),
            WorkspacePreparationPlanReadState.Missing => (null, new(WorkspacePreparationStatus.PlanMissing, ErrorMessage: read.ErrorMessage)),
            WorkspacePreparationPlanReadState.IntegrityFailure => (null, new(WorkspacePreparationStatus.PlanIntegrityFailure, ErrorMessage: read.ErrorMessage)),
            _ => (null, new(WorkspacePreparationStatus.PersistenceUnavailable, ErrorMessage: read.ErrorMessage ?? "Plan could not be read."))
        };
    }

}

/// <summary>Application-facing managed path seam; callers provide GUIDs only.</summary>
public interface IManagedWorkspacePathProvider
{
    string GetWorkspacePath(Guid projectId, Guid workspaceId);
    bool IsSafeManagedWorkspacePath(Guid projectId, Guid workspaceId, out string path, out string? errorMessage);
}
