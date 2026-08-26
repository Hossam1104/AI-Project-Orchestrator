using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Planning;

namespace AIUsageMonitor.Connection.Tests;

public sealed class WorkGraphEvidenceServiceTests
{
    [Theory]
    [InlineData(WorkGraphCompletionState.Succeeded)]
    [InlineData(WorkGraphCompletionState.Failed)]
    [InlineData(WorkGraphCompletionState.Skipped)]
    public async Task ValidTerminalEvidenceIsAccepted(WorkGraphCompletionState state)
    {
        var fixture = Fixture.Create();

        var result = await fixture.Service.RecordAsync(fixture.Evidence(state));

        Assert.Equal(WorkGraphEvidenceRecordStatus.Created, result.Status);
        Assert.Single(fixture.EvidenceRepository.Created);
    }

    [Fact]
    public async Task ForeignProjectEvidenceIsRejectedBeforeWrite()
    {
        var fixture = Fixture.Create();
        var evidence = fixture.Evidence(WorkGraphCompletionState.Succeeded, projectId: Guid.NewGuid());

        var result = await fixture.Service.RecordAsync(evidence);

        Assert.Equal(WorkGraphEvidenceRecordStatus.ProjectMismatch, result.Status);
        Assert.Empty(fixture.EvidenceRepository.Created);
    }

    [Fact]
    public async Task ForeignGraphEvidenceIsRejectedBeforeWrite()
    {
        var fixture = Fixture.Create();
        var foreignReference = new WorkGraphReference(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            fixture.Graph.SchemaVersion,
            fixture.Graph.ContentHash);
        var evidence = fixture.Evidence(
            WorkGraphCompletionState.Succeeded,
            graphReference: foreignReference);

        var result = await fixture.Service.RecordAsync(evidence);

        Assert.Equal(WorkGraphEvidenceRecordStatus.GraphReferenceMismatch, result.Status);
        Assert.Empty(fixture.EvidenceRepository.Created);
    }

    [Fact]
    public async Task UnknownNodeAndMismatchingContractAreRejectedBeforeWrite()
    {
        var fixture = Fixture.Create();
        var unknownNode = fixture.Evidence(
            WorkGraphCompletionState.Succeeded,
            nodeId: Guid.NewGuid());
        var unknownNodeResult = await fixture.Service.RecordAsync(unknownNode);
        Assert.Equal(WorkGraphEvidenceRecordStatus.NodeNotFound, unknownNodeResult.Status);

        var mismatchingContract = fixture.Evidence(
            WorkGraphCompletionState.Succeeded,
            contractReference: new PlanningExecutionContractReference(
                Guid.NewGuid(),
                1,
                PlanningExecutionContractSchema.CurrentVersion,
                new string('c', 64)));
        var contractResult = await fixture.Service.RecordAsync(mismatchingContract);
        Assert.Equal(WorkGraphEvidenceRecordStatus.ContractReferenceMismatch, contractResult.Status);
        Assert.Empty(fixture.EvidenceRepository.Created);
    }

    [Fact]
    public async Task MissingOrInvalidGraphFailsClosed()
    {
        var missing = Fixture.Create();
        missing.GraphRepository.NextRead = new WorkGraphReadResult(WorkGraphReadState.Missing);
        Assert.Equal(
            WorkGraphEvidenceRecordStatus.GraphMissing,
            (await missing.Service.RecordAsync(missing.Evidence(WorkGraphCompletionState.Succeeded))).Status);

        var invalid = Fixture.Create();
        invalid.GraphRepository.NextRead = new WorkGraphReadResult(WorkGraphReadState.IntegrityFailure);
        Assert.Equal(
            WorkGraphEvidenceRecordStatus.GraphInvalid,
            (await invalid.Service.RecordAsync(invalid.Evidence(WorkGraphCompletionState.Succeeded))).Status);
    }

    [Fact]
    public async Task RepositoryCreateOnceOutcomesAreMappedWithoutOverwritingTruth()
    {
        var alreadyRecorded = Fixture.Create();
        alreadyRecorded.EvidenceRepository.NextWrite = new WorkGraphCompletionEvidenceWriteResult(
            WorkGraphCompletionEvidenceWriteStatus.AlreadyRecorded);
        var duplicateResult = await alreadyRecorded.Service.RecordAsync(
            alreadyRecorded.Evidence(WorkGraphCompletionState.Succeeded));
        Assert.Equal(WorkGraphEvidenceRecordStatus.AlreadyRecorded, duplicateResult.Status);

        var conflict = Fixture.Create();
        conflict.EvidenceRepository.NextWrite = new WorkGraphCompletionEvidenceWriteResult(
            WorkGraphCompletionEvidenceWriteStatus.Conflict);
        var conflictResult = await conflict.Service.RecordAsync(
            conflict.Evidence(WorkGraphCompletionState.Failed));
        Assert.Equal(WorkGraphEvidenceRecordStatus.Conflict, conflictResult.Status);
        Assert.Empty(conflict.EvidenceRepository.Created);
    }

    [Fact]
    public async Task CancellationPropagatesBeforeEvidenceWrite()
    {
        var fixture = Fixture.Create();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Service.RecordAsync(
            fixture.Evidence(WorkGraphCompletionState.Succeeded),
            cancellation.Token));
        Assert.Empty(fixture.EvidenceRepository.Created);
    }

    private sealed class Fixture
    {
        private Fixture(WorkGraph graph)
        {
            Graph = graph;
            GraphRepository = new(graph);
            EvidenceRepository = new();
            Service = new WorkGraphCompletionEvidenceService(GraphRepository, EvidenceRepository);
        }

        public WorkGraph Graph { get; }
        public FakeGraphRepository GraphRepository { get; }
        public FakeEvidenceRepository EvidenceRepository { get; }
        public WorkGraphCompletionEvidenceService Service { get; }

        public static Fixture Create() => new(new WorkGraph(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            WorkGraphSchema.CurrentVersion,
            new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero),
            [new WorkGraphNode(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                new PlanningExecutionContractReference(
                    Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    1,
                    PlanningExecutionContractSchema.CurrentVersion,
                    new string('a', 64)))],
            []));

        public WorkGraphCompletionEvidence Evidence(
            WorkGraphCompletionState state,
            Guid? projectId = null,
            WorkGraphReference? graphReference = null,
            Guid? nodeId = null,
            PlanningExecutionContractReference? contractReference = null) => new(
            Guid.NewGuid(),
            projectId ?? Graph.ProjectId,
            graphReference ?? Graph.Reference,
            nodeId ?? Graph.Nodes[0].NodeId,
            contractReference ?? Graph.Nodes[0].ContractReference,
            state,
            $"evidence:{state}",
            new DateTimeOffset(2026, 8, 26, 12, 1, 0, TimeSpan.Zero));
    }

    private sealed class FakeGraphRepository(WorkGraph graph) : IWorkGraphRepository
    {
        public WorkGraphReadResult? NextRead { get; set; }

        public Task<WorkGraphRepositoryWriteResult> CreateAsync(
            WorkGraph value,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorkGraphRepositoryWriteResult(WorkGraphRepositoryWriteStatus.Created));

        public Task<WorkGraphReadResult> GetAsync(
            Guid projectId,
            Guid graphId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (NextRead is not null)
            {
                var result = NextRead;
                NextRead = null;
                return Task.FromResult(result);
            }

            return Task.FromResult(new WorkGraphReadResult(WorkGraphReadState.Valid, graph));
        }
    }

    private sealed class FakeEvidenceRepository : IWorkGraphCompletionEvidenceRepository
    {
        public WorkGraphCompletionEvidenceWriteResult? NextWrite { get; set; }
        public List<WorkGraphCompletionEvidence> Created { get; } = [];

        public Task<WorkGraphCompletionEvidenceWriteResult> CreateAsync(
            WorkGraphCompletionEvidence evidence,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (NextWrite is not null)
            {
                var result = NextWrite;
                NextWrite = null;
                return Task.FromResult(result);
            }

            Created.Add(evidence);
            return Task.FromResult(new WorkGraphCompletionEvidenceWriteResult(
                WorkGraphCompletionEvidenceWriteStatus.Created));
        }

        public Task<WorkGraphCompletionEvidenceReadResult> ReadForGraphAsync(
            Guid projectId,
            WorkGraphReference graphReference,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorkGraphCompletionEvidenceReadResult(
                WorkGraphCompletionEvidenceReadState.Missing,
                []));
    }
}
