using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Projects;

namespace AIUsageMonitor.Connection.Tests;

public sealed class WorkGraphServiceTests
{
    [Fact]
    public async Task ExactContractReferenceIsResolvedBeforeGraphCreation()
    {
        var fixture = Fixture.Create();

        var result = await fixture.Service.CreateAsync(fixture.Request());

        Assert.Equal(WorkGraphCreationStatus.Created, result.Status);
        Assert.NotNull(result.Graph);
        Assert.Single(fixture.Graphs.Created);
        Assert.Equal(fixture.Contract.Reference.ContentHash, result.Graph!.Nodes[0].ContractReference.ContentHash);
    }

    [Fact]
    public async Task MissingContractIsRejectedWithoutPersistingGraph()
    {
        var fixture = Fixture.Create();
        fixture.Contracts.NextRead = new PlanningContractReadResult(PlanningContractReadState.Missing);

        var result = await fixture.Service.CreateAsync(fixture.Request());

        Assert.Equal(WorkGraphCreationStatus.ContractMissing, result.Status);
        Assert.Empty(fixture.Graphs.Created);
    }

    [Fact]
    public async Task InvalidContractReadStateIsRejectedWithoutPersistingGraph()
    {
        var fixture = Fixture.Create();
        fixture.Contracts.NextRead = new PlanningContractReadResult(PlanningContractReadState.Invalid);

        var result = await fixture.Service.CreateAsync(fixture.Request());

        Assert.Equal(WorkGraphCreationStatus.ContractInvalid, result.Status);
        Assert.Equal(PlanningContractReadState.Invalid, result.ContractState);
        Assert.Empty(fixture.Graphs.Created);
    }

    [Fact]
    public async Task ContractProjectMismatchIsRejected()
    {
        var fixture = Fixture.Create();
        fixture.Contracts.NextRead = new PlanningContractReadResult(
            PlanningContractReadState.Valid,
            Fixture.CreateContract(Guid.NewGuid(), fixture.Contract.ContractId));

        var result = await fixture.Service.CreateAsync(fixture.Request());

        Assert.Equal(WorkGraphCreationStatus.ContractProjectMismatch, result.Status);
        Assert.Empty(fixture.Graphs.Created);
    }

    [Fact]
    public async Task ContractIdentityRevisionSchemaAndHashMustMatchExactly()
    {
        var fixture = Fixture.Create();

        fixture.Contracts.NextRead = new PlanningContractReadResult(
            PlanningContractReadState.Valid,
            Fixture.CreateContract(fixture.Project.Id, Guid.NewGuid()));
        Assert.Equal(WorkGraphCreationStatus.ContractIdentityMismatch, (await fixture.Service.CreateAsync(fixture.Request())).Status);

        fixture = Fixture.Create();
        fixture.Contracts.NextRead = new PlanningContractReadResult(
            PlanningContractReadState.Valid,
            Fixture.CreateContract(
                fixture.Project.Id,
                fixture.Contract.ContractId,
                revision: 2,
                previousRevision: 1,
                previousContentHash: fixture.Contract.ContentHash));
        Assert.Equal(WorkGraphCreationStatus.ContractRevisionMismatch, (await fixture.Service.CreateAsync(fixture.Request())).Status);

        fixture = Fixture.Create();
        fixture.Contracts.NextRead = new PlanningContractReadResult(
            PlanningContractReadState.Valid,
            Fixture.CreateContract(fixture.Project.Id, fixture.Contract.ContractId, schemaVersion: 2));
        Assert.Equal(WorkGraphCreationStatus.ContractSchemaMismatch, (await fixture.Service.CreateAsync(fixture.Request())).Status);

        fixture = Fixture.Create();
        fixture.Contracts.NextRead = new PlanningContractReadResult(
            PlanningContractReadState.Valid,
            fixture.Contract);
        var hashMismatch = fixture.Request(
            new PlanningExecutionContractReference(
                fixture.Contract.ContractId,
                fixture.Contract.Revision,
                fixture.Contract.SchemaVersion,
                new string('b', 64)));
        Assert.Equal(WorkGraphCreationStatus.ContractHashMismatch, (await fixture.Service.CreateAsync(hashMismatch)).Status);
    }

    [Fact]
    public async Task RequestedRevisionIsUsedWithoutLatestOrEarlierFallback()
    {
        var fixture = Fixture.Create();
        fixture.Contracts.NextRead = new PlanningContractReadResult(PlanningContractReadState.Missing);

        var result = await fixture.Service.CreateAsync(fixture.Request());

        Assert.Equal(WorkGraphCreationStatus.ContractMissing, result.Status);
        Assert.Equal([(fixture.Project.Id, fixture.Contract.ContractId, fixture.Contract.Revision)], fixture.Contracts.GetCalls);
        Assert.Equal(0, fixture.Contracts.LatestCalls);
        Assert.Empty(fixture.Graphs.Created);
    }

    [Fact]
    public async Task InvalidGraphIsRejectedBeforeContractResolutionOrPersistence()
    {
        var fixture = Fixture.Create();
        var node = fixture.Request().Nodes[0];
        var invalid = new WorkGraphCreationRequest(
            fixture.Project.Id,
            Guid.NewGuid(),
            WorkGraphSchema.CurrentVersion,
            Fixture.Now,
            [node, new WorkGraphNode(node.NodeId, fixture.Contract.Reference)],
            []);

        var result = await fixture.Service.CreateAsync(invalid);

        Assert.Equal(WorkGraphCreationStatus.InvalidGraph, result.Status);
        Assert.Empty(fixture.Contracts.GetCalls);
        Assert.Empty(fixture.Graphs.Created);
    }

    [Fact]
    public async Task CancellationPropagatesAndDoesNotCreateGraph()
    {
        var fixture = Fixture.Create();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Service.CreateAsync(fixture.Request(), cancellation.Token));
        Assert.Empty(fixture.Graphs.Created);
    }

    private sealed class Fixture
    {
        private Fixture(Project project, PlanningExecutionContract contract)
        {
            Project = project;
            Contract = contract;
            Contracts = new FakeContractRepository(contract);
            Graphs = new FakeGraphRepository();
            Service = new WorkGraphService(new FakeProjectRepository(project), Contracts, Graphs);
        }

        public static readonly DateTimeOffset Now = new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

        public Project Project { get; }
        public PlanningExecutionContract Contract { get; }
        public FakeContractRepository Contracts { get; }
        public FakeGraphRepository Graphs { get; }
        public WorkGraphService Service { get; }

        public static Fixture Create()
        {
            var project = new Project(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "APO test project",
                @"C:\APO-Test",
                null,
                ProjectStatus.Active,
                Now,
                Now);
            return new(project, CreateContract(project.Id, Guid.Parse("22222222-2222-2222-2222-222222222222")));
        }

        public static PlanningExecutionContract CreateContract(
            Guid projectId,
            Guid contractId,
            int revision = 1,
            int schemaVersion = PlanningExecutionContractSchema.CurrentVersion,
            int? previousRevision = null,
            string? previousContentHash = null) => new(
            projectId,
            contractId,
            schemaVersion,
            revision,
            Now,
            "owner",
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            new PlanningContextBinding(Guid.Parse("44444444-4444-4444-4444-444444444444"), 1),
            new PlanningWorkItem(PlanningWorkItemSource.Jira, "APO-41", "Work graph"),
            new PlanningRepositoryTarget(PlanningRepositoryMode.None),
            [new PlanningScopeClause("include", "graph")],
            [],
            [new PlanningScopeClause("forbid", "execution")],
            [new PlanningDeliverable("deliverable", "graph", true)],
            [new PlanningValidationRequirement("test", PlanningValidationKind.Test, "tests", true)],
            [new PlanningAcceptanceCriterion("accept", "valid", true)],
            [new PlanningExecutionBudget(PlanningBudgetKind.Attempts, 1)],
            [
                new PlanningStopCondition("stop-target", PlanningStopConditionKind.ImmutableTargetMoved, "target"),
                new PlanningStopCondition("stop-scope", PlanningStopConditionKind.ScopeViolation, "scope"),
                new PlanningStopCondition("stop-budget", PlanningStopConditionKind.BudgetExceeded, "budget")
            ],
            [],
            null,
            null,
            previousRevision,
            previousContentHash);

        public WorkGraphCreationRequest Request(PlanningExecutionContractReference? reference = null)
        {
            var node = new WorkGraphNode(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                reference ?? Contract.Reference);
            return new(
                Project.Id,
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                WorkGraphSchema.CurrentVersion,
                Now,
                [node],
                []);
        }
    }

    private sealed class FakeProjectRepository(Project project) : IProjectRepository
    {
        public Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Project>>([project]);

        public Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Project?>(projectId == project.Id ? project : null);

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
            return Task.FromResult(new PlanningContractReadResult(PlanningContractReadState.Valid, contract));
        }

        public Task<PlanningContractRevisionListResult> ListRevisionsAsync(Guid projectId, Guid contractId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PlanningContractRevisionListResult(PlanningContractReadState.Valid, [contract]));
    }

    private sealed class FakeGraphRepository : IWorkGraphRepository
    {
        public List<WorkGraph> Created { get; } = [];

        public Task<WorkGraphRepositoryWriteResult> CreateAsync(WorkGraph graph, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Created.Add(graph);
            return Task.FromResult(new WorkGraphRepositoryWriteResult(WorkGraphRepositoryWriteStatus.Created));
        }

        public Task<WorkGraphReadResult> GetAsync(Guid projectId, Guid graphId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorkGraphReadResult(WorkGraphReadState.Missing));
    }
}
