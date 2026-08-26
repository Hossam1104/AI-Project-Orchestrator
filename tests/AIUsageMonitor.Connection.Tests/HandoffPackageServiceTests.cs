using AIUsageMonitor.Application.Handoffs;
using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Projects;

namespace AIUsageMonitor.Connection.Tests;

public sealed class HandoffPackageServiceTests
{
    [Fact]
    public async Task FullLifecycleCreatesAllSixSupportedTransitionsWithExactPredecessors()
    {
        var fixture = Fixture.Create();

        var planner = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.PlannerToExecutor,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            nextAction: "Execute the bounded work."));
        var executor = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.ExecutorToReviewer,
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            previous: planner.Package!.Reference,
            evidence: [fixture.Evidence("build")],
            artifacts: [fixture.Artifact("src/feature.cs")],
            outcome: new HandoffOutcomeMetadata(HandoffOutcomeState.Succeeded, "Implementation complete."),
            nextAction: "Review the implementation."));
        var review = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.ReviewerToRemediation,
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            previous: executor.Package!.Reference,
            evidence: [fixture.Evidence("review")],
            findings: [fixture.Finding("finding-1", HandoffFindingState.Unresolved)],
            outcome: new HandoffOutcomeMetadata(HandoffOutcomeState.ChangesRequired, "One finding remains."),
            nextAction: "Address the finding."));
        var remediation = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.RemediationToReviewer,
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            previous: review.Package!.Reference,
            evidence: [fixture.Evidence("remediation")],
            findings: [fixture.Finding("finding-1", HandoffFindingState.Addressed)],
            artifacts: [fixture.Artifact("src/fix.cs")],
            outcome: new HandoffOutcomeMetadata(HandoffOutcomeState.Succeeded, "Finding addressed."),
            nextAction: "Re-review the remediation."));
        var acceptance = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.ReviewerToAcceptance,
            Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            previous: remediation.Package!.Reference,
            evidence: [fixture.Evidence("acceptance")],
            outcome: new HandoffOutcomeMetadata(HandoffOutcomeState.Passed, "Acceptance evidence is complete."),
            nextAction: "Record acceptance."));
        var returnedToPlanner = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.AcceptanceToPlanner,
            Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            previous: acceptance.Package!.Reference,
            outcome: new HandoffOutcomeMetadata(HandoffOutcomeState.Accepted, "Accepted for planning follow-up."),
            nextAction: "Prepare the next planner contract."));

        Assert.Equal(HandoffPackageCreationStatus.Created, planner.Status);
        Assert.Equal(HandoffPackageCreationStatus.Created, executor.Status);
        Assert.Equal(HandoffPackageCreationStatus.Created, review.Status);
        Assert.Equal(HandoffPackageCreationStatus.Created, remediation.Status);
        Assert.Equal(HandoffPackageCreationStatus.Created, acceptance.Status);
        Assert.Equal(HandoffPackageCreationStatus.Created, returnedToPlanner.Status);
        Assert.Equal(6, fixture.Packages.Created.Count);
        Assert.Equal(planner.Package!.PackageId, executor.Package!.PreviousPackageReference!.PackageId);
        Assert.Equal(executor.Package.PackageId, review.Package!.PreviousPackageReference!.PackageId);
        Assert.Equal(review.Package.PackageId, remediation.Package!.PreviousPackageReference!.PackageId);
        Assert.Equal(remediation.Package.PackageId, acceptance.Package!.PreviousPackageReference!.PackageId);
        Assert.Equal(acceptance.Package.PackageId, returnedToPlanner.Package!.PreviousPackageReference!.PackageId);
        Assert.Equal(HandoffRole.Planner, returnedToPlanner.Package.TargetRole);
    }

    [Fact]
    public async Task RoleSpecificSectionsAndReferencesAreIncludedOnlyWhereRelevant()
    {
        var fixture = Fixture.Create();
        var root = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.PlannerToExecutor,
            Guid.NewGuid(),
            evidence: [fixture.Evidence("ignored")],
            findings: [fixture.Finding("ignored", HandoffFindingState.Unresolved)],
            artifacts: [fixture.Artifact("ignored.cs")],
            nextAction: "Execute the bounded work."));

        Assert.Equal(HandoffPackageCreationStatus.Created, root.Status);
        Assert.NotNull(root.Package!.ExecutionScope);
        Assert.Null(root.Package.ReviewScope);
        Assert.Null(root.Package.RemediationScope);
        Assert.Null(root.Package.AcceptanceScope);
        Assert.Empty(root.Package.EvidenceReferences);
        Assert.Empty(root.Package.FindingReferences);
        Assert.Empty(root.Package.ChangedArtifactReferences);
        Assert.Null(root.Package.Outcome);

        var next = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.ExecutorToReviewer,
            Guid.NewGuid(),
            previous: root.Package.Reference,
            evidence: [fixture.Evidence("build")],
            findings: [fixture.Finding("unresolved", HandoffFindingState.Unresolved)],
            artifacts: [fixture.Artifact("src/file.cs")],
            outcome: new HandoffOutcomeMetadata(HandoffOutcomeState.Succeeded),
            nextAction: "Review the bounded work."));

        Assert.Equal(HandoffPackageCreationStatus.Created, next.Status);
        Assert.Null(next.Package!.ExecutionScope);
        Assert.NotNull(next.Package.ReviewScope);
        Assert.Null(next.Package.RemediationScope);
        Assert.Null(next.Package.AcceptanceScope);
        Assert.Single(next.Package.EvidenceReferences);
        Assert.Empty(next.Package.FindingReferences);
        Assert.Single(next.Package.ChangedArtifactReferences);
        Assert.NotNull(next.Package.Outcome);
    }

    [Fact]
    public async Task ExactContractRevisionIsUsedWithoutLatestFallback()
    {
        var fixture = Fixture.Create();
        fixture.Contracts.NextRead = new(PlanningContractReadState.Missing);

        var result = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.PlannerToExecutor,
            Guid.NewGuid(),
            nextAction: "Execute the bounded work."));

        Assert.Equal(HandoffPackageCreationStatus.ContractMissing, result.Status);
        Assert.Equal([(fixture.Project.Id, fixture.Contract.ContractId, fixture.Contract.Revision)], fixture.Contracts.GetCalls);
        Assert.Equal(0, fixture.Contracts.LatestCalls);
        Assert.Empty(fixture.Packages.Created);
    }

    [Fact]
    public async Task CrossProjectRequestIsRejectedBeforeContractOrPackageAccess()
    {
        var fixture = Fixture.Create();
        var request = fixture.Request(
            HandoffTransition.PlannerToExecutor,
            Guid.NewGuid(),
            nextAction: "Execute the bounded work.");

        var result = await fixture.Service.CreateAsync(
            new HandoffPackageCreationRequest(
                Guid.Parse("99999999-9999-9999-9999-999999999999"),
                request.PackageId,
                request.Transition,
                request.PlanningContractReference,
                request.CreatedAt,
                nextAction: request.NextAction));

        Assert.Equal(HandoffPackageCreationStatus.ProjectNotFound, result.Status);
        Assert.Empty(fixture.Contracts.GetCalls);
        Assert.Empty(fixture.Packages.Created);
    }

    [Fact]
    public async Task MissingPredecessorAndWrongTransitionAreTyped()
    {
        var fixture = Fixture.Create();
        var missing = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.ExecutorToReviewer,
            Guid.NewGuid(),
            previous: new HandoffPackageReference(Guid.NewGuid(), HandoffPackageSchema.CurrentVersion, new string('a', 64)),
            evidence: [fixture.Evidence("build")],
            artifacts: [fixture.Artifact("file.cs")],
            outcome: new HandoffOutcomeMetadata(HandoffOutcomeState.Succeeded),
            nextAction: "Review the work."));

        Assert.Equal(HandoffPackageCreationStatus.PredecessorMissing, missing.Status);

        var root = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.PlannerToExecutor,
            Guid.NewGuid(),
            nextAction: "Execute the bounded work."));
        var invalidLineage = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.ReviewerToAcceptance,
            Guid.NewGuid(),
            previous: root.Package!.Reference,
            evidence: [fixture.Evidence("acceptance")],
            outcome: new HandoffOutcomeMetadata(HandoffOutcomeState.Passed),
            nextAction: "Record acceptance."));

        Assert.Equal(HandoffPackageCreationStatus.InvalidLineage, invalidLineage.Status);
    }

    [Fact]
    public async Task RedactionIsAppliedAndRecordedWithoutRetainingTheSecret()
    {
        var fixture = Fixture.Create(includeSecretInContract: true);

        var result = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.PlannerToExecutor,
            Guid.NewGuid(),
            nextAction: "Execute the bounded work."));

        Assert.Equal(HandoffPackageCreationStatus.Created, result.Status);
        Assert.True(result.Package!.Redaction.RedactionApplied);
        Assert.Contains(HandoffRedactionCategory.ConnectionStringPassword, result.Package.Redaction.Categories);
        Assert.DoesNotContain("super-secret", result.Package.ExecutionScope!.IncludedScope[0].Statement, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", result.Package.ExecutionScope.IncludedScope[0].Statement, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DescriptiveTextIsRedactedAcrossScopeFindingOutcomeLimitationAndNextAction()
    {
        var fixture = Fixture.Create(includeSecretInContract: true);
        var planner = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.PlannerToExecutor,
            Guid.NewGuid(),
            nextAction: "password=next-action-secret"));
        var executor = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.ExecutorToReviewer,
            Guid.NewGuid(),
            previous: planner.Package!.Reference,
            evidence: [fixture.Evidence("build")],
            artifacts: [fixture.Artifact("src/file.cs")],
            outcome: new HandoffOutcomeMetadata(HandoffOutcomeState.Succeeded, "password=outcome-secret"),
            limitations: ["password=limitation-secret"],
            nextAction: "Review the bounded work."));
        var reviewer = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.ReviewerToRemediation,
            Guid.NewGuid(),
            previous: executor.Package!.Reference,
            evidence: [fixture.Evidence("review")],
            findings: [fixture.Finding(
                "finding-descriptive",
                HandoffFindingState.Unresolved,
                summary: "password=finding-secret")],
            outcome: new HandoffOutcomeMetadata(HandoffOutcomeState.ChangesRequired),
            nextAction: "Address the finding."));

        Assert.Equal(HandoffPackageCreationStatus.Created, planner.Status);
        Assert.Equal(HandoffPackageCreationStatus.Created, executor.Status);
        Assert.Equal(HandoffPackageCreationStatus.Created, reviewer.Status);
        Assert.Contains("[REDACTED]", planner.Package!.ExecutionScope!.IncludedScope[0].Statement, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", planner.Package.NextAction, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", executor.Package!.Outcome!.Summary, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", executor.Package.Limitations[0], StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", reviewer.Package!.FindingReferences[0].Summary, StringComparison.Ordinal);
        Assert.DoesNotContain("next-action-secret", planner.Package.NextAction, StringComparison.Ordinal);
        Assert.DoesNotContain("outcome-secret", executor.Package.Outcome.Summary, StringComparison.Ordinal);
        Assert.DoesNotContain("limitation-secret", executor.Package.Limitations[0], StringComparison.Ordinal);
        Assert.DoesNotContain("finding-secret", reviewer.Package.FindingReferences[0].Summary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SecretShapedWorkItemReferenceIsRejectedWithoutPersistence()
    {
        var fixture = Fixture.Create(contractIdentity: "work-item-reference");

        var result = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.PlannerToExecutor,
            Guid.NewGuid(),
            nextAction: "Execute the bounded work."));

        Assert.Equal(HandoffPackageCreationStatus.RedactionRejected, result.Status);
        Assert.Null(result.Package);
        Assert.Empty(fixture.Packages.Created);
    }

    [Theory]
    [InlineData("registered-local-path")]
    [InlineData("expected-branch")]
    public async Task SecretShapedRepositoryIdentityIsRejectedWithoutPersistence(string identity)
    {
        var fixture = Fixture.Create(contractIdentity: identity);

        var result = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.PlannerToExecutor,
            Guid.NewGuid(),
            nextAction: "Execute the bounded work."));

        Assert.Equal(HandoffPackageCreationStatus.RedactionRejected, result.Status);
        Assert.Null(result.Package);
        Assert.Empty(fixture.Packages.Created);
    }

    [Theory]
    [InlineData("scope-clause-id")]
    [InlineData("deliverable-id")]
    [InlineData("validation-id")]
    [InlineData("stop-condition-id")]
    public async Task SecretShapedCanonicalContractIdentifierIsRejectedWithoutPersistence(string identity)
    {
        var fixture = Fixture.Create(contractIdentity: identity);

        var result = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.PlannerToExecutor,
            Guid.NewGuid(),
            nextAction: "Execute the bounded work."));

        Assert.Equal(HandoffPackageCreationStatus.RedactionRejected, result.Status);
        Assert.Null(result.Package);
        Assert.Empty(fixture.Packages.Created);
    }

    [Fact]
    public async Task SecretShapedAcceptanceCriterionIdIsRejectedWithoutPersistence()
    {
        var fixture = Fixture.Create(contractIdentity: "acceptance-criterion-id");
        var planner = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.PlannerToExecutor,
            Guid.NewGuid(),
            nextAction: "Execute the bounded work."));

        var result = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.ExecutorToReviewer,
            Guid.NewGuid(),
            previous: planner.Package!.Reference,
            evidence: [fixture.Evidence("build")],
            artifacts: [fixture.Artifact("src/file.cs")],
            outcome: new HandoffOutcomeMetadata(HandoffOutcomeState.Succeeded),
            nextAction: "Review the bounded work."));

        Assert.Equal(HandoffPackageCreationStatus.Created, planner.Status);
        Assert.Equal(HandoffPackageCreationStatus.RedactionRejected, result.Status);
        Assert.Null(result.Package);
        Assert.Single(fixture.Packages.Created);
    }

    [Fact]
    public async Task SecretShapedFindingIdIsRejectedWithoutPersistence()
    {
        var fixture = Fixture.Create();
        var planner = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.PlannerToExecutor,
            Guid.NewGuid(),
            nextAction: "Execute the bounded work."));
        var executor = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.ExecutorToReviewer,
            Guid.NewGuid(),
            previous: planner.Package!.Reference,
            evidence: [fixture.Evidence("build")],
            artifacts: [fixture.Artifact("src/file.cs")],
            outcome: new HandoffOutcomeMetadata(HandoffOutcomeState.Succeeded),
            nextAction: "Review the bounded work."));

        var result = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.ReviewerToRemediation,
            Guid.NewGuid(),
            previous: executor.Package!.Reference,
            evidence: [fixture.Evidence("review")],
            findings: [fixture.Finding("finding-api_key=identity-secret-value", HandoffFindingState.Unresolved)],
            outcome: new HandoffOutcomeMetadata(HandoffOutcomeState.ChangesRequired),
            nextAction: "Address the finding."));

        Assert.Equal(HandoffPackageCreationStatus.RedactionRejected, result.Status);
        Assert.Null(result.Package);
        Assert.Equal(2, fixture.Packages.Created.Count);
    }

    [Fact]
    public async Task SecretShapedEvidenceReferenceIsRejectedWithoutPersistence()
    {
        var fixture = Fixture.Create();
        var planner = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.PlannerToExecutor,
            Guid.NewGuid(),
            nextAction: "Execute the bounded work."));

        var result = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.ExecutorToReviewer,
            Guid.NewGuid(),
            previous: planner.Package!.Reference,
            evidence: [new HandoffEvidenceReference(
                Guid.NewGuid(),
                HandoffEvidenceKind.Test,
                "evidence:api_key=identity-secret-value",
                Fixture.Now,
                HandoffEvidenceFreshness.PointInTime)],
            artifacts: [fixture.Artifact("src/file.cs")],
            outcome: new HandoffOutcomeMetadata(HandoffOutcomeState.Succeeded),
            nextAction: "Review the bounded work."));

        Assert.Equal(HandoffPackageCreationStatus.RedactionRejected, result.Status);
        Assert.Null(result.Package);
        Assert.Single(fixture.Packages.Created);
    }

    [Theory]
    [InlineData("path")]
    [InlineData("external-reference")]
    public async Task SecretShapedChangedArtifactReferenceIsRejectedWithoutPersistence(string identity)
    {
        var fixture = Fixture.Create();
        var planner = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.PlannerToExecutor,
            Guid.NewGuid(),
            nextAction: "Execute the bounded work."));
        var executor = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.ExecutorToReviewer,
            Guid.NewGuid(),
            previous: planner.Package!.Reference,
            evidence: [fixture.Evidence("build")],
            artifacts: [fixture.Artifact("src/file.cs")],
            outcome: new HandoffOutcomeMetadata(HandoffOutcomeState.Succeeded),
            nextAction: "Review the bounded work."));
        var artifact = identity == "path"
            ? new HandoffChangedArtifactReference("src/api_key=identity-secret-value.cs", new string('a', 40))
            : new HandoffChangedArtifactReference(commitSha: new string('a', 40), externalReference: "artifact:api_key=identity-secret-value");

        var result = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.ReviewerToAcceptance,
            Guid.NewGuid(),
            previous: executor.Package!.Reference,
            evidence: [fixture.Evidence("acceptance")],
            artifacts: [artifact],
            outcome: new HandoffOutcomeMetadata(HandoffOutcomeState.Passed),
            nextAction: "Record acceptance."));

        Assert.Equal(HandoffPackageCreationStatus.RedactionRejected, result.Status);
        Assert.Null(result.Package);
        Assert.Equal(2, fixture.Packages.Created.Count);
    }

    [Fact]
    public async Task CancellationPropagatesBeforeAnyPackageIsCreated()
    {
        var fixture = Fixture.Create();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Service.CreateAsync(
            fixture.Request(HandoffTransition.PlannerToExecutor, Guid.NewGuid(), nextAction: "Execute the bounded work."),
            cancellation.Token));
        Assert.Empty(fixture.Packages.Created);
    }

    [Fact]
    public async Task UnsupportedTransitionIsRejectedBeforeAuthorityLookup()
    {
        var fixture = Fixture.Create();

        var result = await fixture.Service.CreateAsync(fixture.Request(
            (HandoffTransition)999,
            Guid.NewGuid(),
            nextAction: "Do not execute."));

        Assert.Equal(HandoffPackageCreationStatus.UnsupportedTransition, result.Status);
        Assert.Empty(fixture.Contracts.GetCalls);
        Assert.Empty(fixture.Packages.Created);
    }

    [Fact]
    public async Task OversizedCanonicalPackageFailsClosedWithoutPersistence()
    {
        var fixture = Fixture.Create(largeScope: true);

        var result = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.PlannerToExecutor,
            Guid.NewGuid(),
            nextAction: "Execute the bounded work."));

        Assert.Equal(HandoffPackageCreationStatus.PackageTooLarge, result.Status);
        Assert.Empty(fixture.Packages.Created);
    }

    [Fact]
    public async Task RedactionRejectsUnsupportedControlCharactersWithoutPersistence()
    {
        var fixture = Fixture.Create();

        var result = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.PlannerToExecutor,
            Guid.NewGuid(),
            nextAction: "invalid\0text"));

        Assert.Equal(HandoffPackageCreationStatus.RedactionRejected, result.Status);
        Assert.Empty(fixture.Packages.Created);
    }

    [Fact]
    public async Task IdenticalAuthorityInputsProduceTheSameContentHash()
    {
        var first = Fixture.Create();
        var second = Fixture.Create();
        var packageId = Guid.Parse("abababab-abab-abab-abab-abababababab");

        var firstResult = await first.Service.CreateAsync(first.Request(
            HandoffTransition.PlannerToExecutor, packageId, nextAction: "Execute the bounded work."));
        var secondResult = await second.Service.CreateAsync(second.Request(
            HandoffTransition.PlannerToExecutor, packageId, nextAction: "Execute the bounded work."));

        Assert.Equal(HandoffPackageCreationStatus.Created, firstResult.Status);
        Assert.Equal(HandoffPackageCreationStatus.Created, secondResult.Status);
        Assert.Equal(firstResult.Package!.ContentHash, secondResult.Package!.ContentHash);
    }

    [Fact]
    public async Task ValidGraphNodeBindingIsCopiedOnlyAfterExactGraphValidation()
    {
        var fixture = Fixture.Create();
        var graph = new WorkGraph(
            fixture.Project.Id,
            Guid.Parse("12121212-1212-1212-1212-121212121212"),
            WorkGraphSchema.CurrentVersion,
            Fixture.Now,
            [new WorkGraphNode(
                Guid.Parse("13131313-1313-1313-1313-131313131313"),
                fixture.Contract.Reference)],
            []);
        fixture.Graphs.Graph = graph;

        var result = await fixture.Service.CreateAsync(new HandoffPackageCreationRequest(
            fixture.Project.Id,
            Guid.NewGuid(),
            HandoffTransition.PlannerToExecutor,
            fixture.Contract.Reference,
            Fixture.Now,
            workGraphReference: graph.Reference,
            workGraphNodeId: graph.Nodes[0].NodeId,
            nextAction: "Execute the bounded work."));

        Assert.Equal(HandoffPackageCreationStatus.Created, result.Status);
        Assert.Equal(graph.GraphId, result.Package!.WorkGraphReference!.GraphId);
        Assert.Equal(graph.Nodes[0].NodeId, result.Package.WorkGraphNodeId);
        Assert.Equal(HandoffPackageCreationStatus.GraphNodeMismatch, (await fixture.Service.CreateAsync(
            new HandoffPackageCreationRequest(
                fixture.Project.Id,
                Guid.NewGuid(),
                HandoffTransition.PlannerToExecutor,
                fixture.Contract.Reference,
                Fixture.Now,
                workGraphReference: new WorkGraphReference(graph.GraphId, graph.SchemaVersion, new string('b', 64)),
                workGraphNodeId: graph.Nodes[0].NodeId,
                nextAction: "Execute the bounded work."))).Status);
    }

    [Fact]
    public async Task RemediationReviewMayReturnToAnotherBoundedRemediationRound()
    {
        var fixture = Fixture.Create();
        var planner = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.PlannerToExecutor,
            Guid.NewGuid(),
            nextAction: "Execute the bounded work."));
        var executor = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.ExecutorToReviewer,
            Guid.NewGuid(),
            previous: planner.Package!.Reference,
            evidence: [fixture.Evidence("build")],
            artifacts: [fixture.Artifact("src/file.cs")],
            outcome: new HandoffOutcomeMetadata(HandoffOutcomeState.Succeeded),
            nextAction: "Review the bounded work."));
        var remediationRequest = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.ReviewerToRemediation,
            Guid.NewGuid(),
            previous: executor.Package!.Reference,
            evidence: [fixture.Evidence("review")],
            findings: [fixture.Finding("finding-loop", HandoffFindingState.Unresolved)],
            outcome: new HandoffOutcomeMetadata(HandoffOutcomeState.ChangesRequired),
            nextAction: "Address the finding."));
        var remediationReview = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.RemediationToReviewer,
            Guid.NewGuid(),
            previous: remediationRequest.Package!.Reference,
            evidence: [fixture.Evidence("remediation")],
            findings: [fixture.Finding("finding-loop", HandoffFindingState.Addressed)],
            artifacts: [fixture.Artifact("src/fix.cs")],
            outcome: new HandoffOutcomeMetadata(HandoffOutcomeState.Succeeded),
            nextAction: "Re-review the remediation."));
        var nextRound = await fixture.Service.CreateAsync(fixture.Request(
            HandoffTransition.ReviewerToRemediation,
            Guid.NewGuid(),
            previous: remediationReview.Package!.Reference,
            evidence: [fixture.Evidence("review-2")],
            findings: [fixture.Finding("finding-loop", HandoffFindingState.Unresolved)],
            outcome: new HandoffOutcomeMetadata(HandoffOutcomeState.ChangesRequired),
            nextAction: "Address the remaining finding."));

        Assert.Equal(HandoffPackageCreationStatus.Created, remediationReview.Status);
        Assert.Equal(HandoffPackageCreationStatus.Created, nextRound.Status);
        Assert.Equal(HandoffFindingState.Addressed, remediationReview.Package!.FindingReferences[0].State);
        Assert.Equal(remediationReview.Package.PackageId, nextRound.Package!.PreviousPackageReference!.PackageId);
    }

    private sealed class Fixture
    {
        private Fixture(Project project, PlanningExecutionContract contract)
        {
            Project = project;
            Contract = contract;
            Contracts = new FakeContractRepository(contract);
            Packages = new FakePackageRepository();
            Graphs = new FakeGraphRepository();
            Service = new HandoffPackageService(
                new FakeProjectRepository(project),
                Contracts,
                Graphs,
                Packages,
                new HandoffRedactionService());
        }

        public static readonly DateTimeOffset Now = new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

        public Project Project { get; }
        public PlanningExecutionContract Contract { get; }
        public FakeContractRepository Contracts { get; }
        public FakeGraphRepository Graphs { get; }
        public FakePackageRepository Packages { get; }
        public HandoffPackageService Service { get; }

        public static Fixture Create(
            bool includeSecretInContract = false,
            bool largeScope = false,
            string? contractIdentity = null)
        {
            var project = new Project(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "APO handoff test project",
                @"C:\APO-Test",
                null,
                ProjectStatus.Active,
                Now,
                Now);
            var contract = CreateContract(project.Id, includeSecretInContract, largeScope, contractIdentity);
            return new(project, contract);
        }

        public HandoffPackageCreationRequest Request(
            HandoffTransition transition,
            Guid packageId,
            HandoffPackageReference? previous = null,
            IReadOnlyList<HandoffEvidenceReference>? evidence = null,
            IReadOnlyList<HandoffFindingReference>? findings = null,
            IReadOnlyList<HandoffChangedArtifactReference>? artifacts = null,
            HandoffOutcomeMetadata? outcome = null,
            string? nextAction = null,
            IReadOnlyList<string>? limitations = null) => new(
            Project.Id,
            packageId,
            transition,
            Contract.Reference,
            Now,
            previousPackageReference: previous,
            evidenceReferences: evidence,
            findingReferences: findings,
            changedArtifactReferences: artifacts,
            outcome: outcome,
            limitations: limitations,
            nextAction: nextAction ?? "Continue the bounded handoff.");

        public HandoffEvidenceReference Evidence(string suffix) => new(
            Guid.NewGuid(),
            HandoffEvidenceKind.Test,
            $"evidence:{suffix}",
            Now,
            HandoffEvidenceFreshness.PointInTime);

        public HandoffFindingReference Finding(
            string id,
            HandoffFindingState state,
            string? summary = null,
            string? sourceReference = null) => new(
            id,
            HandoffFindingCategory.Correctness,
            HandoffFindingSeverity.High,
            state,
            summary ?? "Bounded finding",
            sourceReference ?? "review:finding",
            []);

        public HandoffChangedArtifactReference Artifact(string path) => new(
            path,
            new string('a', 40),
            null);

        private static PlanningExecutionContract CreateContract(
            Guid projectId,
            bool includeSecret,
            bool largeScope,
            string? identity)
        {
            const string identitySecret = "api_key=identity-secret-value";
            var workItemReference = identity == "work-item-reference"
                ? identitySecret
                : "APO-42";
            var repositoryTarget = identity switch
            {
                "registered-local-path" => new PlanningRepositoryTarget(
                    PlanningRepositoryMode.LocalGit,
                    @"C:\repo\api_key=identity-secret-value",
                    "main",
                    new string('a', 40)),
                "expected-branch" => new PlanningRepositoryTarget(
                    PlanningRepositoryMode.LocalGit,
                    @"C:\repo",
                    "feature/api_key=identity-secret-value",
                    new string('a', 40)),
                _ => new PlanningRepositoryTarget(PlanningRepositoryMode.None)
            };
            var includedScopeId = identity == "scope-clause-id" ? identitySecret : "include";
            var deliverableId = identity == "deliverable-id" ? identitySecret : "package";
            var validationId = identity == "validation-id" ? identitySecret : "test";
            var acceptanceCriterionId = identity == "acceptance-criterion-id" ? identitySecret : "accept";
            var stopConditionId = identity == "stop-condition-id" ? identitySecret : "stop-target";

            return new(
                projectId,
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                PlanningExecutionContractSchema.CurrentVersion,
                1,
                Now,
                "owner",
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                new PlanningContextBinding(Guid.Parse("44444444-4444-4444-4444-444444444444"), 1),
                new PlanningWorkItem(PlanningWorkItemSource.Jira, workItemReference, "Structured handoff packages"),
                repositoryTarget,
                largeScope
                    ? Enumerable.Range(0, HandoffPackageLimits.MaxScopeItemsPerSection)
                        .Select(index => new PlanningScopeClause($"include-{index:D3}", new string('x', 4_000)))
                        .ToArray()
                    : [new PlanningScopeClause(includedScopeId, includeSecret ? "password=super-secret" : "structured handoff")],
                [new PlanningScopeClause("constraint", "bounded")],
                [new PlanningScopeClause("forbid", "model invocation")],
                [new PlanningDeliverable(deliverableId, "immutable package", true)],
                [new PlanningValidationRequirement(validationId, PlanningValidationKind.Test, "focused tests", true)],
                [new PlanningAcceptanceCriterion(acceptanceCriterionId, "hash is deterministic", true)],
                [new PlanningExecutionBudget(PlanningBudgetKind.Attempts, 1)],
                [
                    new PlanningStopCondition(stopConditionId, PlanningStopConditionKind.ImmutableTargetMoved, "stop when target moves"),
                    new PlanningStopCondition("stop-scope", PlanningStopConditionKind.ScopeViolation, "stop on scope violation"),
                    new PlanningStopCondition("stop-budget", PlanningStopConditionKind.BudgetExceeded, "stop when budget is exceeded")
                ],
                [],
                null,
                null);
        }
    }

    private sealed class FakeProjectRepository(Project project) : IProjectRepository
    {
        public Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Project>>([project]);

        public Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Project?>(projectId == project.Id ? project : null);
        }

        public Task UpsertAsync(Project value, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeContractRepository(PlanningExecutionContract contract) : IPlanningExecutionContractRepository
    {
        public PlanningContractReadResult? NextRead { get; set; }
        public List<(Guid ProjectId, Guid ContractId, int Revision)> GetCalls { get; } = [];
        public int LatestCalls { get; private set; }

        public Task<PlanningContractRepositoryWriteResult> CreateAsync(PlanningExecutionContract value, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PlanningContractRepositoryWriteResult(PlanningContractRepositoryWriteStatus.Created));

        public Task<PlanningContractReadResult> GetAsync(Guid projectId, Guid contractId, int revision, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GetCalls.Add((projectId, contractId, revision));
            if (NextRead is not null)
            {
                var result = NextRead;
                NextRead = null;
                return Task.FromResult(result);
            }

            return Task.FromResult(
                projectId == contract.ProjectId && contractId == contract.ContractId && revision == contract.Revision
                    ? new PlanningContractReadResult(PlanningContractReadState.Valid, contract)
                    : new PlanningContractReadResult(PlanningContractReadState.Missing));
        }

        public Task<PlanningContractReadResult> GetLatestAsync(Guid projectId, Guid contractId, CancellationToken cancellationToken = default)
        {
            LatestCalls++;
            throw new InvalidOperationException("Latest contract lookup is outside the APO-42 exact-reference contract.");
        }

        public Task<PlanningContractRevisionListResult> ListRevisionsAsync(Guid projectId, Guid contractId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PlanningContractRevisionListResult(PlanningContractReadState.Valid, [contract]));
    }

    private sealed class FakeGraphRepository : IWorkGraphRepository
    {
        public WorkGraph? Graph { get; set; }

        public Task<WorkGraphRepositoryWriteResult> CreateAsync(WorkGraph graph, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorkGraphRepositoryWriteResult(WorkGraphRepositoryWriteStatus.Created));

        public Task<WorkGraphReadResult> GetAsync(Guid projectId, Guid graphId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(
                Graph is not null && Graph.ProjectId == projectId && Graph.GraphId == graphId
                    ? new WorkGraphReadResult(WorkGraphReadState.Valid, Graph)
                    : new WorkGraphReadResult(WorkGraphReadState.Missing));
        }
    }

    private sealed class FakePackageRepository : IHandoffPackageRepository
    {
        private readonly Dictionary<(Guid ProjectId, Guid PackageId), HandoffPackage> _packages = [];

        public List<HandoffPackage> Created { get; } = [];

        public Task<HandoffPackageRepositoryWriteResult> CreateAsync(HandoffPackage package, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_packages.TryAdd((package.ProjectId, package.PackageId), package))
            {
                return Task.FromResult(new HandoffPackageRepositoryWriteResult(
                    HandoffPackageRepositoryWriteStatus.PackageConflict));
            }

            Created.Add(package);
            return Task.FromResult(new HandoffPackageRepositoryWriteResult(HandoffPackageRepositoryWriteStatus.Created));
        }

        public Task<HandoffPackageReadResult> GetAsync(Guid projectId, Guid packageId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_packages.TryGetValue((projectId, packageId), out var package)
                ? new HandoffPackageReadResult(HandoffPackageReadState.Valid, package)
                : new HandoffPackageReadResult(HandoffPackageReadState.Missing));
        }
    }
}
