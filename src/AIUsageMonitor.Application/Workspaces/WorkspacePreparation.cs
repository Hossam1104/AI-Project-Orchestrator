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

public static class WorkspacePreparationLimits
{
    public const int MaxCanonicalPayloadBytes = 128 * 1024;
    public const int MaxBranchLength = 255;
    public const int MaxPathLength = 2_000;
    public const int MaxExplanationLength = 4_000;
    public const int MaxCorrelationLength = 200;
    public const int MaxWorktrees = 128;
    public const int MaxChangedFiles = 128;
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
    PlanConflict
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
    UnsafePath
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
        string? errorMessage = null)
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
        Worktrees = (worktrees ?? Array.Empty<WorkspaceWorktreeEvidence>()).Take(WorkspacePreparationLimits.MaxWorktrees).ToArray();
        LocalBranches = (localBranches ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Take(WorkspacePreparationLimits.MaxWorktrees).ToArray();
        ErrorMessage = Optional(errorMessage, nameof(errorMessage), WorkspacePreparationLimits.MaxExplanationLength);
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
        string? contentHash = null)
    {
        if (projectId == Guid.Empty || workspaceId == Guid.Empty || correlationId == Guid.Empty)
            throw new ArgumentException("Project, workspace, and correlation identifiers are required.");
        PlanReference = planReference ?? throw new ArgumentNullException(nameof(planReference));
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
            localBranches = plan.Repository.LocalBranches.ToArray()
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
        planReference = new { receipt.PlanReference.PlanId, receipt.PlanReference.SchemaVersion, receipt.PlanReference.ContentHash },
        receipt.WorkspacePath,
        receipt.WorkspaceBranch,
        receipt.BaseCommitSha,
        receipt.ActualHeadCommitSha,
        receipt.RepositoryIdentity,
        receipt.CleanupOwnerReference,
        receipt.Limitation
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
            if (!routingRead.IsValid || routingRead.Decision is null || routingRead.Decision.ProjectId != request.ProjectId || !SameRouting(routingRead.Decision.Reference, request.RoutingDecisionReference))
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
        if (repository.LocalBranches.Any(branch => string.Equals(branch, request.WorkspaceBranch, StringComparison.Ordinal)) || repository.Worktrees.Any(worktree => string.Equals(worktree.BranchName, request.WorkspaceBranch, StringComparison.Ordinal)))
            return new(WorkspacePreparationPlanningStatus.BranchConflict, ErrorMessage: "The requested branch already appears in a worktree.");
        if (repository.Worktrees.Any(worktree => SamePath(worktree.Path, _paths.GetWorkspacePath(request.ProjectId, request.WorkspaceId))))
            return new(WorkspacePreparationPlanningStatus.WorktreeConflict, ErrorMessage: "The deterministic workspace path is already a worktree.");
        if (!_paths.IsSafeManagedWorkspacePath(request.ProjectId, request.WorkspaceId, out var workspacePath, out var pathError))
            return new(WorkspacePreparationPlanningStatus.UnsafePath, ErrorMessage: pathError);

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

    private static (WorkspacePreparationPlanningStatus status, string message)? MapDiscovery(WorkspaceRepositoryDiscovery discovery) => discovery.Status switch
    {
        WorkspaceRepositoryDiscoveryStatus.Available => null,
        WorkspaceRepositoryDiscoveryStatus.RepositoryMissing => (WorkspacePreparationPlanningStatus.RepositoryMissing, discovery.ErrorMessage ?? "Repository path is missing."),
        WorkspaceRepositoryDiscoveryStatus.NotGitRepository => (WorkspacePreparationPlanningStatus.NotGitRepository, discovery.ErrorMessage ?? "Path is not a Git repository."),
        _ => (WorkspacePreparationPlanningStatus.RepositoryUnavailable, discovery.ErrorMessage ?? "Repository discovery failed.")
    };

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

    public WorkspacePreparationService(IWorkspacePreparationPlanRepository plans, IWorkspacePreparationReceiptRepository receipts, IWorkspaceRepository repositories, IRepositoryPreparationLock locks, IManagedWorkspacePathProvider paths, IHandoffRedactionService redaction, IClock clock, IProjectContextResolver? contexts = null, IPlanningExecutionContractRepository? contracts = null, IRoutingDecisionRepository? routing = null)
    { _plans = plans; _receipts = receipts; _repositories = repositories; _locks = locks; _paths = paths; _redaction = redaction; _clock = clock; _contexts = contexts; _contracts = contracts; _routing = routing; }

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
        return await PrepareUnderLockAsync(plan, approval, cancellationToken).ConfigureAwait(false);
    }

    public async Task<WorkspacePreparationResult> FinalizeReceiptAsync(WorkspacePreparationPlanReference planReference, WorkspacePreparationApproval approval, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var loaded = await LoadPlanAsync(planReference, cancellationToken).ConfigureAwait(false);
        if (loaded.result is not null) return loaded.result;
        var plan = loaded.plan!;
        var approvalResult = ValidateApproval(plan, approval);
        if (approvalResult is not null) return approvalResult;
        IAsyncDisposable repositoryLock;
        try { repositoryLock = await _locks.AcquireAsync(plan.Repository.CommonDirectory!, cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { throw; }
        catch (IOException exception) { return new(WorkspacePreparationStatus.RepositoryUnavailable, ErrorMessage: exception.Message); }
        await using (repositoryLock)
        {
        var receipt = await _receipts.GetAsync(plan.ProjectId, plan.WorkspaceId, cancellationToken).ConfigureAwait(false);
        if (receipt.State is not (WorkspacePreparationReceiptReadState.Missing or WorkspacePreparationReceiptReadState.Valid)) return new(WorkspacePreparationStatus.PersistenceUnavailable, ErrorMessage: receipt.ErrorMessage ?? "Receipt cannot be read safely.");
        if (receipt.Receipt is not null) return new(WorkspacePreparationStatus.AlreadyPrepared, receipt.Receipt);
        if (!await BoundAuthorityStillMatchesAsync(plan, cancellationToken).ConfigureAwait(false)) return new(WorkspacePreparationStatus.PlanStale, ErrorMessage: "The exact planning contract or context binding changed after planning.");
        var current = await _repositories.DiscoverAsync(plan.Repository.RepositoryRoot!, cancellationToken).ConfigureAwait(false);
        if (!MatchesPreparedWorkspace(plan, current, out var found)) return new(WorkspacePreparationStatus.VerificationFailed, ErrorMessage: "The existing worktree does not exactly match the plan.");
        var newReceipt = CreateReceipt(plan, found!);
        var write = await _receipts.CreateAsync(newReceipt, cancellationToken).ConfigureAwait(false);
        return write.Succeeded ? new(WorkspacePreparationStatus.Prepared, newReceipt) : new(WorkspacePreparationStatus.ReceiptPersistenceFailed, ErrorMessage: write.ErrorMessage);
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
        var matches = WorkspacePreparationPlanningService.SamePath(current.RepositoryRoot, plan.Repository.RepositoryRoot) && WorkspacePreparationPlanningService.SamePath(current.CommonDirectory, plan.Repository.CommonDirectory) && MatchesPreparedWorkspace(plan, current, out var found);
        if (receiptRead.Receipt is not null)
            return matches && WorkspacePreparationPlanningService.SamePath(receiptRead.Receipt.WorkspacePath, path) ? new(WorkspaceRecoveryState.PreparedAndRecorded, receiptRead.Receipt) : new(WorkspaceRecoveryState.Conflict, receiptRead.Receipt, ErrorMessage: "Receipt and current worktree evidence differ.");
        if (matches) return new(WorkspaceRecoveryState.PreparedWithoutReceipt, NextSafeAction: "FinalizeReceipt");
        if (Directory.Exists(path) || File.Exists(path)) return new(WorkspaceRecoveryState.ForeignWorkspace, NextSafeAction: "Owner inspection required.");
        return new(WorkspaceRecoveryState.NotPrepared);
    }

    private async Task<WorkspacePreparationResult> PrepareUnderLockAsync(WorkspacePreparationPlan plan, WorkspacePreparationApproval approval, CancellationToken cancellationToken)
    {
        IAsyncDisposable repositoryLock;
        try { repositoryLock = await _locks.AcquireAsync(plan.Repository.CommonDirectory!, cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { throw; }
        catch (IOException exception) { return new(WorkspacePreparationStatus.RepositoryUnavailable, ErrorMessage: exception.Message); }
        await using (repositoryLock)
        {
        var existingReceipt = await _receipts.GetAsync(plan.ProjectId, plan.WorkspaceId, cancellationToken).ConfigureAwait(false);
        if (existingReceipt.State is not (WorkspacePreparationReceiptReadState.Missing or WorkspacePreparationReceiptReadState.Valid)) return new(WorkspacePreparationStatus.PersistenceUnavailable, ErrorMessage: existingReceipt.ErrorMessage ?? "Receipt cannot be read safely.");
        if (existingReceipt.Receipt is not null) return new(WorkspacePreparationStatus.AlreadyPrepared, existingReceipt.Receipt);
        if (!_paths.IsSafeManagedWorkspacePath(plan.ProjectId, plan.WorkspaceId, out var managedPath, out var pathError)) return new(WorkspacePreparationStatus.UnsafePath, ErrorMessage: pathError);
        if (!await BoundAuthorityStillMatchesAsync(plan, cancellationToken).ConfigureAwait(false)) return new(WorkspacePreparationStatus.PlanStale, ErrorMessage: "The exact planning contract or context binding changed after planning.");
        var current = await _repositories.DiscoverAsync(plan.Repository.RepositoryRoot!, cancellationToken).ConfigureAwait(false);
        if (current.Status != WorkspaceRepositoryDiscoveryStatus.Available) return new(WorkspacePreparationStatus.RepositoryUnavailable, ErrorMessage: current.ErrorMessage);
        if (!WorkspacePreparationPlanningService.SamePath(current.RepositoryRoot, plan.Repository.RepositoryRoot) || !WorkspacePreparationPlanningService.SamePath(current.CommonDirectory, plan.Repository.CommonDirectory) || !string.Equals(current.HeadCommitSha, plan.Repository.HeadCommitSha, StringComparison.OrdinalIgnoreCase) || !string.Equals(current.BranchName, plan.Repository.BranchName, StringComparison.Ordinal))
            return new(WorkspacePreparationStatus.PlanStale, ErrorMessage: "A plan-critical repository fact changed after planning.");
        if (plan.Policy == WorkspacePreparationPolicy.RequireCleanSource && !current.IsClean) return new(WorkspacePreparationStatus.PolicyBlocked, ErrorMessage: "Source is dirty and the approved plan requires a clean source.");
        if (current.LocalBranches.Any(branch => string.Equals(branch, plan.WorkspaceBranch, StringComparison.Ordinal)) || current.Worktrees.Any(worktree => string.Equals(worktree.BranchName, plan.WorkspaceBranch, StringComparison.Ordinal))) return new(WorkspacePreparationStatus.BranchConflict, ErrorMessage: "Workspace branch is already present.");
        if (current.Worktrees.Any(worktree => WorkspacePreparationPlanningService.SamePath(worktree.Path, managedPath))) return new(WorkspacePreparationStatus.WorktreeConflict, ErrorMessage: "Workspace path is already registered as a worktree.");
        if (Directory.Exists(managedPath) || File.Exists(managedPath)) return new(WorkspacePreparationStatus.PathConflict, ErrorMessage: "Managed workspace target already exists and will not be overwritten.");
        if (!WorkspacePreparationPlanningService.SamePath(plan.ProposedWorkspacePath, managedPath)) return new(WorkspacePreparationStatus.UnsafePath, ErrorMessage: "Plan workspace path is not the deterministic managed destination.");
        if (!_redaction.ValidateIdentityText(plan.WorkspaceBranch).RequiresRedaction && !_redaction.ValidateIdentityText(plan.Repository.CommonDirectory!).RequiresRedaction) { }
        else return new(WorkspacePreparationStatus.RedactionRejected, ErrorMessage: "Repository or branch authority crossed the redaction boundary.");

        cancellationToken.ThrowIfCancellationRequested();
        var mutation = await _repositories.AddExactWorktreeAsync(plan.Repository.CommonDirectory!, plan.WorkspaceBranch, managedPath, plan.BaseCommitSha, cancellationToken).ConfigureAwait(false);
        if (!mutation.Succeeded) return new(WorkspacePreparationStatus.GitCommandFailed, ErrorMessage: mutation.ErrorMessage ?? "Git worktree creation failed.");
        // Verification is read-only. No rollback command exists by design if this or receipt write fails.
        var verified = await _repositories.DiscoverAsync(plan.Repository.RepositoryRoot!, cancellationToken).ConfigureAwait(false);
        if (!WorkspacePreparationPlanningService.SamePath(verified.RepositoryRoot, plan.Repository.RepositoryRoot) || !WorkspacePreparationPlanningService.SamePath(verified.CommonDirectory, plan.Repository.CommonDirectory) || !MatchesPreparedWorkspace(plan, verified, out var worktree)) return new(WorkspacePreparationStatus.VerificationFailed, ErrorMessage: "Git succeeded but the exact workspace evidence could not be verified.");
        var receipt = CreateReceipt(plan, worktree!);
        var write = await _receipts.CreateAsync(receipt, cancellationToken).ConfigureAwait(false);
        return write.Succeeded ? new(WorkspacePreparationStatus.Prepared, receipt) : new(WorkspacePreparationStatus.ReceiptPersistenceFailed, ErrorMessage: write.ErrorMessage);
        }
    }

    private WorkspacePreparationReceipt CreateReceipt(WorkspacePreparationPlan plan, WorkspaceWorktreeEvidence worktree) =>
        new(plan.ProjectId, plan.WorkspaceId, plan.CorrelationId, _clock.UtcNow, plan.Reference, worktree.Path, plan.WorkspaceBranch, plan.BaseCommitSha, worktree.HeadCommitSha, plan.Repository.CommonDirectory!, "Owner/project policy", plan.Limitations.FirstOrDefault());

    private static bool MatchesPreparedWorkspace(WorkspacePreparationPlan plan, WorkspaceRepositoryDiscovery discovery, out WorkspaceWorktreeEvidence? worktree)
    {
        worktree = discovery.Worktrees.FirstOrDefault(value => WorkspacePreparationPlanningService.SamePath(value.Path, plan.ProposedWorkspacePath));
        return discovery.Status == WorkspaceRepositoryDiscoveryStatus.Available && worktree is not null && string.Equals(worktree.BranchName, plan.WorkspaceBranch, StringComparison.Ordinal) && string.Equals(worktree.HeadCommitSha, plan.BaseCommitSha, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> BoundAuthorityStillMatchesAsync(WorkspacePreparationPlan plan, CancellationToken cancellationToken)
    {
        if (_contexts is null || _contracts is null) return true;
        var contextResult = await _contexts.ResolveAsync(plan.ProjectId, cancellationToken).ConfigureAwait(false);
        if (contextResult.State != ProjectContextResolutionState.Ready || contextResult.View is null) return false;
        var context = contextResult.View.Context;
        if (context.ProjectId != plan.ProjectId || context.ContextId != plan.Context.ContextId || context.ContractVersion != plan.Context.ContractVersion || context.UpdatedAt != plan.Context.UpdatedAt) return false;
        var contractResult = await _contracts.GetAsync(plan.ProjectId, plan.ContractReference.ContractId, plan.ContractReference.Revision, cancellationToken).ConfigureAwait(false);
        if (!contractResult.IsValid || contractResult.Contract is null || contractResult.Contract.ProjectId != plan.ProjectId || !WorkspacePreparationPlanningService.SameContract(contractResult.Contract.Reference, plan.ContractReference) || contractResult.Contract.Context.ProjectContextId != plan.Context.ContextId || contractResult.Contract.Context.ProjectContextContractVersion != plan.Context.ContractVersion) return false;
        if (plan.RoutingDecisionReference is not null && _routing is not null)
        {
            var routingResult = await _routing.GetAsync(plan.ProjectId, plan.RoutingDecisionReference.DecisionId, cancellationToken).ConfigureAwait(false);
            if (!routingResult.IsValid || routingResult.Decision is null || routingResult.Decision.ProjectId != plan.ProjectId || !WorkspacePreparationPlanningService.SameRouting(routingResult.Decision.Reference, plan.RoutingDecisionReference)) return false;
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
