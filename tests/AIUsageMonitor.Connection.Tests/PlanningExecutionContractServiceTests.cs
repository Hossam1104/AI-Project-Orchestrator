using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Projects;
using AIUsageMonitor.Application.Time;

namespace AIUsageMonitor.Connection.Tests;

public sealed class PlanningExecutionContractServiceTests
{
    [Fact]
    public async Task MissingProjectIsRejectedBeforeContractCreation()
    {
        var fixture = Fixture.Create();
        fixture.Projects.Project = null;

        var result = await fixture.Service.CreateAsync(fixture.Request());

        Assert.Equal(PlanningExecutionContractCreationStatus.ProjectNotFound, result.Status);
        Assert.Empty(fixture.Contracts.Created);
    }

    [Fact]
    public async Task EveryNonReadyContextFailsClosed()
    {
        foreach (var state in Enum.GetValues<ProjectContextResolutionState>().Where(value => value != ProjectContextResolutionState.Ready))
        {
            var fixture = Fixture.Create();
            fixture.Contexts.Resolution = new ProjectContextResolution(state, ErrorMessage: "not ready");

            var result = await fixture.Service.CreateAsync(fixture.Request());

            Assert.Equal(PlanningExecutionContractCreationStatus.ContextNotReady, result.Status);
            Assert.Equal(state, result.ContextState);
        }
    }

    [Fact]
    public async Task MissingPlannerIdentityIsRejected()
    {
        var fixture = Fixture.Create();
        fixture.Agents.Resolution = AgentRegistryResolution.NotFoundResult();

        var result = await fixture.Service.CreateAsync(fixture.Request());

        Assert.Equal(PlanningExecutionContractCreationStatus.PlannerNotFound, result.Status);
    }

    [Fact]
    public async Task DisabledOrNonPlannerIdentityIsRejected()
    {
        var fixture = Fixture.Create();
        fixture.Agents.Resolution = AgentRegistryResolution.FoundResult(
            Fixture.CreateEffectiveAgent(fixture.Project.Id, fixture.PlannerAgentId, enabled: false, AgentRole.Planner));

        var disabled = await fixture.Service.CreateAsync(fixture.Request());
        Assert.Equal(PlanningExecutionContractCreationStatus.PlannerNotAuthorized, disabled.Status);

        fixture.Agents.Resolution = AgentRegistryResolution.FoundResult(
            Fixture.CreateEffectiveAgent(fixture.Project.Id, fixture.PlannerAgentId, enabled: true, AgentRole.Executor));
        var wrongRole = await fixture.Service.CreateAsync(fixture.Request());
        Assert.Equal(PlanningExecutionContractCreationStatus.PlannerNotAuthorized, wrongRole.Status);
    }

    [Fact]
    public async Task EnabledPlannerCapabilitySucceedsWithoutFabricatingAccessTruth()
    {
        var fixture = Fixture.Create();

        var result = await fixture.Service.CreateAsync(fixture.Request());

        Assert.True(result.Succeeded);
        Assert.Equal(PlanningExecutionContractCreationStatus.Created, result.Status);
        Assert.Equal(fixture.Project.Id, result.Contract!.ProjectId);
        Assert.Equal(fixture.Context.ContextId, result.Contract.Context.ProjectContextId);
        Assert.Equal(ProjectContextContract.CurrentVersion, result.Contract.Context.ProjectContextContractVersion);
        Assert.Equal(["governance/from-context"], result.Contract.GovernanceReferences);
        Assert.Equal("routing/from-context", result.Contract.RoutingPolicyReference);
        Assert.Equal("safety/from-context", result.Contract.SafetyPolicyReference);
        Assert.Equal(AgentAvailability.Unknown, fixture.Agents.Resolution.Agent!.Availability);
        Assert.Equal(AgentAuthenticationState.Unknown, fixture.Agents.Resolution.Agent.AuthenticationState);
        Assert.Equal(AgentEntitlementState.Unknown, fixture.Agents.Resolution.Agent.EntitlementState);
        Assert.Single(fixture.Contracts.Created);
    }

    [Fact]
    public async Task SkippedRepositoryContextAllowsExplicitRepositoryFreeTarget()
    {
        var fixture = Fixture.Create();

        var result = await fixture.Service.CreateAsync(
            fixture.Request(repositoryTarget: new PlanningRepositoryTarget(PlanningRepositoryMode.None)));

        Assert.Equal(PlanningExecutionContractCreationStatus.Created, result.Status);
        Assert.Single(fixture.Contracts.Created);
    }

    [Fact]
    public async Task SkippedRepositoryContextRejectsLocalGitTargetWithoutPersisting()
    {
        var fixture = Fixture.Create();

        var result = await fixture.Service.CreateAsync(fixture.Request(
            repositoryTarget: CreateLocalGitTarget(fixture.Project.LocalPath)));

        Assert.Equal(PlanningExecutionContractCreationStatus.RepositoryTargetMismatch, result.Status);
        Assert.Equal(
            "A LocalGit repository target requires inspected canonical repository context.",
            result.ErrorMessage);
        Assert.Empty(fixture.Contracts.Created);
    }

    [Fact]
    public async Task InspectedRepositoryContextAllowsMatchingLocalGitTargetAndPreservesAssertions()
    {
        var fixture = Fixture.Create();
        fixture.UseInspectedContext();
        var expectedBranch = "feature/planning";
        var expectedHeadCommit = new string('a', 64);

        var result = await fixture.Service.CreateAsync(fixture.Request(
            repositoryTarget: new PlanningRepositoryTarget(
                PlanningRepositoryMode.LocalGit,
                fixture.Project.LocalPath,
                expectedBranch,
                expectedHeadCommit)));

        Assert.Equal(PlanningExecutionContractCreationStatus.Created, result.Status);
        Assert.Equal(PlanningRepositoryMode.LocalGit, result.Contract!.RepositoryTarget.Mode);
        Assert.Equal(fixture.Project.LocalPath, result.Contract.RepositoryTarget.RegisteredLocalPath);
        Assert.Equal(expectedBranch, result.Contract.RepositoryTarget.ExpectedBranch);
        Assert.Equal(expectedHeadCommit, result.Contract.RepositoryTarget.ExpectedHeadCommit);
        Assert.Single(fixture.Contracts.Created);
    }

    [Fact]
    public async Task InspectedRepositoryContextRejectsForeignLocalGitTargetWithoutPersisting()
    {
        var fixture = Fixture.Create();
        fixture.UseInspectedContext();

        var result = await fixture.Service.CreateAsync(fixture.Request(
            repositoryTarget: CreateLocalGitTarget(@"D:\another-project")));

        Assert.Equal(PlanningExecutionContractCreationStatus.RepositoryTargetMismatch, result.Status);
        Assert.Equal(
            "The LocalGit repository target does not belong to the resolved project context.",
            result.ErrorMessage);
        Assert.Empty(fixture.Contracts.Created);
    }

    [Fact]
    public async Task InspectedRepositoryContextUsesCaseInsensitivePathBelonging()
    {
        var fixture = Fixture.Create();
        fixture.UseInspectedContext();

        var result = await fixture.Service.CreateAsync(fixture.Request(
            repositoryTarget: CreateLocalGitTarget(@"d:\PLANNING-PROJECT")));

        Assert.Equal(PlanningExecutionContractCreationStatus.Created, result.Status);
        Assert.Equal(@"d:\PLANNING-PROJECT", result.Contract!.RepositoryTarget.RegisteredLocalPath);
        Assert.Single(fixture.Contracts.Created);
    }

    [Fact]
    public async Task InspectedRepositoryContextStillAllowsExplicitRepositoryFreeTarget()
    {
        var fixture = Fixture.Create();
        fixture.UseInspectedContext();

        var result = await fixture.Service.CreateAsync(
            fixture.Request(repositoryTarget: new PlanningRepositoryTarget(PlanningRepositoryMode.None)));

        Assert.Equal(PlanningExecutionContractCreationStatus.Created, result.Status);
        Assert.Equal(PlanningRepositoryMode.None, result.Contract!.RepositoryTarget.Mode);
        Assert.Single(fixture.Contracts.Created);
    }

    [Fact]
    public async Task LaterRevisionRejectsForeignLocalGitTargetWithoutPersistingRevision()
    {
        var fixture = Fixture.Create();
        var first = await fixture.Service.CreateAsync(fixture.Request());

        var result = await fixture.Service.CreateAsync(fixture.Request(
            revision: 2,
            repositoryTarget: CreateLocalGitTarget(@"D:\another-project")));

        Assert.True(first.Succeeded);
        Assert.Equal(PlanningExecutionContractCreationStatus.RepositoryTargetMismatch, result.Status);
        Assert.Single(fixture.Contracts.Created);
        Assert.Equal(1, fixture.Contracts.Created[0].Revision);
    }

    private static PlanningRepositoryTarget CreateLocalGitTarget(string registeredLocalPath) =>
        new(
            PlanningRepositoryMode.LocalGit,
            registeredLocalPath,
            "main",
            new string('b', 40));

    [Fact]
    public async Task RevisionTwoBindsToImmediatePredecessorAndKeepsLogicalIdentity()
    {
        var fixture = Fixture.Create();
        var first = await fixture.Service.CreateAsync(fixture.Request());
        var second = await fixture.Service.CreateAsync(fixture.Request(revision: 2));

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(1, second.Contract!.PreviousRevision);
        Assert.Equal(first.Contract!.ContentHash, second.Contract.PreviousContentHash);
        Assert.Equal(first.Contract.Reference.ToString(), fixture.Contracts.Created[0].Reference.ToString());
    }

    [Fact]
    public async Task RevisionThreeCannotSkipRevisionTwo()
    {
        var fixture = Fixture.Create();
        await fixture.Service.CreateAsync(fixture.Request());

        var result = await fixture.Service.CreateAsync(fixture.Request(revision: 3));

        Assert.Equal(PlanningExecutionContractCreationStatus.PredecessorMissing, result.Status);
    }

    [Fact]
    public async Task RevisionCannotChangeOwnerOrWorkItemOrClaimWrongPredecessorHash()
    {
        var fixture = Fixture.Create();
        var first = await fixture.Service.CreateAsync(fixture.Request());
        Assert.True(first.Succeeded);

        var changedOwner = await fixture.Service.CreateAsync(fixture.Request(revision: 2, ownerReference: "different-owner"));
        Assert.Equal(PlanningExecutionContractCreationStatus.PredecessorMismatch, changedOwner.Status);

        var changedWorkItem = await fixture.Service.CreateAsync(fixture.Request(
            revision: 2,
            workItem: new PlanningWorkItem(PlanningWorkItemSource.Jira, "APO-41", "Different work")));
        Assert.Equal(PlanningExecutionContractCreationStatus.PredecessorMismatch, changedWorkItem.Status);

        var wrongHash = await fixture.Service.CreateAsync(fixture.Request(
            revision: 2,
            previousContentHash: new string('a', 64)));
        Assert.Equal(PlanningExecutionContractCreationStatus.PredecessorMismatch, wrongHash.Status);
    }

    [Fact]
    public async Task RepositoryInvalidLineageWriteStatusMapsToPredecessorMismatch()
    {
        var fixture = Fixture.Create();
        var first = await fixture.Service.CreateAsync(fixture.Request());
        fixture.Contracts.NextWriteResult = new(
            PlanningContractRepositoryWriteStatus.InvalidLineage,
            "durable lineage rejected");

        var result = await fixture.Service.CreateAsync(fixture.Request(revision: 2));

        Assert.True(first.Succeeded);
        Assert.Equal(PlanningExecutionContractCreationStatus.PredecessorMismatch, result.Status);
        Assert.Equal("durable lineage rejected", result.ErrorMessage);
        Assert.Single(fixture.Contracts.Created);
    }

    [Fact]
    public async Task ExistingRevisionIsAConflictAndInvalidRequestsAreTyped()
    {
        var fixture = Fixture.Create();
        var first = await fixture.Service.CreateAsync(fixture.Request());
        var conflict = await fixture.Service.CreateAsync(fixture.Request());
        var invalid = await fixture.Service.CreateAsync(fixture.Request(projectId: Guid.Empty));

        Assert.True(first.Succeeded);
        Assert.Equal(PlanningExecutionContractCreationStatus.RevisionConflict, conflict.Status);
        Assert.Equal(PlanningExecutionContractCreationStatus.InvalidContract, invalid.Status);
    }

    [Fact]
    public async Task CancellationIsNotConvertedToAFalseCreationResult()
    {
        var fixture = Fixture.Create();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.Service.CreateAsync(fixture.Request(), cancellation.Token));
        Assert.Empty(fixture.Contracts.Created);
    }

    private sealed class Fixture
    {
        private Fixture(
            Project project,
            ProjectContextReference context,
            PlanningExecutionContractService service,
            Guid plannerAgentId,
            FakeProjectRepository projects,
            FakeContextResolver contexts,
            FakeAgentRegistry agents,
            FakeContractRepository contracts)
        {
            Project = project;
            Context = context;
            Service = service;
            PlannerAgentId = plannerAgentId;
            Projects = projects;
            Contexts = contexts;
            Agents = agents;
            Contracts = contracts;
        }

        public Project Project { get; }
        public ProjectContextReference Context { get; }
        public PlanningExecutionContractService Service { get; }
        public Guid PlannerAgentId { get; }
        public FakeProjectRepository Projects { get; }
        public FakeContextResolver Contexts { get; }
        public FakeAgentRegistry Agents { get; }
        public FakeContractRepository Contracts { get; }

        public static Fixture Create()
        {
            var projectId = Guid.NewGuid();
            var plannerAgentId = Guid.NewGuid();
            var project = new Project(
                projectId,
                "Planning project",
                @"D:\planning-project",
                "main",
                ProjectStatus.Active,
                new DateTimeOffset(2026, 8, 26, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 26, 0, 0, 0, TimeSpan.Zero));
            var context = new ProjectContextReference(
                projectId,
                Guid.NewGuid(),
                ProjectContextContract.CurrentVersion,
                project.CreatedAt,
                project.UpdatedAt,
                ProjectRepositoryContextReference.Skipped(projectId, project.LocalPath),
                new ProjectTrackerContextReference(TrackerReferenceState.Skipped),
                [],
                new ProjectCurrentWorkReference(CurrentWorkState.NotSelected),
                ["governance/from-context"],
                "routing/from-context",
                "safety/from-context",
                ProjectNextSafeAction.ReadyForPlanning);
            var projects = new FakeProjectRepository(project);
            var contexts = new FakeContextResolver(new ProjectContextResolution(
                ProjectContextResolutionState.Ready,
                new ProjectContextView(
                    project,
                    context,
                    [CreateEffectiveAgent(projectId, plannerAgentId, enabled: true, AgentRole.Planner)])));
            var agents = new FakeAgentRegistry(AgentRegistryResolution.FoundResult(
                CreateEffectiveAgent(projectId, plannerAgentId, enabled: true, AgentRole.Planner)));
            var contracts = new FakeContractRepository();
            var service = new PlanningExecutionContractService(
                projects,
                contexts,
                agents,
                contracts,
                new FixedClock(project.CreatedAt));
            return new(project, context, service, plannerAgentId, projects, contexts, agents, contracts);
        }

        public PlanningExecutionContractRequest Request(
            Guid? projectId = null,
            int revision = 1,
            string ownerReference = "owner-ref",
            PlanningWorkItem? workItem = null,
            string? previousContentHash = null,
            PlanningRepositoryTarget? repositoryTarget = null) =>
            new(
                projectId ?? Project.Id,
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                revision,
                ownerReference,
                PlannerAgentId,
                workItem ?? new PlanningWorkItem(PlanningWorkItemSource.Jira, "APO-40", "Define contracts"),
                repositoryTarget ?? new PlanningRepositoryTarget(PlanningRepositoryMode.None),
                [new("included", "Included")],
                [new("constraint", "Constraint")],
                [new("forbidden", "Forbidden")],
                [new("deliverable", "Deliverable", true)],
                [new("build", PlanningValidationKind.Build, "Build", true)],
                [new("accept", "Acceptance", true)],
                [new(PlanningBudgetKind.Attempts, 2)],
                [
                    new("target", PlanningStopConditionKind.ImmutableTargetMoved, "Target moved"),
                    new("scope", PlanningStopConditionKind.ScopeViolation, "Scope violation"),
                    new("budget", PlanningStopConditionKind.BudgetExceeded, "Budget exceeded")
                ],
                previousContentHash: previousContentHash);

        public void UseInspectedContext(
            string? registeredLocalPath = null,
            RepositoryVerificationStatus status = RepositoryVerificationStatus.AvailableClean)
        {
            var inspection = new LocalRepositoryInspection(
                status,
                registeredLocalPath ?? Project.LocalPath,
                repositoryRoot: registeredLocalPath ?? Project.LocalPath,
                localPathIsRepositoryRoot: true,
                branchName: "main",
                headSha: new string('c', 40),
                headShortSha: new string('c', 7),
                isClean: status == RepositoryVerificationStatus.AvailableClean,
                capturedAt: Project.CreatedAt);
            var context = new ProjectContextReference(
                Project.Id,
                Guid.NewGuid(),
                ProjectContextContract.CurrentVersion,
                Project.CreatedAt,
                Project.UpdatedAt,
                ProjectRepositoryContextReference.FromInspection(Project.Id, inspection),
                new ProjectTrackerContextReference(TrackerReferenceState.Skipped),
                [],
                new ProjectCurrentWorkReference(CurrentWorkState.NotSelected),
                ["governance/from-context"],
                "routing/from-context",
                "safety/from-context",
                ProjectNextSafeAction.ReadyForPlanning);
            Contexts.Resolution = new ProjectContextResolution(
                ProjectContextResolutionState.Ready,
                new ProjectContextView(
                    Project,
                    context,
                    [CreateEffectiveAgent(Project.Id, PlannerAgentId, enabled: true, AgentRole.Planner)]));
        }

        public static EffectiveAgentDefinition CreateEffectiveAgent(
            Guid projectId,
            Guid agentId,
            bool enabled,
            AgentRole role)
        {
            var definition = new AgentDefinition(
                agentId,
                "Planner test agent",
                role.ToString(),
                AgentConnectionMode.Manual,
                AgentAvailability.Unknown,
                enabled,
                new DateTimeOffset(2026, 8, 26, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 26, 0, 0, 0, TimeSpan.Zero),
                roleCapabilities: [role],
                supportedConnectionModes: [AgentConnectionMode.Manual],
                authenticationState: AgentAuthenticationState.Unknown,
                entitlementState: AgentEntitlementState.Unknown);
            return new EffectiveAgentDefinition(projectId, definition, null);
        }
    }

    private sealed class FixedClock(DateTimeOffset value) : IClock
    {
        public DateTimeOffset UtcNow { get; } = value;
    }

    private sealed class FakeProjectRepository(Project? project) : IProjectRepository
    {
        public Project? Project { get; set; } = project;

        public Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<Project>>(Project is null ? [] : [Project]);
        }

        public Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Project?.Id == projectId ? Project : null);
        }

        public Task UpsertAsync(Project project, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Project = project;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeContextResolver(ProjectContextResolution resolution) : IProjectContextResolver
    {
        public ProjectContextResolution Resolution { get; set; } = resolution;

        public Task<ProjectContextResolution> ResolveAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Resolution);
        }
    }

    private sealed class FakeAgentRegistry(AgentRegistryResolution resolution) : IAgentRegistryService
    {
        public AgentRegistryResolution Resolution { get; set; } = resolution;

        public Task<IReadOnlyList<EffectiveAgentDefinition>> GetEffectiveAgentsAsync(
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<EffectiveAgentDefinition>>(
                Resolution.Agent is null ? [] : [Resolution.Agent]);
        }

        public Task<AgentRegistryResolution> ResolveAsync(
            Guid projectId,
            Guid agentId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Resolution);
        }
    }

    private sealed class FakeContractRepository : IPlanningExecutionContractRepository
    {
        private readonly Dictionary<(Guid ProjectId, Guid ContractId, int Revision), PlanningExecutionContract> _items = [];

        public List<PlanningExecutionContract> Created { get; } = [];

        public PlanningContractRepositoryWriteResult? NextWriteResult { get; set; }

        public Task<PlanningContractRepositoryWriteResult> CreateAsync(
            PlanningExecutionContract contract,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (NextWriteResult is not null)
            {
                var result = NextWriteResult;
                NextWriteResult = null;
                return Task.FromResult(result);
            }

            var key = (contract.ProjectId, contract.ContractId, contract.Revision);
            if (_items.ContainsKey(key))
            {
                return Task.FromResult(new PlanningContractRepositoryWriteResult(
                    PlanningContractRepositoryWriteStatus.RevisionConflict));
            }

            _items[key] = contract;
            Created.Add(contract);
            return Task.FromResult(new PlanningContractRepositoryWriteResult(
                PlanningContractRepositoryWriteStatus.Created));
        }

        public Task<PlanningContractReadResult> GetAsync(
            Guid projectId,
            Guid contractId,
            int revision,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_items.TryGetValue((projectId, contractId, revision), out var contract)
                ? new PlanningContractReadResult(PlanningContractReadState.Valid, contract)
                : new PlanningContractReadResult(PlanningContractReadState.Missing));
        }

        public Task<PlanningContractReadResult> GetLatestAsync(
            Guid projectId,
            Guid contractId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var contract = _items
                .Where(pair => pair.Key.ProjectId == projectId && pair.Key.ContractId == contractId)
                .OrderByDescending(pair => pair.Key.Revision)
                .Select(pair => pair.Value)
                .FirstOrDefault();
            return Task.FromResult(contract is null
                ? new PlanningContractReadResult(PlanningContractReadState.Missing)
                : new PlanningContractReadResult(PlanningContractReadState.Valid, contract));
        }

        public Task<PlanningContractRevisionListResult> ListRevisionsAsync(
            Guid projectId,
            Guid contractId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var contracts = _items
                .Where(pair => pair.Key.ProjectId == projectId && pair.Key.ContractId == contractId)
                .OrderBy(pair => pair.Key.Revision)
                .Select(pair => pair.Value)
                .ToArray();
            return Task.FromResult(new PlanningContractRevisionListResult(
                contracts.Length == 0 ? PlanningContractReadState.Missing : PlanningContractReadState.Valid,
                contracts));
        }
    }
}
