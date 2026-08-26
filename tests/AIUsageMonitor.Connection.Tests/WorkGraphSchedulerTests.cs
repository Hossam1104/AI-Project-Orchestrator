using AIUsageMonitor.Application.Orchestration;

namespace AIUsageMonitor.Connection.Tests;

public sealed class WorkGraphSchedulerTests
{
    [Fact]
    public void NoPrerequisiteIsEligibleAndSelectedWhenGatesAllow()
    {
        var node = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var graph = WorkGraphTests.CreateGraph(WorkGraphTests.Nodes(node));

        var result = Evaluate(graph, gates: Gates(1, node));

        Assert.True(result.IsValid);
        var decision = Assert.Single(result.Decisions);
        Assert.Equal(WorkGraphDecisionState.Eligible, decision.State);
        Assert.Equal(WorkGraphSelectionDisposition.Selected, decision.Selection);
        Assert.Equal([node], result.SelectedNodeIds);
    }

    [Fact]
    public void MissingPrerequisiteEvidenceIsIncompleteAndBlocksDependent()
    {
        var prerequisite = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var dependent = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var graph = WorkGraphTests.CreateGraph(
            WorkGraphTests.Nodes(prerequisite, dependent),
            [new WorkGraphEdge(Guid.NewGuid(), prerequisite, dependent)]);

        var result = Evaluate(graph, gates: Gates(2, prerequisite, dependent));

        var dependentDecision = result.Decisions.Single(decision => decision.NodeId == dependent);
        Assert.Equal(WorkGraphDecisionState.Blocked, dependentDecision.State);
        var reason = Assert.Single(dependentDecision.BlockedReasons);
        Assert.Equal(WorkGraphBlockedReasonKind.PrerequisiteIncomplete, reason.Kind);
        Assert.Equal(prerequisite, reason.PrerequisiteNodeId);
    }

    [Fact]
    public void SucceededPrerequisiteSatisfiesDependency()
    {
        var (graph, prerequisite, dependent) = DependentGraph();
        var evidence = Evidence(graph, prerequisite, WorkGraphCompletionState.Succeeded);

        var result = Evaluate(graph, [evidence], Gates(1, prerequisite, dependent));

        Assert.Equal(WorkGraphDecisionState.TerminalSucceeded, result.Decisions.Single(value => value.NodeId == prerequisite).State);
        Assert.Equal(WorkGraphDecisionState.Eligible, result.Decisions.Single(value => value.NodeId == dependent).State);
        Assert.Equal([dependent], result.SelectedNodeIds);
    }

    [Theory]
    [InlineData(WorkGraphCompletionState.Failed, WorkGraphBlockedReasonKind.PrerequisiteFailed, WorkGraphDecisionState.TerminalFailed)]
    [InlineData(WorkGraphCompletionState.Skipped, WorkGraphBlockedReasonKind.PrerequisiteSkipped, WorkGraphDecisionState.TerminalSkipped)]
    public void FailedOrSkippedPrerequisiteBlocksDependent(
        WorkGraphCompletionState state,
        WorkGraphBlockedReasonKind reasonKind,
        WorkGraphDecisionState terminalState)
    {
        var (graph, prerequisite, dependent) = DependentGraph();
        var result = Evaluate(
            graph,
            [Evidence(graph, prerequisite, state)],
            Gates(2, prerequisite, dependent));

        Assert.Equal(terminalState, result.Decisions.Single(value => value.NodeId == prerequisite).State);
        var dependentDecision = result.Decisions.Single(value => value.NodeId == dependent);
        Assert.Equal(WorkGraphDecisionState.Blocked, dependentDecision.State);
        Assert.Equal(reasonKind, Assert.Single(dependentDecision.BlockedReasons).Kind);
    }

    [Fact]
    public void MultiplePrerequisitesReturnAllBlockingReasonsInStableOrder()
    {
        var first = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var second = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var dependent = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var graph = WorkGraphTests.CreateGraph(
            WorkGraphTests.Nodes(first, second, dependent),
            [
                new WorkGraphEdge(Guid.NewGuid(), second, dependent),
                new WorkGraphEdge(Guid.NewGuid(), first, dependent)
            ]);
        var evidence = new[] { Evidence(graph, first, WorkGraphCompletionState.Failed) };

        var result = Evaluate(graph, evidence, Gates(3, first, second, dependent));

        var reasons = result.Decisions.Single(value => value.NodeId == dependent).BlockedReasons;
        Assert.Equal(
            [
                (WorkGraphBlockedReasonKind.PrerequisiteIncomplete, second),
                (WorkGraphBlockedReasonKind.PrerequisiteFailed, first)
            ],
            reasons.Select(reason => (reason.Kind, reason.PrerequisiteNodeId!.Value)));
    }

    [Theory]
    [InlineData(SchedulingBudgetState.Available, null)]
    [InlineData(SchedulingBudgetState.Unavailable, WorkGraphBlockedReasonKind.BudgetUnavailable)]
    [InlineData(SchedulingBudgetState.Unknown, WorkGraphBlockedReasonKind.BudgetUnknown)]
    [InlineData(SchedulingBudgetState.NotProvided, WorkGraphBlockedReasonKind.BudgetUnknown)]
    public void BudgetGateIsTypedAndFailClosed(SchedulingBudgetState state, WorkGraphBlockedReasonKind? expectedReason)
    {
        var node = Guid.NewGuid();
        var graph = WorkGraphTests.CreateGraph(WorkGraphTests.Nodes(node));
        var gates = new SchedulingGateSnapshot(
            1,
            budgetStates: new Dictionary<Guid, SchedulingBudgetState> { [node] = state },
            approvalStates: new Dictionary<Guid, SchedulingApprovalState> { [node] = SchedulingApprovalState.NotRequired });

        var result = Evaluate(graph, gates: gates);
        var decision = Assert.Single(result.Decisions);
        if (expectedReason is null)
        {
            Assert.Equal(WorkGraphDecisionState.Eligible, decision.State);
        }
        else
        {
            Assert.Equal(WorkGraphDecisionState.Blocked, decision.State);
            Assert.Equal(expectedReason, Assert.Single(decision.BlockedReasons).Kind);
        }
    }

    [Theory]
    [InlineData(SchedulingApprovalState.NotRequired, null)]
    [InlineData(SchedulingApprovalState.Approved, null)]
    [InlineData(SchedulingApprovalState.RequiredButNotApproved, WorkGraphBlockedReasonKind.ApprovalRequired)]
    [InlineData(SchedulingApprovalState.Unknown, WorkGraphBlockedReasonKind.ApprovalUnknown)]
    [InlineData(SchedulingApprovalState.NotProvided, WorkGraphBlockedReasonKind.ApprovalUnknown)]
    public void ApprovalGateIsTypedAndFailClosed(SchedulingApprovalState state, WorkGraphBlockedReasonKind? expectedReason)
    {
        var node = Guid.NewGuid();
        var graph = WorkGraphTests.CreateGraph(WorkGraphTests.Nodes(node));
        var gates = new SchedulingGateSnapshot(
            1,
            budgetStates: new Dictionary<Guid, SchedulingBudgetState> { [node] = SchedulingBudgetState.NotRequired },
            approvalStates: new Dictionary<Guid, SchedulingApprovalState> { [node] = state });

        var result = Evaluate(graph, gates: gates);
        var decision = Assert.Single(result.Decisions);
        Assert.Equal(expectedReason is null ? WorkGraphDecisionState.Eligible : WorkGraphDecisionState.Blocked, decision.State);
        if (expectedReason is not null)
        {
            Assert.Equal(expectedReason, Assert.Single(decision.BlockedReasons).Kind);
        }
    }

    [Fact]
    public void DependencyAndGateBlockersAreBothReturned()
    {
        var (graph, prerequisite, dependent) = DependentGraph();
        var result = Evaluate(
            graph,
            gates: new SchedulingGateSnapshot(
                1,
                budgetStates: new Dictionary<Guid, SchedulingBudgetState> { [dependent] = SchedulingBudgetState.Unavailable },
                approvalStates: new Dictionary<Guid, SchedulingApprovalState> { [dependent] = SchedulingApprovalState.RequiredButNotApproved }));

        var reasons = result.Decisions.Single(value => value.NodeId == dependent).BlockedReasons;
        Assert.Equal(
            [
                WorkGraphBlockedReasonKind.PrerequisiteIncomplete,
                WorkGraphBlockedReasonKind.BudgetUnavailable,
                WorkGraphBlockedReasonKind.ApprovalRequired
            ],
            reasons.Select(static reason => reason.Kind));
        Assert.Equal(prerequisite, reasons[0].PrerequisiteNodeId);
    }

    [Fact]
    public void CapacitySelectionUsesCanonicalTopologicalIdentityOrder()
    {
        var first = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var second = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var third = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var graph = WorkGraphTests.CreateGraph(WorkGraphTests.Nodes(first, second, third));

        var result = Evaluate(graph, gates: Gates(1, first, second, third));

        Assert.Equal([first], result.SelectedNodeIds);
        Assert.Equal(WorkGraphSelectionDisposition.CapacityBlocked, result.Decisions.Single(value => value.NodeId == second).Selection);
        Assert.Equal(WorkGraphSelectionDisposition.CapacityBlocked, result.Decisions.Single(value => value.NodeId == third).Selection);
    }

    [Fact]
    public void ActiveNodesReduceCapacityAndAreNotSelectedAgain()
    {
        var first = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var second = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var graph = WorkGraphTests.CreateGraph(WorkGraphTests.Nodes(first, second));

        var result = Evaluate(graph, gates: Gates(2, new[] { first, second }, new[] { first }));

        Assert.Equal([second], result.SelectedNodeIds);
        Assert.Equal(WorkGraphSelectionDisposition.AlreadyActive, result.Decisions.Single(value => value.NodeId == first).Selection);
    }

    [Fact]
    public void FullCapacitySelectsNoneButLeavesEligibleNodesExplainable()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var graph = WorkGraphTests.CreateGraph(WorkGraphTests.Nodes(first, second));

        var result = Evaluate(graph, gates: Gates(1, new[] { first, second }, new[] { first }));

        Assert.Empty(result.SelectedNodeIds);
        Assert.All(result.Decisions, decision => Assert.Equal(WorkGraphDecisionState.Eligible, decision.State));
        Assert.Equal(WorkGraphSelectionDisposition.CapacityBlocked, result.Decisions.Single(value => value.NodeId == second).Selection);
    }

    [Fact]
    public void ForeignActiveNodeIsRejected()
    {
        var node = Guid.NewGuid();
        var graph = WorkGraphTests.CreateGraph(WorkGraphTests.Nodes(node));
        var result = Evaluate(graph, gates: Gates(1, new[] { node }, new[] { Guid.NewGuid() }));

        Assert.Equal(WorkGraphEvaluationStatus.InvalidSchedulingSnapshot, result.Status);
    }

    [Fact]
    public void TerminalNodeCannotAlsoBeActive()
    {
        var node = Guid.NewGuid();
        var graph = WorkGraphTests.CreateGraph(WorkGraphTests.Nodes(node));
        var result = Evaluate(
            graph,
            [Evidence(graph, node, WorkGraphCompletionState.Succeeded)],
            Gates(1, new[] { node }, new[] { node }));

        Assert.Equal(WorkGraphEvaluationStatus.InvalidSchedulingSnapshot, result.Status);
    }

    [Fact]
    public void ProjectOrGraphReferenceMismatchFailsClosed()
    {
        var node = Guid.NewGuid();
        var graph = WorkGraphTests.CreateGraph(WorkGraphTests.Nodes(node));

        var projectMismatch = Evaluate(
            graph,
            projectId: Guid.NewGuid(),
            gates: Gates(1, node));
        Assert.Equal(WorkGraphEvaluationStatus.InvalidGraph, projectMismatch.Status);

        var referenceMismatch = new WorkGraphReference(graph.GraphId, graph.SchemaVersion, new string('b', 64));
        var request = new WorkGraphSchedulingRequest(
            graph.ProjectId,
            referenceMismatch,
            graph,
            [],
            Gates(1, node));
        Assert.Equal(WorkGraphEvaluationStatus.InvalidGraph, new WorkGraphScheduler().Evaluate(request).Status);
    }

    [Fact]
    public void ForeignOrDuplicateEvidenceFailsClosed()
    {
        var node = Guid.NewGuid();
        var graph = WorkGraphTests.CreateGraph(WorkGraphTests.Nodes(node));
        var foreign = new WorkGraphCompletionEvidence(
            Guid.NewGuid(),
            Guid.NewGuid(),
            graph.Reference,
            node,
            graph.Nodes[0].ContractReference,
            WorkGraphCompletionState.Succeeded,
            "evidence:foreign",
            DateTimeOffset.UtcNow);

        var result = Evaluate(graph, [foreign], Gates(1, node));
        Assert.Equal(WorkGraphEvaluationStatus.InvalidEvidence, result.Status);

        var duplicate = Evidence(graph, node, WorkGraphCompletionState.Succeeded);
        result = Evaluate(graph, [duplicate, duplicate], Gates(1, node));
        Assert.Equal(WorkGraphEvaluationStatus.InvalidEvidence, result.Status);
    }

    private static WorkGraphEvaluationResult Evaluate(
        WorkGraph graph,
        IReadOnlyList<WorkGraphCompletionEvidence>? evidence = null,
        SchedulingGateSnapshot? gates = null,
        Guid? projectId = null) =>
        new WorkGraphScheduler().Evaluate(new WorkGraphSchedulingRequest(
            projectId ?? graph.ProjectId,
            graph.Reference,
            graph,
            evidence ?? [],
            gates ?? Gates(1, graph.Nodes.Select(static node => node.NodeId).ToArray())));

    private static SchedulingGateSnapshot Gates(int maxConcurrency, params Guid[] nodeIds) =>
        Gates(maxConcurrency, nodeIds, active: []);

    private static SchedulingGateSnapshot Gates(int maxConcurrency, Guid[] nodeIds, Guid[] active) =>
        new(
            maxConcurrency,
            active,
            nodeIds.ToDictionary(static nodeId => nodeId, static _ => SchedulingBudgetState.NotRequired),
            nodeIds.ToDictionary(static nodeId => nodeId, static _ => SchedulingApprovalState.NotRequired));

    private static (WorkGraph Graph, Guid Prerequisite, Guid Dependent) DependentGraph()
    {
        var prerequisite = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var dependent = Guid.Parse("00000000-0000-0000-0000-000000000002");
        return (
            WorkGraphTests.CreateGraph(
                WorkGraphTests.Nodes(prerequisite, dependent),
                [new WorkGraphEdge(Guid.NewGuid(), prerequisite, dependent)]),
            prerequisite,
            dependent);
    }

    private static WorkGraphCompletionEvidence Evidence(
        WorkGraph graph,
        Guid nodeId,
        WorkGraphCompletionState state) => new(
        Guid.NewGuid(),
        graph.ProjectId,
        graph.Reference,
        nodeId,
        graph.Nodes.Single(node => node.NodeId == nodeId).ContractReference,
        state,
        $"evidence:{nodeId:D}",
        new DateTimeOffset(2026, 8, 26, 12, 1, 0, TimeSpan.Zero));
}
