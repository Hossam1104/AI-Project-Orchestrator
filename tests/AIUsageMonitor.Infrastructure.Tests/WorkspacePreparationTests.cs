using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Handoffs;
using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Projects;
using AIUsageMonitor.Application.Routing;
using AIUsageMonitor.Application.Time;
using AIUsageMonitor.Application.Workspaces;
using AIUsageMonitor.Infrastructure.Persistence;
using AIUsageMonitor.Infrastructure.Workspaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageMonitor.Infrastructure.Tests;

[Collection("SystemLocalPathProbe")]
public sealed class WorkspacePreparationTests : IDisposable
{
    private readonly string _repoPath = Path.Combine(Path.GetTempPath(), "apo-workspace-repo-" + Guid.NewGuid().ToString("N"));
    private readonly TemporaryStore _store = new();

    [Fact]
    public void WorktreePorcelainParser_PreservesSpacesAndFlags()
    {
        var worktrees = GitWorkspaceRepository.ParseWorktreeList(
            "worktree C:/repo with spaces\nHEAD aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\nbranch refs/heads/main\n\n" +
            "worktree C:/detached\nHEAD bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb\ndetached\nlocked reason\nprunable reason\n\n" +
            "worktree C:/second\nHEAD cccccccccccccccccccccccccccccccccccccccc\nbranch refs/heads/feature\n");

        Assert.True(worktrees.Count == 3);
        Assert.Equal("C:/repo with spaces", worktrees[0].Path);
        Assert.Equal("main", worktrees[0].BranchName);
        Assert.True(worktrees[1].IsDetached);
        Assert.True(worktrees[1].IsLocked);
        Assert.True(worktrees[1].IsPrunable);
        Assert.Equal("feature", worktrees[2].BranchName);
    }

    [Fact]
    public async Task RealRepository_PlansThenCreatesExactIsolatedWorktreeAndReceipt()
    {
        InitializeRepository();
        var baseSha = RunGit("rev-parse", "HEAD");
        var projectId = Guid.NewGuid();
        var contextId = Guid.NewGuid();
        var contract = CreateContract(projectId, contextId, baseSha, _repoPath);
        var project = new Project(projectId, "Workspace test", _repoPath, "main", ProjectStatus.Active, Now, Now);
        var context = CreateContext(projectId, contextId, _repoPath);
        var repository = new GitWorkspaceRepository();
        var pathProvider = new ManagedWorkspacePathProvider(_store.Paths);
        var plans = new JsonWorkspacePreparationPlanRepository(_store.Paths, _store.Files, NullLogger<JsonWorkspacePreparationPlanRepository>.Instance);
        var receipts = new JsonWorkspacePreparationReceiptRepository(_store.Paths, _store.Files, NullLogger<JsonWorkspacePreparationReceiptRepository>.Instance);
        var planning = new WorkspacePreparationPlanningService(
            new FakeProjectRepository(project), new FakeContextResolver(project, context), new FakeContractRepository(contract),
            new FakeRoutingDecisionRepository(), repository, plans, new HandoffRedactionService(), new FixedClock(Now), pathProvider);

        var request = new WorkspacePreparationRequest(projectId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), contract.Reference, baseSha, "apo-test-workspace");
        var planned = await planning.CreatePlanAsync(request);
        Assert.True(planned.Succeeded, planned.ErrorMessage);
        Assert.NotNull(planned.Plan);
        Assert.Empty(Directory.EnumerateDirectories(_repoPath, "apo-test-workspace", SearchOption.TopDirectoryOnly));

        var approval = new WorkspacePreparationApproval(Guid.NewGuid(), planned.Plan!.Reference, "owner:test", Now, "approved for isolated test work");
        var preparation = new WorkspacePreparationService(plans, receipts, repository, new RepositoryPreparationFileLock(_store.Paths), pathProvider, new HandoffRedactionService(), new FixedClock(Now));
        var prepared = await preparation.PrepareAsync(planned.Plan.Reference, approval);
        var debugDiscovery = await repository.DiscoverAsync(_repoPath);
        Assert.True(debugDiscovery.Status == WorkspaceRepositoryDiscoveryStatus.Available, $"{debugDiscovery.Status} {debugDiscovery.ErrorMessage}");
        Assert.True(debugDiscovery.Worktrees.Any(value => WorkspacePreparationPlanningService.SamePath(value.Path, planned.Plan.ProposedWorkspacePath)), $"target={planned.Plan.ProposedWorkspacePath}; {string.Join(" | ", debugDiscovery.Worktrees.Select(value => $"{value.Path}:{value.BranchName}:{value.HeadCommitSha}"))}");

        Assert.True(prepared.Status == WorkspacePreparationStatus.Prepared, prepared.ErrorMessage);
        Assert.NotNull(prepared.Receipt);
        Assert.True(Directory.Exists(prepared.Receipt!.WorkspacePath));
        Assert.Equal(baseSha, prepared.Receipt.ActualHeadCommitSha, ignoreCase: true);
        Assert.Equal("apo-test-workspace", prepared.Receipt.WorkspaceBranch);
        Assert.Equal(WorkspacePreparationStatus.AlreadyPrepared, (await preparation.PrepareAsync(planned.Plan.Reference, approval)).Status);
        Assert.Equal(WorkspaceRecoveryState.PreparedAndRecorded, (await preparation.InspectAsync(planned.Plan.Reference)).State);
        Assert.Equal(baseSha, RunGit("rev-parse", "HEAD"), ignoreCase: true);
        Assert.Equal("main", RunGit("branch", "--show-current"));
    }

    [Fact]
    public async Task PreparationWithoutApproval_IsExplainBeforeWrite()
    {
        var projectId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var plan = CreatePlan(projectId, workspaceId);
        var plans = new InMemoryPlanRepository(plan);
        var repository = new CountingRepository(plan.Repository);
        var service = new WorkspacePreparationService(
            plans, new InMemoryReceiptRepository(), repository, new NoopLock(), new TestPathProvider(plan.ProposedWorkspacePath), new HandoffRedactionService(), new FixedClock(Now));

        var result = await service.PrepareAsync(plan.Reference, null);

        Assert.Equal(WorkspacePreparationStatus.ApprovalRequired, result.Status);
        Assert.Equal(0, repository.MutationCount);
    }

    [Fact]
    public async Task ApprovalMismatchAndCancellation_DoNotEnterMutationGate()
    {
        var plan = CreatePlan(Guid.NewGuid(), Guid.NewGuid());
        var repository = new CountingRepository(plan.Repository);
        var service = new WorkspacePreparationService(new InMemoryPlanRepository(plan), new InMemoryReceiptRepository(), repository, new NoopLock(), new TestPathProvider(plan.ProposedWorkspacePath), new HandoffRedactionService(), new FixedClock(Now));
        var wrongApproval = new WorkspacePreparationApproval(Guid.NewGuid(), new WorkspacePreparationPlanReference(plan.PlanId, 1, new string('c', 64), plan.ProjectId), "owner:test", Now);

        var mismatch = await service.PrepareAsync(plan.Reference, wrongApproval);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        Assert.Equal(WorkspacePreparationStatus.ApprovalMismatch, mismatch.Status);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.PrepareAsync(plan.Reference, new WorkspacePreparationApproval(Guid.NewGuid(), plan.Reference, "owner:test", Now), cancelled.Token));
        Assert.Equal(0, repository.MutationCount);
    }

    [Fact]
    public async Task StaleAndDirtyPlansAreBlockedWithoutGitMutation()
    {
        var plan = CreatePlan(Guid.NewGuid(), Guid.NewGuid());
        var repository = new CountingRepository(plan.Repository);
        var service = new WorkspacePreparationService(new InMemoryPlanRepository(plan), new InMemoryReceiptRepository(), repository, new NoopLock(), new TestPathProvider(plan.ProposedWorkspacePath), new HandoffRedactionService(), new FixedClock(Now));
        var approval = new WorkspacePreparationApproval(Guid.NewGuid(), plan.Reference, "owner:test", Now);
        repository.Current = new WorkspaceRepositoryDiscovery(WorkspaceRepositoryDiscoveryStatus.Available, plan.Repository.RegisteredPath, plan.Repository.RepositoryRoot, plan.Repository.CommonDirectory, false, new string('c', 40), "main", false, true, 0, [], ["main"]);
        var stale = await service.PrepareAsync(plan.Reference, approval);
        repository.Current = plan.Repository;
        Assert.Equal(WorkspacePreparationStatus.PlanStale, stale.Status);

        var dirtyEvidence = new WorkspaceRepositoryDiscovery(WorkspaceRepositoryDiscoveryStatus.Available, plan.Repository.RegisteredPath, plan.Repository.RepositoryRoot, plan.Repository.CommonDirectory, false, plan.BaseCommitSha, "main", false, false, 1, [], ["main"]);
        var dirtyPlan = new WorkspacePreparationPlan(plan.ProjectId, plan.WorkspaceId, Guid.NewGuid(), plan.CorrelationId, Now, plan.Context, plan.ContractReference, null, null, null, dirtyEvidence, plan.BaseCommitSha, plan.WorkspaceBranch, plan.ProposedWorkspacePath, WorkspacePreparationPolicy.RequireCleanSource, true, "dirty source", ["source is dirty"]);
        repository.Current = dirtyEvidence;
        var dirtyService = new WorkspacePreparationService(new InMemoryPlanRepository(dirtyPlan), new InMemoryReceiptRepository(), repository, new NoopLock(), new TestPathProvider(dirtyPlan.ProposedWorkspacePath), new HandoffRedactionService(), new FixedClock(Now));
        var blocked = await dirtyService.PrepareAsync(dirtyPlan.Reference, new WorkspacePreparationApproval(Guid.NewGuid(), dirtyPlan.Reference, "owner:test", Now));
        Assert.Equal(WorkspacePreparationStatus.PolicyBlocked, blocked.Status);
        Assert.Equal(0, repository.MutationCount);
    }

    [Fact]
    public async Task BranchAndPathConflictsAreFailClosed()
    {
        var plan = CreatePlan(Guid.NewGuid(), Guid.NewGuid());
        var repository = new CountingRepository(plan.Repository);
        var path = plan.ProposedWorkspacePath;
        Directory.CreateDirectory(path);
        try
        {
            var service = new WorkspacePreparationService(new InMemoryPlanRepository(plan), new InMemoryReceiptRepository(), repository, new NoopLock(), new TestPathProvider(path), new HandoffRedactionService(), new FixedClock(Now));
            var result = await service.PrepareAsync(plan.Reference, new WorkspacePreparationApproval(Guid.NewGuid(), plan.Reference, "owner:test", Now));
            Assert.Equal(WorkspacePreparationStatus.PathConflict, result.Status);
            Assert.Equal(0, repository.MutationCount);
        }
        finally { Directory.Delete(path, true); }
        repository.Current = new WorkspaceRepositoryDiscovery(WorkspaceRepositoryDiscoveryStatus.Available, plan.Repository.RegisteredPath, plan.Repository.RepositoryRoot, plan.Repository.CommonDirectory, false, plan.BaseCommitSha, "main", false, true, 0, [], [plan.WorkspaceBranch]);
        var branchService = new WorkspacePreparationService(new InMemoryPlanRepository(plan), new InMemoryReceiptRepository(), repository, new NoopLock(), new TestPathProvider(path), new HandoffRedactionService(), new FixedClock(Now));
        Assert.Equal(WorkspacePreparationStatus.BranchConflict, (await branchService.PrepareAsync(plan.Reference, new WorkspacePreparationApproval(Guid.NewGuid(), plan.Reference, "owner:test", Now))).Status);
        Assert.False(WorkspacePreparationPlanningService.IsSafeBranchName("--upload; evil"));
    }

    [Fact]
    public async Task ReceiptFailureLeavesWorkspaceForPreparedWithoutReceiptRecovery()
    {
        var plan = CreatePlan(Guid.NewGuid(), Guid.NewGuid());
        var repository = new StatefulRepository(plan);
        var failingReceipts = new FailingReceiptRepository();
        var service = new WorkspacePreparationService(new InMemoryPlanRepository(plan), failingReceipts, repository, new NoopLock(), new TestPathProvider(plan.ProposedWorkspacePath), new HandoffRedactionService(), new FixedClock(Now));
        var approval = new WorkspacePreparationApproval(Guid.NewGuid(), plan.Reference, "owner:test", Now);

        var failedReceipt = await service.PrepareAsync(plan.Reference, approval);
        var recovery = await service.InspectAsync(plan.Reference);

        Assert.Equal(WorkspacePreparationStatus.ReceiptPersistenceFailed, failedReceipt.Status);
        Assert.Equal(WorkspaceRecoveryState.PreparedWithoutReceipt, recovery.State);
        Assert.Equal(1, repository.MutationCount);
    }

    [Fact]
    public async Task FileLock_SerializesSameRepositoryAndAllowsDifferentRepository()
    {
        var first = new RepositoryPreparationFileLock(_store.Paths, TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(250));
        await using var held = await first.AcquireAsync(Path.Combine(_repoPath, ".git"));
        using var cancelled = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first.AcquireAsync(Path.Combine(_repoPath, ".git"), cancelled.Token));
        await using var different = await first.AcquireAsync(Path.Combine(_repoPath, "other.git"));
    }

    [Fact]
    public async Task PlanAndReceiptRepositories_RejectTamperingWithoutQuarantineOrOverwrite()
    {
        var plan = CreatePlan(Guid.NewGuid(), Guid.NewGuid());
        var repository = new JsonWorkspacePreparationPlanRepository(_store.Paths, _store.Files, NullLogger<JsonWorkspacePreparationPlanRepository>.Instance);
        Assert.True((await repository.CreateAsync(plan)).Succeeded);
        var path = _store.Paths.GetWorkspacePreparationPlanFile(plan.ProjectId, plan.PlanId);
        var original = await File.ReadAllTextAsync(path);
        var document = JsonNode.Parse(original)!.AsObject();
        document["payload"]!["workspaceBranch"] = "tampered";
        await File.WriteAllTextAsync(path, document.ToJsonString(JsonFileStore.SerializerOptions));

        var read = await repository.GetAsync(plan.ProjectId, plan.PlanId);
        Assert.Equal(WorkspacePreparationPlanReadState.IntegrityFailure, read.State);
        Assert.NotEqual(original, await File.ReadAllTextAsync(path));
        Assert.Empty(Directory.EnumerateFiles(_store.RootDirectory, "*.bak", SearchOption.AllDirectories));
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_repoPath)) Directory.Delete(_repoPath, true); } catch { }
        _store.Dispose();
    }

    private void InitializeRepository()
    {
        Directory.CreateDirectory(_repoPath);
        RunGit("init", "-q", "-b", "main");
        RunGit("config", "user.email", "apo-test@example.invalid");
        RunGit("config", "user.name", "APO Test");
        File.WriteAllText(Path.Combine(_repoPath, "tracked.txt"), "initial\n");
        RunGit("add", "tracked.txt");
        RunGit("commit", "-m", "initial");
    }

    private string RunGit(params string[] arguments)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = _repoPath, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        process.WaitForExit();
        if (process.ExitCode != 0) throw new InvalidOperationException(process.StandardError.ReadToEnd());
        return process.StandardOutput.ReadToEnd().Trim();
    }

    private static DateTimeOffset Now => new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

    private static WorkspacePreparationPlan CreatePlan(Guid projectId, Guid workspaceId)
    {
        var path = Path.Combine(Path.GetTempPath(), "managed", projectId.ToString("D"), workspaceId.ToString("D"), "repo");
        return new WorkspacePreparationPlan(projectId, workspaceId, Guid.NewGuid(), Guid.NewGuid(), Now,
            new WorkspaceContextIdentity(projectId, Guid.NewGuid(), 1, Now),
            new PlanningExecutionContractReference(Guid.NewGuid(), 1, 1, new string('a', 64)), null, null, null,
            new WorkspaceRepositoryDiscovery(WorkspaceRepositoryDiscoveryStatus.Available, path, path, Path.Combine(path, ".git"), false, new string('b', 40), "main", false, true),
            new string('b', 40), "workspace", path, WorkspacePreparationPolicy.RequireCleanSource, true, "test plan");
    }

    private static PlanningExecutionContract CreateContract(Guid projectId, Guid contextId, string baseSha, string repositoryPath) => new(
        projectId, Guid.NewGuid(), PlanningExecutionContractSchema.CurrentVersion, 1, Now, "owner:test", Guid.NewGuid(),
        new PlanningContextBinding(contextId, 1), new PlanningWorkItem(PlanningWorkItemSource.Jira, "APO-46", "Prepare workspace"),
        new PlanningRepositoryTarget(PlanningRepositoryMode.LocalGit, repositoryPath, "main", baseSha),
        [new("include", "workspace")], [new("constraint", "exact commit")], [new("forbid", "destructive git")],
        [new("workspace", "isolated workspace", true)], [new("test", PlanningValidationKind.Test, "focused tests", true)],
        [new("safe", "safe", true)], [new(PlanningBudgetKind.Attempts, 1)],
        [new("scope", PlanningStopConditionKind.ScopeViolation, "scope"), new("target", PlanningStopConditionKind.ImmutableTargetMoved, "target"), new("budget", PlanningStopConditionKind.BudgetExceeded, "budget")],
        [], null, null);

    private static ProjectContextReference CreateContext(Guid projectId, Guid contextId, string root) => new(
        projectId, contextId, 1, Now, Now,
        new ProjectRepositoryContextReference(projectId, root, RepositorySelectionState.Inspect, RepositoryVerificationStatus.AvailableClean, root, true, "main", false, [], Now),
        new ProjectTrackerContextReference(TrackerReferenceState.NotConfigured), [], new ProjectCurrentWorkReference(CurrentWorkState.NotSelected), [], null, null, ProjectNextSafeAction.ReadyForPlanning);

    private sealed class FixedClock(DateTimeOffset value) : IClock { public DateTimeOffset UtcNow => value; }
    private sealed class FakeProjectRepository(Project project) : IProjectRepository
    {
        public Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Project>>([project]);
        public Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default) => Task.FromResult<Project?>(projectId == project.Id ? project : null);
        public Task UpsertAsync(Project value, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
    private sealed class FakeContextResolver(Project project, ProjectContextReference context) : IProjectContextResolver
    {
        public Task<ProjectContextResolution> ResolveAsync(Guid projectId, CancellationToken cancellationToken = default) => Task.FromResult(projectId == project.Id ? new ProjectContextResolution(ProjectContextResolutionState.Ready, new ProjectContextView(project, context, [])) : new(ProjectContextResolutionState.ProjectNotFound));
    }
    private sealed class FakeContractRepository(PlanningExecutionContract contract) : IPlanningExecutionContractRepository
    {
        public Task<PlanningContractRepositoryWriteResult> CreateAsync(PlanningExecutionContract value, CancellationToken cancellationToken = default) => Task.FromResult(new PlanningContractRepositoryWriteResult(PlanningContractRepositoryWriteStatus.Created));
        public Task<PlanningContractReadResult> GetAsync(Guid projectId, Guid contractId, int revision, CancellationToken cancellationToken = default) => Task.FromResult(projectId == contract.ProjectId && contractId == contract.ContractId && revision == contract.Revision ? new PlanningContractReadResult(PlanningContractReadState.Valid, contract) : new(PlanningContractReadState.Missing));
        public Task<PlanningContractReadResult> GetLatestAsync(Guid projectId, Guid contractId, CancellationToken cancellationToken = default) => GetAsync(projectId, contractId, 1, cancellationToken);
        public Task<PlanningContractRevisionListResult> ListRevisionsAsync(Guid projectId, Guid contractId, CancellationToken cancellationToken = default) => Task.FromResult(new PlanningContractRevisionListResult(PlanningContractReadState.Valid, [contract]));
    }
    private sealed class FakeRoutingDecisionRepository : IRoutingDecisionRepository
    {
        public Task<RoutingDecisionRepositoryWriteResult> CreateAsync(RoutingDecision decision, CancellationToken cancellationToken = default) => Task.FromResult(new RoutingDecisionRepositoryWriteResult(RoutingDecisionRepositoryWriteStatus.Created));
        public Task<RoutingDecisionReadResult> GetAsync(Guid projectId, Guid decisionId, CancellationToken cancellationToken = default) => Task.FromResult(new RoutingDecisionReadResult(RoutingDecisionReadState.Missing));
    }
    private sealed class InMemoryPlanRepository(WorkspacePreparationPlan plan) : IWorkspacePreparationPlanRepository
    {
        public Task<WorkspacePreparationPlanWriteResult> CreateAsync(WorkspacePreparationPlan value, CancellationToken cancellationToken = default) => Task.FromResult(new WorkspacePreparationPlanWriteResult(WorkspacePreparationPlanWriteStatus.Created));
        public Task<WorkspacePreparationPlanReadResult> GetAsync(Guid projectId, Guid planId, CancellationToken cancellationToken = default) => Task.FromResult(projectId == plan.ProjectId && planId == plan.PlanId ? new WorkspacePreparationPlanReadResult(WorkspacePreparationPlanReadState.Valid, plan) : new(WorkspacePreparationPlanReadState.Missing));
    }
    private sealed class InMemoryReceiptRepository : IWorkspacePreparationReceiptRepository
    {
        public Task<WorkspacePreparationReceiptWriteResult> CreateAsync(WorkspacePreparationReceipt receipt, CancellationToken cancellationToken = default) => Task.FromResult(new WorkspacePreparationReceiptWriteResult(WorkspacePreparationReceiptWriteStatus.Created));
        public Task<WorkspacePreparationReceiptReadResult> GetAsync(Guid projectId, Guid workspaceId, CancellationToken cancellationToken = default) => Task.FromResult(new WorkspacePreparationReceiptReadResult(WorkspacePreparationReceiptReadState.Missing));
    }
    private sealed class CountingRepository(WorkspaceRepositoryDiscovery discovery) : IWorkspaceRepository
    {
        public WorkspaceRepositoryDiscovery Current { get; set; } = discovery;
        public int MutationCount { get; private set; }
        public Task<WorkspaceRepositoryDiscovery> DiscoverAsync(string registeredPath, CancellationToken cancellationToken = default) => Task.FromResult(Current);
        public Task<WorkspaceRepositoryMutationResult> AddExactWorktreeAsync(string commonDirectory, string workspaceBranch, string managedWorkspacePath, string exactBaseCommitSha, CancellationToken cancellationToken = default) { MutationCount++; return Task.FromResult(new WorkspaceRepositoryMutationResult(true)); }
    }
    private sealed class StatefulRepository(WorkspacePreparationPlan plan) : IWorkspaceRepository
    {
        public int MutationCount { get; private set; }
        public WorkspaceRepositoryDiscovery Current { get; private set; } = plan.Repository;
        public Task<WorkspaceRepositoryDiscovery> DiscoverAsync(string registeredPath, CancellationToken cancellationToken = default) => Task.FromResult(Current);
        public Task<WorkspaceRepositoryMutationResult> AddExactWorktreeAsync(string commonDirectory, string workspaceBranch, string managedWorkspacePath, string exactBaseCommitSha, CancellationToken cancellationToken = default)
        {
            MutationCount++;
            Current = new WorkspaceRepositoryDiscovery(WorkspaceRepositoryDiscoveryStatus.Available, plan.Repository.RegisteredPath, plan.Repository.RepositoryRoot, plan.Repository.CommonDirectory, false, plan.BaseCommitSha, plan.Repository.BranchName, false, true, 0, [new WorkspaceWorktreeEvidence(managedWorkspacePath, plan.BaseCommitSha, plan.WorkspaceBranch, false, false, false)], [plan.Repository.BranchName! , plan.WorkspaceBranch]);
            return Task.FromResult(new WorkspaceRepositoryMutationResult(true));
        }
    }
    private sealed class FailingReceiptRepository : IWorkspacePreparationReceiptRepository
    {
        public Task<WorkspacePreparationReceiptWriteResult> CreateAsync(WorkspacePreparationReceipt receipt, CancellationToken cancellationToken = default) => Task.FromResult(new WorkspacePreparationReceiptWriteResult(WorkspacePreparationReceiptWriteStatus.Unavailable, "forced test failure"));
        public Task<WorkspacePreparationReceiptReadResult> GetAsync(Guid projectId, Guid workspaceId, CancellationToken cancellationToken = default) => Task.FromResult(new WorkspacePreparationReceiptReadResult(WorkspacePreparationReceiptReadState.Missing));
    }
    private sealed class NoopLock : IRepositoryPreparationLock { public Task<IAsyncDisposable> AcquireAsync(string repositoryIdentity, CancellationToken cancellationToken = default) => Task.FromResult<IAsyncDisposable>(new NoopAsyncDisposable()); }
    private sealed class NoopAsyncDisposable : IAsyncDisposable { public ValueTask DisposeAsync() => ValueTask.CompletedTask; }
    private sealed class TestPathProvider(string path) : IManagedWorkspacePathProvider
    {
        public string GetWorkspacePath(Guid projectId, Guid workspaceId) => path;
        public bool IsSafeManagedWorkspacePath(Guid projectId, Guid workspaceId, out string resolved, out string? errorMessage) { resolved = path; errorMessage = null; return true; }
    }
}
