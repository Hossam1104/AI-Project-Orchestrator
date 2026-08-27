using System.Text.Json;
using System.Text.Json.Nodes;
using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Handoffs;
using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Projects;
using AIUsageMonitor.Application.Time;
using AIUsageMonitor.Infrastructure.Persistence;
using AIUsageMonitor.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageMonitor.Infrastructure.Tests;

public sealed class RecoveryCheckpointPersistenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ReadyCheckpointRoundTripsThroughItsProjectAndCheckpointGuidPath()
    {
        using var harness = await RecoveryHarness.CreateAsync();

        var created = await harness.CreateAsync(RecoveryCheckpointLifecycleState.Ready);
        var read = await harness.Checkpoints.GetAsync(harness.Project.Id, created.Checkpoint!.CheckpointId);

        Assert.Equal(RecoveryCheckpointCreationStatus.Created, created.Status);
        Assert.Equal(RecoveryCheckpointReadState.Valid, read.State);
        Assert.Equal(created.Checkpoint.ContentHash, read.Checkpoint!.ContentHash);
        Assert.Equal(
            Path.Combine("projects", harness.Project.Id.ToString("D"), "continuation", "checkpoints", created.Checkpoint.CheckpointId.ToString("D"), "checkpoint.json"),
            Path.GetRelativePath(harness.Store.RootDirectory, harness.Store.Paths.GetRecoveryCheckpointFile(harness.Project.Id, created.Checkpoint.CheckpointId)));
    }

    [Fact]
    public async Task CheckpointIsCreateOnceAndOriginalBytesRemainUnchanged()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        var checkpoint = (await harness.CreateAsync(RecoveryCheckpointLifecycleState.Ready)).Checkpoint!;
        var path = harness.Store.Paths.GetRecoveryCheckpointFile(harness.Project.Id, checkpoint.CheckpointId);
        var original = await File.ReadAllBytesAsync(path);

        var conflict = await harness.Checkpoints.CreateAsync(checkpoint);

        Assert.Equal(RecoveryCheckpointRepositoryWriteStatus.CheckpointConflict, conflict.Status);
        Assert.Equal(original, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    public async Task CheckpointReadIsProjectIsolated()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        var checkpoint = (await harness.CreateAsync(RecoveryCheckpointLifecycleState.Ready)).Checkpoint!;

        var otherProjectRead = await harness.Checkpoints.GetAsync(Guid.NewGuid(), checkpoint.CheckpointId);

        Assert.Equal(RecoveryCheckpointReadState.Missing, otherProjectRead.State);
        Assert.False(File.Exists(harness.Store.Paths.GetRecoveryCheckpointFile(Guid.NewGuid(), checkpoint.CheckpointId)));
    }

    [Fact]
    public async Task TamperedCheckpointIsIntegrityFailureOnRepeatedObservationalReads()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        var checkpoint = (await harness.CreateAsync(RecoveryCheckpointLifecycleState.Ready)).Checkpoint!;
        var path = harness.Store.Paths.GetRecoveryCheckpointFile(harness.Project.Id, checkpoint.CheckpointId);
        TamperPayload(path, payload => payload["nextSafeAction"] = "replan");
        var tamperedBytes = await File.ReadAllBytesAsync(path);

        var first = await harness.Checkpoints.GetAsync(harness.Project.Id, checkpoint.CheckpointId);
        var second = await harness.Checkpoints.GetAsync(harness.Project.Id, checkpoint.CheckpointId);

        Assert.Equal(RecoveryCheckpointReadState.IntegrityFailure, first.State);
        Assert.Equal(RecoveryCheckpointReadState.IntegrityFailure, second.State);
        Assert.Equal(tamperedBytes, await File.ReadAllBytesAsync(path));
        Assert.Empty(Directory.EnumerateFiles(harness.Store.RootDirectory, "*.bak", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task HeadGenerationsAlternateAThenBThenA()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        var first = (await harness.CreateAsync(RecoveryCheckpointLifecycleState.Ready)).Checkpoint!;
        var second = (await harness.CreateAsync(RecoveryCheckpointLifecycleState.Waiting, first.Reference)).Checkpoint!;
        var third = (await harness.CreateAsync(RecoveryCheckpointLifecycleState.Blocked, second.Reference)).Checkpoint!;

        var head = await harness.Heads.GetAsync(harness.Project.Id);

        Assert.Equal(ContinuationHeadReadState.Valid, head.State);
        Assert.Equal(3, head.Head!.Generation);
        Assert.Equal(third.Reference.ContentHash, head.Head.LatestCheckpointReference.ContentHash);
        Assert.Equal(third.Reference.ContentHash, head.Head.LastSafeCheckpointReference!.ContentHash);
        Assert.True(File.Exists(harness.Store.Paths.GetProjectContinuationHeadSlotAFile(harness.Project.Id)));
        Assert.True(File.Exists(harness.Store.Paths.GetProjectContinuationHeadSlotBFile(harness.Project.Id)));
        Assert.Equal(2, Directory.EnumerateFiles(harness.Store.Paths.GetProjectContinuationDirectory(harness.Project.Id), "head-*.json").Count());
    }

    [Fact]
    public async Task CorruptNewestHeadFallsBackToPreviousValidGenerationWithoutRepair()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        var first = (await harness.CreateAsync(RecoveryCheckpointLifecycleState.Ready)).Checkpoint!;
        _ = await harness.CreateAsync(RecoveryCheckpointLifecycleState.Waiting, first.Reference);
        var newestPath = harness.Store.Paths.GetProjectContinuationHeadSlotBFile(harness.Project.Id);
        await File.WriteAllTextAsync(newestPath, "{ invalid head json");
        var corruptedBytes = await File.ReadAllBytesAsync(newestPath);

        var head = await harness.Heads.GetAsync(harness.Project.Id);
        var result = await harness.Resolver.ResolveAsync(harness.Project.Id);

        Assert.Equal(ContinuationHeadReadState.Valid, head.State);
        Assert.True(head.FallbackToPreviousGeneration);
        Assert.Equal(1, head.Head!.Generation);
        Assert.Equal(SmartContinueResolutionState.Resumable, result.ResolutionState);
        Assert.Equal(first.CheckpointId, result.SelectedCheckpointReference!.CheckpointId);
        Assert.True(result.FallbackToLastKnownGood);
        Assert.Equal(corruptedBytes, await File.ReadAllBytesAsync(newestPath));
        Assert.Empty(Directory.EnumerateFiles(harness.Store.RootDirectory, "*.bak", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task BothCorruptHeadSlotsFailClosed()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        var first = (await harness.CreateAsync(RecoveryCheckpointLifecycleState.Ready)).Checkpoint!;
        _ = await harness.CreateAsync(RecoveryCheckpointLifecycleState.Waiting, first.Reference);
        var a = harness.Store.Paths.GetProjectContinuationHeadSlotAFile(harness.Project.Id);
        var b = harness.Store.Paths.GetProjectContinuationHeadSlotBFile(harness.Project.Id);
        await File.WriteAllTextAsync(a, "{ invalid a");
        await File.WriteAllTextAsync(b, "{ invalid b");

        var head = await harness.Heads.GetAsync(harness.Project.Id);
        var result = await harness.Resolver.ResolveAsync(harness.Project.Id);

        Assert.Equal(ContinuationHeadReadState.IntegrityFailure, head.State);
        Assert.Equal(SmartContinueResolutionState.IntegrityFailure, result.ResolutionState);
    }

    [Fact]
    public async Task MissingHeadIsNotReconstructedByScanningCheckpointDirectories()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        var checkpoint = (await harness.CreateAsync(RecoveryCheckpointLifecycleState.Ready)).Checkpoint!;
        File.Delete(harness.Store.Paths.GetProjectContinuationHeadSlotAFile(harness.Project.Id));

        var result = await harness.Resolver.ResolveAsync(harness.Project.Id);

        Assert.Equal(SmartContinueResolutionState.CheckpointMissing, result.ResolutionState);
        Assert.Equal(checkpoint.CheckpointId, checkpoint.Reference.CheckpointId);
    }

    [Fact]
    public async Task ReadyResolvesToResumableContinueFromCheckpoint()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        var checkpoint = (await harness.CreateAsync(RecoveryCheckpointLifecycleState.Ready)).Checkpoint!;

        var result = await harness.Resolver.ResolveAsync(harness.Project.Id);

        Assert.Equal(SmartContinueResolutionState.Resumable, result.ResolutionState);
        Assert.Equal(RecoveryNextSafeAction.ContinueFromCheckpoint, result.NextSafeAction);
        Assert.Equal(checkpoint.CheckpointId, result.SelectedCheckpointReference!.CheckpointId);
    }

    [Theory]
    [InlineData(RecoveryCheckpointLifecycleState.Waiting)]
    [InlineData(RecoveryCheckpointLifecycleState.Blocked)]
    public async Task WaitingAndBlockedLatestStatesDoNotBypassTheCanonicalCheckpoint(RecoveryCheckpointLifecycleState state)
    {
        using var harness = await RecoveryHarness.CreateAsync();
        var first = (await harness.CreateAsync(RecoveryCheckpointLifecycleState.Ready)).Checkpoint!;
        var latest = (await harness.CreateAsync(state, first.Reference, explanation: "owner action is required")).Checkpoint!;

        var result = await harness.Resolver.ResolveAsync(harness.Project.Id);

        Assert.Equal(SmartContinueResolutionState.Blocked, result.ResolutionState);
        Assert.Equal(latest.CheckpointId, result.SelectedCheckpointReference!.CheckpointId);
        Assert.Equal(latest.Explanation, result.Explanation);
        Assert.False(result.FallbackToLastKnownGood);
    }

    [Fact]
    public async Task ApprovalRequiredLifecycleReturnsRequestApproval()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        _ = await harness.CreateAsync(RecoveryCheckpointLifecycleState.ApprovalRequired);

        var result = await harness.Resolver.ResolveAsync(harness.Project.Id);

        Assert.Equal(SmartContinueResolutionState.ApprovalRequired, result.ResolutionState);
        Assert.Equal(RecoveryNextSafeAction.RequestApproval, result.NextSafeAction);
    }

    [Fact]
    public async Task PendingApprovalGateReturnsApprovalRequired()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        var approvalEvidence = Evidence(RecoveryEvidenceKind.Approval, RecoveryEvidenceFreshness.Verified);
        _ = await harness.CreateAsync(
            RecoveryCheckpointLifecycleState.Ready,
            evidence: [approvalEvidence],
            gates: [new RecoveryGateSnapshot(RecoveryGateKind.Approval, RecoveryGateState.Pending)]);

        var result = await harness.Resolver.ResolveAsync(harness.Project.Id);

        Assert.Equal(SmartContinueResolutionState.ApprovalRequired, result.ResolutionState);
        Assert.Equal(RecoveryGateState.Pending, result.RequiredGateState);
        Assert.Equal(RecoveryNextSafeAction.RequestApproval, result.NextSafeAction);
    }

    [Fact]
    public async Task CompletedLatestStateIsTerminalAndDoesNotFallback()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        var first = (await harness.CreateAsync(RecoveryCheckpointLifecycleState.Ready)).Checkpoint!;
        var completed = (await harness.CreateAsync(RecoveryCheckpointLifecycleState.Completed, first.Reference)).Checkpoint!;

        var result = await harness.Resolver.ResolveAsync(harness.Project.Id);

        Assert.Equal(SmartContinueResolutionState.Completed, result.ResolutionState);
        Assert.Equal(RecoveryNextSafeAction.NoActionCompleted, result.NextSafeAction);
        Assert.Equal(completed.CheckpointId, result.SelectedCheckpointReference!.CheckpointId);
        Assert.False(result.FallbackToLastKnownGood);
    }

    [Theory]
    [InlineData(RecoveryCheckpointLifecycleState.Interrupted)]
    [InlineData(RecoveryCheckpointLifecycleState.Failed)]
    public async Task InterruptedAndFailedStatesRecoverOnlyFromLastSafe(RecoveryCheckpointLifecycleState state)
    {
        using var harness = await RecoveryHarness.CreateAsync();
        var first = (await harness.CreateAsync(RecoveryCheckpointLifecycleState.Ready)).Checkpoint!;
        var latest = (await harness.CreateAsync(state, first.Reference, explanation: "latest execution did not complete")).Checkpoint!;

        var result = await harness.Resolver.ResolveAsync(harness.Project.Id);

        Assert.Equal(SmartContinueResolutionState.Resumable, result.ResolutionState);
        Assert.Equal(first.CheckpointId, result.SelectedCheckpointReference!.CheckpointId);
        Assert.Equal(latest.LifecycleState, result.LatestLifecycleState);
        Assert.True(result.FallbackToLastKnownGood);
        Assert.Contains(state.ToString(), result.Explanation, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(RecoveryNextSafeAction.ContinueFromCheckpoint, result.NextSafeAction);
    }

    [Fact]
    public async Task CancelledStateDoesNotSilentlyRestartLastSafeWork()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        var first = (await harness.CreateAsync(RecoveryCheckpointLifecycleState.Ready)).Checkpoint!;
        var latest = (await harness.CreateAsync(RecoveryCheckpointLifecycleState.Cancelled, first.Reference)).Checkpoint!;

        var result = await harness.Resolver.ResolveAsync(harness.Project.Id);

        Assert.Equal(SmartContinueResolutionState.Blocked, result.ResolutionState);
        Assert.Equal(first.CheckpointId, result.SelectedCheckpointReference!.CheckpointId);
        Assert.Equal(latest.LifecycleState, result.LatestLifecycleState);
        Assert.True(result.FallbackToLastKnownGood);
        Assert.Equal(RecoveryNextSafeAction.ResolveBlocker, result.NextSafeAction);
        Assert.Contains("cancelled", result.Explanation, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(RecoveryEvidenceKind.Repository, RecoveryNextSafeAction.RefreshRepositoryEvidence)]
    [InlineData(RecoveryEvidenceKind.Tracker, RecoveryNextSafeAction.RefreshTrackerEvidence)]
    [InlineData(RecoveryEvidenceKind.Routing, RecoveryNextSafeAction.RefreshRoutingEvidence)]
    public async Task StaleMutableEvidencePreventsResumable(
        RecoveryEvidenceKind kind,
        RecoveryNextSafeAction expectedAction)
    {
        using var harness = await RecoveryHarness.CreateAsync();
        _ = await harness.CreateAsync(
            RecoveryCheckpointLifecycleState.Ready,
            evidence: [Evidence(kind, RecoveryEvidenceFreshness.PointInTime)]);

        var result = await harness.Resolver.ResolveAsync(harness.Project.Id);

        Assert.Equal(SmartContinueResolutionState.Stale, result.ResolutionState);
        Assert.Equal(expectedAction, result.NextSafeAction);
        Assert.Single(result.StaleEvidence);
    }

    [Fact]
    public async Task ExpiredVerifiedRepositoryEvidenceIsStale()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        _ = await harness.CreateAsync(
            RecoveryCheckpointLifecycleState.Ready,
            evidence: [Evidence(RecoveryEvidenceKind.Repository, RecoveryEvidenceFreshness.Verified, Now.AddHours(-1))]);

        var result = await harness.Resolver.ResolveAsync(harness.Project.Id);

        Assert.Equal(SmartContinueResolutionState.Stale, result.ResolutionState);
        Assert.Equal(RecoveryNextSafeAction.RefreshRepositoryEvidence, result.NextSafeAction);
    }

    [Fact]
    public async Task CurrentContextMismatchReturnsContextInsufficient()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        _ = await harness.CreateAsync(RecoveryCheckpointLifecycleState.Ready);
        var incompatible = CreateContext(harness.Project.Id, Guid.NewGuid(), Now.AddMinutes(1));
        await harness.Contexts.UpsertAsync(incompatible);

        var result = await harness.Resolver.ResolveAsync(harness.Project.Id);

        Assert.Equal(SmartContinueResolutionState.ContextInsufficient, result.ResolutionState);
        Assert.Contains("context", result.Explanation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExactContractTamperingPreventsContinuationWithoutLatestFallback()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        _ = await harness.CreateAsync(RecoveryCheckpointLifecycleState.Ready);
        var contractPath = harness.Store.Paths.GetPlanningExecutionContractRevisionFile(
            harness.Project.Id,
            harness.Contract.ContractId,
            harness.Contract.Revision);
        TamperPayload(contractPath, payload => payload["ownerReference"] = "tampered");

        var result = await harness.Resolver.ResolveAsync(harness.Project.Id);

        Assert.Equal(SmartContinueResolutionState.ContextInsufficient, result.ResolutionState);
        Assert.Contains("contract", result.Explanation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MissingOptionalGraphAndHandoffAreExplicitCreationFailures()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        var graphReference = new WorkGraphReference(Guid.NewGuid(), WorkGraphSchema.CurrentVersion, new string('a', 64));
        var graphResult = await harness.CreateAsync(
            RecoveryCheckpointLifecycleState.Ready,
            workGraphReference: graphReference,
            workGraphNodeId: Guid.NewGuid());
        Assert.Equal(RecoveryCheckpointCreationStatus.GraphMissing, graphResult.Status);

        var handoffReference = new HandoffPackageReference(Guid.NewGuid(), HandoffPackageSchema.CurrentVersion, new string('b', 64));
        var handoffResult = await harness.CreateAsync(
            RecoveryCheckpointLifecycleState.Ready,
            handoffPackageReference: handoffReference);
        Assert.Equal(RecoveryCheckpointCreationStatus.HandoffMissing, handoffResult.Status);
    }

    [Fact]
    public async Task ValidExactWorkGraphBindingIsRetainedAndResolved()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        var nodeId = Guid.NewGuid();
        var graph = new WorkGraph(
            harness.Project.Id,
            Guid.NewGuid(),
            WorkGraphSchema.CurrentVersion,
            Now,
            [new WorkGraphNode(nodeId, harness.Contract.Reference)],
            []);
        Assert.Equal(WorkGraphRepositoryWriteStatus.Created, (await harness.Graphs.CreateAsync(graph)).Status);

        var created = await harness.CreateAsync(
            RecoveryCheckpointLifecycleState.Ready,
            workGraphReference: graph.Reference,
            workGraphNodeId: nodeId);
        var result = await harness.Resolver.ResolveAsync(harness.Project.Id);

        Assert.Equal(RecoveryCheckpointCreationStatus.Created, created.Status);
        Assert.Equal(graph.Reference.ContentHash, created.Checkpoint!.WorkGraphReference!.ContentHash);
        Assert.Equal(SmartContinueResolutionState.Resumable, result.ResolutionState);
    }

    [Fact]
    public async Task TamperedExactWorkGraphPreventsContinuation()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        var nodeId = Guid.NewGuid();
        var graph = new WorkGraph(
            harness.Project.Id,
            Guid.NewGuid(),
            WorkGraphSchema.CurrentVersion,
            Now,
            [new WorkGraphNode(nodeId, harness.Contract.Reference)],
            []);
        await harness.Graphs.CreateAsync(graph);
        _ = await harness.CreateAsync(RecoveryCheckpointLifecycleState.Ready, workGraphReference: graph.Reference, workGraphNodeId: nodeId);
        var graphPath = harness.Store.Paths.GetWorkGraphFile(harness.Project.Id, graph.GraphId);
        TamperPayload(graphPath, payload => payload["contentHash"] = new string('f', 64));

        var result = await harness.Resolver.ResolveAsync(harness.Project.Id);

        Assert.Equal(SmartContinueResolutionState.ContextInsufficient, result.ResolutionState);
        Assert.Contains("work graph", result.Explanation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidExactHandoffBindingIsRetainedAndResolved()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        var nodeId = Guid.NewGuid();
        var graph = new WorkGraph(
            harness.Project.Id,
            Guid.NewGuid(),
            WorkGraphSchema.CurrentVersion,
            Now,
            [new WorkGraphNode(nodeId, harness.Contract.Reference)],
            []);
        await harness.Graphs.CreateAsync(graph);
        var handoff = CreateHandoff(harness, graph, nodeId);
        Assert.Equal(HandoffPackageRepositoryWriteStatus.Created, (await harness.Handoffs.CreateAsync(handoff)).Status);

        var created = await harness.CreateAsync(
            RecoveryCheckpointLifecycleState.Ready,
            workGraphReference: graph.Reference,
            workGraphNodeId: nodeId,
            handoffPackageReference: handoff.Reference);
        var result = await harness.Resolver.ResolveAsync(harness.Project.Id);

        Assert.Equal(RecoveryCheckpointCreationStatus.Created, created.Status);
        Assert.Equal(handoff.Reference.ContentHash, created.Checkpoint!.HandoffPackageReference!.ContentHash);
        Assert.Equal(SmartContinueResolutionState.Resumable, result.ResolutionState);
    }

    [Fact]
    public async Task DescriptiveTextIsRedactedButAuthorityTextIsRejected()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        var descriptive = await harness.CreateAsync(
            RecoveryCheckpointLifecycleState.Waiting,
            explanation: "waiting because password=super-secret-value was supplied");
        var persisted = await File.ReadAllTextAsync(harness.Store.Paths.GetRecoveryCheckpointFile(
            harness.Project.Id,
            descriptive.Checkpoint!.CheckpointId));
        Assert.Contains("[REDACTED]", persisted, StringComparison.Ordinal);
        Assert.DoesNotContain("super-secret-value", persisted, StringComparison.Ordinal);

        var rejectedId = Guid.NewGuid();
        var rejected = await harness.CreateAsync(
            RecoveryCheckpointLifecycleState.Ready,
            checkpointId: rejectedId,
            previous: descriptive.Checkpoint.Reference,
            evidence: [new RecoveryEvidenceReference(
                Guid.NewGuid(),
                RecoveryEvidenceKind.Repository,
                "api_key=identity-secret-value",
                Now,
                RecoveryEvidenceFreshness.Verified)]);

        Assert.Equal(RecoveryCheckpointCreationStatus.RedactionRejected, rejected.Status);
        Assert.False(File.Exists(harness.Store.Paths.GetRecoveryCheckpointFile(harness.Project.Id, rejectedId)));
    }

    [Fact]
    public async Task ForeignPredecessorIsRejectedInTheRequestedProjectScope()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        var checkpoint = (await harness.CreateAsync(RecoveryCheckpointLifecycleState.Ready)).Checkpoint!;
        var foreignProject = Guid.NewGuid();
        var result = await harness.CreateAsync(
            RecoveryCheckpointLifecycleState.Ready,
            projectId: foreignProject,
            previous: checkpoint.Reference);

        Assert.Equal(RecoveryCheckpointCreationStatus.ProjectNotFound, result.Status);
    }

    [Fact]
    public async Task CancellationPropagatesWithoutCreatingCheckpointOrHead()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => harness.CreateAsync(
            RecoveryCheckpointLifecycleState.Ready,
            cancellationToken: cancellation.Token));

        Assert.Empty(Directory.EnumerateFiles(
            harness.Store.Paths.GetProjectContinuationDirectory(harness.Project.Id),
            "*.json",
            SearchOption.AllDirectories));
    }

    [Fact]
    public async Task ResolverRestartReadsTheSameRecoveryAuthorityFromDisk()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        var checkpoint = (await harness.CreateAsync(RecoveryCheckpointLifecycleState.Ready)).Checkpoint!;
        var fresh = harness.CreateFreshResolver();

        var result = await fresh.ResolveAsync(harness.Project.Id);

        Assert.Equal(SmartContinueResolutionState.Resumable, result.ResolutionState);
        Assert.Equal(checkpoint.Reference.ToString(), result.SelectedCheckpointReference!.ToString());
        Assert.Equal(RecoveryNextSafeAction.ContinueFromCheckpoint, result.NextSafeAction);
    }

    [Fact]
    public async Task InvalidCreationRequestReturnsTypedStatus()
    {
        using var harness = await RecoveryHarness.CreateAsync();
        var result = await harness.CreateAsync(RecoveryCheckpointLifecycleState.Ready, projectId: Guid.Empty);

        Assert.Equal(RecoveryCheckpointCreationStatus.InvalidRequest, result.Status);
    }

    private static RecoveryEvidenceReference Evidence(
        RecoveryEvidenceKind kind,
        RecoveryEvidenceFreshness freshness,
        DateTimeOffset? validUntil = null) => new(
        Guid.NewGuid(),
        kind,
        $"evidence:{kind.ToString().ToLowerInvariant()}",
        validUntil is null ? Now.AddMinutes(-1) : validUntil.Value.AddMinutes(-1),
        freshness,
        validUntil);

    private static void TamperPayload(string path, Action<JsonObject> tamper)
    {
        var root = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        tamper(root["payload"]!.AsObject());
        File.WriteAllText(path, root.ToJsonString(JsonFileStore.SerializerOptions));
    }

    private static ProjectContextReference CreateContext(
        Guid projectId,
        Guid contextId,
        DateTimeOffset updatedAt) => new(
        projectId,
        contextId,
        ProjectContextContract.CurrentVersion,
        Now,
        updatedAt,
        ProjectRepositoryContextReference.Skipped(projectId, @"C:\APO-Test"),
        new ProjectTrackerContextReference(TrackerReferenceState.Skipped),
        [],
        new ProjectCurrentWorkReference(CurrentWorkState.NotSelected),
        [],
        null,
        null,
        ProjectNextSafeAction.ReadyForPlanning);

    private sealed class RecoveryHarness : IDisposable
    {
        private RecoveryHarness(TemporaryStore store, Project project, ProjectContextReference context, PlanningExecutionContract contract)
        {
            Store = store;
            Project = project;
            Context = context;
            Contract = contract;
            Projects = new JsonProjectRepository(Store.Paths, Store.Files, NullLogger<JsonProjectRepository>.Instance);
            Contexts = new JsonProjectContextReferenceRepository(Store.Paths, Store.Files, NullLogger<JsonProjectContextReferenceRepository>.Instance);
            Contracts = new JsonPlanningExecutionContractRepository(Store.Paths, Store.Files, NullLogger<JsonPlanningExecutionContractRepository>.Instance);
            Graphs = new JsonWorkGraphRepository(Store.Paths, Store.Files, NullLogger<JsonWorkGraphRepository>.Instance);
            Handoffs = new JsonHandoffPackageRepository(Store.Paths, Store.Files, NullLogger<JsonHandoffPackageRepository>.Instance);
            Checkpoints = new JsonRecoveryCheckpointRepository(Store.Paths, Store.Files, NullLogger<JsonRecoveryCheckpointRepository>.Instance);
            Heads = new JsonContinuationHeadRepository(Store.Paths, Store.Files, NullLogger<JsonContinuationHeadRepository>.Instance);
            Clock = new FixedClock(Now);
            Service = CreateService();
            Resolver = CreateResolver();
        }

        public TemporaryStore Store { get; }
        public Project Project { get; }
        public ProjectContextReference Context { get; }
        public PlanningExecutionContract Contract { get; }
        public JsonProjectRepository Projects { get; }
        public JsonProjectContextReferenceRepository Contexts { get; }
        public JsonPlanningExecutionContractRepository Contracts { get; }
        public JsonWorkGraphRepository Graphs { get; }
        public JsonHandoffPackageRepository Handoffs { get; }
        public JsonRecoveryCheckpointRepository Checkpoints { get; }
        public JsonContinuationHeadRepository Heads { get; }
        public FixedClock Clock { get; }
        public RecoveryCheckpointService Service { get; }
        public SmartContinueResolver Resolver { get; }

        public static async Task<RecoveryHarness> CreateAsync()
        {
            var store = new TemporaryStore();
            var projectId = Guid.NewGuid();
            var contextId = Guid.NewGuid();
            var context = CreateContext(projectId, contextId, Now);
            var project = new Project(projectId, "Recovery project", @"C:\APO-Test", null, ProjectStatus.Active, Now, Now);
            var contract = CreateContract(projectId, contextId);
            var harness = new RecoveryHarness(store, project, context, contract);
            try
            {
                await harness.Projects.UpsertAsync(project);
                await harness.Contexts.UpsertAsync(context);
                var contractWrite = await harness.Contracts.CreateAsync(contract);
                Assert.Equal(PlanningContractRepositoryWriteStatus.Created, contractWrite.Status);
                return harness;
            }
            catch
            {
                harness.Dispose();
                throw;
            }
        }

        public async Task<RecoveryCheckpointCreationResult> CreateAsync(
            RecoveryCheckpointLifecycleState state,
            RecoveryCheckpointReference? previous = null,
            string? explanation = null,
            IReadOnlyList<RecoveryEvidenceReference>? evidence = null,
            IReadOnlyList<RecoveryGateSnapshot>? gates = null,
            Guid? checkpointId = null,
            Guid? projectId = null,
            WorkGraphReference? workGraphReference = null,
            Guid? workGraphNodeId = null,
            HandoffPackageReference? handoffPackageReference = null,
            CancellationToken cancellationToken = default) => await Service.CreateAsync(new RecoveryCheckpointCreationRequest(
                projectId ?? Project.Id,
                checkpointId ?? Guid.NewGuid(),
                state,
                Contract.Reference,
                evidenceReferences: evidence,
                gateSnapshots: gates,
                explanation: explanation,
                createdAt: Now,
                workGraphReference: workGraphReference,
                workGraphNodeId: workGraphNodeId,
                handoffPackageReference: handoffPackageReference,
                previousCheckpointReference: previous), cancellationToken);

        public SmartContinueResolver CreateFreshResolver() => new(
            new JsonProjectRepository(Store.Paths, Store.Files, NullLogger<JsonProjectRepository>.Instance),
            new JsonProjectContextReferenceRepository(Store.Paths, Store.Files, NullLogger<JsonProjectContextReferenceRepository>.Instance),
            new JsonPlanningExecutionContractRepository(Store.Paths, Store.Files, NullLogger<JsonPlanningExecutionContractRepository>.Instance),
            new JsonWorkGraphRepository(Store.Paths, Store.Files, NullLogger<JsonWorkGraphRepository>.Instance),
            new JsonHandoffPackageRepository(Store.Paths, Store.Files, NullLogger<JsonHandoffPackageRepository>.Instance),
            new JsonRecoveryCheckpointRepository(Store.Paths, Store.Files, NullLogger<JsonRecoveryCheckpointRepository>.Instance),
            new JsonContinuationHeadRepository(Store.Paths, Store.Files, NullLogger<JsonContinuationHeadRepository>.Instance),
            Clock);

        private RecoveryCheckpointService CreateService() => new(
            Projects,
            Contexts,
            Contracts,
            Graphs,
            Handoffs,
            Checkpoints,
            Heads,
            new HandoffRedactionService(),
            Clock);

        private SmartContinueResolver CreateResolver() => new(
            Projects,
            Contexts,
            Contracts,
            Graphs,
            Handoffs,
            Checkpoints,
            Heads,
            Clock);

        public void Dispose() => Store.Dispose();
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = now;
    }

    private static PlanningExecutionContract CreateContract(Guid projectId, Guid contextId) => new(
        projectId,
        Guid.NewGuid(),
        PlanningExecutionContractSchema.CurrentVersion,
        1,
        Now,
        "APO-43 owner",
        Guid.NewGuid(),
        new PlanningContextBinding(contextId, ProjectContextContract.CurrentVersion),
        new PlanningWorkItem(PlanningWorkItemSource.Jira, "APO-43", "Smart Continue recovery checkpoints"),
        new PlanningRepositoryTarget(PlanningRepositoryMode.None),
        [new PlanningScopeClause("include", "checkpoint authority")],
        [new PlanningScopeClause("constraint", "read-only Smart Continue resolution")],
        [new PlanningScopeClause("forbid", "model routing")],
        [new PlanningDeliverable("checkpoint", "immutable recovery checkpoint", true)],
        [new PlanningValidationRequirement("tests", PlanningValidationKind.Test, "focused recovery tests", true)],
        [new PlanningAcceptanceCriterion("integrity", "authority is integrity checked", true)],
        [new PlanningExecutionBudget(PlanningBudgetKind.Attempts, 1)],
        [
            new PlanningStopCondition("target", PlanningStopConditionKind.ImmutableTargetMoved, "target moved"),
            new PlanningStopCondition("scope", PlanningStopConditionKind.ScopeViolation, "scope changed"),
            new PlanningStopCondition("budget", PlanningStopConditionKind.BudgetExceeded, "budget exceeded")
        ],
        ["APO-43"],
        null,
        null);

    private static HandoffPackage CreateHandoff(RecoveryHarness harness, WorkGraph graph, Guid nodeId)
    {
        var scope = new HandoffExecutionScope(
            [new PlanningScopeClause("include", "recovery checkpoint")],
            [new PlanningScopeClause("constraint", "bounded authority")],
            [new PlanningScopeClause("forbid", "model invocation")],
            [new PlanningDeliverable("checkpoint", "immutable checkpoint", true)],
            [new PlanningValidationRequirement("tests", PlanningValidationKind.Test, "focused tests", true)],
            [new PlanningExecutionBudget(PlanningBudgetKind.Attempts, 1)],
            [
                new PlanningStopCondition("target", PlanningStopConditionKind.ImmutableTargetMoved, "target moved"),
                new PlanningStopCondition("scope", PlanningStopConditionKind.ScopeViolation, "scope changed"),
                new PlanningStopCondition("budget", PlanningStopConditionKind.BudgetExceeded, "budget exceeded")
            ],
            [],
            null,
            null);
        const int scopeItemCount = 9;
        var provisional = new HandoffPackage(
            harness.Project.Id,
            Guid.NewGuid(),
            HandoffPackageSchema.CurrentVersion,
            Now,
            HandoffTransition.PlannerToExecutor,
            HandoffRole.Planner,
            HandoffRole.Executor,
            harness.Contract.Reference,
            new PlanningWorkItem(PlanningWorkItemSource.Jira, "APO-43", "Recovery checkpoint"),
            new HandoffContextReference(harness.Context.ContextId, harness.Context.ContractVersion, Now, Now),
            new PlanningRepositoryTarget(PlanningRepositoryMode.None),
            graph.Reference,
            nodeId,
            null,
            scope,
            null,
            null,
            null,
            [],
            [],
            [],
            null,
            [],
            "Continue the bounded work.",
            new HandoffRedactionMetadata(false, 0, []),
            new HandoffPackageSizeMetadata(HandoffPackageLimits.MaxCanonicalPayloadBytes, 0, 0, 0, 0, 0, scopeItemCount));
        var finalSize = new HandoffPackageSizeMetadata(
            HandoffPackageLimits.MaxCanonicalPayloadBytes,
            HandoffPackageIntegrity.ComputeCanonicalPayloadBytes(provisional),
            0,
            0,
            0,
            0,
            scopeItemCount);
        return new HandoffPackage(
            provisional.ProjectId,
            provisional.PackageId,
            provisional.SchemaVersion,
            provisional.CreatedAt,
            provisional.Transition,
            provisional.SourceRole,
            provisional.TargetRole,
            provisional.PlanningContractReference,
            provisional.WorkItem,
            provisional.Context,
            provisional.RepositoryTarget,
            provisional.WorkGraphReference,
            provisional.WorkGraphNodeId,
            provisional.PreviousPackageReference,
            provisional.ExecutionScope,
            provisional.ReviewScope,
            provisional.RemediationScope,
            provisional.AcceptanceScope,
            provisional.EvidenceReferences,
            provisional.FindingReferences,
            provisional.ChangedArtifactReferences,
            provisional.Outcome,
            provisional.Limitations,
            provisional.NextAction,
            provisional.Redaction,
            finalSize);
    }
}
