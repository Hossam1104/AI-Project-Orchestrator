using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Handoffs;
using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Projects;
using AIUsageMonitor.Application.RemoteEvidence;
using AIUsageMonitor.Application.Routing;
using AIUsageMonitor.Application.Time;
using AIUsageMonitor.Application.Validation;
using AIUsageMonitor.Application.Workspaces;
using AIUsageMonitor.Infrastructure.Execution;
using AIUsageMonitor.Infrastructure.Persistence;
using AIUsageMonitor.Infrastructure.Persistence.Repositories;
using AIUsageMonitor.Infrastructure.Validation;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageMonitor.Infrastructure.Tests;

public sealed class ValidationApo48RemediationTests
{
    [Fact]
    public async Task RemoteCiExplicitPullRequestBindsToPullRequestHeadInsteadOfBranch()
    {
        var requirement = RemoteRequirement(pullRequestNumber: 42, expectedCommit: "B");
        var fixture = CreateFixture(requirement);
        var collector = new RemoteValidationEvidenceCollector(new FakeRemoteService(Remote("B", "B", RemoteCiState.Passing, ["B"])));

        var evidence = await collector.CaptureAsync(fixture.Context);

        Assert.Equal(ValidationEvidenceState.Available, evidence.State);
        Assert.Equal(ValidationOutcome.Passed, evidence.Outcome);
        Assert.Equal("B", evidence.RemoteCommitId);
    }

    [Fact]
    public async Task RemoteCiWrongExpectedShaCannotSatisfyGate()
    {
        var requirement = RemoteRequirement(pullRequestNumber: 42, expectedCommit: "A");
        var fixture = CreateFixture(requirement);
        var collector = new RemoteValidationEvidenceCollector(new FakeRemoteService(Remote("B", "B", RemoteCiState.Passing, ["B"])));
        var evidence = await collector.CaptureAsync(fixture.Context);

        var decision = ValidationGateEvaluator.Evaluate(fixture.Plan, [evidence], fixture.Now.AddMinutes(1));

        Assert.Equal(ValidationGateDecisionState.Blocked, decision.State);
        Assert.Equal(ValidationReasonCodes.RepositoryMismatch, decision.RequirementDecisions.Single().ReasonCode);
    }

    [Fact]
    public async Task RemoteCiConflictingCommitEvidenceFailsClosed()
    {
        var fixture = CreateFixture(RemoteRequirement(pullRequestNumber: 42, expectedCommit: "B"));
        var collector = new RemoteValidationEvidenceCollector(new FakeRemoteService(Remote("B", "B", RemoteCiState.Passing, ["B", "C"])));

        var evidence = await collector.CaptureAsync(fixture.Context);

        Assert.Equal(ValidationEvidenceState.Invalid, evidence.State);
        Assert.Equal(ValidationReasonCodes.RemoteCiCommitConflict, evidence.ReasonCode);
        Assert.NotEqual(ValidationOutcome.Passed, evidence.Outcome);
    }

    [Fact]
    public async Task RemoteCiMissingCommitEvidenceCannotPass()
    {
        var fixture = CreateFixture(RemoteRequirement(pullRequestNumber: 42, expectedCommit: "B"));
        var remote = Remote("B", "B", RemoteCiState.Passing, [null]);
        var collector = new RemoteValidationEvidenceCollector(new FakeRemoteService(remote));

        var evidence = await collector.CaptureAsync(fixture.Context);

        Assert.Equal(ValidationEvidenceState.Invalid, evidence.State);
        Assert.Equal(ValidationReasonCodes.RemoteCiCommitIdentityMissing, evidence.ReasonCode);
    }

    [Fact]
    public async Task RemoteCiFailingEvidencePreservesFailureOutcome()
    {
        var fixture = CreateFixture(RemoteRequirement(pullRequestNumber: 42, expectedCommit: "B"));
        var collector = new RemoteValidationEvidenceCollector(new FakeRemoteService(Remote("B", "B", RemoteCiState.Failing, ["B"])));

        var evidence = await collector.CaptureAsync(fixture.Context);

        Assert.Equal(ValidationEvidenceState.Available, evidence.State);
        Assert.Equal(ValidationOutcome.Failed, evidence.Outcome);
    }

    [Theory]
    [InlineData(RemoteEvidenceState.Partial, ValidationEvidenceState.Partial)]
    [InlineData(RemoteEvidenceState.AuthenticationRequired, ValidationEvidenceState.AuthenticationRequired)]
    [InlineData(RemoteEvidenceState.PermissionDenied, ValidationEvidenceState.PermissionDenied)]
    [InlineData(RemoteEvidenceState.RateLimited, ValidationEvidenceState.RateLimited)]
    [InlineData(RemoteEvidenceState.Stale, ValidationEvidenceState.Stale)]
    [InlineData(RemoteEvidenceState.Unsupported, ValidationEvidenceState.Unsupported)]
    [InlineData(RemoteEvidenceState.Unavailable, ValidationEvidenceState.Unavailable)]
    [InlineData(RemoteEvidenceState.NotConfigured, ValidationEvidenceState.Missing)]
    [InlineData(RemoteEvidenceState.InvalidResponse, ValidationEvidenceState.Invalid)]
    public async Task RemoteCiNonPassingStateIsNeverUpgraded(RemoteEvidenceState remoteState, ValidationEvidenceState expectedState)
    {
        var fixture = CreateFixture(RemoteRequirement());
        var remote = Remote("A", null, RemoteCiState.Unknown, [], remoteState);
        var collector = new RemoteValidationEvidenceCollector(new FakeRemoteService(remote));

        var evidence = await collector.CaptureAsync(fixture.Context);

        Assert.Equal(expectedState, evidence.State);
        Assert.NotEqual(ValidationOutcome.Passed, evidence.Outcome);
    }

    [Fact]
    public async Task TrustedPlanCreationRejectsAuthorityMismatchWithoutPersistingPlan()
    {
        var fixture = CreateFixture();
        var plans = new FakePlanRepository(fixture.Plan);
        var service = CreateEvidenceService(fixture, plans, new FakeEvidenceRepository([]), new FakeCollectorResolver());
        var mismatched = CreateAuthority(fixture, runId: Guid.NewGuid());
        var authorities = new FakeAuthorityRepository(mismatched);
        service = CreateEvidenceService(fixture, plans, new FakeEvidenceRepository([]), new FakeCollectorResolver(), authorities);

        var result = await service.CreatePlanAsync(fixture.Plan);

        Assert.False(result.Succeeded);
        Assert.Equal(0, plans.CreateCalls);
    }

    [Fact]
    public async Task CaptureDoesNotInvokeCollectorAfterAuthorityMismatch()
    {
        var fixture = CreateFixture();
        var collector = new FakeCollector();
        var resolver = new FakeCollectorResolver(collector);
        var service = CreateEvidenceService(fixture, new FakePlanRepository(fixture.Plan), new FakeEvidenceRepository([]), resolver,
            new FakeAuthorityRepository(CreateAuthority(fixture, contract: new PlanningExecutionContractReference(Guid.NewGuid(), 2, 1, Hash('1')))));

        var result = await service.CaptureAsync(new ValidationCaptureRequest(fixture.Plan.ProjectId, fixture.Plan.Reference,
            fixture.Requirement.RequirementId, fixture.Plan.CurrentRecoveryCheckpointReference));

        Assert.False(result.Succeeded);
        Assert.Equal(0, collector.InvocationCount);
    }

    [Fact]
    public async Task ExplicitBaselineIsPassedToProductionRegressionCapture()
    {
        const string definition = "validation-definition:production-regression";
        var baselineRequirement = new ValidationRequirement("tests", ValidationEvidenceKind.Test, true, ValidationCoverageScope.Targeted,
            ValidationBaselineRelation.Baseline, "fake", validationDefinitionId: definition);
        var baselineFixture = CreateFixture(baselineRequirement);
        var baseline = CreateEvidence(baselineFixture);
        var regressionRequirement = new ValidationRequirement("tests", ValidationEvidenceKind.Test, true, ValidationCoverageScope.Targeted,
            ValidationBaselineRelation.Regression, "fake", validationDefinitionId: definition,
            baselineBinding: new ValidationBaselineBinding(baselineFixture.Plan.Reference, baseline.Reference, definition));
        var regressionFixture = CreateFixture(regressionRequirement, baselineFixture.Plan.ProjectId);
        var plans = new FakePlanRepository(baselineFixture.Plan, regressionFixture.Plan);
        var evidenceRepository = new FakeEvidenceRepository([baseline]);
        var collector = new FakeCollector();
        var service = CreateEvidenceService(regressionFixture, plans, evidenceRepository, new FakeCollectorResolver(collector));

        var planWrite = await service.CreatePlanAsync(regressionFixture.Plan);
        var capture = await service.CaptureAsync(new ValidationCaptureRequest(regressionFixture.Plan.ProjectId, regressionFixture.Plan.Reference,
            regressionRequirement.RequirementId, regressionFixture.Plan.CurrentRecoveryCheckpointReference));

        Assert.True(planWrite.Succeeded);
        Assert.True(capture.Succeeded);
        Assert.Equal(1, collector.InvocationCount);
        Assert.Equal(baseline.Reference.EvidenceId, capture.Evidence!.BaselineEvidenceReference!.EvidenceId);
        Assert.Equal(baseline.Reference.ContentHash, capture.Evidence.BaselineEvidenceReference.ContentHash);
    }

    [Fact]
    public async Task MissingExplicitBaselineCannotSatisfyRegressionPlan()
    {
        const string definition = "validation-definition:missing-baseline";
        var baselineRequirement = new ValidationRequirement("tests", ValidationEvidenceKind.Test, true, ValidationCoverageScope.Targeted,
            ValidationBaselineRelation.Baseline, "fake", validationDefinitionId: definition);
        var baselineFixture = CreateFixture(baselineRequirement);
        var missing = new ValidationEvidenceReference(Guid.NewGuid(), 1, Hash('9'));
        var regressionRequirement = new ValidationRequirement("tests", ValidationEvidenceKind.Test, true, ValidationCoverageScope.Targeted,
            ValidationBaselineRelation.Regression, "fake", validationDefinitionId: definition,
            baselineBinding: new ValidationBaselineBinding(baselineFixture.Plan.Reference, missing, definition));
        var regressionFixture = CreateFixture(regressionRequirement, baselineFixture.Plan.ProjectId);
        var service = CreateEvidenceService(regressionFixture, new FakePlanRepository(baselineFixture.Plan, regressionFixture.Plan),
            new FakeEvidenceRepository([]), new FakeCollectorResolver());

        var result = await service.CreatePlanAsync(regressionFixture.Plan);

        Assert.False(result.Succeeded);
        Assert.Equal(ValidationPlanRepositoryWriteStatus.PlanConflict, result.Status);
    }

    [Fact]
    public async Task BaselineDefinitionMismatchCannotSatisfyRegressionPlan()
    {
        var baselineRequirement = new ValidationRequirement("tests", ValidationEvidenceKind.Test, true, ValidationCoverageScope.Targeted,
            ValidationBaselineRelation.Baseline, "fake", validationDefinitionId: "validation-definition:one");
        var baselineFixture = CreateFixture(baselineRequirement);
        var baseline = CreateEvidence(baselineFixture);
        var regressionRequirement = new ValidationRequirement("tests", ValidationEvidenceKind.Test, true, ValidationCoverageScope.Targeted,
            ValidationBaselineRelation.Regression, "fake", validationDefinitionId: "validation-definition:two",
            baselineBinding: new ValidationBaselineBinding(baselineFixture.Plan.Reference, baseline.Reference, "validation-definition:two"));
        var regressionFixture = CreateFixture(regressionRequirement, baselineFixture.Plan.ProjectId);
        var service = CreateEvidenceService(regressionFixture, new FakePlanRepository(baselineFixture.Plan, regressionFixture.Plan),
            new FakeEvidenceRepository([baseline]), new FakeCollectorResolver());

        var result = await service.CreatePlanAsync(regressionFixture.Plan);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ValidationPlanRevisionsCoexistAndResolveExactly()
    {
        using var store = new TemporaryStore();
        var fixture = CreateFixture();
        var revisionTwo = CreateRevision(fixture, 2);
        var repository = new JsonValidationPlanRepository(store.Paths, store.Files, NullLogger<JsonValidationPlanRepository>.Instance);

        Assert.Equal(ValidationPlanRepositoryWriteStatus.Created, (await repository.CreateAsync(fixture.Plan)).Status);
        Assert.Equal(ValidationPlanRepositoryWriteStatus.Created, (await repository.CreateAsync(revisionTwo)).Status);
        Assert.True((await repository.GetAsync(fixture.Plan.ProjectId, fixture.Plan.Reference)).IsValid);
        Assert.True((await repository.GetAsync(revisionTwo.ProjectId, revisionTwo.Reference)).IsValid);
    }

    [Fact]
    public async Task WrongPlanRevisionOrHashCannotResolveAnotherRevision()
    {
        using var store = new TemporaryStore();
        var fixture = CreateFixture();
        var repository = new JsonValidationPlanRepository(store.Paths, store.Files, NullLogger<JsonValidationPlanRepository>.Instance);
        await repository.CreateAsync(fixture.Plan);
        var wrongHash = new ValidationPlanReference(fixture.Plan.ProjectId, fixture.Plan.PlanId, fixture.Plan.Revision, fixture.Plan.SchemaVersion, Hash('9'));

        var result = await repository.GetAsync(fixture.Plan.ProjectId, wrongHash);

        Assert.Equal(ValidationPlanReadState.IntegrityFailure, result.State);
    }

    [Fact]
    public async Task CurrentPlanEvidenceIsCompleteDespiteMoreThan128UnrelatedRecords()
    {
        using var store = new TemporaryStore();
        var fixture = CreateFixture();
        var other = CreateRevision(fixture, 2);
        var repository = new JsonValidationEvidenceRepository(store.Paths, store.Files, NullLogger<JsonValidationEvidenceRepository>.Instance);
        await repository.CreateAsync(CreateEvidence(fixture));
        for (var index = 0; index < ValidationLimits.MaxEvidenceItems + 1; index++)
            await repository.CreateAsync(CreateEvidence(fixture, other, Guid.NewGuid()));

        var result = await repository.GetForPlanAsync(fixture.Plan.ProjectId, fixture.Plan.Reference);

        Assert.True(result.IsComplete);
        Assert.Single(result.Evidence!);
    }

    [Fact]
    public async Task ExactPlanEvidenceCapacityIsExplicitlyBounded()
    {
        using var store = new TemporaryStore();
        var fixture = CreateFixture();
        var repository = new JsonValidationEvidenceRepository(store.Paths, store.Files, NullLogger<JsonValidationEvidenceRepository>.Instance);
        for (var index = 0; index < ValidationLimits.MaxEvidenceItems; index++)
            await repository.CreateAsync(CreateEvidence(fixture, fixture.Plan, Guid.NewGuid()));

        var result = await repository.GetForPlanAsync(fixture.Plan.ProjectId, fixture.Plan.Reference);

        Assert.Equal(ValidationEvidenceSetReadState.Valid, result.State);
        Assert.Equal(ValidationLimits.MaxEvidenceItems, result.Evidence!.Count);
    }

    [Fact]
    public async Task EvidenceSetCorruptionIsNotReturnedAsOrdinaryMissingEvidence()
    {
        using var store = new TemporaryStore();
        var fixture = CreateFixture();
        var evidence = CreateEvidence(fixture);
        var repository = new JsonValidationEvidenceRepository(store.Paths, store.Files, NullLogger<JsonValidationEvidenceRepository>.Instance);
        await repository.CreateAsync(evidence);
        var path = store.Paths.GetValidationEvidenceFile(fixture.Plan.ProjectId, fixture.Plan.PlanId, fixture.Plan.Revision, evidence.EvidenceId);
        File.WriteAllText(path, "{");

        var result = await repository.GetForPlanAsync(fixture.Plan.ProjectId, fixture.Plan.Reference);

        Assert.Equal(ValidationEvidenceSetReadState.Invalid, result.State);
        Assert.False(result.IsComplete);
    }

    [Fact]
    public async Task PlanHashTamperingIsRejectedOnExactRead()
    {
        using var store = new TemporaryStore();
        var fixture = CreateFixture();
        var repository = new JsonValidationPlanRepository(store.Paths, store.Files, NullLogger<JsonValidationPlanRepository>.Instance);
        await repository.CreateAsync(fixture.Plan);
        var path = store.Paths.GetValidationPlanFile(fixture.Plan.ProjectId, fixture.Plan.PlanId, fixture.Plan.Revision);
        File.WriteAllText(path, File.ReadAllText(path).Replace(fixture.Plan.ContentHash, Hash('9'), StringComparison.Ordinal));

        var result = await repository.GetAsync(fixture.Plan.ProjectId, fixture.Plan.Reference);

        Assert.Equal(ValidationPlanReadState.IntegrityFailure, result.State);
    }

    [Fact]
    public async Task EvidenceHashTamperingIsRejectedOnExactRead()
    {
        using var store = new TemporaryStore();
        var fixture = CreateFixture();
        var evidence = CreateEvidence(fixture);
        var repository = new JsonValidationEvidenceRepository(store.Paths, store.Files, NullLogger<JsonValidationEvidenceRepository>.Instance);
        await repository.CreateAsync(evidence);
        var path = store.Paths.GetValidationEvidenceFile(fixture.Plan.ProjectId, fixture.Plan.PlanId, fixture.Plan.Revision, evidence.EvidenceId);
        File.WriteAllText(path, File.ReadAllText(path).Replace(evidence.ContentHash, Hash('9'), StringComparison.Ordinal));

        var result = await repository.GetAsync(fixture.Plan.ProjectId, fixture.Plan.Reference, evidence.Reference);

        Assert.Equal(ValidationEvidenceReadState.IntegrityFailure, result.State);
    }

    [Fact]
    public async Task FreshEvidenceMapsToVerifiedRecoveryFreshness()
    {
        var fixture = CreateFixture();
        var result = await EvaluateGate(fixture, CreateEvidence(fixture));

        Assert.Equal(RecoveryEvidenceFreshness.Verified, result.Recovery!.Checkpoint!.EvidenceReferences.Single().Freshness);
    }

    [Fact]
    public async Task StaleEvidenceMapsToStaleRecoveryFreshness()
    {
        var fixture = CreateFixture();
        var result = await EvaluateGate(fixture, CreateEvidence(fixture, capturedAt: fixture.Now, state: ValidationEvidenceState.Stale));

        Assert.Equal(RecoveryEvidenceFreshness.Stale, result.Recovery!.Checkpoint!.EvidenceReferences.Single().Freshness);
    }

    [Fact]
    public async Task FutureEvidenceMapsToUnknownRecoveryFreshness()
    {
        var fixture = CreateFixture();
        var result = await EvaluateGate(fixture, CreateEvidence(fixture, capturedAt: fixture.Now.AddTicks(1)));

        Assert.Equal(RecoveryEvidenceFreshness.Unknown, result.Recovery!.Checkpoint!.EvidenceReferences.Single().Freshness);
    }

    [Fact]
    public async Task FreshFailedEvidenceRetainsVerifiedFreshnessButFailsGate()
    {
        var fixture = CreateFixture();
        var result = await EvaluateGate(fixture, CreateEvidence(fixture, outcome: ValidationOutcome.Failed));

        Assert.Equal(ValidationGateDecisionState.Failed, result.Decision!.State);
        Assert.Equal(RecoveryEvidenceFreshness.Verified, result.Recovery!.Checkpoint!.EvidenceReferences.Single().Freshness);
    }

    [Theory]
    [InlineData(BoundedProcessOutcome.NonZeroExit, ValidationEvidenceState.Available, ValidationOutcome.Failed)]
    [InlineData(BoundedProcessOutcome.TimedOut, ValidationEvidenceState.TimedOut, ValidationOutcome.Unknown)]
    [InlineData(BoundedProcessOutcome.Cancelled, ValidationEvidenceState.Cancelled, ValidationOutcome.Unknown)]
    [InlineData(BoundedProcessOutcome.StartFailed, ValidationEvidenceState.Unavailable, ValidationOutcome.Unknown)]
    [InlineData(BoundedProcessOutcome.TerminationFailure, ValidationEvidenceState.Unavailable, ValidationOutcome.Unknown)]
    public async Task DotNetCollectorPreservesBoundedProcessOutcome(BoundedProcessOutcome processOutcome, ValidationEvidenceState expectedState, ValidationOutcome expectedOutcome)
    {
        var fixture = CreateFixture(new ValidationRequirement("tests", ValidationEvidenceKind.Test, true, ValidationCoverageScope.Targeted, ValidationBaselineRelation.Standalone, "fake", targetPath: "project.csproj"));
        var result = new BoundedProcessResult(processOutcome, processOutcome == BoundedProcessOutcome.NonZeroExit ? 1 : null, "", "", false, false, false, true, TimeSpan.Zero);
        var collector = new DotNetValidationEvidenceCollector(new FakeProcessHost(result), new FixedClock(fixture.Now), new HandoffRedactionService());

        var evidence = await collector.CaptureAsync(fixture.Context);

        Assert.Equal(expectedState, evidence.State);
        Assert.Equal(expectedOutcome, evidence.Outcome);
    }

    [Fact]
    public async Task DotNetCollectorReportsOutputTruncationWithoutPersistingOutput()
    {
        var fixture = CreateFixture(new ValidationRequirement("tests", ValidationEvidenceKind.Test, true, ValidationCoverageScope.Targeted, ValidationBaselineRelation.Standalone, "fake", targetPath: "project.csproj"));
        var result = new BoundedProcessResult(BoundedProcessOutcome.ExitedSuccessfully, 0, "safe", "", true, false, false, true, TimeSpan.Zero);
        var evidence = await new DotNetValidationEvidenceCollector(new FakeProcessHost(result), new FixedClock(fixture.Now), new HandoffRedactionService()).CaptureAsync(fixture.Context);

        Assert.True(evidence.OutputTruncated);
        Assert.Null(evidence.DiagnosticSummary);
    }

    private static ValidationEvidenceService CreateEvidenceService(Fixture fixture, FakePlanRepository plans, FakeEvidenceRepository evidence,
        FakeCollectorResolver resolver, FakeAuthorityRepository? authorities = null) =>
        new(plans, evidence, new FakeProjectRepository(fixture.Project), authorities ?? new FakeAuthorityRepository(fixture.Authority),
            new FakeReceiptRepository(fixture.Receipt), new FakeCheckpointRepository(fixture.Checkpoint), resolver, new FixedClock(fixture.Now));

    private static async Task<ValidationGateEvaluationResult> EvaluateGate(Fixture fixture, ValidationEvidence evidence)
    {
        var recovery = new FakeRecoveryService(fixture.Checkpoint);
        var service = new ValidationGateService(new FakePlanRepository(fixture.Plan), new FakeEvidenceRepository([evidence]), new FakeDecisionRepository(),
            new FakeAuthorityRepository(fixture.Authority), new FakeReceiptRepository(fixture.Receipt), new FakeCheckpointRepository(fixture.Checkpoint),
            recovery, new FixedClock(fixture.Now));
        return await service.EvaluateAsync(new ValidationGateRequest(fixture.Plan.ProjectId, fixture.Plan.Reference, fixture.Plan.CurrentRecoveryCheckpointReference));
    }

    private static ValidationRequirement RemoteRequirement(int? pullRequestNumber = null, string? expectedCommit = null) =>
        new("remote-ci", ValidationEvidenceKind.RemoteCi, true, ValidationCoverageScope.Targeted, ValidationBaselineRelation.Standalone,
            RemoteValidationEvidenceCollector.CollectorIdentifier, expectedRemoteCommitId: expectedCommit, requestedBranch: "main", pullRequestNumber: pullRequestNumber);

    private static RemoteRepositoryEvidence Remote(string branchCommit, string? pullRequestCommit, RemoteCiState result,
        IReadOnlyList<string?> commits, RemoteEvidenceState state = RemoteEvidenceState.Available)
    {
        var repository = new RemoteRepositoryIdentity(RemoteRepositoryProvider.GitHub, RemoteEvidenceSource.GitHubRest, "repo-1", "owner/repo", "owner", "repo", "main");
        var branch = new RemoteBranchEvidence("main", branchCommit, true);
        var pullRequest = pullRequestCommit is null ? null : new RemotePullRequestEvidence("42", "open", false, "feature", "main", pullRequestCommit, "base", RemoteMergeability.Available);
        var runs = commits.Select((commit, index) => new RemoteCiRunEvidence(RemoteStatusKind.CheckRun, $"run-{index}", "checks", result, headCommitId: commit)).ToArray();
        return new(Guid.NewGuid(), state, RemoteEvidenceSource.GitHubRest, new DateTimeOffset(2026, 8, 31, 10, 0, 0, TimeSpan.Zero), repository,
            RemoteEvidenceState.Available, branch, RemoteEvidenceState.Available, pullRequest,
            pullRequest is null ? RemoteEvidenceState.NotConfigured : RemoteEvidenceState.Available, ciRuns: runs,
            ciState: state, ciResult: result);
    }

    private static Fixture CreateFixture(ValidationRequirement? requirement = null, Guid? projectId = null, DateTimeOffset? now = null)
    {
        var capturedAt = now ?? new DateTimeOffset(2026, 8, 31, 10, 0, 0, TimeSpan.Zero);
        var id = projectId ?? Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var contract = new PlanningExecutionContractReference(Guid.NewGuid(), 1, 1, Hash('a'));
        var graph = new WorkGraphReference(Guid.NewGuid(), 1, Hash('b'));
        var handoff = new HandoffPackageReference(Guid.NewGuid(), 1, Hash('c'));
        var routing = new RoutingDecisionReference(Guid.NewGuid(), 1, Hash('d'));
        var workspacePlan = new WorkspacePreparationPlanReference(Guid.NewGuid(), 1, Hash('e'), id);
        var workspace = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "apo-remediation-workspace"));
        var checkpoint = new RecoveryCheckpoint(id, Guid.NewGuid(), 1, capturedAt, RecoveryCheckpointLifecycleState.Ready,
            new RecoveryContextReference(Guid.NewGuid(), 1, capturedAt), contract, graph, Guid.NewGuid(), handoff,
            nextSafeAction: RecoveryNextSafeAction.RunValidation);
        var receipt = new WorkspacePreparationReceipt(id, workspaceId, Guid.NewGuid(), capturedAt, workspacePlan, workspace, "main", Sha40('f'), Sha40('f'), "local-repository", "test");
        var authority = new ExecutionRunAuthority(id, Guid.NewGuid(), capturedAt, contract, graph, checkpoint.WorkGraphNodeId!.Value, handoff, routing, workspacePlan,
            workspaceId, workspace, receipt.ContentHash, checkpoint.Reference, Guid.NewGuid(), "provider", "model", AgentConnectionMode.Cli, "adapter", new ExecutionBudgetEnvelope(1, 1));
        var value = requirement ?? new ValidationRequirement("tests", ValidationEvidenceKind.Test, true, ValidationCoverageScope.Targeted, ValidationBaselineRelation.Standalone, "fake");
        var plan = new ValidationPlan(id, Guid.NewGuid(), 1, capturedAt, authority.Reference, contract, graph, checkpoint.WorkGraphNodeId!.Value,
            workspaceId, workspace, receipt.ContentHash, checkpoint.Reference, [value], handoff);
        var project = new Project(id, "Validation", workspace, null, ProjectStatus.Active, capturedAt, capturedAt);
        var context = new ValidationCollectionContext(plan, value, project, authority, receipt, checkpoint, []);
        return new(capturedAt, plan, value, project, authority, receipt, checkpoint, context, workspace);
    }

    private static ValidationPlan CreateRevision(Fixture fixture, int revision) =>
        new(fixture.Plan.ProjectId, fixture.Plan.PlanId, revision, fixture.Plan.CreatedAt, fixture.Authority.Reference,
            fixture.Plan.PlanningContractReference, fixture.Plan.WorkGraphReference, fixture.Plan.WorkGraphNodeId, fixture.Plan.WorkspaceId,
            fixture.Plan.WorkspacePath, fixture.Plan.WorkspaceReceiptContentHash, fixture.Plan.CurrentRecoveryCheckpointReference,
            fixture.Plan.Requirements, fixture.Plan.HandoffPackageReference, evidenceNotBefore: fixture.Plan.EvidenceNotBefore);

    private static ExecutionRunAuthority CreateAuthority(Fixture fixture, Guid? runId = null, PlanningExecutionContractReference? contract = null) =>
        new(fixture.Plan.ProjectId, runId ?? fixture.Authority.RunId, fixture.Now, contract ?? fixture.Plan.PlanningContractReference,
            fixture.Plan.WorkGraphReference, fixture.Plan.WorkGraphNodeId, fixture.Authority.HandoffPackageReference, fixture.Authority.RoutingDecisionReference,
            new WorkspacePreparationPlanReference(Guid.NewGuid(), 1, Hash('e'), fixture.Plan.ProjectId), fixture.Plan.WorkspaceId, fixture.Plan.WorkspacePath,
            fixture.Plan.WorkspaceReceiptContentHash, fixture.Plan.CurrentRecoveryCheckpointReference, fixture.Authority.AgentId, fixture.Authority.Provider,
            fixture.Authority.ModelIdentifier, fixture.Authority.ConnectionMode, fixture.Authority.AdapterIdentifier, fixture.Authority.Budgets);

    private static ValidationEvidence CreateEvidence(Fixture fixture, ValidationPlan? plan = null, Guid? evidenceId = null,
        ValidationEvidenceState state = ValidationEvidenceState.Available, ValidationOutcome? outcome = null,
        DateTimeOffset? capturedAt = null, ValidationBaselineRelation? baselineRelation = null) =>
        new((plan ?? fixture.Plan).ProjectId, evidenceId ?? Guid.NewGuid(), (plan ?? fixture.Plan).Reference, fixture.Requirement.RequirementId,
            fixture.Authority.RunId, fixture.Authority.Reference, fixture.Plan.PlanningContractReference, fixture.Plan.WorkGraphReference, fixture.Plan.WorkGraphNodeId,
            fixture.Plan.CurrentRecoveryCheckpointReference, fixture.Plan.WorkspaceId, fixture.Plan.WorkspacePath, fixture.Plan.WorkspaceReceiptContentHash,
            fixture.Requirement.CollectorIdentifier, fixture.Requirement.EvidenceKind, state, outcome ?? (state == ValidationEvidenceState.Available ? ValidationOutcome.Passed : ValidationOutcome.Unknown),
            fixture.Requirement.Coverage, baselineRelation ?? fixture.Requirement.BaselineRelation, capturedAt ?? fixture.Now,
            validationDefinitionId: fixture.Requirement.ValidationDefinitionId);

    private static string Hash(char value) => new(value, 64);
    private static string Sha40(char value) => new(value, 40);

    private sealed record Fixture(DateTimeOffset Now, ValidationPlan Plan, ValidationRequirement Requirement, Project Project,
        ExecutionRunAuthority Authority, WorkspacePreparationReceipt Receipt, RecoveryCheckpoint Checkpoint,
        ValidationCollectionContext Context, string Workspace);

    private sealed class FixedClock(DateTimeOffset value) : IClock { public DateTimeOffset UtcNow => value; }

    private sealed class FakeRemoteService(RemoteRepositoryEvidence value) : IRemoteRepositoryEvidenceService
    {
        public Task<RemoteRepositoryEvidence> InspectAsync(Project project, string? requestedBranch = null, int? pullRequestNumber = null, CancellationToken cancellationToken = default) => Task.FromResult(new RemoteRepositoryEvidence(project.Id, value.State, value.Source, value.CapturedAt, value.Repository, value.RepositoryState, value.Branch, value.BranchState, value.PullRequest, value.PullRequestState, value.Reviews, value.ReviewState, value.Statuses, value.StatusState, value.Checks, value.CheckState, value.CiRuns, value.CiState, value.CiResult, value.Limitations, value.SafeErrorMessage));
    }

    private sealed class FakeProcessHost(BoundedProcessResult result) : IBoundedProcessHost
    {
        public Task<BoundedProcessResult> RunAsync(BoundedProcessRequest request, CancellationToken cancellationToken = default) => Task.FromResult(result);
    }

    private sealed class FakeCollector : IValidationEvidenceCollector
    {
        public int InvocationCount { get; private set; }
        public ValidationEvidenceCollectorDescriptor Descriptor { get; } = new("fake", [ValidationEvidenceKind.Test, ValidationEvidenceKind.Build], false, true);
        public Task<ValidationEvidence> CaptureAsync(ValidationCollectionContext context, CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            return Task.FromResult(new ValidationEvidence(context.Plan.ProjectId, Guid.NewGuid(), context.Plan.Reference, context.Requirement.RequirementId,
                context.Authority.RunId, context.Authority.Reference, context.Plan.PlanningContractReference, context.Plan.WorkGraphReference, context.Plan.WorkGraphNodeId,
                context.CurrentCheckpoint.Reference, context.Plan.WorkspaceId, context.Plan.WorkspacePath, context.Plan.WorkspaceReceiptContentHash,
                context.Requirement.CollectorIdentifier, context.Requirement.EvidenceKind, ValidationEvidenceState.Available, ValidationOutcome.Passed,
                context.Requirement.Coverage, context.Requirement.BaselineRelation, context.Plan.CreatedAt.AddMinutes(1), baselineEvidenceReference: context.BaselineEvidenceReference,
                validationDefinitionId: context.Requirement.ValidationDefinitionId));
        }
    }

    private sealed class FakeCollectorResolver(FakeCollector? collector = null) : IValidationEvidenceCollectorResolver
    {
        private readonly FakeCollector _collector = collector ?? new();
        public ValidationCollectorResolution Resolve(string collectorIdentifier, ValidationEvidenceKind kind) =>
            collectorIdentifier == _collector.Descriptor.Identifier && _collector.Descriptor.SupportedKinds.Contains(kind)
                ? new(ValidationCollectorResolutionStatus.Resolved, _collector) : new(ValidationCollectorResolutionStatus.Unsupported);
    }

    private sealed class FakePlanRepository(params ValidationPlan[] values) : IValidationPlanRepository
    {
        private readonly Dictionary<ValidationPlanReference, ValidationPlan> _values = values.ToDictionary(value => value.Reference);
        public int CreateCalls { get; private set; }
        public Task<ValidationPlanRepositoryWriteResult> CreateAsync(ValidationPlan value, CancellationToken cancellationToken = default) { CreateCalls++; _values[value.Reference] = value; return Task.FromResult(new ValidationPlanRepositoryWriteResult(ValidationPlanRepositoryWriteStatus.Created)); }
        public Task<ValidationPlanReadResult> GetAsync(Guid projectId, ValidationPlanReference reference, CancellationToken cancellationToken = default) => Task.FromResult(_values.TryGetValue(reference, out var value) ? new ValidationPlanReadResult(ValidationPlanReadState.Valid, value) : new ValidationPlanReadResult(ValidationPlanReadState.Missing));
    }

    private sealed class FakeEvidenceRepository(IReadOnlyList<ValidationEvidence> values) : IValidationEvidenceRepository
    {
        private readonly List<ValidationEvidence> _values = values.ToList();
        public Task<ValidationEvidenceRepositoryWriteResult> CreateAsync(ValidationEvidence evidence, CancellationToken cancellationToken = default) { _values.Add(evidence); return Task.FromResult(new ValidationEvidenceRepositoryWriteResult(ValidationEvidenceRepositoryWriteStatus.Created)); }
        public Task<ValidationEvidenceReadResult> GetAsync(Guid projectId, ValidationPlanReference planReference, ValidationEvidenceReference evidenceReference, CancellationToken cancellationToken = default) => Task.FromResult(_values.FirstOrDefault(value => value.ProjectId == projectId && value.PlanReference.PlanId == planReference.PlanId && value.PlanReference.Revision == planReference.Revision && value.EvidenceId == evidenceReference.EvidenceId && value.ContentHash == evidenceReference.ContentHash) is { } value ? new ValidationEvidenceReadResult(ValidationEvidenceReadState.Valid, value) : new ValidationEvidenceReadResult(ValidationEvidenceReadState.Missing));
        public Task<ValidationEvidenceSetReadResult> GetForPlanAsync(Guid projectId, ValidationPlanReference planReference, CancellationToken cancellationToken = default) => Task.FromResult(new ValidationEvidenceSetReadResult(ValidationEvidenceSetReadState.Valid, _values.Where(value => value.ProjectId == projectId && value.PlanReference.PlanId == planReference.PlanId && value.PlanReference.Revision == planReference.Revision && value.PlanReference.ContentHash == planReference.ContentHash).ToArray()));
    }

    private sealed class FakeProjectRepository(Project project) : IProjectRepository
    {
        public Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Project>>([project]);
        public Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default) => Task.FromResult<Project?>(projectId == project.Id ? project : null);
        public Task UpsertAsync(Project value, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeAuthorityRepository(ExecutionRunAuthority authority) : IExecutionRunAuthorityRepository
    {
        public Task<ExecutionRunAuthorityRepositoryWriteResult> CreateAsync(ExecutionRunAuthority value, CancellationToken cancellationToken = default) => Task.FromResult(new ExecutionRunAuthorityRepositoryWriteResult(ExecutionRunAuthorityRepositoryWriteStatus.Created));
        public Task<ExecutionRunAuthorityReadResult> GetAsync(Guid projectId, Guid runId, CancellationToken cancellationToken = default) => Task.FromResult(new ExecutionRunAuthorityReadResult(ExecutionRunAuthorityReadState.Valid, authority));
    }

    private sealed class FakeReceiptRepository(WorkspacePreparationReceipt receipt) : IWorkspacePreparationReceiptRepository
    {
        public Task<WorkspacePreparationReceiptWriteResult> CreateAsync(WorkspacePreparationReceipt value, CancellationToken cancellationToken = default) => Task.FromResult(new WorkspacePreparationReceiptWriteResult(WorkspacePreparationReceiptWriteStatus.Created));
        public Task<WorkspacePreparationReceiptReadResult> GetAsync(Guid projectId, Guid workspaceId, CancellationToken cancellationToken = default) => Task.FromResult(new WorkspacePreparationReceiptReadResult(WorkspacePreparationReceiptReadState.Valid, receipt));
    }

    private sealed class FakeCheckpointRepository(RecoveryCheckpoint checkpoint) : IRecoveryCheckpointRepository
    {
        public Task<RecoveryCheckpointRepositoryWriteResult> CreateAsync(RecoveryCheckpoint value, CancellationToken cancellationToken = default) => Task.FromResult(new RecoveryCheckpointRepositoryWriteResult(RecoveryCheckpointRepositoryWriteStatus.Created));
        public Task<RecoveryCheckpointReadResult> GetAsync(Guid projectId, Guid checkpointId, CancellationToken cancellationToken = default) => Task.FromResult(new RecoveryCheckpointReadResult(RecoveryCheckpointReadState.Valid, checkpoint));
    }

    private sealed class FakeDecisionRepository : IValidationGateDecisionRepository
    {
        public Task<ValidationDecisionRepositoryWriteResult> CreateAsync(ValidationGateDecision decision, CancellationToken cancellationToken = default) => Task.FromResult(new ValidationDecisionRepositoryWriteResult(ValidationDecisionRepositoryWriteStatus.Created));
        public Task<ValidationDecisionReadResult> GetAsync(Guid projectId, Guid decisionId, CancellationToken cancellationToken = default) => Task.FromResult(new ValidationDecisionReadResult(ValidationDecisionReadState.Missing));
    }

    private sealed class FakeRecoveryService(RecoveryCheckpoint predecessor) : IRecoveryCheckpointService
    {
        public Task<RecoveryCheckpointCreationResult> CreateAsync(RecoveryCheckpointCreationRequest request, CancellationToken cancellationToken = default)
        {
            var checkpoint = new RecoveryCheckpoint(request.ProjectId, request.CheckpointId, 1, request.CreatedAt ?? predecessor.CreatedAt, request.LifecycleState,
                predecessor.Context, request.PlanningContractReference, request.WorkGraphReference, request.WorkGraphNodeId, request.HandoffPackageReference,
                request.PreviousCheckpointReference, request.SelectedAgentRoleReferences, request.EvidenceReferences, request.GateSnapshots, request.Blockers,
                request.NextSafeAction, request.Explanation);
            var head = new ContinuationHead(request.ProjectId, 1, 2, checkpoint.Reference, predecessor.Reference, checkpoint.CreatedAt);
            return Task.FromResult(new RecoveryCheckpointCreationResult(RecoveryCheckpointCreationStatus.Created, checkpoint, head));
        }
    }
}
