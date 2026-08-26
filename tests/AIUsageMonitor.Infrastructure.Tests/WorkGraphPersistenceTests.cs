using System.Text.Json;
using System.Text.Json.Nodes;
using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageMonitor.Infrastructure.Tests;

public sealed class WorkGraphPersistenceTests
{
    [Fact]
    public async Task GraphRoundTripsIntoProjectAndGraphGuidScopedPath()
    {
        using var store = new TemporaryStore();
        var repository = CreateGraphRepository(store);
        var graph = CreateGraph(ProjectA);

        var write = await repository.CreateAsync(graph);
        var read = await repository.GetAsync(graph.ProjectId, graph.GraphId);

        Assert.Equal(WorkGraphRepositoryWriteStatus.Created, write.Status);
        Assert.Equal(WorkGraphReadState.Valid, read.State);
        Assert.Equal(graph.ContentHash, read.Graph!.ContentHash);
        Assert.Equal(graph.TopologicalOrder, read.Graph.TopologicalOrder);

        var path = store.Paths.GetWorkGraphFile(graph.ProjectId, graph.GraphId);
        Assert.True(File.Exists(path));
        Assert.Contains(
            $"projects{Path.DirectorySeparatorChar}{ProjectA:D}{Path.DirectorySeparatorChar}work-graphs{Path.DirectorySeparatorChar}{graph.GraphId:D}",
            path,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExistingGraphCannotBeOverwrittenAndBytesRemainUnchanged()
    {
        using var store = new TemporaryStore();
        var repository = CreateGraphRepository(store);
        var graph = CreateGraph(ProjectA);
        var path = store.Paths.GetWorkGraphFile(graph.ProjectId, graph.GraphId);

        Assert.Equal(WorkGraphRepositoryWriteStatus.Created, (await repository.CreateAsync(graph)).Status);
        var originalBytes = await File.ReadAllBytesAsync(path);

        var conflict = await repository.CreateAsync(graph);
        var afterBytes = await File.ReadAllBytesAsync(path);

        Assert.Equal(WorkGraphRepositoryWriteStatus.GraphConflict, conflict.Status);
        Assert.Equal(originalBytes, afterBytes);
    }

    [Fact]
    public async Task GraphReadIsProjectIsolated()
    {
        using var store = new TemporaryStore();
        var repository = CreateGraphRepository(store);
        var graph = CreateGraph(ProjectB);

        Assert.Equal(WorkGraphRepositoryWriteStatus.Created, (await repository.CreateAsync(graph)).Status);

        var readFromOtherProject = await repository.GetAsync(ProjectA, graph.GraphId);

        Assert.Equal(WorkGraphReadState.Missing, readFromOtherProject.State);
        Assert.False(File.Exists(store.Paths.GetWorkGraphFile(ProjectA, graph.GraphId)));
    }

    [Fact]
    public async Task MissingGraphIsReportedWithoutCreatingStorage()
    {
        using var store = new TemporaryStore();
        var repository = CreateGraphRepository(store);
        var graphId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var read = await repository.GetAsync(ProjectA, graphId);

        Assert.Equal(WorkGraphReadState.Missing, read.State);
        Assert.False(Directory.Exists(store.Paths.GetWorkGraphDirectory(ProjectA, graphId)));
    }

    [Fact]
    public async Task CorruptGraphReadIsObservationalAndByteForByteStable()
    {
        using var store = new TemporaryStore();
        var repository = CreateGraphRepository(store);
        var graph = CreateGraph(ProjectA);
        var path = store.Paths.GetWorkGraphFile(graph.ProjectId, graph.GraphId);
        await repository.CreateAsync(graph);
        await File.WriteAllTextAsync(path, "{");
        var originalBytes = await File.ReadAllBytesAsync(path);

        var first = await repository.GetAsync(graph.ProjectId, graph.GraphId);
        var second = await repository.GetAsync(graph.ProjectId, graph.GraphId);

        Assert.Equal(WorkGraphReadState.Invalid, first.State);
        Assert.Equal(WorkGraphReadState.Invalid, second.State);
        Assert.Equal(originalBytes, await File.ReadAllBytesAsync(path));
        Assert.Empty(Directory.EnumerateFiles(Path.GetDirectoryName(path)!, "*.bak"));
    }

    [Fact]
    public async Task MissingPayloadAndUnsupportedGraphSchemaAreTypedAndPreserved()
    {
        using var store = new TemporaryStore();
        var repository = CreateGraphRepository(store);
        var graph = CreateGraph(ProjectA);
        var path = store.Paths.GetWorkGraphFile(graph.ProjectId, graph.GraphId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await File.WriteAllTextAsync(path, "{\"schemaVersion\":1}");
        var missingPayloadBytes = await File.ReadAllBytesAsync(path);
        var missingPayload = await repository.GetAsync(graph.ProjectId, graph.GraphId);
        Assert.Equal(WorkGraphReadState.Invalid, missingPayload.State);
        Assert.Equal(missingPayloadBytes, await File.ReadAllBytesAsync(path));

        await File.WriteAllTextAsync(path, "{\"schemaVersion\":999,\"payload\":{}}");
        var futureBytes = await File.ReadAllBytesAsync(path);
        var future = await repository.GetAsync(graph.ProjectId, graph.GraphId);
        Assert.Equal(WorkGraphReadState.UnsupportedFutureVersion, future.State);
        Assert.Equal(futureBytes, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    public async Task GraphHashTamperingIsIntegrityFailureAndIsNotRepaired()
    {
        using var store = new TemporaryStore();
        var repository = CreateGraphRepository(store);
        var graph = CreateGraph(ProjectA);
        var path = store.Paths.GetWorkGraphFile(graph.ProjectId, graph.GraphId);
        await repository.CreateAsync(graph);
        ReplacePayload(path, payload => payload["contentHash"] = new string('b', 64));
        var originalBytes = await File.ReadAllBytesAsync(path);

        var read = await repository.GetAsync(graph.ProjectId, graph.GraphId);

        Assert.Equal(WorkGraphReadState.IntegrityFailure, read.State);
        Assert.Equal(originalBytes, await File.ReadAllBytesAsync(path));
        Assert.Empty(Directory.EnumerateFiles(Path.GetDirectoryName(path)!, "*.bak"));
    }

    [Fact]
    public async Task CancelledGraphCreationDoesNotLeavePartialAuthority()
    {
        using var store = new TemporaryStore();
        var repository = CreateGraphRepository(store);
        var graph = CreateGraph(ProjectA);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => repository.CreateAsync(graph, cancellation.Token));

        Assert.False(File.Exists(store.Paths.GetWorkGraphFile(graph.ProjectId, graph.GraphId)));
    }

    [Fact]
    public async Task CompletionEvidenceRoundTripsAndUsesCreateOnceTerminalTruth()
    {
        using var store = new TemporaryStore();
        var repository = CreateEvidenceRepository(store);
        var graph = CreateGraph(ProjectA);
        var evidence = CreateEvidence(graph, WorkGraphCompletionState.Succeeded);
        var path = store.Paths.GetWorkGraphCompletionEvidenceFile(
            graph.ProjectId,
            graph.GraphId,
            evidence.NodeId);

        var write = await repository.CreateAsync(evidence);
        var originalBytes = await File.ReadAllBytesAsync(path);
        var duplicate = await repository.CreateAsync(evidence);
        var conflict = await repository.CreateAsync(CreateEvidence(graph, WorkGraphCompletionState.Failed));
        var read = await repository.ReadForGraphAsync(graph.ProjectId, graph.Reference);

        Assert.Equal(WorkGraphCompletionEvidenceWriteStatus.Created, write.Status);
        Assert.Equal(WorkGraphCompletionEvidenceWriteStatus.AlreadyRecorded, duplicate.Status);
        Assert.Equal(WorkGraphCompletionEvidenceWriteStatus.Conflict, conflict.Status);
        Assert.Equal(originalBytes, await File.ReadAllBytesAsync(path));
        Assert.Equal(WorkGraphCompletionEvidenceReadState.Valid, read.State);
        Assert.Equal(WorkGraphCompletionState.Succeeded, Assert.Single(read.Evidence).State);
        Assert.Contains(
            $"work-graphs{Path.DirectorySeparatorChar}{graph.GraphId:D}{Path.DirectorySeparatorChar}completion-evidence",
            path,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompletionEvidenceReadIsObservationalForCorruptAndFutureRecords()
    {
        using var store = new TemporaryStore();
        var repository = CreateEvidenceRepository(store);
        var graph = CreateGraph(ProjectA);
        var evidence = CreateEvidence(graph, WorkGraphCompletionState.Skipped);
        var path = store.Paths.GetWorkGraphCompletionEvidenceFile(
            graph.ProjectId,
            graph.GraphId,
            evidence.NodeId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await File.WriteAllTextAsync(path, "{");
        var corruptBytes = await File.ReadAllBytesAsync(path);
        var corrupt = await repository.ReadForGraphAsync(graph.ProjectId, graph.Reference);
        var corruptAgain = await repository.ReadForGraphAsync(graph.ProjectId, graph.Reference);
        Assert.Equal(WorkGraphCompletionEvidenceReadState.Invalid, corrupt.State);
        Assert.Equal(WorkGraphCompletionEvidenceReadState.Invalid, corruptAgain.State);
        Assert.Equal(corruptBytes, await File.ReadAllBytesAsync(path));
        Assert.Empty(Directory.EnumerateFiles(Path.GetDirectoryName(path)!, "*.bak"));

        await File.WriteAllTextAsync(path, "{\"schemaVersion\":999,\"payload\":{}}");
        var futureBytes = await File.ReadAllBytesAsync(path);
        var future = await repository.ReadForGraphAsync(graph.ProjectId, graph.Reference);
        Assert.Equal(WorkGraphCompletionEvidenceReadState.UnsupportedFutureVersion, future.State);
        Assert.Equal(futureBytes, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    public async Task CompletionEvidenceWithFutureInnerGraphSchemaIsNotAccepted()
    {
        using var store = new TemporaryStore();
        var repository = CreateEvidenceRepository(store);
        var graph = CreateGraph(ProjectA);
        var evidence = CreateEvidence(graph, WorkGraphCompletionState.Succeeded);
        var path = store.Paths.GetWorkGraphCompletionEvidenceFile(
            graph.ProjectId,
            graph.GraphId,
            evidence.NodeId);
        await repository.CreateAsync(evidence);
        ReplacePayload(path, payload => payload["graphSchemaVersion"] = WorkGraphSchema.CurrentVersion + 1);
        var originalBytes = await File.ReadAllBytesAsync(path);

        var read = await repository.ReadForGraphAsync(graph.ProjectId, graph.Reference);

        Assert.Equal(WorkGraphCompletionEvidenceReadState.UnsupportedFutureVersion, read.State);
        Assert.Equal(originalBytes, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    public async Task CancelledCompletionEvidenceCreationDoesNotLeavePartialTruth()
    {
        using var store = new TemporaryStore();
        var repository = CreateEvidenceRepository(store);
        var graph = CreateGraph(ProjectA);
        var evidence = CreateEvidence(graph, WorkGraphCompletionState.Succeeded);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => repository.CreateAsync(evidence, cancellation.Token));

        Assert.False(File.Exists(store.Paths.GetWorkGraphCompletionEvidenceFile(
            graph.ProjectId,
            graph.GraphId,
            evidence.NodeId)));
    }

    [Fact]
    public async Task CompletionEvidenceReadRejectsMismatchingGraphReferenceAndOtherProjectIsIsolated()
    {
        using var store = new TemporaryStore();
        var repository = CreateEvidenceRepository(store);
        var graph = CreateGraph(ProjectB);
        var evidence = CreateEvidence(graph, WorkGraphCompletionState.Succeeded);
        await repository.CreateAsync(evidence);

        var wrongHashReference = new WorkGraphReference(
            graph.GraphId,
            graph.SchemaVersion,
            new string('c', 64));
        var mismatching = await repository.ReadForGraphAsync(ProjectB, wrongHashReference);
        var otherProject = await repository.ReadForGraphAsync(ProjectA, graph.Reference);

        Assert.Equal(WorkGraphCompletionEvidenceReadState.Invalid, mismatching.State);
        Assert.Equal(WorkGraphCompletionEvidenceReadState.Missing, otherProject.State);
    }

    private static JsonWorkGraphRepository CreateGraphRepository(TemporaryStore store) =>
        new(store.Paths, store.Files, NullLogger<JsonWorkGraphRepository>.Instance);

    private static JsonWorkGraphCompletionEvidenceRepository CreateEvidenceRepository(TemporaryStore store) =>
        new(store.Paths, store.Files, NullLogger<JsonWorkGraphCompletionEvidenceRepository>.Instance);

    private static WorkGraph CreateGraph(Guid projectId) => new(
        projectId,
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
        []);

    private static WorkGraphCompletionEvidence CreateEvidence(
        WorkGraph graph,
        WorkGraphCompletionState state) => new(
        Guid.NewGuid(),
        graph.ProjectId,
        graph.Reference,
        graph.Nodes[0].NodeId,
        graph.Nodes[0].ContractReference,
        state,
        $"evidence:{state}",
        new DateTimeOffset(2026, 8, 26, 12, 1, 0, TimeSpan.Zero));

    private static void ReplacePayload(string path, Action<JsonObject> update)
    {
        var root = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        update(root["payload"]!.AsObject());
        File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static readonly Guid ProjectA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProjectB = Guid.Parse("22222222-2222-2222-2222-222222222222");
}
