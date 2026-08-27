using System.Text;
using AIUsageMonitor.Application.Handoffs;
using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Time;
using AIUsageMonitor.Application.Workspaces;
using AIUsageMonitor.Infrastructure.Git;
using AIUsageMonitor.Infrastructure.Persistence;
using AIUsageMonitor.Infrastructure.Workspaces;

namespace AIUsageMonitor.Infrastructure.Tests;

[Collection("SystemLocalPathProbe")]
public sealed class WorkspacePreparationRemediationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ApprovalEvidence_IsPersistedBeforeGitMutation()
    {
        var plan = CreatePlan(Guid.NewGuid(), Guid.NewGuid());
        var repository = new ScriptedRepository(plan);
        var approvals = new InMemoryApprovalEvidenceRepository();
        var service = CreateService(plan, repository, new InMemoryReceiptRepository(), approvals);

        var result = await service.PrepareAsync(plan.Reference, new WorkspacePreparationApproval(Guid.NewGuid(), plan.Reference, "owner:test", Now, "api_key=abc123456789"));

        Assert.Equal(WorkspacePreparationStatus.Prepared, result.Status);
        Assert.Equal(1, approvals.CreateCount);
        Assert.True(repository.ApprovalWasPersistedBeforeMutation);
        Assert.NotNull(result.Receipt?.ApprovalReference);
        Assert.Equal(approvals.Evidence!.ContentHash, result.Receipt!.ApprovalReference!.ContentHash);
    }

    [Fact]
    public async Task ApprovalEvidencePersistenceFailure_PreventsGitMutation()
    {
        var plan = CreatePlan(Guid.NewGuid(), Guid.NewGuid());
        var repository = new ScriptedRepository(plan);
        var approvals = new InMemoryApprovalEvidenceRepository { WriteStatus = WorkspacePreparationApprovalEvidenceWriteStatus.Unavailable };
        var service = CreateService(plan, repository, new InMemoryReceiptRepository(), approvals);

        var result = await service.PrepareAsync(plan.Reference, NewApproval(plan));

        Assert.Equal(WorkspacePreparationStatus.ApprovalEvidencePersistenceFailed, result.Status);
        Assert.Equal(0, repository.MutationCount);
    }

    [Fact]
    public async Task SecretShapedActor_IsRejectedBeforeApprovalPersistenceOrGit()
    {
        var plan = CreatePlan(Guid.NewGuid(), Guid.NewGuid());
        var repository = new ScriptedRepository(plan);
        var approvals = new InMemoryApprovalEvidenceRepository();
        var service = CreateService(plan, repository, new InMemoryReceiptRepository(), approvals);

        var result = await service.PrepareAsync(plan.Reference,
            new WorkspacePreparationApproval(Guid.NewGuid(), plan.Reference, "api" + "_key=placeholder-value", Now));

        Assert.Equal(WorkspacePreparationStatus.RedactionRejected, result.Status);
        Assert.Equal(0, approvals.CreateCount);
        Assert.Equal(0, repository.MutationCount);
    }

    [Fact]
    public async Task DirtyOrSourceFingerprintChange_IsPlanStaleBeforeMutation()
    {
        var plan = CreatePlan(Guid.NewGuid(), Guid.NewGuid());
        var repository = new ScriptedRepository(plan)
        {
            Current = WithSource(plan.Repository, fingerprint: new string('b', 64))
        };
        var service = CreateService(plan, repository, new InMemoryReceiptRepository(), new InMemoryApprovalEvidenceRepository());

        var result = await service.PrepareAsync(plan.Reference, NewApproval(plan));

        Assert.Equal(WorkspacePreparationStatus.PlanStale, result.Status);
        Assert.Equal(0, repository.MutationCount);
    }

    [Fact]
    public async Task PostMutationSourceChange_IsVerificationFailureWithoutRollback()
    {
        var plan = CreatePlan(Guid.NewGuid(), Guid.NewGuid());
        var repository = new ScriptedRepository(plan)
        {
            PostMutation = WithSource(plan.Repository, fingerprint: new string('b', 64),
                worktree: new WorkspaceWorktreeEvidence(plan.ProposedWorkspacePath, plan.BaseCommitSha, plan.WorkspaceBranch, false, false, false))
        };
        var service = CreateService(plan, repository, new InMemoryReceiptRepository(), new InMemoryApprovalEvidenceRepository());

        var result = await service.PrepareAsync(plan.Reference, NewApproval(plan));

        Assert.Equal(WorkspacePreparationStatus.VerificationFailed, result.Status);
        Assert.Equal(1, repository.MutationCount);
    }

    [Fact]
    public async Task ValidReceiptForDifferentPlan_IsConflictWithoutMutation()
    {
        var projectId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var planA = CreatePlan(projectId, workspaceId);
        var evidenceA = new WorkspacePreparationApprovalEvidence(projectId, workspaceId, Guid.NewGuid(), planA.Reference, "owner:test", Now, Now);
        var receiptA = CreateReceipt(planA, evidenceA);
        var planB = CreatePlan(projectId, workspaceId);
        var receipts = new InMemoryReceiptRepository { Receipt = receiptA };
        var repository = new ScriptedRepository(planB)
        {
            Current = WithSource(planB.Repository, worktree: new WorkspaceWorktreeEvidence(planB.ProposedWorkspacePath, planB.BaseCommitSha, planB.WorkspaceBranch, false, false, false))
        };
        var service = CreateService(planB, repository, receipts, new InMemoryApprovalEvidenceRepository());

        var result = await service.PrepareAsync(planB.Reference, NewApproval(planB));

        Assert.Equal(WorkspacePreparationStatus.ReceiptConflict, result.Status);
        Assert.Equal(0, repository.MutationCount);
        Assert.Equal(planA.PlanId, receipts.Receipt!.PlanReference.PlanId);
    }

    [Fact]
    public async Task ApprovalRetry_ReusesImmutableEvidenceAndReturnsAlreadyPrepared()
    {
        var plan = CreatePlan(Guid.NewGuid(), Guid.NewGuid());
        var repository = new ScriptedRepository(plan);
        var receipts = new InMemoryReceiptRepository();
        var approvals = new InMemoryApprovalEvidenceRepository();
        var service = CreateService(plan, repository, receipts, approvals);
        var approval = NewApproval(plan);

        var first = await service.PrepareAsync(plan.Reference, approval);
        var recordedHash = approvals.Evidence!.ContentHash;
        var second = await service.PrepareAsync(plan.Reference, approval);

        Assert.Equal(WorkspacePreparationStatus.Prepared, first.Status);
        Assert.Equal(WorkspacePreparationStatus.AlreadyPrepared, second.Status);
        Assert.Equal(recordedHash, approvals.Evidence!.ContentHash);
        Assert.Equal(1, repository.MutationCount);
    }

    [Fact]
    public async Task RealPersistence_RestartsPreparedWithoutReceiptThenFinalizesWithoutSecondMutation()
    {
        using var store = new TemporaryStore();
        var plan = CreatePlan(Guid.NewGuid(), Guid.NewGuid());
        var plans = new JsonWorkspacePreparationPlanRepository(store.Paths, store.Files,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<JsonWorkspacePreparationPlanRepository>.Instance);
        var receipts = new JsonWorkspacePreparationReceiptRepository(store.Paths, store.Files,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<JsonWorkspacePreparationReceiptRepository>.Instance);
        var approvals = new JsonWorkspacePreparationApprovalEvidenceRepository(store.Paths, store.Files,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<JsonWorkspacePreparationApprovalEvidenceRepository>.Instance);
        Assert.True((await plans.CreateAsync(plan)).Succeeded);
        var repository = new ScriptedRepository(plan);
        var approval = NewApproval(plan);
        var firstService = new WorkspacePreparationService(plans, new FailingReceiptRepository(), repository, new NoopLock(),
            new TestPathProvider(plan.ProposedWorkspacePath), new HandoffRedactionService(), new FixedClock(Now), approvalEvidence: approvals);

        var first = await firstService.PrepareAsync(plan.Reference, approval);
        var restartedService = new WorkspacePreparationService(plans, receipts, repository, new NoopLock(),
            new TestPathProvider(plan.ProposedWorkspacePath), new HandoffRedactionService(), new FixedClock(Now), approvalEvidence: approvals);
        var inspection = await restartedService.InspectAsync(plan.Reference);
        var finalized = await restartedService.FinalizeReceiptAsync(plan.Reference, approval);

        Assert.Equal(WorkspacePreparationStatus.ReceiptPersistenceFailed, first.Status);
        Assert.Equal(WorkspaceRecoveryState.PreparedWithoutReceipt, inspection.State);
        Assert.Equal(WorkspacePreparationStatus.Prepared, finalized.Status);
        Assert.Equal(1, repository.MutationCount);
        Assert.Equal(approval.ApprovalId, finalized.Receipt!.ApprovalReference!.ApprovalId);
        Assert.Equal(plan.Reference.ContentHash, finalized.Receipt.PlanReference.ContentHash);
    }

    [Fact]
    public void RootReparsePoint_IsRejectedByManagedPathBoundary()
    {
        using var store = new TemporaryStore();
        var root = Path.GetFullPath(store.Paths.WorkspacesDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var provider = new ManagedWorkspacePathProvider(store.Paths, new FixedPathProbe(root, PathEntryInspection.ReparsePoint));

        var safe = provider.IsSafeManagedWorkspacePath(Guid.NewGuid(), Guid.NewGuid(), out _, out var error);

        Assert.False(safe);
        Assert.Contains("reparse", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WorktreeEvidenceOverflow_IsExplicitAndBounded()
    {
        var output = new StringBuilder();
        for (var index = 0; index < WorkspacePreparationLimits.MaxWorktrees + 1; index++)
        {
            output.Append("worktree C:/workspace-").Append(index).Append("\n");
            output.Append("HEAD ").Append(new string((char)('a' + index % 6), 40)).Append("\n");
            output.Append("branch refs/heads/branch-").Append(index).Append("\n\n");
        }

        var worktrees = GitWorkspaceRepository.ParseWorktreeList(output.ToString(), out var overflow);

        Assert.True(overflow);
        Assert.Equal(WorkspacePreparationLimits.MaxWorktrees, worktrees.Count);
    }

    [Fact]
    public void DiscoveryFlagsLocalBranchOverflowWithoutPretendingEnumerationIsComplete()
    {
        var branches = Enumerable.Range(0, WorkspacePreparationLimits.MaxWorktrees + 1).Select(index => $"branch-{index}").ToArray();
        var discovery = new WorkspaceRepositoryDiscovery(WorkspaceRepositoryDiscoveryStatus.Available, "C:/repo", "C:/repo", "C:/repo/.git",
            headCommitSha: new string('a', 40), branchName: "main", isClean: true, localBranches: branches);

        Assert.True(discovery.LocalBranchEvidenceOverflow);
        Assert.Equal(WorkspacePreparationLimits.MaxWorktrees, discovery.LocalBranches.Count);
    }

    [Fact]
    public void ReceiptExactAssociation_RejectsDifferentCorrelationAndPreservesApprovalBinding()
    {
        var plan = CreatePlan(Guid.NewGuid(), Guid.NewGuid());
        var evidence = new WorkspacePreparationApprovalEvidence(plan.ProjectId, plan.WorkspaceId, Guid.NewGuid(), plan.Reference,
            "owner:test", Now, Now, "approved");
        var receipt = CreateReceipt(plan, evidence);
        var discovery = WithSource(plan.Repository, worktree: new WorkspaceWorktreeEvidence(plan.ProposedWorkspacePath, plan.BaseCommitSha, plan.WorkspaceBranch, false, false, false));

        Assert.True(WorkspacePreparationPlanningService.ReceiptMatchesExactPlan(plan, receipt, evidence, plan.ProposedWorkspacePath, discovery, out _));

        var wrongCorrelation = new WorkspacePreparationReceipt(plan.ProjectId, plan.WorkspaceId, Guid.NewGuid(), Now, plan.Reference,
            receipt.WorkspacePath, receipt.WorkspaceBranch, receipt.BaseCommitSha, receipt.ActualHeadCommitSha, receipt.RepositoryIdentity,
            "APO", approvalReference: receipt.ApprovalReference);
        Assert.False(WorkspacePreparationPlanningService.ReceiptMatchesExactPlan(plan, wrongCorrelation, evidence, plan.ProposedWorkspacePath, discovery, out _));
    }

    [Fact]
    public async Task ApprovalEvidenceRepository_IsCreateOnceAndProjectIsolated()
    {
        using var store = new TemporaryStore();
        var repository = new JsonWorkspacePreparationApprovalEvidenceRepository(store.Paths, store.Files,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<JsonWorkspacePreparationApprovalEvidenceRepository>.Instance);
        var plan = CreatePlan(Guid.NewGuid(), Guid.NewGuid());
        var evidence = new WorkspacePreparationApprovalEvidence(plan.ProjectId, plan.WorkspaceId, Guid.NewGuid(), plan.Reference,
            "owner:test", Now, Now, "approved");

        var first = await repository.CreateAsync(evidence);
        var path = store.Paths.GetWorkspaceApprovalEvidenceFile(plan.ProjectId, plan.WorkspaceId, evidence.ApprovalId);
        var original = await File.ReadAllTextAsync(path);
        var duplicate = await repository.CreateAsync(evidence);
        var read = await repository.GetAsync(plan.ProjectId, plan.WorkspaceId, evidence.ApprovalId);

        Assert.Equal(WorkspacePreparationApprovalEvidenceWriteStatus.Created, first.Status);
        Assert.Equal(WorkspacePreparationApprovalEvidenceWriteStatus.ApprovalEvidenceConflict, duplicate.Status);
        Assert.True(read.IsValid);
        Assert.Equal(original, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task ApprovalEvidencePlanIndex_TamperingIsIntegrityFailure()
    {
        using var store = new TemporaryStore();
        var repository = new JsonWorkspacePreparationApprovalEvidenceRepository(store.Paths, store.Files,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<JsonWorkspacePreparationApprovalEvidenceRepository>.Instance);
        var plan = CreatePlan(Guid.NewGuid(), Guid.NewGuid());
        var evidence = new WorkspacePreparationApprovalEvidence(plan.ProjectId, plan.WorkspaceId, Guid.NewGuid(), plan.Reference,
            "owner:test", Now, Now);
        await repository.CreateAsync(evidence);
        var path = store.Paths.GetWorkspaceApprovalEvidenceByPlanFile(plan.ProjectId, plan.WorkspaceId, plan.PlanId);
        var json = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
        json["payload"]!["recordType"] = "workspace-preparation-approval-evidence";
        await File.WriteAllTextAsync(path, json.ToJsonString(JsonFileStore.SerializerOptions));

        var read = await repository.GetForPlanAsync(plan.ProjectId, plan.WorkspaceId, plan.PlanId);

        Assert.Equal(WorkspacePreparationApprovalEvidenceReadState.IntegrityFailure, read.State);
    }

    [Fact]
    public void WorkingTreeFingerprint_IsDeterministicAndSensitiveToBoundedStatus()
    {
        var clean = WorkspacePreparationIntegrity.ComputeWorkingTreeStateFingerprint(string.Empty);
        var cleanAgain = WorkspacePreparationIntegrity.ComputeWorkingTreeStateFingerprint(string.Empty);
        var dirty = WorkspacePreparationIntegrity.ComputeWorkingTreeStateFingerprint(" M tracked.txt\0");

        Assert.Equal(clean, cleanAgain);
        Assert.NotEqual(clean, dirty);
        Assert.True(WorkspacePreparationIntegrity.IsSha256(clean));
    }

    [Fact]
    public void GitArguments_PreserveBranchTextAsOneShellFreeArgument()
    {
        var branch = "feature/one;two 'quoted'";
        var startInfo = SystemGitCommandRunner.CreateStartInfo(["--git-dir", "C:/repo/.git", "check-ref-format", "--branch", branch]);

        Assert.Equal(branch, startInfo.ArgumentList[^1]);
        Assert.False(startInfo.UseShellExecute);
        Assert.Equal("0", startInfo.Environment["GIT_TERMINAL_PROMPT"]);
    }

    private static WorkspacePreparationService CreateService(
        WorkspacePreparationPlan plan,
        ScriptedRepository repository,
        IWorkspacePreparationReceiptRepository receipts,
        IWorkspacePreparationApprovalEvidenceRepository approvals) =>
        new(new InMemoryPlanRepository(plan), receipts, repository, new NoopLock(), new TestPathProvider(plan.ProposedWorkspacePath),
            new HandoffRedactionService(), new FixedClock(Now), approvalEvidence: approvals);

    private static WorkspacePreparationApproval NewApproval(WorkspacePreparationPlan plan) =>
        new(Guid.NewGuid(), plan.Reference, "owner:test", Now, "approved for isolated preparation");

    private static WorkspacePreparationReceipt CreateReceipt(WorkspacePreparationPlan plan, WorkspacePreparationApprovalEvidence evidence) =>
        new(plan.ProjectId, plan.WorkspaceId, plan.CorrelationId, Now, plan.Reference, plan.ProposedWorkspacePath,
            plan.WorkspaceBranch, plan.BaseCommitSha, plan.BaseCommitSha, plan.Repository.CommonDirectory!, "APO",
            approvalReference: new WorkspacePreparationApprovalReference(evidence.ApprovalId,
                WorkspacePreparationApprovalEvidenceSchema.CurrentVersion, evidence.ContentHash));

    private static WorkspaceRepositoryDiscovery WithSource(
        WorkspaceRepositoryDiscovery source,
        string? fingerprint = null,
        WorkspaceWorktreeEvidence? worktree = null) =>
        new(WorkspaceRepositoryDiscoveryStatus.Available, source.RegisteredPath, source.RepositoryRoot, source.CommonDirectory,
            source.IsBareRepository, source.HeadCommitSha, source.BranchName, source.IsDetached, source.IsClean,
            source.ChangedFileCount, worktree is null ? Array.Empty<WorkspaceWorktreeEvidence>() : [worktree],
            source.LocalBranches, workingTreeStateFingerprint: fingerprint ?? source.WorkingTreeStateFingerprint, divergence: source.Divergence);

    private static WorkspacePreparationPlan CreatePlan(Guid projectId, Guid workspaceId, string? fingerprint = null)
    {
        var path = Path.Combine(Path.GetTempPath(), "apo-remediation", projectId.ToString("D"), workspaceId.ToString("D"), "repo");
        var head = new string('b', 40);
        var repository = new WorkspaceRepositoryDiscovery(WorkspaceRepositoryDiscoveryStatus.Available, path, path,
            Path.Combine(path, ".git"), headCommitSha: head, branchName: "main", isClean: true,
            workingTreeStateFingerprint: fingerprint ?? WorkspacePreparationIntegrity.ComputeWorkingTreeStateFingerprint(string.Empty));
        return new WorkspacePreparationPlan(projectId, workspaceId, Guid.NewGuid(), Guid.NewGuid(), Now,
            new WorkspaceContextIdentity(projectId, Guid.NewGuid(), 1, Now),
            new PlanningExecutionContractReference(Guid.NewGuid(), 1, 1, new string('a', 64)), null, null, null,
            repository, head, "workspace", path, WorkspacePreparationPolicy.RequireCleanSource, true, "remediation test");
    }

    private sealed class FixedClock(DateTimeOffset value) : IClock { public DateTimeOffset UtcNow => value; }

    private sealed class InMemoryPlanRepository(WorkspacePreparationPlan plan) : IWorkspacePreparationPlanRepository
    {
        public Task<WorkspacePreparationPlanWriteResult> CreateAsync(WorkspacePreparationPlan value, CancellationToken cancellationToken = default) => Task.FromResult(new WorkspacePreparationPlanWriteResult(WorkspacePreparationPlanWriteStatus.Created));
        public Task<WorkspacePreparationPlanReadResult> GetAsync(Guid projectId, Guid planId, CancellationToken cancellationToken = default) => Task.FromResult(projectId == plan.ProjectId && planId == plan.PlanId ? new WorkspacePreparationPlanReadResult(WorkspacePreparationPlanReadState.Valid, plan) : new(WorkspacePreparationPlanReadState.Missing));
    }

    private sealed class InMemoryReceiptRepository : IWorkspacePreparationReceiptRepository
    {
        public WorkspacePreparationReceipt? Receipt { get; set; }
        public Task<WorkspacePreparationReceiptWriteResult> CreateAsync(WorkspacePreparationReceipt receipt, CancellationToken cancellationToken = default) { Receipt = receipt; return Task.FromResult(new WorkspacePreparationReceiptWriteResult(WorkspacePreparationReceiptWriteStatus.Created)); }
        public Task<WorkspacePreparationReceiptReadResult> GetAsync(Guid projectId, Guid workspaceId, CancellationToken cancellationToken = default) => Task.FromResult(Receipt is null ? new WorkspacePreparationReceiptReadResult(WorkspacePreparationReceiptReadState.Missing) : new(WorkspacePreparationReceiptReadState.Valid, Receipt));
    }

    private sealed class FailingReceiptRepository : IWorkspacePreparationReceiptRepository
    {
        public Task<WorkspacePreparationReceiptWriteResult> CreateAsync(WorkspacePreparationReceipt receipt, CancellationToken cancellationToken = default) => Task.FromResult(new WorkspacePreparationReceiptWriteResult(WorkspacePreparationReceiptWriteStatus.Unavailable, "forced receipt failure"));
        public Task<WorkspacePreparationReceiptReadResult> GetAsync(Guid projectId, Guid workspaceId, CancellationToken cancellationToken = default) => Task.FromResult(new WorkspacePreparationReceiptReadResult(WorkspacePreparationReceiptReadState.Missing));
    }

    private sealed class InMemoryApprovalEvidenceRepository : IWorkspacePreparationApprovalEvidenceRepository
    {
        public WorkspacePreparationApprovalEvidenceWriteStatus WriteStatus { get; set; } = WorkspacePreparationApprovalEvidenceWriteStatus.Created;
        public WorkspacePreparationApprovalEvidence? Evidence { get; private set; }
        public int CreateCount { get; private set; }
        public Task<WorkspacePreparationApprovalEvidenceWriteResult> CreateAsync(WorkspacePreparationApprovalEvidence evidence, CancellationToken cancellationToken = default)
        {
            CreateCount++;
            if (WriteStatus == WorkspacePreparationApprovalEvidenceWriteStatus.Created) Evidence = evidence;
            return Task.FromResult(new WorkspacePreparationApprovalEvidenceWriteResult(WriteStatus));
        }
        public Task<WorkspacePreparationApprovalEvidenceReadResult> GetAsync(Guid projectId, Guid workspaceId, Guid approvalId, CancellationToken cancellationToken = default) => Task.FromResult<WorkspacePreparationApprovalEvidenceReadResult>(Evidence is not null && Evidence.ProjectId == projectId && Evidence.WorkspaceId == workspaceId && Evidence.ApprovalId == approvalId ? new WorkspacePreparationApprovalEvidenceReadResult(WorkspacePreparationApprovalEvidenceReadState.Valid, Evidence) : new WorkspacePreparationApprovalEvidenceReadResult(WorkspacePreparationApprovalEvidenceReadState.Missing));
        public Task<WorkspacePreparationApprovalEvidenceReadResult> GetForPlanAsync(Guid projectId, Guid workspaceId, Guid planId, CancellationToken cancellationToken = default) => Task.FromResult<WorkspacePreparationApprovalEvidenceReadResult>(Evidence is not null && Evidence.ProjectId == projectId && Evidence.WorkspaceId == workspaceId && Evidence.PlanReference.PlanId == planId ? new WorkspacePreparationApprovalEvidenceReadResult(WorkspacePreparationApprovalEvidenceReadState.Valid, Evidence) : new WorkspacePreparationApprovalEvidenceReadResult(WorkspacePreparationApprovalEvidenceReadState.Missing));
    }

    private sealed class ScriptedRepository(WorkspacePreparationPlan plan) : IWorkspaceRepository
    {
        public WorkspaceRepositoryDiscovery Current { get; set; } = plan.Repository;
        public WorkspaceRepositoryDiscovery PostMutation { get; set; } = WithSource(plan.Repository, worktree: new WorkspaceWorktreeEvidence(plan.ProposedWorkspacePath, plan.BaseCommitSha, plan.WorkspaceBranch, false, false, false));
        public int MutationCount { get; private set; }
        public bool ApprovalWasPersistedBeforeMutation { get; private set; }
        public Task<WorkspaceRepositoryDiscovery> DiscoverAsync(string registeredPath, CancellationToken cancellationToken = default) => Task.FromResult(Current);
        public Task<WorkspaceRepositoryMutationResult> AddExactWorktreeAsync(string commonDirectory, string workspaceBranch, string managedWorkspacePath, string exactBaseCommitSha, CancellationToken cancellationToken = default)
        {
            MutationCount++;
            ApprovalWasPersistedBeforeMutation = true;
            Current = PostMutation;
            return Task.FromResult(new WorkspaceRepositoryMutationResult(true));
        }
    }

    private sealed class NoopLock : IRepositoryPreparationLock { public Task<IAsyncDisposable> AcquireAsync(string repositoryIdentity, CancellationToken cancellationToken = default) => Task.FromResult<IAsyncDisposable>(new NoopAsyncDisposable()); }
    private sealed class NoopAsyncDisposable : IAsyncDisposable { public ValueTask DisposeAsync() => ValueTask.CompletedTask; }
    private sealed class TestPathProvider(string path) : IManagedWorkspacePathProvider
    {
        public string GetWorkspacePath(Guid projectId, Guid workspaceId) => path;
        public bool IsSafeManagedWorkspacePath(Guid projectId, Guid workspaceId, out string resolved, out string? errorMessage) { resolved = path; errorMessage = null; return true; }
    }

    private sealed class FixedPathProbe(string reparsePath, PathEntryInspection reparseInspection) : IManagedWorkspacePathProbe
    {
        public PathEntryInspection Inspect(string path) => string.Equals(path, reparsePath, StringComparison.OrdinalIgnoreCase) ? reparseInspection : PathEntryInspection.Missing;
    }
}
