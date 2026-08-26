using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Projects;

namespace AIUsageMonitor.Connection.Tests;

public sealed class WorkGraphTests
{
    [Fact]
    public void EmptyProjectAndGraphIdsAreRejected()
    {
        var reference = ContractReference(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        Assert.Throws<ArgumentException>(() => new WorkGraph(
            Guid.Empty,
            Guid.NewGuid(),
            WorkGraphSchema.CurrentVersion,
            Now,
            [new WorkGraphNode(Guid.NewGuid(), reference)],
            []));
        Assert.Throws<ArgumentException>(() => new WorkGraph(
            ProjectId,
            Guid.Empty,
            WorkGraphSchema.CurrentVersion,
            Now,
            [new WorkGraphNode(Guid.NewGuid(), reference)],
            []));
    }

    [Fact]
    public void DuplicateNodeAndContractIdentityAreRejected()
    {
        var nodeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var contract = ContractReference(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        Assert.Throws<ArgumentException>(() => CreateGraph(
            [new WorkGraphNode(nodeId, contract), new WorkGraphNode(nodeId, ContractReference(Guid.NewGuid()))]));
        Assert.Throws<ArgumentException>(() => CreateGraph(
            [new WorkGraphNode(Guid.NewGuid(), contract), new WorkGraphNode(Guid.NewGuid(), contract)]));
        Assert.Throws<ArgumentException>(() => CreateGraph(
            [
                new WorkGraphNode(Guid.NewGuid(), contract),
                new WorkGraphNode(Guid.NewGuid(), ContractReference(contract.ContractId, revision: 2))
            ]));
    }

    [Fact]
    public void InvalidEdgesAreRejected()
    {
        var first = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var second = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var nodes = new[] {
            new WorkGraphNode(first, ContractReference(Guid.NewGuid())),
            new WorkGraphNode(second, ContractReference(Guid.NewGuid()))
        };

        Assert.Throws<ArgumentException>(() => { CreateGraph(nodes, [new WorkGraphEdge(Guid.NewGuid(), first, first)]); });
        Assert.Throws<ArgumentException>(() => { CreateGraph(nodes, [new WorkGraphEdge(Guid.NewGuid(), Guid.NewGuid(), second)]); });
        Assert.Throws<ArgumentException>(() => { CreateGraph(nodes, [new WorkGraphEdge(Guid.NewGuid(), first, Guid.NewGuid())]); });

        var edgeId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        Assert.Throws<ArgumentException>(() => { CreateGraph(
            nodes,
            [
                new WorkGraphEdge(edgeId, first, second),
                new WorkGraphEdge(edgeId, second, first)
            ]); });
        Assert.Throws<ArgumentException>(() => { CreateGraph(
            nodes,
            [
                new WorkGraphEdge(Guid.NewGuid(), first, second),
                new WorkGraphEdge(Guid.NewGuid(), first, second)
            ]); });
    }

    [Fact]
    public void DirectTwoNodeCycleIsRejected()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var nodes = Nodes(first, second);

        Assert.Throws<ArgumentException>(() => CreateGraph(
            nodes,
            [
                new WorkGraphEdge(Guid.NewGuid(), first, second),
                new WorkGraphEdge(Guid.NewGuid(), second, first)
            ]));
    }

    [Fact]
    public void MultiNodeAndDisconnectedCyclesAreRejected()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var third = Guid.NewGuid();
        var isolatedFirst = Guid.NewGuid();
        var isolatedSecond = Guid.NewGuid();
        var nodes = Nodes(first, second, third, isolatedFirst, isolatedSecond);

        Assert.Throws<ArgumentException>(() => CreateGraph(
            nodes,
            [
                new WorkGraphEdge(Guid.NewGuid(), first, second),
                new WorkGraphEdge(Guid.NewGuid(), second, third),
                new WorkGraphEdge(Guid.NewGuid(), third, first)
            ]));
        Assert.Throws<ArgumentException>(() => CreateGraph(
            nodes,
            [new WorkGraphEdge(Guid.NewGuid(), isolatedFirst, isolatedSecond), new WorkGraphEdge(Guid.NewGuid(), isolatedSecond, isolatedFirst)]));
    }

    [Fact]
    public void ValidDisconnectedDagHasCanonicalOrder()
    {
        var first = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var second = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var third = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var graph = CreateGraph(
            Nodes(first, second, third),
            [new WorkGraphEdge(Guid.NewGuid(), first, third)]);

        Assert.Equal([first, second, third], graph.TopologicalOrder);
        Assert.Equal([first, second, third], graph.Nodes.Select(static node => node.NodeId));
        Assert.Equal(64, graph.ContentHash.Length);
    }

    [Fact]
    public void EquivalentCollectionOrderingProducesEquivalentAuthorityAndHash()
    {
        var first = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var second = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var third = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var edgeOne = new WorkGraphEdge(Guid.Parse("10000000-0000-0000-0000-000000000001"), first, third);
        var edgeTwo = new WorkGraphEdge(Guid.Parse("10000000-0000-0000-0000-000000000002"), second, third);
        var nodes = Nodes(first, second, third);
        var left = CreateGraph(nodes, [edgeTwo, edgeOne]);
        var right = CreateGraph(nodes.Reverse().ToArray(), [edgeOne, edgeTwo]);

        Assert.Equal(left.ContentHash, right.ContentHash);
        Assert.Equal(left.Nodes.Select(static node => node.NodeId), right.Nodes.Select(static node => node.NodeId));
        Assert.Equal(left.Edges.Select(static edge => edge.EdgeId), right.Edges.Select(static edge => edge.EdgeId));
        Assert.Equal(left.TopologicalOrder, right.TopologicalOrder);
    }

    [Fact]
    public void GraphBoundsAreEnforced()
    {
        var nodes = Enumerable.Range(0, WorkGraphLimits.MaxNodes + 1)
            .Select(_ => new WorkGraphNode(Guid.NewGuid(), ContractReference(Guid.NewGuid())))
            .ToArray();

        Assert.Throws<ArgumentException>(() => CreateGraph(nodes));
    }

    [Fact]
    public void UnsupportedGraphSchemaIsRejectedBeforeAuthorityCreation()
    {
        Assert.Throws<ArgumentException>(() => new WorkGraph(
            ProjectId,
            Guid.NewGuid(),
            WorkGraphSchema.CurrentVersion + 1,
            Now,
            Nodes(Guid.NewGuid()),
            []));
    }

    [Fact]
    public void GraphReferenceIsIntegrityEvidenceNotAnAuthenticityClaim()
    {
        var graph = CreateGraph(Nodes(Guid.NewGuid()));

        Assert.Equal(graph.GraphId, graph.Reference.GraphId);
        Assert.Equal(graph.SchemaVersion, graph.Reference.SchemaVersion);
        Assert.Equal(graph.ContentHash, graph.Reference.ContentHash);
        Assert.Contains("sha256:", graph.Reference.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void CompletionEvidenceCalculatesAndValidatesDeterministicContentHash()
    {
        var graph = CreateGraph(Nodes(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")));
        var evidenceId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var recordedAt = new DateTimeOffset(2026, 8, 26, 12, 1, 0, TimeSpan.Zero);
        var evidence = new WorkGraphCompletionEvidence(
            evidenceId,
            graph.ProjectId,
            graph.Reference,
            graph.Nodes[0].NodeId,
            graph.Nodes[0].ContractReference,
            WorkGraphCompletionState.Failed,
            "evidence:failed",
            recordedAt);
        var equivalent = new WorkGraphCompletionEvidence(
            evidenceId,
            graph.ProjectId,
            graph.Reference,
            graph.Nodes[0].NodeId,
            graph.Nodes[0].ContractReference,
            WorkGraphCompletionState.Failed,
            "evidence:failed",
            recordedAt,
            evidence.ContentHash.ToUpperInvariant());

        Assert.Equal(64, evidence.ContentHash.Length);
        Assert.Equal(
            WorkGraphCompletionEvidenceIntegrity.ComputeContentHash(evidence),
            evidence.ContentHash);
        Assert.Equal(evidence.ContentHash, equivalent.ContentHash);
        Assert.Throws<ArgumentException>(() => new WorkGraphCompletionEvidence(
            evidenceId,
            graph.ProjectId,
            graph.Reference,
            graph.Nodes[0].NodeId,
            graph.Nodes[0].ContractReference,
            WorkGraphCompletionState.Failed,
            "evidence:failed",
            recordedAt,
            new string('b', 64)));
    }

    private static readonly Guid ProjectId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Now = new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

    internal static WorkGraph CreateGraph(
        IReadOnlyList<WorkGraphNode> nodes,
        IReadOnlyList<WorkGraphEdge>? edges = null,
        Guid? graphId = null,
        Guid? projectId = null) => new(
        projectId ?? ProjectId,
        graphId ?? Guid.Parse("33333333-3333-3333-3333-333333333333"),
        WorkGraphSchema.CurrentVersion,
        Now,
        nodes,
        edges ?? []);

    internal static WorkGraphNode[] Nodes(params Guid[] nodeIds) =>
        nodeIds.Select(nodeId => new WorkGraphNode(nodeId, ContractReference(Guid.NewGuid()))).ToArray();

    internal static PlanningExecutionContractReference ContractReference(
        Guid? contractId = null,
        int revision = 1,
        int schemaVersion = PlanningExecutionContractSchema.CurrentVersion,
        string? contentHash = null) => new(
        contractId ?? Guid.NewGuid(),
        revision,
        schemaVersion,
        contentHash ?? new string('a', 64));
}
