using System.Diagnostics;
using AIUsageMonitor.Application.Handoffs;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Time;
using AIUsageMonitor.Application.Workspaces;
using AIUsageMonitor.Infrastructure.Git;
using AIUsageMonitor.Infrastructure.Persistence;
using AIUsageMonitor.Infrastructure.Workspaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageMonitor.Infrastructure.Tests;

[Collection("SystemLocalPathProbe")]
public sealed class WorkspacePreparationApo68Tests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);
    private static readonly string BaseSha = new('b', 40);

    [Fact]
    public void GitEnvironmentSanitization_RemovesRepositoryRedirectionButPreservesSafeEnvironment()
    {
        var environment = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["GIT_DIR"] = "inherited-repo",
            ["git_work_tree"] = "inherited-worktree",
            ["GIT_INDEX_FILE"] = "inherited-index",
            ["GIT_OBJECT_DIRECTORY"] = "objects",
            ["GIT_ALTERNATE_OBJECT_DIRECTORIES"] = "alternates",
            ["GIT_CEILING_DIRECTORIES"] = "ceiling",
            ["GIT_COMMON_DIR"] = "common",
            ["GIT_NAMESPACE"] = "namespace",
            ["PATH"] = "preserve-me",
            ["APO_TEST_VALUE"] = "preserve-me-too",
            ["GIT_TERMINAL_PROMPT"] = "1",
            ["LC_ALL"] = "fr-FR"
        };

        GitProcessEnvironment.Sanitize(environment);

        foreach (var name in new[] { "GIT_DIR", "GIT_WORK_TREE", "GIT_INDEX_FILE", "GIT_OBJECT_DIRECTORY",
                     "GIT_ALTERNATE_OBJECT_DIRECTORIES", "GIT_CEILING_DIRECTORIES", "GIT_COMMON_DIR", "GIT_NAMESPACE" })
        {
            Assert.DoesNotContain(name, environment.Keys, StringComparer.OrdinalIgnoreCase);
        }

        Assert.Equal("preserve-me", environment["PATH"]);
        Assert.Equal("preserve-me-too", environment["APO_TEST_VALUE"]);
        Assert.Equal("0", environment["GIT_TERMINAL_PROMPT"]);
        Assert.Equal("0", environment["GIT_OPTIONAL_LOCKS"]);
        Assert.Equal("C", environment["LC_ALL"]);
        Assert.Equal("C", environment["LANG"]);
    }

    [Fact]
    public async Task GitOperations_UseReadOnlyAndLongerMutationProfiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "apo-68-profile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var runner = new RecordingGitRunner((arguments, _) =>
            {
                if (arguments.Contains("worktree") && arguments.Contains("add")) return new GitCommandResult(0, string.Empty, string.Empty);
                if (arguments.Contains("--show-toplevel")) return new GitCommandResult(0, root, string.Empty);
                if (arguments.Contains("--git-common-dir")) return new GitCommandResult(0, ".git", string.Empty);
                if (arguments.Contains("--is-bare-repository")) return new GitCommandResult(0, "false", string.Empty);
                if (arguments.Contains("--verify")) return new GitCommandResult(0, BaseSha, string.Empty);
                if (arguments.Contains("symbolic-ref")) return new GitCommandResult(0, "main", string.Empty);
                if (arguments.Contains("status")) return new GitCommandResult(0, string.Empty, string.Empty);
                if (arguments.Contains("worktree") && arguments.Contains("list"))
                    return new GitCommandResult(0, $"worktree {root}\nHEAD {BaseSha}\nbranch refs/heads/main\n\n", string.Empty);
                if (arguments.Contains("for-each-ref")) return new GitCommandResult(0, "main\n", string.Empty);
                if (arguments.Contains("@{upstream}")) return new GitCommandResult(1, string.Empty, "no upstream");
                if (arguments.Contains("check-ref-format")) return new GitCommandResult(0, string.Empty, string.Empty);
                return new GitCommandResult(0, string.Empty, string.Empty);
            });
            var repository = new GitWorkspaceRepository(runner);

            var discovery = await repository.DiscoverAsync(root);
            var mutation = await repository.AddExactWorktreeAsync(Path.Combine(root, ".git"), "apo-workspace", Path.Combine(root, "managed"), BaseSha);

            Assert.Equal(WorkspaceRepositoryDiscoveryStatus.Available, discovery.Status);
            Assert.True(mutation.Succeeded);
            Assert.NotEmpty(runner.Calls);
            Assert.All(runner.Calls.Where(call => !(call.Arguments.Contains("worktree") && call.Arguments.Contains("add"))),
                call => Assert.Equal(GitCommandExecutionProfile.ReadOnly, call.Profile));
            Assert.Equal(GitCommandExecutionProfile.WorktreeMutation, runner.Calls.Last().Profile);
            Assert.Equal(TimeSpan.FromSeconds(10), SystemGitCommandRunner.DefaultReadOnlyCommandTimeout);
            Assert.Equal(TimeSpan.FromSeconds(120), SystemGitCommandRunner.DefaultWorktreeMutationCommandTimeout);
            Assert.Equal(TimeSpan.FromSeconds(2), SystemGitCommandRunner.DefaultDrainTimeout);
        }
        finally
        {
            DeleteTree(root);
        }
    }

    [Fact]
    public async Task TimedOutWorktreeMutation_FailsClosedWithoutRollbackCommands()
    {
        var root = Path.Combine(Path.GetTempPath(), "apo-68-timeout-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var runner = new RecordingGitRunner((arguments, profile) =>
                arguments.Contains("worktree") && arguments.Contains("add")
                    ? new GitCommandResult(-1, string.Empty, string.Empty, TimedOut: true)
                    : new GitCommandResult(0, string.Empty, string.Empty));
            var repository = new GitWorkspaceRepository(runner);

            var result = await repository.AddExactWorktreeAsync(Path.Combine(root, ".git"), "apo-workspace", Path.Combine(root, "managed"), BaseSha);

            Assert.False(result.Succeeded);
            Assert.True(result.CommandFailed);
            Assert.True(result.TimedOut);
            Assert.Equal(2, runner.Calls.Count);
            Assert.DoesNotContain(runner.Calls.SelectMany(call => call.Arguments), argument =>
                argument is "remove" or "reset" or "clean" or "prune" or "checkout");
            Assert.Equal(GitCommandExecutionProfile.ReadOnly, runner.Calls[0].Profile);
            Assert.Equal(GitCommandExecutionProfile.WorktreeMutation, runner.Calls[1].Profile);
        }
        finally
        {
            DeleteTree(root);
        }
    }

    [Fact]
    public async Task WorkspaceLocalVerification_RealGitProvesExactCleanCheckout()
    {
        using var fixture = new GitFixture();
        if (!fixture.IsGitAvailable) return;

        fixture.Initialize();
        var repository = new GitWorkspaceRepository();
        var discovery = await repository.DiscoverAsync(fixture.SourcePath);
        var sha = discovery.HeadCommitSha!;
        var mutation = await repository.AddExactWorktreeAsync(fixture.CommonDirectory, "apo-workspace", fixture.ManagedPath, sha);

        var verification = await repository.VerifyPreparedWorkspaceAsync(fixture.ManagedPath);

        Assert.True(mutation.Succeeded);
        Assert.Equal(WorkspacePreparedWorkspaceVerificationStatus.Verified, verification.Status);
        Assert.True(verification.IsGitWorktree);
        Assert.True(WorkspaceRepositoryIdentity.AreEqual(fixture.ManagedPath, verification.WorkspacePath));
        Assert.True(WorkspaceRepositoryIdentity.AreEqual(fixture.ManagedPath, verification.RepositoryRoot!));
        Assert.True(WorkspaceRepositoryIdentity.AreEqual(fixture.CommonDirectory, verification.CommonDirectory!));
        Assert.Equal(sha, verification.HeadCommitSha);
        Assert.Equal("apo-workspace", verification.BranchName);
        Assert.False(verification.IsDetached);
        Assert.True(verification.IsClean);
        Assert.Equal(0, verification.ChangedFileCount);
        Assert.Equal(WorkspacePreparationIntegrity.ComputeWorkingTreeStateFingerprint(string.Empty), verification.WorkingTreeStateFingerprint);
    }

    [Fact]
    public async Task Inspect_RequiresWorkspaceLocalEvidenceInAdditionToSourceRegistration()
    {
        var plan = CreatePlan();
        var evidence = CreateEvidence(plan);
        var repository = new ServiceRepository(plan, new WorkspacePreparedWorkspaceVerification(
            WorkspacePreparedWorkspaceVerificationStatus.Verified, plan.ProposedWorkspacePath,
            IsGitWorktree: true, RepositoryRoot: plan.ProposedWorkspacePath,
            CommonDirectory: plan.Repository.CommonDirectory, HeadCommitSha: plan.BaseCommitSha,
            BranchName: plan.WorkspaceBranch, IsClean: false, ChangedFileCount: 1,
            WorkingTreeStateFingerprint: "dirty"));
        var service = CreateService(plan, repository, new ApprovalRepository(evidence));

        var result = await service.InspectAsync(plan.Reference);

        Assert.Equal(WorkspaceRecoveryState.Conflict, result.State);
        Assert.Equal(0, repository.MutationCount);
    }

    [Fact]
    public async Task FinalizeReceipt_RejectsLocallyDirtyWorkspaceBeforeAnyMutation()
    {
        var plan = CreatePlan();
        var evidence = CreateEvidence(plan);
        var repository = new ServiceRepository(plan, new WorkspacePreparedWorkspaceVerification(
            WorkspacePreparedWorkspaceVerificationStatus.Verified, plan.ProposedWorkspacePath,
            IsGitWorktree: true, RepositoryRoot: plan.ProposedWorkspacePath,
            CommonDirectory: plan.Repository.CommonDirectory, HeadCommitSha: plan.BaseCommitSha,
            BranchName: plan.WorkspaceBranch, IsClean: false, ChangedFileCount: 1,
            WorkingTreeStateFingerprint: "dirty"));
        var service = CreateService(plan, repository, new ApprovalRepository(evidence));

        var result = await service.FinalizeReceiptAsync(plan.Reference,
            new WorkspacePreparationApproval(evidence.ApprovalId, plan.Reference, evidence.ActorReference, evidence.ApprovedAt, evidence.SanitizedReason));

        Assert.Equal(WorkspacePreparationStatus.VerificationFailed, result.Status);
        Assert.Equal(0, repository.MutationCount);
    }

    [Fact]
    public async Task Inspect_RejectsRecordedReceiptWhenWorkspaceLocalEvidenceIsNoLongerValid()
    {
        var plan = CreatePlan();
        var evidence = CreateEvidence(plan);
        var repository = new ServiceRepository(plan, new WorkspacePreparedWorkspaceVerification(
            WorkspacePreparedWorkspaceVerificationStatus.Verified, plan.ProposedWorkspacePath,
            IsGitWorktree: true, RepositoryRoot: plan.ProposedWorkspacePath,
            CommonDirectory: plan.Repository.CommonDirectory, HeadCommitSha: plan.BaseCommitSha,
            BranchName: plan.WorkspaceBranch, IsClean: false, ChangedFileCount: 1,
            WorkingTreeStateFingerprint: "dirty"));
        var receipts = new ReceiptRepository { Receipt = CreateReceipt(plan, evidence) };
        var service = CreateService(plan, repository, new ApprovalRepository(evidence), receipts);

        var result = await service.InspectAsync(plan.Reference);

        Assert.Equal(WorkspaceRecoveryState.Conflict, result.State);
        Assert.NotNull(result.Receipt);
    }

    [Fact]
    public async Task ApprovalCrashWindow_ServiceFindsCanonicalEvidenceAndRepairsOnlyIndex()
    {
        var plan = CreatePlan();
        var evidence = CreateEvidence(plan);
        var approvals = new CrashWindowApprovalRepository(evidence);
        var repository = new ServiceRepository(plan, ExactVerification(plan));
        var service = CreateService(plan, repository, approvals);

        var inspection = await service.InspectAsync(plan.Reference);
        var finalized = await service.FinalizeReceiptAsync(plan.Reference,
            new WorkspacePreparationApproval(evidence.ApprovalId, plan.Reference, evidence.ActorReference, evidence.ApprovedAt, evidence.SanitizedReason));

        Assert.Equal(WorkspaceRecoveryState.PreparedWithoutReceipt, inspection.State);
        Assert.Equal(WorkspacePreparationStatus.Prepared, finalized.Status);
        Assert.True(approvals.IndexPresent);
        Assert.Equal(1, approvals.EnsureIndexCount);
        Assert.Equal(0, repository.MutationCount);
        Assert.Equal(evidence.ContentHash, approvals.Evidence!.ContentHash);
    }

    [Fact]
    public async Task ApprovalEvidenceRepository_MissingIndexFindsExactCanonicalAndRepairsIndexOnly()
    {
        using var store = new TemporaryStore();
        var plan = CreatePlan();
        var evidence = CreateEvidence(plan);
        var repository = CreateApprovalRepository(store);
        Assert.Equal(WorkspacePreparationApprovalEvidenceWriteStatus.Created, (await repository.CreateAsync(evidence)).Status);
        var canonicalPath = store.Paths.GetWorkspaceApprovalEvidenceFile(plan.ProjectId, plan.WorkspaceId, evidence.ApprovalId);
        var indexDirectory = store.Paths.GetWorkspaceApprovalEvidenceByPlanDirectory(plan.ProjectId, plan.WorkspaceId, plan.PlanId);
        var canonicalBytes = await File.ReadAllTextAsync(canonicalPath);
        Directory.Delete(indexDirectory, recursive: true);

        var indexedBeforeRecovery = await repository.GetForPlanAsync(plan.ProjectId, plan.WorkspaceId, plan.PlanId);
        var found = await repository.FindForPlanAsync(plan.ProjectId, plan.WorkspaceId, plan.Reference);
        var repaired = await repository.EnsurePlanIndexAsync(found.Evidence!);
        var repairedAgain = await repository.EnsurePlanIndexAsync(found.Evidence!);

        Assert.Equal(WorkspacePreparationApprovalEvidenceReadState.Missing, indexedBeforeRecovery.State);
        Assert.True(found.IsValid);
        Assert.Equal(WorkspacePreparationApprovalEvidenceIndexWriteStatus.Created, repaired.Status);
        Assert.Equal(WorkspacePreparationApprovalEvidenceIndexWriteStatus.AlreadyExists, repairedAgain.Status);
        Assert.Equal(canonicalBytes, await File.ReadAllTextAsync(canonicalPath));
    }

    [Fact]
    public async Task PresentPlanIndexWithMissingCanonical_DoesNotFallbackToAlternateAuthority()
    {
        using var store = new TemporaryStore();
        var plan = CreatePlan();
        var repository = CreateApprovalRepository(store);
        var original = CreateEvidence(plan);
        Assert.Equal(WorkspacePreparationApprovalEvidenceWriteStatus.Created, (await repository.CreateAsync(original)).Status);

        var indexPath = store.Paths.GetWorkspaceApprovalEvidenceByPlanFile(plan.ProjectId, plan.WorkspaceId, plan.PlanId);
        var originalIndexBytes = await File.ReadAllTextAsync(indexPath);
        var originalCanonicalPath = store.Paths.GetWorkspaceApprovalEvidenceFile(plan.ProjectId, plan.WorkspaceId, original.ApprovalId);
        File.Delete(originalCanonicalPath);

        var alternate = CreateEvidence(plan);
        Assert.Equal(WorkspacePreparationApprovalEvidenceWriteStatus.ApprovalEvidenceConflict, (await repository.CreateAsync(alternate)).Status);
        var alternatePath = store.Paths.GetWorkspaceApprovalEvidenceFile(plan.ProjectId, plan.WorkspaceId, alternate.ApprovalId);
        var alternateBytes = await File.ReadAllTextAsync(alternatePath);

        var indexed = await repository.GetForPlanAsync(plan.ProjectId, plan.WorkspaceId, plan.PlanId);
        var found = await repository.FindForPlanAsync(plan.ProjectId, plan.WorkspaceId, plan.Reference);

        Assert.Equal(WorkspacePreparationApprovalEvidenceReadState.IntegrityFailure, indexed.State);
        Assert.Equal(WorkspacePreparationApprovalEvidenceReadState.IntegrityFailure, found.State);
        Assert.False(found.IsValid);
        Assert.Null(found.Evidence);
        Assert.Equal(originalIndexBytes, await File.ReadAllTextAsync(indexPath));
        Assert.Equal(alternateBytes, await File.ReadAllTextAsync(alternatePath));
        Assert.False(File.Exists(originalCanonicalPath));
        Assert.Empty(Directory.EnumerateFiles(store.RootDirectory, "*.bak", SearchOption.AllDirectories));
        Assert.Empty(Directory.EnumerateFiles(store.RootDirectory, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task PresentPlanIndexWithMissingCanonical_FailsClosedWithoutAlternate()
    {
        using var store = new TemporaryStore();
        var plan = CreatePlan();
        var repository = CreateApprovalRepository(store);
        var original = CreateEvidence(plan);
        Assert.Equal(WorkspacePreparationApprovalEvidenceWriteStatus.Created, (await repository.CreateAsync(original)).Status);

        var indexPath = store.Paths.GetWorkspaceApprovalEvidenceByPlanFile(plan.ProjectId, plan.WorkspaceId, plan.PlanId);
        var originalIndexBytes = await File.ReadAllTextAsync(indexPath);
        File.Delete(store.Paths.GetWorkspaceApprovalEvidenceFile(plan.ProjectId, plan.WorkspaceId, original.ApprovalId));

        var indexed = await repository.GetForPlanAsync(plan.ProjectId, plan.WorkspaceId, plan.PlanId);
        var found = await repository.FindForPlanAsync(plan.ProjectId, plan.WorkspaceId, plan.Reference);

        Assert.Equal(WorkspacePreparationApprovalEvidenceReadState.IntegrityFailure, indexed.State);
        Assert.Equal(WorkspacePreparationApprovalEvidenceReadState.IntegrityFailure, found.State);
        Assert.Equal(originalIndexBytes, await File.ReadAllTextAsync(indexPath));
        Assert.Empty(Directory.EnumerateFiles(store.RootDirectory, "*.bak", SearchOption.AllDirectories));
        Assert.Empty(Directory.EnumerateFiles(store.RootDirectory, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task EnsurePlanIndexWithMissingIndexedCanonical_ReturnsConflictWithoutReplacingIndex()
    {
        using var store = new TemporaryStore();
        var plan = CreatePlan();
        var repository = CreateApprovalRepository(store);
        var original = CreateEvidence(plan);
        Assert.Equal(WorkspacePreparationApprovalEvidenceWriteStatus.Created, (await repository.CreateAsync(original)).Status);

        var indexPath = store.Paths.GetWorkspaceApprovalEvidenceByPlanFile(plan.ProjectId, plan.WorkspaceId, plan.PlanId);
        var originalIndexBytes = await File.ReadAllTextAsync(indexPath);
        File.Delete(store.Paths.GetWorkspaceApprovalEvidenceFile(plan.ProjectId, plan.WorkspaceId, original.ApprovalId));

        var alternate = CreateEvidence(plan);
        Assert.Equal(WorkspacePreparationApprovalEvidenceWriteStatus.ApprovalEvidenceConflict, (await repository.CreateAsync(alternate)).Status);
        var alternatePath = store.Paths.GetWorkspaceApprovalEvidenceFile(plan.ProjectId, plan.WorkspaceId, alternate.ApprovalId);
        var alternateBytes = await File.ReadAllTextAsync(alternatePath);

        var result = await repository.EnsurePlanIndexAsync(alternate);

        Assert.Equal(WorkspacePreparationApprovalEvidenceIndexWriteStatus.Conflict, result.Status);
        Assert.Equal(originalIndexBytes, await File.ReadAllTextAsync(indexPath));
        Assert.Equal(alternateBytes, await File.ReadAllTextAsync(alternatePath));
        Assert.Empty(Directory.EnumerateFiles(store.RootDirectory, "*.bak", SearchOption.AllDirectories));
        Assert.Empty(Directory.EnumerateFiles(store.RootDirectory, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task InspectWithPresentBrokenPlanIndex_FailsClosedWithoutReceiptOrGitMutation()
    {
        using var store = new TemporaryStore();
        var plan = CreatePlan();
        var plans = new JsonWorkspacePreparationPlanRepository(store.Paths, store.Files,
            NullLogger<JsonWorkspacePreparationPlanRepository>.Instance);
        var receipts = new JsonWorkspacePreparationReceiptRepository(store.Paths, store.Files,
            NullLogger<JsonWorkspacePreparationReceiptRepository>.Instance);
        var approvals = CreateApprovalRepository(store);
        Assert.Equal(WorkspacePreparationPlanWriteStatus.Created, (await plans.CreateAsync(plan)).Status);

        var original = CreateEvidence(plan);
        Assert.Equal(WorkspacePreparationApprovalEvidenceWriteStatus.Created, (await approvals.CreateAsync(original)).Status);
        var indexPath = store.Paths.GetWorkspaceApprovalEvidenceByPlanFile(plan.ProjectId, plan.WorkspaceId, plan.PlanId);
        var originalIndexBytes = await File.ReadAllTextAsync(indexPath);
        File.Delete(store.Paths.GetWorkspaceApprovalEvidenceFile(plan.ProjectId, plan.WorkspaceId, original.ApprovalId));

        var alternate = CreateEvidence(plan);
        Assert.Equal(WorkspacePreparationApprovalEvidenceWriteStatus.ApprovalEvidenceConflict, (await approvals.CreateAsync(alternate)).Status);
        var alternatePath = store.Paths.GetWorkspaceApprovalEvidenceFile(plan.ProjectId, plan.WorkspaceId, alternate.ApprovalId);
        var alternateBytes = await File.ReadAllTextAsync(alternatePath);
        var repository = new ServiceRepository(plan, ExactVerification(plan));
        var service = new WorkspacePreparationService(plans, receipts, repository, new NoopLock(),
            new PathProvider(plan.ProposedWorkspacePath), new HandoffRedactionService(), new FixedClock(Now),
            approvalEvidence: approvals);

        var result = await service.InspectAsync(plan.Reference);
        var receipt = await receipts.GetAsync(plan.ProjectId, plan.WorkspaceId);

        Assert.Equal(WorkspaceRecoveryState.IntegrityFailure, result.State);
        Assert.NotEqual(WorkspaceRecoveryState.PreparedWithoutReceipt, result.State);
        Assert.NotEqual(WorkspaceRecoveryState.PreparedAndRecorded, result.State);
        Assert.Equal(0, repository.MutationCount);
        Assert.Equal(WorkspacePreparationReceiptReadState.Missing, receipt.State);
        Assert.False(File.Exists(store.Paths.GetWorkspaceReceiptFile(plan.ProjectId, plan.WorkspaceId)));
        Assert.Equal(originalIndexBytes, await File.ReadAllTextAsync(indexPath));
        Assert.Equal(alternateBytes, await File.ReadAllTextAsync(alternatePath));
        Assert.Empty(Directory.EnumerateFiles(store.RootDirectory, "*.bak", SearchOption.AllDirectories));
        Assert.Empty(Directory.EnumerateFiles(store.RootDirectory, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task ApprovalEvidenceRepository_AmbiguousOrOverflowAuthorityFailsClosed()
    {
        using var store = new TemporaryStore();
        var plan = CreatePlan();
        var repository = CreateApprovalRepository(store);
        var first = CreateEvidence(plan);
        var second = CreateEvidence(plan);
        Assert.Equal(WorkspacePreparationApprovalEvidenceWriteStatus.Created, (await repository.CreateAsync(first)).Status);
        Assert.Equal(WorkspacePreparationApprovalEvidenceWriteStatus.ApprovalEvidenceConflict, (await repository.CreateAsync(second)).Status);
        Directory.Delete(store.Paths.GetWorkspaceApprovalEvidenceByPlanDirectory(plan.ProjectId, plan.WorkspaceId, plan.PlanId), recursive: true);

        var ambiguous = await repository.FindForPlanAsync(plan.ProjectId, plan.WorkspaceId, plan.Reference);

        Assert.Equal(WorkspacePreparationApprovalEvidenceReadState.IntegrityFailure, ambiguous.State);

        using var overflowStore = new TemporaryStore();
        var overflowRepository = CreateApprovalRepository(overflowStore);
        var root = Path.Combine(overflowStore.Paths.GetWorkspaceReceiptDirectory(plan.ProjectId, plan.WorkspaceId), "approval-evidence");
        Directory.CreateDirectory(root);
        for (var index = 0; index <= WorkspacePreparationLimits.MaxApprovalEvidenceCandidates; index++)
            Directory.CreateDirectory(Path.Combine(root, Guid.NewGuid().ToString("D")));

        var overflow = await overflowRepository.FindForPlanAsync(plan.ProjectId, plan.WorkspaceId, plan.Reference);

        Assert.Equal(WorkspacePreparationApprovalEvidenceReadState.Unavailable, overflow.State);
    }

    [Fact]
    public async Task ApprovalEvidenceRepository_CorruptCanonicalIsNotAdopted()
    {
        using var store = new TemporaryStore();
        var plan = CreatePlan();
        var evidence = CreateEvidence(plan);
        var repository = CreateApprovalRepository(store);
        Assert.Equal(WorkspacePreparationApprovalEvidenceWriteStatus.Created, (await repository.CreateAsync(evidence)).Status);
        Directory.Delete(store.Paths.GetWorkspaceApprovalEvidenceByPlanDirectory(plan.ProjectId, plan.WorkspaceId, plan.PlanId), recursive: true);
        await File.WriteAllTextAsync(store.Paths.GetWorkspaceApprovalEvidenceFile(plan.ProjectId, plan.WorkspaceId, evidence.ApprovalId), "not-json");

        var result = await repository.FindForPlanAsync(plan.ProjectId, plan.WorkspaceId, plan.Reference);

        Assert.Equal(WorkspacePreparationApprovalEvidenceReadState.Invalid, result.State);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task RepositoryPreparationFileLock_EquivalentWindowsIdentitiesSerialize()
    {
        using var store = new TemporaryStore();
        var identity = Path.Combine(store.RootDirectory, "Repo", ".git");
        var equivalent = identity.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (OperatingSystem.IsWindows()) equivalent = equivalent.ToUpperInvariant();
        var firstLock = new RepositoryPreparationFileLock(store.Paths, TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(150));
        var secondLock = new RepositoryPreparationFileLock(store.Paths, TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(150));

        await using var held = await firstLock.AcquireAsync(identity);
        Assert.Equal(WorkspaceRepositoryIdentity.Normalize(identity), WorkspaceRepositoryIdentity.Normalize(equivalent));
        Assert.Equal(store.Paths.GetWorkspaceLockFile(WorkspaceRepositoryIdentity.Normalize(identity)),
            store.Paths.GetWorkspaceLockFile(WorkspaceRepositoryIdentity.Normalize(equivalent)));
        Assert.StartsWith(store.Paths.WorkspaceLocksDirectory, store.Paths.GetWorkspaceLockFile(WorkspaceRepositoryIdentity.Normalize(identity)), StringComparison.OrdinalIgnoreCase);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => secondLock.AcquireAsync(equivalent, cancellation.Token));

        await using var different = await firstLock.AcquireAsync(Path.Combine(store.RootDirectory, "Other", ".git"));
    }

    private static WorkspacePreparationService CreateService(
        WorkspacePreparationPlan plan,
        IWorkspaceRepository repository,
        IWorkspacePreparationApprovalEvidenceRepository approvals,
        IWorkspacePreparationReceiptRepository? receipts = null) =>
        new(new PlanRepository(plan), receipts ?? new ReceiptRepository(), repository, new NoopLock(),
            new PathProvider(plan.ProposedWorkspacePath), new HandoffRedactionService(), new FixedClock(Now), approvalEvidence: approvals);

    private static JsonWorkspacePreparationApprovalEvidenceRepository CreateApprovalRepository(TemporaryStore store) =>
        new(store.Paths, store.Files, NullLogger<JsonWorkspacePreparationApprovalEvidenceRepository>.Instance);

    private static WorkspacePreparationPlan CreatePlan()
    {
        var projectId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var source = Path.Combine(Path.GetTempPath(), "apo-68-source", projectId.ToString("D"));
        var managed = Path.Combine(Path.GetTempPath(), "apo-68-managed", projectId.ToString("D"), workspaceId.ToString("D"), "repo");
        var fingerprint = WorkspacePreparationIntegrity.ComputeWorkingTreeStateFingerprint(string.Empty);
        var repository = new WorkspaceRepositoryDiscovery(WorkspaceRepositoryDiscoveryStatus.Available, source, source,
            Path.Combine(source, ".git"), false, BaseSha, "main", false, true, 0,
            [new WorkspaceWorktreeEvidence(source, BaseSha, "main", false, false, false)], ["main"],
            workingTreeStateFingerprint: fingerprint, divergence: new WorkspaceRepositoryDivergence(WorkspaceDivergenceState.NotConfigured));
        return new WorkspacePreparationPlan(projectId, workspaceId, planId, Guid.NewGuid(), Now,
            new WorkspaceContextIdentity(projectId, Guid.NewGuid(), 1, Now),
            new PlanningExecutionContractReference(Guid.NewGuid(), 1, 1, new string('a', 64)), null, null, null,
            repository, BaseSha, "apo-workspace", managed, WorkspacePreparationPolicy.RequireCleanSource, true, "APO-68 test plan");
    }

    private static WorkspacePreparationApprovalEvidence CreateEvidence(WorkspacePreparationPlan plan) =>
        new(plan.ProjectId, plan.WorkspaceId, Guid.NewGuid(), plan.Reference, "owner:apo-68", Now, Now, "test approval");

    private static WorkspacePreparationReceipt CreateReceipt(WorkspacePreparationPlan plan, WorkspacePreparationApprovalEvidence evidence) =>
        new(plan.ProjectId, plan.WorkspaceId, plan.CorrelationId, Now, plan.Reference, plan.ProposedWorkspacePath,
            plan.WorkspaceBranch, plan.BaseCommitSha, plan.BaseCommitSha, plan.Repository.CommonDirectory!, "APO",
            approvalReference: new WorkspacePreparationApprovalReference(evidence.ApprovalId,
                WorkspacePreparationApprovalEvidenceSchema.CurrentVersion, evidence.ContentHash));

    private static WorkspacePreparedWorkspaceVerification ExactVerification(WorkspacePreparationPlan plan) =>
        new(WorkspacePreparedWorkspaceVerificationStatus.Verified, plan.ProposedWorkspacePath, true,
            plan.ProposedWorkspacePath, plan.Repository.CommonDirectory, plan.BaseCommitSha, plan.WorkspaceBranch,
            false, true, 0, WorkspacePreparationIntegrity.ComputeWorkingTreeStateFingerprint(string.Empty));

    private static void DeleteTree(string path)
    {
        if (!Directory.Exists(path)) return;
        try { Directory.Delete(path, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void RunGit(string workingDirectory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start Git fixture command.");
        process.WaitForExit(10_000);
        if (!process.HasExited || process.ExitCode != 0)
            throw new InvalidOperationException(process.StandardError.ReadToEnd());
    }

    private sealed class RecordingGitRunner(Func<IReadOnlyList<string>, GitCommandExecutionProfile, GitCommandResult> handler) : IGitCommandRunner
    {
        public List<(IReadOnlyList<string> Arguments, GitCommandExecutionProfile Profile)> Calls { get; } = [];

        public Task<GitCommandResult> RunAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken = default) =>
            RunAsync(arguments, GitCommandExecutionProfile.ReadOnly, cancellationToken);

        public Task<GitCommandResult> RunAsync(IReadOnlyList<string> arguments, GitCommandExecutionProfile profile, CancellationToken cancellationToken = default)
        {
            Calls.Add((arguments.ToArray(), profile));
            return Task.FromResult(handler(arguments, profile));
        }
    }

    private sealed class GitFixture : IDisposable
    {
        public GitFixture()
        {
            RootPath = Path.Combine(Path.GetTempPath(), "apo-68-real-git-" + Guid.NewGuid().ToString("N"));
            SourcePath = Path.Combine(RootPath, "source");
            ManagedPath = Path.Combine(RootPath, "managed");
            CommonDirectory = Path.Combine(SourcePath, ".git");
            IsGitAvailable = ProbeGit();
        }

        public string RootPath { get; }
        public string SourcePath { get; }
        public string ManagedPath { get; }
        public string CommonDirectory { get; }
        public bool IsGitAvailable { get; }

        public void Initialize()
        {
            Directory.CreateDirectory(SourcePath);
            RunGit(SourcePath, "init", "-q", "-b", "main");
            RunGit(SourcePath, "config", "user.email", "apo-test@example.invalid");
            RunGit(SourcePath, "config", "user.name", "APO Test");
            File.WriteAllText(Path.Combine(SourcePath, "tracked.txt"), "initial\n");
            RunGit(SourcePath, "add", "tracked.txt");
            RunGit(SourcePath, "commit", "-m", "initial");
        }

        public void Dispose() => DeleteTree(RootPath);

        private static bool ProbeGit()
        {
            try
            {
                var startInfo = new ProcessStartInfo("git", "--version")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var process = Process.Start(startInfo);
                if (process is null) return false;
                process.WaitForExit(5_000);
                return process.HasExited && process.ExitCode == 0;
            }
            catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                return false;
            }
        }
    }

    private sealed class FixedClock(DateTimeOffset value) : IClock
    {
        public DateTimeOffset UtcNow => value;
    }

    private sealed class PlanRepository(WorkspacePreparationPlan plan) : IWorkspacePreparationPlanRepository
    {
        public Task<WorkspacePreparationPlanWriteResult> CreateAsync(WorkspacePreparationPlan value, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorkspacePreparationPlanWriteResult(WorkspacePreparationPlanWriteStatus.Created));

        public Task<WorkspacePreparationPlanReadResult> GetAsync(Guid projectId, Guid planId, CancellationToken cancellationToken = default) =>
            Task.FromResult(projectId == plan.ProjectId && planId == plan.PlanId
                ? new WorkspacePreparationPlanReadResult(WorkspacePreparationPlanReadState.Valid, plan)
                : new WorkspacePreparationPlanReadResult(WorkspacePreparationPlanReadState.Missing));
    }

    private sealed class ReceiptRepository : IWorkspacePreparationReceiptRepository
    {
        private WorkspacePreparationReceipt? _receipt;

        public WorkspacePreparationReceipt? Receipt
        {
            get => _receipt;
            init => _receipt = value;
        }

        public Task<WorkspacePreparationReceiptWriteResult> CreateAsync(WorkspacePreparationReceipt receipt, CancellationToken cancellationToken = default)
        {
            _receipt = receipt;
            return Task.FromResult(new WorkspacePreparationReceiptWriteResult(WorkspacePreparationReceiptWriteStatus.Created));
        }

        public Task<WorkspacePreparationReceiptReadResult> GetAsync(Guid projectId, Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_receipt is null
                ? new WorkspacePreparationReceiptReadResult(WorkspacePreparationReceiptReadState.Missing)
                : new WorkspacePreparationReceiptReadResult(WorkspacePreparationReceiptReadState.Valid, _receipt));
    }

    private sealed class ServiceRepository(
        WorkspacePreparationPlan plan,
        WorkspacePreparedWorkspaceVerification verification) : IWorkspaceRepository, IWorkspacePreparedWorkspaceVerifier
    {
        public int MutationCount { get; private set; }

        public Task<WorkspaceRepositoryDiscovery> DiscoverAsync(string registeredPath, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorkspaceRepositoryDiscovery(WorkspaceRepositoryDiscoveryStatus.Available, plan.Repository.RegisteredPath,
                plan.Repository.RepositoryRoot, plan.Repository.CommonDirectory, false, plan.Repository.HeadCommitSha, plan.Repository.BranchName,
                false, true, 0,
                [new WorkspaceWorktreeEvidence(plan.Repository.RepositoryRoot!, plan.BaseCommitSha, plan.Repository.BranchName, false, false, false),
                 new WorkspaceWorktreeEvidence(plan.ProposedWorkspacePath, plan.BaseCommitSha, plan.WorkspaceBranch, false, false, false)],
                ["main", plan.WorkspaceBranch], workingTreeStateFingerprint: plan.Repository.WorkingTreeStateFingerprint,
                divergence: plan.Repository.Divergence));

        public Task<WorkspacePreparedWorkspaceVerification> VerifyPreparedWorkspaceAsync(string workspacePath, CancellationToken cancellationToken = default) =>
            Task.FromResult(verification);

        public Task<WorkspaceRepositoryMutationResult> AddExactWorktreeAsync(string commonDirectory, string workspaceBranch, string managedWorkspacePath, string exactBaseCommitSha, CancellationToken cancellationToken = default)
        {
            MutationCount++;
            return Task.FromResult(new WorkspaceRepositoryMutationResult(true));
        }
    }

    private class ApprovalRepository(WorkspacePreparationApprovalEvidence evidence) : IWorkspacePreparationApprovalEvidenceRepository
    {
        public virtual Task<WorkspacePreparationApprovalEvidenceWriteResult> CreateAsync(WorkspacePreparationApprovalEvidence value, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorkspacePreparationApprovalEvidenceWriteResult(WorkspacePreparationApprovalEvidenceWriteStatus.Created));

        public virtual Task<WorkspacePreparationApprovalEvidenceReadResult> GetAsync(Guid projectId, Guid workspaceId, Guid approvalId, CancellationToken cancellationToken = default) =>
            Task.FromResult(projectId == evidence.ProjectId && workspaceId == evidence.WorkspaceId && approvalId == evidence.ApprovalId
                ? new WorkspacePreparationApprovalEvidenceReadResult(WorkspacePreparationApprovalEvidenceReadState.Valid, evidence)
                : new WorkspacePreparationApprovalEvidenceReadResult(WorkspacePreparationApprovalEvidenceReadState.Missing));

        public virtual Task<WorkspacePreparationApprovalEvidenceReadResult> GetForPlanAsync(Guid projectId, Guid workspaceId, Guid planId, CancellationToken cancellationToken = default) =>
            Task.FromResult(projectId == evidence.ProjectId && workspaceId == evidence.WorkspaceId && planId == evidence.PlanReference.PlanId
                ? new WorkspacePreparationApprovalEvidenceReadResult(WorkspacePreparationApprovalEvidenceReadState.Valid, evidence)
                : new WorkspacePreparationApprovalEvidenceReadResult(WorkspacePreparationApprovalEvidenceReadState.Missing));

        public virtual Task<WorkspacePreparationApprovalEvidenceReadResult> FindForPlanAsync(Guid projectId, Guid workspaceId, WorkspacePreparationPlanReference planReference, CancellationToken cancellationToken = default) =>
            GetForPlanAsync(projectId, workspaceId, planReference.PlanId, cancellationToken);

        public virtual Task<WorkspacePreparationApprovalEvidenceIndexWriteResult> EnsurePlanIndexAsync(WorkspacePreparationApprovalEvidence value, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorkspacePreparationApprovalEvidenceIndexWriteResult(WorkspacePreparationApprovalEvidenceIndexWriteStatus.AlreadyExists));
    }

    private sealed class CrashWindowApprovalRepository : ApprovalRepository
    {
        private readonly WorkspacePreparationApprovalEvidence _evidence;

        public CrashWindowApprovalRepository(WorkspacePreparationApprovalEvidence evidence) : base(evidence) => _evidence = evidence;

        public WorkspacePreparationApprovalEvidence? Evidence => _evidence;
        public bool IndexPresent { get; private set; }
        public int EnsureIndexCount { get; private set; }

        public override Task<WorkspacePreparationApprovalEvidenceReadResult> GetForPlanAsync(Guid projectId, Guid workspaceId, Guid planId, CancellationToken cancellationToken = default) =>
            Task.FromResult(IndexPresent ? new WorkspacePreparationApprovalEvidenceReadResult(WorkspacePreparationApprovalEvidenceReadState.Valid, _evidence)
                : new WorkspacePreparationApprovalEvidenceReadResult(WorkspacePreparationApprovalEvidenceReadState.Missing));

        public override Task<WorkspacePreparationApprovalEvidenceReadResult> FindForPlanAsync(Guid projectId, Guid workspaceId, WorkspacePreparationPlanReference planReference, CancellationToken cancellationToken = default) =>
            Task.FromResult(projectId == _evidence.ProjectId && workspaceId == _evidence.WorkspaceId && planReference.PlanId == _evidence.PlanReference.PlanId
                ? new WorkspacePreparationApprovalEvidenceReadResult(WorkspacePreparationApprovalEvidenceReadState.Valid, _evidence)
                : new WorkspacePreparationApprovalEvidenceReadResult(WorkspacePreparationApprovalEvidenceReadState.Missing));

        public override Task<WorkspacePreparationApprovalEvidenceIndexWriteResult> EnsurePlanIndexAsync(WorkspacePreparationApprovalEvidence value, CancellationToken cancellationToken = default)
        {
            EnsureIndexCount++;
            if (IndexPresent) return Task.FromResult(new WorkspacePreparationApprovalEvidenceIndexWriteResult(WorkspacePreparationApprovalEvidenceIndexWriteStatus.AlreadyExists));
            IndexPresent = true;
            return Task.FromResult(new WorkspacePreparationApprovalEvidenceIndexWriteResult(WorkspacePreparationApprovalEvidenceIndexWriteStatus.Created));
        }
    }

    private sealed class NoopLock : IRepositoryPreparationLock
    {
        public Task<IAsyncDisposable> AcquireAsync(string repositoryIdentity, CancellationToken cancellationToken = default) =>
            Task.FromResult<IAsyncDisposable>(new NoopAsyncDisposable());
    }

    private sealed class NoopAsyncDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class PathProvider(string path) : IManagedWorkspacePathProvider
    {
        public string GetWorkspacePath(Guid projectId, Guid workspaceId) => path;
        public bool IsSafeManagedWorkspacePath(Guid projectId, Guid workspaceId, out string resolved, out string? errorMessage)
        {
            resolved = path;
            errorMessage = null;
            return true;
        }
    }
}
