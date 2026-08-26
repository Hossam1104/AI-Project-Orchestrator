using System.Text.Json;
using System.Text.Json.Nodes;
using AIUsageMonitor.Application.Handoffs;
using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Projects;
using AIUsageMonitor.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageMonitor.Infrastructure.Tests;

public sealed class HandoffPackagePersistenceTests
{
    [Fact]
    public async Task PackageRoundTripsIntoProjectAndPackageGuidScopedPath()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var package = CreatePackage(ProjectA);

        var write = await repository.CreateAsync(package);
        var read = await repository.GetAsync(package.ProjectId, package.PackageId);

        Assert.Equal(HandoffPackageRepositoryWriteStatus.Created, write.Status);
        Assert.Equal(HandoffPackageReadState.Valid, read.State);
        Assert.Equal(package.ContentHash, read.Package!.ContentHash);
        Assert.Equal(package.NextAction, read.Package.NextAction);
        Assert.Equal(package.ExecutionScope!.IncludedScope[0].Statement, read.Package.ExecutionScope!.IncludedScope[0].Statement);
        Assert.Equal(
            store.Paths.GetHandoffPackageFile(package.ProjectId, package.PackageId),
            Path.Combine(
                store.Paths.ProjectsDirectory,
                ProjectA.ToString("D"),
                "handoffs",
                package.PackageId.ToString("D"),
                "package.json"));
        Assert.True(File.Exists(store.Paths.GetHandoffPackageFile(package.ProjectId, package.PackageId)));
    }

    [Fact]
    public async Task ExistingPackageCannotBeOverwrittenAndBytesRemainUnchanged()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var package = CreatePackage(ProjectA);
        var path = store.Paths.GetHandoffPackageFile(package.ProjectId, package.PackageId);

        Assert.Equal(HandoffPackageRepositoryWriteStatus.Created, (await repository.CreateAsync(package)).Status);
        var originalBytes = await File.ReadAllBytesAsync(path);

        var conflict = await repository.CreateAsync(package);

        Assert.Equal(HandoffPackageRepositoryWriteStatus.PackageConflict, conflict.Status);
        Assert.Equal(originalBytes, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    public async Task PackageReadIsProjectIsolated()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var package = CreatePackage(ProjectB);

        Assert.Equal(HandoffPackageRepositoryWriteStatus.Created, (await repository.CreateAsync(package)).Status);

        var readFromOtherProject = await repository.GetAsync(ProjectA, package.PackageId);

        Assert.Equal(HandoffPackageReadState.Missing, readFromOtherProject.State);
        Assert.False(File.Exists(store.Paths.GetHandoffPackageFile(ProjectA, package.PackageId)));
    }

    [Fact]
    public async Task TamperingWithAuthorityIsIntegrityFailureAndReadDoesNotRepairOrQuarantine()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var package = CreatePackage(ProjectA);
        var path = store.Paths.GetHandoffPackageFile(package.ProjectId, package.PackageId);
        await repository.CreateAsync(package);
        ReplacePayload(path, payload => payload["nextAction"] = "tampered action");
        var originalBytes = await File.ReadAllBytesAsync(path);

        var first = await repository.GetAsync(package.ProjectId, package.PackageId);
        var second = await repository.GetAsync(package.ProjectId, package.PackageId);

        Assert.Equal(HandoffPackageReadState.IntegrityFailure, first.State);
        Assert.Equal(HandoffPackageReadState.IntegrityFailure, second.State);
        Assert.Equal(originalBytes, await File.ReadAllBytesAsync(path));
        Assert.Empty(Directory.EnumerateFiles(Path.GetDirectoryName(path)!, "*.bak"));
    }

    [Fact]
    public Task TransitionTamperingIsIntegrityFailure() =>
        AssertTamperedPackageAsync(HandoffTransition.ExecutorToReviewer, payload => payload["transition"] = "reviewerToAcceptance");

    [Fact]
    public Task PlanningContractRevisionTamperingIsIntegrityFailure() =>
        AssertTamperedPackageAsync(
            HandoffTransition.ExecutorToReviewer,
            payload => payload["planningContractReference"]!["revision"] = 2);

    [Fact]
    public Task RepositoryTargetTamperingIsIntegrityFailure() =>
        AssertTamperedPackageAsync(
            HandoffTransition.ExecutorToReviewer,
            payload => payload["repositoryTarget"]!["expectedHeadCommit"] = new string('b', 40));

    [Fact]
    public Task FindingTamperingIsIntegrityFailure() =>
        AssertTamperedPackageAsync(
            HandoffTransition.ExecutorToReviewer,
            payload => payload["findingReferences"]![0]!["state"] = "addressed");

    [Fact]
    public Task EvidenceTamperingIsIntegrityFailure() =>
        AssertTamperedPackageAsync(
            HandoffTransition.ExecutorToReviewer,
            payload => payload["evidenceReferences"]![0]!["reference"] = "evidence:tampered");

    [Fact]
    public Task PredecessorTamperingIsIntegrityFailure() =>
        AssertTamperedPackageAsync(
            HandoffTransition.ExecutorToReviewer,
            payload => payload["previousPackageReference"]!["contentHash"] = new string('b', 64));

    [Fact]
    public Task PackageContentHashTamperingIsIntegrityFailure() =>
        AssertTamperedPackageAsync(
            HandoffTransition.ExecutorToReviewer,
            payload => payload["contentHash"] = new string('b', 64));

    [Fact]
    public Task SizeMetadataTamperingIsIntegrityFailure() =>
        AssertTamperedPackageAsync(
            HandoffTransition.ExecutorToReviewer,
            payload => payload["size"]!["canonicalPayloadBytes"] = 1);

    [Fact]
    public async Task MissingOrMalformedHashIsIntegrityFailure()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var package = CreatePackage(ProjectA);
        var path = store.Paths.GetHandoffPackageFile(package.ProjectId, package.PackageId);
        await repository.CreateAsync(package);

        ReplacePayload(path, payload => payload.Remove("contentHash"));
        var missing = await repository.GetAsync(package.ProjectId, package.PackageId);
        Assert.Equal(HandoffPackageReadState.IntegrityFailure, missing.State);

        ReplacePayload(path, payload => payload["contentHash"] = "not-a-sha256");
        var malformed = await repository.GetAsync(package.ProjectId, package.PackageId);
        Assert.Equal(HandoffPackageReadState.IntegrityFailure, malformed.State);
    }

    [Fact]
    public async Task FutureInnerSchemaIsTypedAndPreserved()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var package = CreatePackage(ProjectA);
        var path = store.Paths.GetHandoffPackageFile(package.ProjectId, package.PackageId);
        await repository.CreateAsync(package);
        ReplacePayload(path, payload => payload["schemaVersion"] = HandoffPackageSchema.CurrentVersion + 1);
        var originalBytes = await File.ReadAllBytesAsync(path);

        var read = await repository.GetAsync(package.ProjectId, package.PackageId);

        Assert.Equal(HandoffPackageReadState.UnsupportedFutureVersion, read.State);
        Assert.Equal(originalBytes, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    public async Task CancelledPackageCreationLeavesNoPartialAuthority()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var package = CreatePackage(ProjectA);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => repository.CreateAsync(package, cancellation.Token));

        Assert.False(File.Exists(store.Paths.GetHandoffPackageFile(package.ProjectId, package.PackageId)));
    }

    [Fact]
    public async Task ServicePersistsRedactedTextWithoutTheOriginalSecret()
    {
        using var store = new TemporaryStore();
        var project = new Project(
            ProjectA,
            "Redaction project",
            @"C:\APO-Test",
            null,
            ProjectStatus.Active,
            Now,
            Now);
        var contract = CreateRedactionContract(project.Id);
        var repository = CreateRepository(store);
        var service = new HandoffPackageService(
            new SingleProjectRepository(project),
            new SingleContractRepository(contract),
            new MissingGraphRepository(),
            repository,
            new HandoffRedactionService());

        var result = await service.CreateAsync(new HandoffPackageCreationRequest(
            project.Id,
            Guid.Parse("abababab-abab-abab-abab-abababababab"),
            HandoffTransition.PlannerToExecutor,
            contract.Reference,
            Now,
            nextAction: "Execute the bounded work."));
        var path = store.Paths.GetHandoffPackageFile(project.Id, result.Package!.PackageId);
        var persisted = await File.ReadAllTextAsync(path);

        Assert.Equal(HandoffPackageCreationStatus.Created, result.Status);
        Assert.Contains("[REDACTED]", persisted, StringComparison.Ordinal);
        Assert.DoesNotContain("super-secret", persisted, StringComparison.Ordinal);
        Assert.Equal(HandoffPackageReadState.Valid, (await repository.GetAsync(project.Id, result.Package.PackageId)).State);
    }

    private static JsonHandoffPackageRepository CreateRepository(TemporaryStore store) =>
        new(store.Paths, store.Files, NullLogger<JsonHandoffPackageRepository>.Instance);

    private static PlanningExecutionContract CreateRedactionContract(Guid projectId) => new(
        projectId,
        Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
        PlanningExecutionContractSchema.CurrentVersion,
        1,
        Now,
        "owner",
        Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
        new PlanningContextBinding(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), 1),
        new PlanningWorkItem(PlanningWorkItemSource.Jira, "APO-42", "Handoff packages"),
        new PlanningRepositoryTarget(PlanningRepositoryMode.None),
        [new PlanningScopeClause("include", "password=super-secret")],
        [],
        [new PlanningScopeClause("forbid", "model invocation")],
        [new PlanningDeliverable("package", "immutable package", true)],
        [new PlanningValidationRequirement("test", PlanningValidationKind.Test, "focused tests", true)],
        [new PlanningAcceptanceCriterion("accept", "package is valid", true)],
        [new PlanningExecutionBudget(PlanningBudgetKind.Attempts, 1)],
        [
            new PlanningStopCondition("stop-target", PlanningStopConditionKind.ImmutableTargetMoved, "target"),
            new PlanningStopCondition("stop-scope", PlanningStopConditionKind.ScopeViolation, "scope"),
            new PlanningStopCondition("stop-budget", PlanningStopConditionKind.BudgetExceeded, "budget")
        ],
        [],
        null,
        null);

    private static async Task AssertTamperedPackageAsync(
        HandoffTransition transition,
        Action<JsonObject> tamper)
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var package = CreatePackage(ProjectA, transition);
        var path = store.Paths.GetHandoffPackageFile(package.ProjectId, package.PackageId);
        await repository.CreateAsync(package);
        ReplacePayload(path, tamper);
        var originalBytes = await File.ReadAllBytesAsync(path);

        var first = await repository.GetAsync(package.ProjectId, package.PackageId);
        var second = await repository.GetAsync(package.ProjectId, package.PackageId);

        Assert.Equal(HandoffPackageReadState.IntegrityFailure, first.State);
        Assert.Equal(HandoffPackageReadState.IntegrityFailure, second.State);
        Assert.Equal(originalBytes, await File.ReadAllBytesAsync(path));
        Assert.Empty(Directory.EnumerateFiles(Path.GetDirectoryName(path)!, "*.bak"));
    }

    private static HandoffPackage CreatePackage(Guid projectId, HandoffTransition transition = HandoffTransition.PlannerToExecutor)
    {
        var contractReference = new PlanningExecutionContractReference(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            1,
            PlanningExecutionContractSchema.CurrentVersion,
            new string('a', 64));
        var workItem = new PlanningWorkItem(PlanningWorkItemSource.Jira, "APO-42", "Structured handoff packages");
        var context = new HandoffContextReference(Guid.Parse("33333333-3333-3333-3333-333333333333"), 1);
        var repositoryTarget = new PlanningRepositoryTarget(
            PlanningRepositoryMode.LocalGit,
            @"C:\APO-Test",
            "main",
            new string('c', 40));
        var graphReference = new WorkGraphReference(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            WorkGraphSchema.CurrentVersion,
            new string('b', 64));
        var graphNodeId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var executionScope = new HandoffExecutionScope(
            [new PlanningScopeClause("include", "structured handoff")],
            [new PlanningScopeClause("constraint", "bounded")],
            [new PlanningScopeClause("forbid", "model invocation")],
            [new PlanningDeliverable("package", "immutable package", true)],
            [new PlanningValidationRequirement("test", PlanningValidationKind.Test, "focused tests", true)],
            [new PlanningExecutionBudget(PlanningBudgetKind.Attempts, 1)],
            [
                new PlanningStopCondition("stop-target", PlanningStopConditionKind.ImmutableTargetMoved, "target"),
                new PlanningStopCondition("stop-scope", PlanningStopConditionKind.ScopeViolation, "scope"),
                new PlanningStopCondition("stop-budget", PlanningStopConditionKind.BudgetExceeded, "budget")
            ],
            ["governance:apo"],
            "routing:default",
            "safety:default");
        var reviewScope = new HandoffReviewScope(
            [new PlanningScopeClause("include", "structured handoff")],
            [new PlanningScopeClause("constraint", "bounded")],
            [new PlanningScopeClause("forbid", "model invocation")],
            [new PlanningAcceptanceCriterion("accept", "hash is deterministic", true)]);
        var evidence = new HandoffEvidenceReference(
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            HandoffEvidenceKind.Test,
            "evidence:test",
            new DateTimeOffset(2026, 8, 27, 12, 1, 0, TimeSpan.Zero),
            HandoffEvidenceFreshness.PointInTime,
            new string('d', 64));
        var finding = new HandoffFindingReference(
            "finding-1",
            HandoffFindingCategory.Correctness,
            HandoffFindingSeverity.High,
            HandoffFindingState.Unresolved,
            "bounded finding",
            "review:finding",
            [evidence.EvidenceId]);
        var artifact = new HandoffChangedArtifactReference("src/feature.cs", new string('e', 40), "artifact:1");
        var previous = new HandoffPackageReference(
            Guid.Parse("77777777-7777-7777-7777-777777777777"),
            HandoffPackageSchema.CurrentVersion,
            new string('f', 64));
        IReadOnlyList<HandoffEvidenceReference> evidenceReferences = transition == HandoffTransition.PlannerToExecutor ? [] : [evidence];
        IReadOnlyList<HandoffFindingReference> findingReferences = transition == HandoffTransition.PlannerToExecutor ? [] : [finding];
        IReadOnlyList<HandoffChangedArtifactReference> artifactReferences = transition == HandoffTransition.PlannerToExecutor ? [] : [artifact];
        IReadOnlyList<string> limitations = transition == HandoffTransition.PlannerToExecutor ? ["no transcript"] : ["one bounded limitation"];
        var outcome = transition == HandoffTransition.PlannerToExecutor
            ? null
            : new HandoffOutcomeMetadata(HandoffOutcomeState.Succeeded, "bounded outcome", "result:1");
        var packageId = transition == HandoffTransition.PlannerToExecutor
            ? Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            : Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var sourceRole = transition == HandoffTransition.PlannerToExecutor ? HandoffRole.Planner : HandoffRole.Executor;
        var targetRole = transition == HandoffTransition.PlannerToExecutor ? HandoffRole.Executor : HandoffRole.Reviewer;
        var scopeItemCount = transition == HandoffTransition.PlannerToExecutor ? 10 : 4;
        var provisionalSize = new HandoffPackageSizeMetadata(
            HandoffPackageLimits.MaxCanonicalPayloadBytes,
            0,
            evidenceReferences.Count,
            findingReferences.Count,
            artifactReferences.Count,
            limitations.Count,
            scopeItemCount);
        var provisional = new HandoffPackage(
            projectId,
            packageId,
            HandoffPackageSchema.CurrentVersion,
            new DateTimeOffset(2026, 8, 27, 12, 0, 0, TimeSpan.Zero),
            transition,
            sourceRole,
            targetRole,
            contractReference,
            workItem,
            context,
            repositoryTarget,
            graphReference,
            graphNodeId,
            transition == HandoffTransition.PlannerToExecutor ? null : previous,
            transition == HandoffTransition.PlannerToExecutor ? executionScope : null,
            transition == HandoffTransition.PlannerToExecutor ? null : reviewScope,
            null,
            null,
            evidenceReferences,
            findingReferences,
            artifactReferences,
            outcome,
            limitations,
            transition == HandoffTransition.PlannerToExecutor ? "Execute the bounded work." : "Review the bounded work.",
            new HandoffRedactionMetadata(false, 0, []),
            provisionalSize);
        var canonicalBytes = HandoffPackageIntegrity.ComputeCanonicalPayloadBytes(provisional);
        var finalSize = new HandoffPackageSizeMetadata(
            HandoffPackageLimits.MaxCanonicalPayloadBytes,
            canonicalBytes,
            evidenceReferences.Count,
            findingReferences.Count,
            artifactReferences.Count,
            limitations.Count,
            scopeItemCount);
        return new HandoffPackage(
            projectId,
            packageId,
            HandoffPackageSchema.CurrentVersion,
            new DateTimeOffset(2026, 8, 27, 12, 0, 0, TimeSpan.Zero),
            transition,
            sourceRole,
            targetRole,
            contractReference,
            workItem,
            context,
            repositoryTarget,
            graphReference,
            graphNodeId,
            transition == HandoffTransition.PlannerToExecutor ? null : previous,
            transition == HandoffTransition.PlannerToExecutor ? executionScope : null,
            transition == HandoffTransition.PlannerToExecutor ? null : reviewScope,
            null,
            null,
            evidenceReferences,
            findingReferences,
            artifactReferences,
            outcome,
            limitations,
            transition == HandoffTransition.PlannerToExecutor ? "Execute the bounded work." : "Review the bounded work.",
            new HandoffRedactionMetadata(false, 0, []),
            finalSize);
    }

    private static void ReplacePayload(string path, Action<JsonObject> update)
    {
        var root = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        update(root["payload"]!.AsObject());
        File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static readonly Guid ProjectA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProjectB = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly DateTimeOffset Now = new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

    private sealed class SingleProjectRepository(Project project) : IProjectRepository
    {
        public Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Project>>([project]);

        public Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Project?>(projectId == project.Id ? project : null);

        public Task UpsertAsync(Project value, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class SingleContractRepository(PlanningExecutionContract contract) : IPlanningExecutionContractRepository
    {
        public Task<PlanningContractRepositoryWriteResult> CreateAsync(PlanningExecutionContract value, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PlanningContractRepositoryWriteResult(PlanningContractRepositoryWriteStatus.Created));

        public Task<PlanningContractReadResult> GetAsync(Guid projectId, Guid contractId, int revision, CancellationToken cancellationToken = default) =>
            Task.FromResult(
                projectId == contract.ProjectId && contractId == contract.ContractId && revision == contract.Revision
                    ? new PlanningContractReadResult(PlanningContractReadState.Valid, contract)
                    : new PlanningContractReadResult(PlanningContractReadState.Missing));

        public Task<PlanningContractReadResult> GetLatestAsync(Guid projectId, Guid contractId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Latest lookup is not allowed.");

        public Task<PlanningContractRevisionListResult> ListRevisionsAsync(Guid projectId, Guid contractId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PlanningContractRevisionListResult(PlanningContractReadState.Valid, [contract]));
    }

    private sealed class MissingGraphRepository : IWorkGraphRepository
    {
        public Task<WorkGraphRepositoryWriteResult> CreateAsync(WorkGraph graph, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorkGraphRepositoryWriteResult(WorkGraphRepositoryWriteStatus.Created));

        public Task<WorkGraphReadResult> GetAsync(Guid projectId, Guid graphId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorkGraphReadResult(WorkGraphReadState.Missing));
    }
}
