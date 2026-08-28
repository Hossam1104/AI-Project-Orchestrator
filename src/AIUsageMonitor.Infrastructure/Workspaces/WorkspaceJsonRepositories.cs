using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Routing;
using AIUsageMonitor.Application.Workspaces;
using AIUsageMonitor.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AIUsageMonitor.Infrastructure.Workspaces;

internal sealed class WorkspacePlanRecord
{
    public string RecordType { get; set; } = "workspace-preparation-plan";
    public int SchemaVersion { get; set; }
    public Guid ProjectId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid PlanId { get; set; }
    public Guid CorrelationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public WorkspaceContextRecord? Context { get; set; }
    public ReferenceRecord? ContractReference { get; set; }
    public ReferenceRecord? WorkGraphReference { get; set; }
    public Guid? WorkGraphNodeId { get; set; }
    public ReferenceRecord? RoutingDecisionReference { get; set; }
    public RepositoryRecord? Repository { get; set; }
    public string? BaseCommitSha { get; set; }
    public string? WorkspaceBranch { get; set; }
    public string? ProposedWorkspacePath { get; set; }
    public WorkspacePreparationPolicy Policy { get; set; }
    public bool ApprovalRequired { get; set; }
    public string? Explanation { get; set; }
    public List<string>? Limitations { get; set; }
    public string? ContentHash { get; set; }

    public static WorkspacePlanRecord FromApplication(WorkspacePreparationPlan value) => new()
    {
        SchemaVersion = WorkspacePreparationPlanSchema.CurrentVersion, ProjectId = value.ProjectId, WorkspaceId = value.WorkspaceId,
        PlanId = value.PlanId, CorrelationId = value.CorrelationId, CreatedAt = value.CreatedAt,
        Context = new() { ProjectId = value.Context.ProjectId, ContextId = value.Context.ContextId, ContractVersion = value.Context.ContractVersion, UpdatedAt = value.Context.UpdatedAt },
        ContractReference = ReferenceRecord.From(value.ContractReference),
        WorkGraphReference = value.WorkGraphReference is null ? null : ReferenceRecord.From(value.WorkGraphReference),
        WorkGraphNodeId = value.WorkGraphNodeId,
        RoutingDecisionReference = value.RoutingDecisionReference is null ? null : ReferenceRecord.From(value.RoutingDecisionReference),
        Repository = RepositoryRecord.From(value.Repository), BaseCommitSha = value.BaseCommitSha, WorkspaceBranch = value.WorkspaceBranch,
        ProposedWorkspacePath = value.ProposedWorkspacePath, Policy = value.Policy, ApprovalRequired = value.ApprovalRequired,
        Explanation = value.Explanation, Limitations = value.Limitations.ToList(), ContentHash = value.ContentHash
    };

    public WorkspacePreparationPlan ToApplication() => new(
        ProjectId, WorkspaceId, PlanId, CorrelationId, CreatedAt,
        Context?.ToApplication() ?? throw new ArgumentException("Workspace context is missing."),
        ContractReference?.ToPlanning() ?? throw new ArgumentException("Planning contract reference is missing."),
        WorkGraphReference?.ToGraph(), WorkGraphNodeId, RoutingDecisionReference?.ToRouting(),
        Repository?.ToApplication() ?? throw new ArgumentException("Repository evidence is missing."),
        BaseCommitSha ?? throw new ArgumentException("Base commit is missing."), WorkspaceBranch ?? throw new ArgumentException("Branch is missing."),
        ProposedWorkspacePath ?? throw new ArgumentException("Workspace path is missing."), Policy, ApprovalRequired, Explanation, Limitations);
}

internal sealed class WorkspaceReceiptRecord
{
    public string RecordType { get; set; } = "workspace-preparation-receipt";
    public int SchemaVersion { get; set; }
    public Guid ProjectId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid CorrelationId { get; set; }
    public DateTimeOffset PreparedAt { get; set; }
    public ReferenceRecord? PlanReference { get; set; }
    public string? WorkspacePath { get; set; }
    public string? WorkspaceBranch { get; set; }
    public string? BaseCommitSha { get; set; }
    public string? ActualHeadCommitSha { get; set; }
    public string? RepositoryIdentity { get; set; }
    public string? CleanupOwnerReference { get; set; }
    public WorkspaceCleanupOwner CleanupOwner { get; set; }
    public WorkspaceCleanupPolicy CleanupPolicy { get; set; }
    public bool AutomaticCleanupAllowed { get; set; }
    public ApprovalReferenceRecord? ApprovalReference { get; set; }
    public string? Limitation { get; set; }
    public string? ContentHash { get; set; }

    public static WorkspaceReceiptRecord FromApplication(WorkspacePreparationReceipt value) => new()
    {
        SchemaVersion = WorkspacePreparationReceiptSchema.CurrentVersion, ProjectId = value.ProjectId, WorkspaceId = value.WorkspaceId,
        CorrelationId = value.CorrelationId, PreparedAt = value.PreparedAt, PlanReference = ReferenceRecord.From(value.PlanReference),
        WorkspacePath = value.WorkspacePath, WorkspaceBranch = value.WorkspaceBranch, BaseCommitSha = value.BaseCommitSha,
        ActualHeadCommitSha = value.ActualHeadCommitSha, RepositoryIdentity = value.RepositoryIdentity,
        CleanupOwnerReference = value.CleanupOwnerReference, CleanupOwner = value.CleanupOwner,
        CleanupPolicy = value.CleanupPolicy, AutomaticCleanupAllowed = value.AutomaticCleanupAllowed,
        ApprovalReference = value.ApprovalReference is null ? null : ApprovalReferenceRecord.From(value.ApprovalReference),
        Limitation = value.Limitation, ContentHash = value.ContentHash
    };

    public WorkspacePreparationReceipt ToApplication() => new(
        ProjectId, WorkspaceId, CorrelationId, PreparedAt,
        PlanReference?.ToPlan(ProjectId) ?? throw new ArgumentException("Plan reference is missing."),
        WorkspacePath ?? throw new ArgumentException("Workspace path is missing."), WorkspaceBranch ?? throw new ArgumentException("Branch is missing."),
        BaseCommitSha ?? throw new ArgumentException("Base commit is missing."), ActualHeadCommitSha ?? throw new ArgumentException("Actual head is missing."),
        RepositoryIdentity ?? throw new ArgumentException("Repository identity is missing."), CleanupOwnerReference ?? throw new ArgumentException("Cleanup owner is missing."), Limitation,
        cleanupOwner: CleanupOwner, cleanupPolicy: CleanupPolicy, automaticCleanupAllowed: AutomaticCleanupAllowed,
        approvalReference: ApprovalReference?.ToApplication());
}

internal sealed class ApprovalReferenceRecord
{
    public Guid ApprovalId { get; set; }
    public int SchemaVersion { get; set; }
    public string? ContentHash { get; set; }

    public static ApprovalReferenceRecord From(WorkspacePreparationApprovalReference value) => new()
    {
        ApprovalId = value.ApprovalId,
        SchemaVersion = value.SchemaVersion,
        ContentHash = value.ContentHash
    };

    public WorkspacePreparationApprovalReference ToApplication() =>
        new(ApprovalId, SchemaVersion, ContentHash ?? string.Empty);
}

internal sealed class ReferenceRecord
{
    public Guid ProjectId { get; set; }
    public Guid Id { get; set; }
    public int Revision { get; set; }
    public int SchemaVersion { get; set; }
    public string? ContentHash { get; set; }

    public static ReferenceRecord From(PlanningExecutionContractReference value) => new() { Id = value.ContractId, Revision = value.Revision, SchemaVersion = value.SchemaVersion, ContentHash = value.ContentHash };
    public static ReferenceRecord From(WorkGraphReference value) => new() { Id = value.GraphId, SchemaVersion = value.SchemaVersion, ContentHash = value.ContentHash };
    public static ReferenceRecord From(RoutingDecisionReference value) => new() { Id = value.DecisionId, SchemaVersion = value.SchemaVersion, ContentHash = value.ContentHash };
    public static ReferenceRecord From(WorkspacePreparationPlanReference value) => new() { ProjectId = value.ProjectId, Id = value.PlanId, SchemaVersion = value.SchemaVersion, ContentHash = value.ContentHash };
    public PlanningExecutionContractReference ToPlanning() => new(Id, Revision, SchemaVersion, ContentHash ?? string.Empty);
    public WorkGraphReference ToGraph() => new(Id, SchemaVersion, ContentHash ?? string.Empty);
    public RoutingDecisionReference ToRouting() => new(Id, SchemaVersion, ContentHash ?? string.Empty);
    public WorkspacePreparationPlanReference ToPlan(Guid projectId = default) => new(Id, SchemaVersion, ContentHash ?? string.Empty, ProjectId == Guid.Empty ? projectId : ProjectId);
}

internal sealed class WorkspaceContextRecord
{
    public Guid ProjectId { get; set; }
    public Guid ContextId { get; set; }
    public int ContractVersion { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public WorkspaceContextIdentity ToApplication() => new(ProjectId, ContextId, ContractVersion, UpdatedAt);
}

internal sealed class RepositoryRecord
{
    public WorkspaceRepositoryDiscoveryStatus Status { get; set; }
    public string? RegisteredPath { get; set; }
    public string? RepositoryRoot { get; set; }
    public string? CommonDirectory { get; set; }
    public bool IsBareRepository { get; set; }
    public string? HeadCommitSha { get; set; }
    public string? BranchName { get; set; }
    public bool IsDetached { get; set; }
    public bool IsClean { get; set; }
    public int ChangedFileCount { get; set; }
    public List<WorktreeRecord>? Worktrees { get; set; }
    public List<string>? LocalBranches { get; set; }
    public string? ErrorMessage { get; set; }
    public string? WorkingTreeStateFingerprint { get; set; }
    public WorkspaceRepositoryDivergenceRecord? Divergence { get; set; }
    public bool WorktreeEvidenceOverflow { get; set; }
    public bool LocalBranchEvidenceOverflow { get; set; }
    public static RepositoryRecord From(WorkspaceRepositoryDiscovery value) => new()
    {
        Status = value.Status, RegisteredPath = value.RegisteredPath, RepositoryRoot = value.RepositoryRoot, CommonDirectory = value.CommonDirectory,
        IsBareRepository = value.IsBareRepository, HeadCommitSha = value.HeadCommitSha, BranchName = value.BranchName, IsDetached = value.IsDetached,
        IsClean = value.IsClean, ChangedFileCount = value.ChangedFileCount, Worktrees = value.Worktrees.Select(WorktreeRecord.From).ToList(), LocalBranches = value.LocalBranches.ToList(), ErrorMessage = value.ErrorMessage,
        WorkingTreeStateFingerprint = value.WorkingTreeStateFingerprint,
        Divergence = WorkspaceRepositoryDivergenceRecord.From(value.Divergence),
        WorktreeEvidenceOverflow = value.WorktreeEvidenceOverflow,
        LocalBranchEvidenceOverflow = value.LocalBranchEvidenceOverflow
    };
    public WorkspaceRepositoryDiscovery ToApplication() => new(Status, RegisteredPath ?? throw new ArgumentException("Registered path is missing."), RepositoryRoot, CommonDirectory, IsBareRepository, HeadCommitSha, BranchName, IsDetached, IsClean, ChangedFileCount, (Worktrees ?? []).Select(value => value.ToApplication()).ToArray(), LocalBranches, ErrorMessage, WorkingTreeStateFingerprint, Divergence?.ToApplication(), WorktreeEvidenceOverflow, LocalBranchEvidenceOverflow);
}

internal sealed class WorkspaceRepositoryDivergenceRecord
{
    public WorkspaceDivergenceState State { get; set; }
    public string? LocalUpstreamReference { get; set; }
    public int? AheadCount { get; set; }
    public int? BehindCount { get; set; }

    public static WorkspaceRepositoryDivergenceRecord From(WorkspaceRepositoryDivergence value) => new()
    {
        State = value.State,
        LocalUpstreamReference = value.LocalUpstreamReference,
        AheadCount = value.AheadCount,
        BehindCount = value.BehindCount
    };

    public WorkspaceRepositoryDivergence ToApplication() =>
        new(State, LocalUpstreamReference, AheadCount, BehindCount);
}

internal sealed class WorktreeRecord
{
    public string? Path { get; set; }
    public string? HeadCommitSha { get; set; }
    public string? BranchName { get; set; }
    public bool IsDetached { get; set; }
    public bool IsLocked { get; set; }
    public bool IsPrunable { get; set; }
    public static WorktreeRecord From(WorkspaceWorktreeEvidence value) => new() { Path = value.Path, HeadCommitSha = value.HeadCommitSha, BranchName = value.BranchName, IsDetached = value.IsDetached, IsLocked = value.IsLocked, IsPrunable = value.IsPrunable };
    public WorkspaceWorktreeEvidence ToApplication() => new(Path ?? throw new ArgumentException("Worktree path is missing."), HeadCommitSha ?? throw new ArgumentException("Worktree head is missing."), BranchName, IsDetached, IsLocked, IsPrunable);
}

public sealed class JsonWorkspacePreparationPlanRepository : IWorkspacePreparationPlanRepository
{
    private const string RecordType = "workspace-preparation-plan";
    private readonly ApplicationDataPaths _paths; private readonly JsonFileStore _files; private readonly ILogger<JsonWorkspacePreparationPlanRepository> _logger;
    public JsonWorkspacePreparationPlanRepository(ApplicationDataPaths paths, JsonFileStore files, ILogger<JsonWorkspacePreparationPlanRepository> logger) { _paths = paths; _files = files; _logger = logger; }
    public async Task<WorkspacePreparationPlanWriteResult> CreateAsync(WorkspacePreparationPlan plan, CancellationToken cancellationToken = default)
    {
        var path = _paths.GetWorkspacePreparationPlanFile(plan.ProjectId, plan.PlanId);
        try { var record = WorkspacePlanRecord.FromApplication(plan); if (JsonSerializer.SerializeToUtf8Bytes(record, JsonFileStore.SerializerOptions).Length > WorkspacePreparationLimits.MaxCanonicalPayloadBytes) return new(WorkspacePreparationPlanWriteStatus.Unavailable, "Workspace plan payload exceeds the bounded persistence limit."); await _paths.EnsureProjectDirectoriesAsync(plan.ProjectId, cancellationToken); Directory.CreateDirectory(_paths.GetWorkspacePreparationPlanDirectory(plan.ProjectId, plan.PlanId)); await _files.CreateNewAsync(path, record, cancellationToken); return new(WorkspacePreparationPlanWriteStatus.Created); }
        catch (OperationCanceledException) { throw; }
        catch (IOException) when (File.Exists(path)) { return new(WorkspacePreparationPlanWriteStatus.PlanConflict, "The immutable workspace plan already exists."); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException) { _logger.LogWarning(exception, "Could not persist workspace plan"); return new(WorkspacePreparationPlanWriteStatus.Unavailable, "Workspace plan persistence is unavailable."); }
    }
    public async Task<WorkspacePreparationPlanReadResult> GetAsync(Guid projectId, Guid planId, CancellationToken cancellationToken = default)
    {
        var path = _paths.GetWorkspacePreparationPlanFile(projectId, planId); var read = await _files.ReadPreservingAsync<WorkspacePlanRecord>(path, cancellationToken);
        if (read.Status == FileReadStatus.Missing) return new(WorkspacePreparationPlanReadState.Missing);
        if (read.Status == FileReadStatus.UnsupportedSchema) return new(WorkspacePreparationPlanReadState.UnsupportedFutureVersion);
        if (read.Status is FileReadStatus.Empty or FileReadStatus.Corrupt) return new(WorkspacePreparationPlanReadState.Invalid);
        if (read.Status is FileReadStatus.IoFailure or FileReadStatus.PermissionFailure) return new(WorkspacePreparationPlanReadState.Unavailable);
        try
        {
            var record = read.Value!;
            if (!string.Equals(record.RecordType, RecordType, StringComparison.Ordinal)) return new(WorkspacePreparationPlanReadState.IntegrityFailure, ErrorMessage: "Workspace plan record type was tampered or is unsupported.");
            if (record.SchemaVersion > WorkspacePreparationPlanSchema.CurrentVersion) return new(WorkspacePreparationPlanReadState.UnsupportedFutureVersion);
            if (record.SchemaVersion < WorkspacePreparationPlanSchema.CurrentVersion) return new(WorkspacePreparationPlanReadState.MigrationRequired);
            if (record.ProjectId != projectId || record.PlanId != planId) return new(WorkspacePreparationPlanReadState.IntegrityFailure, ErrorMessage: "Workspace plan identity does not match its GUID-derived path.");
            var plan = record.ToApplication();
            return plan.ProjectId == projectId && string.Equals(record.ContentHash, plan.ContentHash, StringComparison.OrdinalIgnoreCase) ? new(WorkspacePreparationPlanReadState.Valid, plan) : new(WorkspacePreparationPlanReadState.IntegrityFailure, ErrorMessage: "Workspace plan content integrity failed.");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) { return new(WorkspacePreparationPlanReadState.IntegrityFailure, ErrorMessage: "Workspace plan payload integrity failed."); }
    }
}

public sealed class JsonWorkspacePreparationReceiptRepository : IWorkspacePreparationReceiptRepository
{
    private const string RecordType = "workspace-preparation-receipt";
    private readonly ApplicationDataPaths _paths; private readonly JsonFileStore _files; private readonly ILogger<JsonWorkspacePreparationReceiptRepository> _logger;
    public JsonWorkspacePreparationReceiptRepository(ApplicationDataPaths paths, JsonFileStore files, ILogger<JsonWorkspacePreparationReceiptRepository> logger) { _paths = paths; _files = files; _logger = logger; }
    public async Task<WorkspacePreparationReceiptWriteResult> CreateAsync(WorkspacePreparationReceipt receipt, CancellationToken cancellationToken = default)
    {
        var path = _paths.GetWorkspaceReceiptFile(receipt.ProjectId, receipt.WorkspaceId);
        try { var record = WorkspaceReceiptRecord.FromApplication(receipt); if (JsonSerializer.SerializeToUtf8Bytes(record, JsonFileStore.SerializerOptions).Length > WorkspacePreparationLimits.MaxCanonicalPayloadBytes) return new(WorkspacePreparationReceiptWriteStatus.Unavailable, "Workspace receipt payload exceeds the bounded persistence limit."); await _paths.EnsureProjectDirectoriesAsync(receipt.ProjectId, cancellationToken); Directory.CreateDirectory(_paths.GetWorkspaceReceiptDirectory(receipt.ProjectId, receipt.WorkspaceId)); await _files.CreateNewAsync(path, record, cancellationToken); return new(WorkspacePreparationReceiptWriteStatus.Created); }
        catch (OperationCanceledException) { throw; }
        catch (IOException) when (File.Exists(path)) { return new(WorkspacePreparationReceiptWriteStatus.ReceiptConflict, "The immutable workspace receipt already exists."); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException) { _logger.LogWarning(exception, "Could not persist workspace receipt"); return new(WorkspacePreparationReceiptWriteStatus.Unavailable, "Workspace receipt persistence is unavailable."); }
    }
    public async Task<WorkspacePreparationReceiptReadResult> GetAsync(Guid projectId, Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var path = _paths.GetWorkspaceReceiptFile(projectId, workspaceId); var read = await _files.ReadPreservingAsync<WorkspaceReceiptRecord>(path, cancellationToken);
        if (read.Status == FileReadStatus.Missing) return new(WorkspacePreparationReceiptReadState.Missing);
        if (read.Status == FileReadStatus.UnsupportedSchema) return new(WorkspacePreparationReceiptReadState.UnsupportedFutureVersion);
        if (read.Status is FileReadStatus.Empty or FileReadStatus.Corrupt) return new(WorkspacePreparationReceiptReadState.Invalid);
        if (read.Status is FileReadStatus.IoFailure or FileReadStatus.PermissionFailure) return new(WorkspacePreparationReceiptReadState.Unavailable);
        try
        {
            var record = read.Value!;
            if (!string.Equals(record.RecordType, RecordType, StringComparison.Ordinal)) return new(WorkspacePreparationReceiptReadState.IntegrityFailure, ErrorMessage: "Workspace receipt record type was tampered or is unsupported.");
            if (record.SchemaVersion > WorkspacePreparationReceiptSchema.CurrentVersion) return new(WorkspacePreparationReceiptReadState.UnsupportedFutureVersion);
            if (record.SchemaVersion < WorkspacePreparationReceiptSchema.CurrentVersion) return new(WorkspacePreparationReceiptReadState.MigrationRequired);
            if (record.ProjectId != projectId || record.WorkspaceId != workspaceId) return new(WorkspacePreparationReceiptReadState.IntegrityFailure, ErrorMessage: "Workspace receipt identity does not match its GUID-derived path.");
            if (record.ApprovalReference is null || record.AutomaticCleanupAllowed || record.CleanupOwner != WorkspaceCleanupOwner.APO || record.CleanupPolicy != WorkspaceCleanupPolicy.ExplicitActionRequired)
                return new(WorkspacePreparationReceiptReadState.IntegrityFailure, ErrorMessage: "Workspace receipt is missing the durable approval or safe cleanup policy evidence.");
            var receipt = record.ToApplication();
            return string.Equals(record.ContentHash, receipt.ContentHash, StringComparison.OrdinalIgnoreCase) ? new(WorkspacePreparationReceiptReadState.Valid, receipt) : new(WorkspacePreparationReceiptReadState.IntegrityFailure, ErrorMessage: "Workspace receipt content integrity failed.");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) { return new(WorkspacePreparationReceiptReadState.IntegrityFailure, ErrorMessage: "Workspace receipt payload integrity failed."); }
    }
}

internal sealed class WorkspaceApprovalEvidenceRecord
{
    public string RecordType { get; set; } = "workspace-preparation-approval-evidence";
    public int SchemaVersion { get; set; }
    public Guid ProjectId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ApprovalId { get; set; }
    public ReferenceRecord? PlanReference { get; set; }
    public string? ActorReference { get; set; }
    public DateTimeOffset ApprovedAt { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
    public string? SanitizedReason { get; set; }
    public string? ContentHash { get; set; }

    public static WorkspaceApprovalEvidenceRecord FromApplication(
        WorkspacePreparationApprovalEvidence value,
        string recordType) => new()
    {
        RecordType = recordType,
        SchemaVersion = WorkspacePreparationApprovalEvidenceSchema.CurrentVersion,
        ProjectId = value.ProjectId,
        WorkspaceId = value.WorkspaceId,
        ApprovalId = value.ApprovalId,
        PlanReference = ReferenceRecord.From(value.PlanReference),
        ActorReference = value.ActorReference,
        ApprovedAt = value.ApprovedAt,
        RecordedAt = value.RecordedAt,
        SanitizedReason = value.SanitizedReason,
        ContentHash = value.ContentHash
    };

    public WorkspacePreparationApprovalEvidence ToApplication() => new(
        ProjectId,
        WorkspaceId,
        ApprovalId,
        PlanReference?.ToPlan(ProjectId) ?? throw new ArgumentException("Approval plan reference is missing."),
        ActorReference ?? throw new ArgumentException("Approval actor reference is missing."),
        ApprovedAt,
        RecordedAt,
        SanitizedReason);
}

public sealed class JsonWorkspacePreparationApprovalEvidenceRepository : IWorkspacePreparationApprovalEvidenceRepository
{
    private const string RecordType = "workspace-preparation-approval-evidence";
    private const string PlanIndexRecordType = "workspace-preparation-approval-by-plan";
    private readonly ApplicationDataPaths _paths;
    private readonly JsonFileStore _files;
    private readonly ILogger<JsonWorkspacePreparationApprovalEvidenceRepository> _logger;

    public JsonWorkspacePreparationApprovalEvidenceRepository(
        ApplicationDataPaths paths,
        JsonFileStore files,
        ILogger<JsonWorkspacePreparationApprovalEvidenceRepository> logger)
    {
        _paths = paths;
        _files = files;
        _logger = logger;
    }

    public async Task<WorkspacePreparationApprovalEvidenceWriteResult> CreateAsync(
        WorkspacePreparationApprovalEvidence evidence,
        CancellationToken cancellationToken = default)
    {
        var evidencePath = _paths.GetWorkspaceApprovalEvidenceFile(evidence.ProjectId, evidence.WorkspaceId, evidence.ApprovalId);
        var planPath = _paths.GetWorkspaceApprovalEvidenceByPlanFile(evidence.ProjectId, evidence.WorkspaceId, evidence.PlanReference.PlanId);
        try
        {
            var record = WorkspaceApprovalEvidenceRecord.FromApplication(evidence, RecordType);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(record, JsonFileStore.SerializerOptions);
            if (bytes.Length > WorkspacePreparationLimits.MaxCanonicalPayloadBytes)
            {
                return new(WorkspacePreparationApprovalEvidenceWriteStatus.Unavailable, "Approval evidence exceeds the bounded persistence limit.");
            }

            await _paths.EnsureProjectDirectoriesAsync(evidence.ProjectId, cancellationToken);
            Directory.CreateDirectory(_paths.GetWorkspaceApprovalEvidenceDirectory(evidence.ProjectId, evidence.WorkspaceId, evidence.ApprovalId));
            await _files.CreateNewAsync(evidencePath, record, cancellationToken);

            var planRecord = WorkspaceApprovalEvidenceRecord.FromApplication(evidence, PlanIndexRecordType);
            Directory.CreateDirectory(_paths.GetWorkspaceApprovalEvidenceByPlanDirectory(evidence.ProjectId, evidence.WorkspaceId, evidence.PlanReference.PlanId));
            await _files.CreateNewAsync(planPath, planRecord, cancellationToken);
            return new(WorkspacePreparationApprovalEvidenceWriteStatus.Created);
        }
        catch (OperationCanceledException) { throw; }
        catch (IOException) when (File.Exists(evidencePath) || File.Exists(planPath))
        {
            var existing = await GetAsync(evidence.ProjectId, evidence.WorkspaceId, evidence.ApprovalId, cancellationToken).ConfigureAwait(false);
            if (!existing.IsValid || existing.Evidence is null)
            {
                return new(WorkspacePreparationApprovalEvidenceWriteStatus.Unavailable, "Existing approval evidence is not valid and cannot be replaced.");
            }

            if (!SameEvidenceAuthority(existing.Evidence, evidence))
            {
                return new(WorkspacePreparationApprovalEvidenceWriteStatus.ApprovalEvidenceConflict, "Approval id is already bound to different evidence.");
            }

            var planDirectory = _paths.GetWorkspaceApprovalEvidenceByPlanDirectory(evidence.ProjectId, evidence.WorkspaceId, evidence.PlanReference.PlanId);
            if (File.Exists(planDirectory))
            {
                return new(WorkspacePreparationApprovalEvidenceWriteStatus.Unavailable, "The approval evidence plan-index directory is not a directory.");
            }

            if (!File.Exists(planPath))
            {
                var repair = await EnsurePlanIndexAsync(existing.Evidence, cancellationToken).ConfigureAwait(false);
                return repair.Status switch
                {
                    WorkspacePreparationApprovalEvidenceIndexWriteStatus.Created => new(WorkspacePreparationApprovalEvidenceWriteStatus.Created),
                    WorkspacePreparationApprovalEvidenceIndexWriteStatus.AlreadyExists => new(WorkspacePreparationApprovalEvidenceWriteStatus.ApprovalEvidenceConflict, "Immutable approval evidence already exists."),
                    WorkspacePreparationApprovalEvidenceIndexWriteStatus.Conflict => new(WorkspacePreparationApprovalEvidenceWriteStatus.ApprovalEvidenceConflict, repair.ErrorMessage),
                    _ => new(WorkspacePreparationApprovalEvidenceWriteStatus.Unavailable, repair.ErrorMessage ?? "Approval evidence plan index persistence is unavailable.")
                };
            }

            return new(WorkspacePreparationApprovalEvidenceWriteStatus.ApprovalEvidenceConflict, "Immutable approval evidence already exists.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            _logger.LogWarning(exception, "Could not persist workspace approval evidence");
            return new(WorkspacePreparationApprovalEvidenceWriteStatus.Unavailable, "Approval evidence persistence is unavailable.");
        }
    }

    public Task<WorkspacePreparationApprovalEvidenceReadResult> GetAsync(
        Guid projectId,
        Guid workspaceId,
        Guid approvalId,
        CancellationToken cancellationToken = default) =>
        ReadAsync(_paths.GetWorkspaceApprovalEvidenceFile(projectId, workspaceId, approvalId), projectId, workspaceId, approvalId, RecordType, cancellationToken);

    public async Task<WorkspacePreparationApprovalEvidenceReadResult> GetForPlanAsync(
        Guid projectId,
        Guid workspaceId,
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        var path = _paths.GetWorkspaceApprovalEvidenceByPlanFile(projectId, workspaceId, planId);
        var index = await ReadRecordAsync(path, cancellationToken).ConfigureAwait(false);
        if (index.State != WorkspacePreparationApprovalEvidenceReadState.Valid || index.Record is null)
        {
            return index.ToResult();
        }

        if (!string.Equals(index.Record.RecordType, PlanIndexRecordType, StringComparison.Ordinal))
        {
            return new(WorkspacePreparationApprovalEvidenceReadState.IntegrityFailure, ErrorMessage: "Approval evidence plan index record type is invalid.");
        }

        if (index.Record.ProjectId != projectId || index.Record.WorkspaceId != workspaceId ||
            index.Record.PlanReference?.Id != planId)
        {
            return new(WorkspacePreparationApprovalEvidenceReadState.IntegrityFailure, ErrorMessage: "Approval evidence plan index identity does not match its path.");
        }

        var approvalId = index.Record.ApprovalId;
        var evidence = await GetAsync(projectId, workspaceId, approvalId, cancellationToken).ConfigureAwait(false);
        if (!evidence.IsValid || evidence.Evidence is null)
        {
            return evidence;
        }

        return SamePlanReference(index.Record.PlanReference, evidence.Evidence.PlanReference, projectId) &&
               string.Equals(evidence.Evidence.ContentHash, index.Record.ContentHash, StringComparison.OrdinalIgnoreCase)
            ? evidence
            : new(WorkspacePreparationApprovalEvidenceReadState.IntegrityFailure, ErrorMessage: "Approval evidence index does not match the immutable evidence.");
    }

    public async Task<WorkspacePreparationApprovalEvidenceReadResult> FindForPlanAsync(
        Guid projectId,
        Guid workspaceId,
        WorkspacePreparationPlanReference planReference,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(planReference);
        if (planReference.ProjectId != projectId)
            return new(WorkspacePreparationApprovalEvidenceReadState.IntegrityFailure, ErrorMessage: "Approval plan reference belongs to another project.");

        // An existing index is authoritative for the exact plan key. Do not search around a
        // present, invalid, or conflicting index because doing so could select another authority.
        var indexed = await GetForPlanAsync(projectId, workspaceId, planReference.PlanId, cancellationToken).ConfigureAwait(false);
        if (indexed.State != WorkspacePreparationApprovalEvidenceReadState.Missing)
        {
            return indexed.IsValid && indexed.Evidence is not null &&
                   SamePlanReference(indexed.Evidence.PlanReference, planReference)
                ? indexed
                : indexed.IsValid
                    ? new(WorkspacePreparationApprovalEvidenceReadState.IntegrityFailure, ErrorMessage: "Approval evidence index does not bind the exact plan reference.")
                    : indexed;
        }

        var evidenceRoot = Path.Combine(_paths.GetWorkspaceReceiptDirectory(projectId, workspaceId), "approval-evidence");
        if (!Directory.Exists(evidenceRoot)) return new(WorkspacePreparationApprovalEvidenceReadState.Missing);

        string[] candidateDirectories;
        try
        {
            candidateDirectories = Directory.EnumerateDirectories(evidenceRoot)
                .Take(WorkspacePreparationLimits.MaxApprovalEvidenceCandidates + 1)
                .ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new(WorkspacePreparationApprovalEvidenceReadState.Unavailable, ErrorMessage: "Approval evidence authority enumeration is unavailable.");
        }

        if (candidateDirectories.Length > WorkspacePreparationLimits.MaxApprovalEvidenceCandidates)
        {
            return new(WorkspacePreparationApprovalEvidenceReadState.Unavailable, ErrorMessage: "Approval evidence authority enumeration exceeded the supported bound.");
        }

        WorkspacePreparationApprovalEvidence? match = null;
        foreach (var candidateDirectory in candidateDirectories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = Path.GetFileName(candidateDirectory);
            if (!Guid.TryParseExact(name, "D", out var approvalId))
            {
                return new(WorkspacePreparationApprovalEvidenceReadState.IntegrityFailure, ErrorMessage: "Approval evidence authority path is invalid.");
            }

            var candidate = await GetAsync(projectId, workspaceId, approvalId, cancellationToken).ConfigureAwait(false);
            if (!candidate.IsValid || candidate.Evidence is null)
            {
                return candidate.State == WorkspacePreparationApprovalEvidenceReadState.Missing
                    ? new(WorkspacePreparationApprovalEvidenceReadState.IntegrityFailure, ErrorMessage: "Approval evidence authority is incomplete.")
                    : candidate;
            }

            if (!SamePlanReference(candidate.Evidence.PlanReference, planReference)) continue;
            if (match is not null)
            {
                return new(WorkspacePreparationApprovalEvidenceReadState.IntegrityFailure, ErrorMessage: "Multiple canonical approval authorities claim the exact plan.");
            }

            match = candidate.Evidence;
        }

        return match is null
            ? new(WorkspacePreparationApprovalEvidenceReadState.Missing)
            : new(WorkspacePreparationApprovalEvidenceReadState.Valid, match);
    }

    public async Task<WorkspacePreparationApprovalEvidenceIndexWriteResult> EnsurePlanIndexAsync(
        WorkspacePreparationApprovalEvidence evidence,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        var canonical = await GetAsync(evidence.ProjectId, evidence.WorkspaceId, evidence.ApprovalId, cancellationToken).ConfigureAwait(false);
        if (!canonical.IsValid || canonical.Evidence is null)
        {
            return new(WorkspacePreparationApprovalEvidenceIndexWriteStatus.Conflict,
                canonical.ErrorMessage ?? "Canonical approval evidence is unavailable or invalid.");
        }

        if (!SameEvidenceAuthority(canonical.Evidence, evidence))
        {
            return new(WorkspacePreparationApprovalEvidenceIndexWriteStatus.Conflict,
                "Approval id is already bound to different evidence.");
        }

        var planDirectory = _paths.GetWorkspaceApprovalEvidenceByPlanDirectory(evidence.ProjectId, evidence.WorkspaceId, evidence.PlanReference.PlanId);
        if (File.Exists(planDirectory))
        {
            return new(WorkspacePreparationApprovalEvidenceIndexWriteStatus.Unavailable,
                "The approval evidence plan-index directory is not a directory.");
        }

        var indexed = await GetForPlanAsync(evidence.ProjectId, evidence.WorkspaceId, evidence.PlanReference.PlanId, cancellationToken).ConfigureAwait(false);
        if (indexed.IsValid && indexed.Evidence is not null)
        {
            return SameEvidenceAuthority(indexed.Evidence, canonical.Evidence) &&
                   SamePlanReference(indexed.Evidence.PlanReference, evidence.PlanReference)
                ? new(WorkspacePreparationApprovalEvidenceIndexWriteStatus.AlreadyExists)
                : new(WorkspacePreparationApprovalEvidenceIndexWriteStatus.Conflict, "The existing plan index points to conflicting approval authority.");
        }

        if (indexed.State != WorkspacePreparationApprovalEvidenceReadState.Missing)
        {
            return indexed.State == WorkspacePreparationApprovalEvidenceReadState.Unavailable
                ? new(WorkspacePreparationApprovalEvidenceIndexWriteStatus.Unavailable,
                    indexed.ErrorMessage ?? "The existing plan index is unavailable.")
                : new(WorkspacePreparationApprovalEvidenceIndexWriteStatus.Conflict,
                    indexed.ErrorMessage ?? "The existing plan index cannot be read safely.");
        }

        var planPath = _paths.GetWorkspaceApprovalEvidenceByPlanFile(evidence.ProjectId, evidence.WorkspaceId, evidence.PlanReference.PlanId);
        try
        {
            var planRecord = WorkspaceApprovalEvidenceRecord.FromApplication(canonical.Evidence, PlanIndexRecordType);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(planRecord, JsonFileStore.SerializerOptions);
            if (bytes.Length > WorkspacePreparationLimits.MaxCanonicalPayloadBytes)
            {
                return new(WorkspacePreparationApprovalEvidenceIndexWriteStatus.Unavailable, "Approval evidence plan index exceeds the bounded persistence limit.");
            }

            await _paths.EnsureProjectDirectoriesAsync(evidence.ProjectId, cancellationToken).ConfigureAwait(false);
            Directory.CreateDirectory(_paths.GetWorkspaceApprovalEvidenceByPlanDirectory(evidence.ProjectId, evidence.WorkspaceId, evidence.PlanReference.PlanId));
            await _files.CreateNewAsync(planPath, planRecord, cancellationToken).ConfigureAwait(false);
            return new(WorkspacePreparationApprovalEvidenceIndexWriteStatus.Created);
        }
        catch (OperationCanceledException) { throw; }
        catch (IOException)
        {
            var raced = await GetForPlanAsync(evidence.ProjectId, evidence.WorkspaceId, evidence.PlanReference.PlanId, cancellationToken).ConfigureAwait(false);
            if (raced.IsValid && raced.Evidence is not null && SameEvidenceAuthority(raced.Evidence, canonical.Evidence))
                return new(WorkspacePreparationApprovalEvidenceIndexWriteStatus.AlreadyExists);
            return raced.State == WorkspacePreparationApprovalEvidenceReadState.Unavailable
                ? new(WorkspacePreparationApprovalEvidenceIndexWriteStatus.Unavailable, "The plan index is unavailable.")
                : new(WorkspacePreparationApprovalEvidenceIndexWriteStatus.Conflict, "The plan index was created by conflicting authority.");
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or ArgumentException)
        {
            _logger.LogWarning(exception, "Could not persist workspace approval evidence plan index");
            return new(WorkspacePreparationApprovalEvidenceIndexWriteStatus.Unavailable, "Approval evidence plan index persistence is unavailable.");
        }
    }

    private async Task<WorkspacePreparationApprovalEvidenceReadResult> ReadAsync(
        string path,
        Guid projectId,
        Guid workspaceId,
        Guid approvalId,
        string recordType,
        CancellationToken cancellationToken)
    {
        var read = await ReadRecordAsync(path, cancellationToken).ConfigureAwait(false);
        if (read.State != WorkspacePreparationApprovalEvidenceReadState.Valid || read.Record is null)
        {
            return read.ToResult();
        }

        var record = read.Record;
        if (!string.Equals(record.RecordType, recordType, StringComparison.Ordinal) ||
            record.ProjectId != projectId || record.WorkspaceId != workspaceId || record.ApprovalId != approvalId)
        {
            return new(WorkspacePreparationApprovalEvidenceReadState.IntegrityFailure, ErrorMessage: "Approval evidence identity does not match its GUID-derived path.");
        }

        try
        {
            var evidence = record.ToApplication();
            return string.Equals(record.ContentHash, evidence.ContentHash, StringComparison.OrdinalIgnoreCase)
                ? new(WorkspacePreparationApprovalEvidenceReadState.Valid, evidence)
                : new(WorkspacePreparationApprovalEvidenceReadState.IntegrityFailure, ErrorMessage: "Approval evidence content integrity failed.");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return new(WorkspacePreparationApprovalEvidenceReadState.IntegrityFailure, ErrorMessage: "Approval evidence payload integrity failed.");
        }
    }

    private async Task<ApprovalEvidenceRecordRead> ReadRecordAsync(string path, CancellationToken cancellationToken)
    {
        var read = await _files.ReadPreservingAsync<WorkspaceApprovalEvidenceRecord>(path, cancellationToken).ConfigureAwait(false);
        return read.Status switch
        {
            FileReadStatus.Missing => new(WorkspacePreparationApprovalEvidenceReadState.Missing),
            FileReadStatus.UnsupportedSchema => new(WorkspacePreparationApprovalEvidenceReadState.UnsupportedFutureVersion),
            FileReadStatus.Empty or FileReadStatus.Corrupt => new(WorkspacePreparationApprovalEvidenceReadState.Invalid),
            FileReadStatus.IoFailure or FileReadStatus.PermissionFailure => new(WorkspacePreparationApprovalEvidenceReadState.Unavailable),
            _ when read.Value is null => new(WorkspacePreparationApprovalEvidenceReadState.Invalid),
            _ when read.Value.SchemaVersion > WorkspacePreparationApprovalEvidenceSchema.CurrentVersion => new(WorkspacePreparationApprovalEvidenceReadState.UnsupportedFutureVersion),
            _ when read.Value.SchemaVersion < WorkspacePreparationApprovalEvidenceSchema.CurrentVersion => new(WorkspacePreparationApprovalEvidenceReadState.MigrationRequired),
            _ => new(WorkspacePreparationApprovalEvidenceReadState.Valid, read.Value)
        };
    }

    private sealed record ApprovalEvidenceRecordRead(
        WorkspacePreparationApprovalEvidenceReadState State,
        WorkspaceApprovalEvidenceRecord? Record = null,
        string? ErrorMessage = null)
    {
        public WorkspacePreparationApprovalEvidenceReadResult ToResult() => new(State, ErrorMessage: ErrorMessage);
    }

    private static bool SameEvidenceAuthority(WorkspacePreparationApprovalEvidence left, WorkspacePreparationApprovalEvidence right) =>
        left.ProjectId == right.ProjectId && left.WorkspaceId == right.WorkspaceId && left.ApprovalId == right.ApprovalId &&
        left.PlanReference.ProjectId == right.PlanReference.ProjectId && left.PlanReference.PlanId == right.PlanReference.PlanId &&
        left.PlanReference.SchemaVersion == right.PlanReference.SchemaVersion &&
        string.Equals(left.PlanReference.ContentHash, right.PlanReference.ContentHash, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.ActorReference, right.ActorReference, StringComparison.Ordinal) && left.ApprovedAt == right.ApprovedAt &&
        string.Equals(left.SanitizedReason, right.SanitizedReason, StringComparison.Ordinal);

    private static bool SamePlanReference(WorkspacePreparationPlanReference left, WorkspacePreparationPlanReference right) =>
        left.ProjectId == right.ProjectId && left.PlanId == right.PlanId &&
        left.SchemaVersion == right.SchemaVersion &&
        string.Equals(left.ContentHash, right.ContentHash, StringComparison.OrdinalIgnoreCase);

    private static bool SamePlanReference(ReferenceRecord? left, WorkspacePreparationPlanReference right, Guid projectId)
    {
        if (left is null) return false;
        try
        {
            return SamePlanReference(left.ToPlan(projectId), right);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
