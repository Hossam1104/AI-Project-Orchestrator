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
    public string? Limitation { get; set; }
    public string? ContentHash { get; set; }

    public static WorkspaceReceiptRecord FromApplication(WorkspacePreparationReceipt value) => new()
    {
        SchemaVersion = WorkspacePreparationReceiptSchema.CurrentVersion, ProjectId = value.ProjectId, WorkspaceId = value.WorkspaceId,
        CorrelationId = value.CorrelationId, PreparedAt = value.PreparedAt, PlanReference = ReferenceRecord.From(value.PlanReference),
        WorkspacePath = value.WorkspacePath, WorkspaceBranch = value.WorkspaceBranch, BaseCommitSha = value.BaseCommitSha,
        ActualHeadCommitSha = value.ActualHeadCommitSha, RepositoryIdentity = value.RepositoryIdentity,
        CleanupOwnerReference = value.CleanupOwnerReference, Limitation = value.Limitation, ContentHash = value.ContentHash
    };

    public WorkspacePreparationReceipt ToApplication() => new(
        ProjectId, WorkspaceId, CorrelationId, PreparedAt,
        PlanReference?.ToPlan(ProjectId) ?? throw new ArgumentException("Plan reference is missing."),
        WorkspacePath ?? throw new ArgumentException("Workspace path is missing."), WorkspaceBranch ?? throw new ArgumentException("Branch is missing."),
        BaseCommitSha ?? throw new ArgumentException("Base commit is missing."), ActualHeadCommitSha ?? throw new ArgumentException("Actual head is missing."),
        RepositoryIdentity ?? throw new ArgumentException("Repository identity is missing."), CleanupOwnerReference ?? throw new ArgumentException("Cleanup owner is missing."), Limitation);
}

internal sealed class ReferenceRecord
{
    public Guid Id { get; set; }
    public int Revision { get; set; }
    public int SchemaVersion { get; set; }
    public string? ContentHash { get; set; }

    public static ReferenceRecord From(PlanningExecutionContractReference value) => new() { Id = value.ContractId, Revision = value.Revision, SchemaVersion = value.SchemaVersion, ContentHash = value.ContentHash };
    public static ReferenceRecord From(WorkGraphReference value) => new() { Id = value.GraphId, SchemaVersion = value.SchemaVersion, ContentHash = value.ContentHash };
    public static ReferenceRecord From(RoutingDecisionReference value) => new() { Id = value.DecisionId, SchemaVersion = value.SchemaVersion, ContentHash = value.ContentHash };
    public static ReferenceRecord From(WorkspacePreparationPlanReference value) => new() { Id = value.PlanId, SchemaVersion = value.SchemaVersion, ContentHash = value.ContentHash };
    public PlanningExecutionContractReference ToPlanning() => new(Id, Revision, SchemaVersion, ContentHash ?? string.Empty);
    public WorkGraphReference ToGraph() => new(Id, SchemaVersion, ContentHash ?? string.Empty);
    public RoutingDecisionReference ToRouting() => new(Id, SchemaVersion, ContentHash ?? string.Empty);
    public WorkspacePreparationPlanReference ToPlan(Guid projectId = default) => new(Id, SchemaVersion, ContentHash ?? string.Empty, projectId);
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
    public static RepositoryRecord From(WorkspaceRepositoryDiscovery value) => new()
    {
        Status = value.Status, RegisteredPath = value.RegisteredPath, RepositoryRoot = value.RepositoryRoot, CommonDirectory = value.CommonDirectory,
        IsBareRepository = value.IsBareRepository, HeadCommitSha = value.HeadCommitSha, BranchName = value.BranchName, IsDetached = value.IsDetached,
        IsClean = value.IsClean, ChangedFileCount = value.ChangedFileCount, Worktrees = value.Worktrees.Select(WorktreeRecord.From).ToList(), LocalBranches = value.LocalBranches.ToList(), ErrorMessage = value.ErrorMessage
    };
    public WorkspaceRepositoryDiscovery ToApplication() => new(Status, RegisteredPath ?? throw new ArgumentException("Registered path is missing."), RepositoryRoot, CommonDirectory, IsBareRepository, HeadCommitSha, BranchName, IsDetached, IsClean, ChangedFileCount, (Worktrees ?? []).Select(value => value.ToApplication()).ToArray(), LocalBranches, ErrorMessage);
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
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) { return new(WorkspacePreparationPlanReadState.Invalid, ErrorMessage: "Workspace plan payload is invalid."); }
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
            var receipt = record.ToApplication();
            return string.Equals(record.ContentHash, receipt.ContentHash, StringComparison.OrdinalIgnoreCase) ? new(WorkspacePreparationReceiptReadState.Valid, receipt) : new(WorkspacePreparationReceiptReadState.IntegrityFailure, ErrorMessage: "Workspace receipt content integrity failed.");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) { return new(WorkspacePreparationReceiptReadState.Invalid, ErrorMessage: "Workspace receipt payload is invalid."); }
    }
}
