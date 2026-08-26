using System.Collections.ObjectModel;
using AIUsageMonitor.Application.Planning;

namespace AIUsageMonitor.Application.Orchestration;

public enum SchedulingBudgetState
{
    NotRequired,
    Available,
    Unavailable,
    Unknown,
    NotProvided
}

public enum SchedulingApprovalState
{
    NotRequired,
    Approved,
    RequiredButNotApproved,
    Unknown,
    NotProvided
}

/// <summary>Explicit, bounded caller-provided scheduling truth. It performs no work.</summary>
public sealed class SchedulingGateSnapshot
{
    public SchedulingGateSnapshot(
        int maxConcurrency,
        IReadOnlyCollection<Guid>? activeNodeIds = null,
        IReadOnlyDictionary<Guid, SchedulingBudgetState>? budgetStates = null,
        IReadOnlyDictionary<Guid, SchedulingApprovalState>? approvalStates = null)
    {
        if (maxConcurrency <= 0 || maxConcurrency > WorkGraphLimits.MaxConcurrency)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxConcurrency),
                $"Maximum concurrency must be between 1 and {WorkGraphLimits.MaxConcurrency}.");
        }

        activeNodeIds ??= Array.Empty<Guid>();
        if (activeNodeIds.Count > WorkGraphLimits.MaxActiveNodes)
        {
            throw new ArgumentException(
                $"Active node entries cannot exceed {WorkGraphLimits.MaxActiveNodes}.",
                nameof(activeNodeIds));
        }

        var active = activeNodeIds.ToArray();
        if (active.Any(static nodeId => nodeId == Guid.Empty) || active.Distinct().Count() != active.Length)
        {
            throw new ArgumentException("Active node identities must be non-empty and unique.", nameof(activeNodeIds));
        }

        MaxConcurrency = maxConcurrency;
        ActiveNodeIds = Array.AsReadOnly(active.OrderBy(static nodeId => nodeId).ToArray());
        BudgetStates = CopyStates(budgetStates, nameof(budgetStates));
        ApprovalStates = CopyStates(approvalStates, nameof(approvalStates));
    }

    public int MaxConcurrency { get; }

    public IReadOnlyList<Guid> ActiveNodeIds { get; }

    public IReadOnlyDictionary<Guid, SchedulingBudgetState> BudgetStates { get; }

    public IReadOnlyDictionary<Guid, SchedulingApprovalState> ApprovalStates { get; }

    private static IReadOnlyDictionary<Guid, TState> CopyStates<TState>(
        IReadOnlyDictionary<Guid, TState>? values,
        string parameterName)
        where TState : struct, Enum
    {
        if (values is null || values.Count == 0)
        {
            return new ReadOnlyDictionary<Guid, TState>(new Dictionary<Guid, TState>());
        }

        if (values.Count > WorkGraphLimits.MaxNodes)
        {
            throw new ArgumentException(
                $"Scheduling gate entries cannot exceed {WorkGraphLimits.MaxNodes}.",
                parameterName);
        }

        var copy = new Dictionary<Guid, TState>();
        foreach (var pair in values)
        {
            if (pair.Key == Guid.Empty || !Enum.IsDefined(pair.Value))
            {
                throw new ArgumentException("Scheduling gate entries must use defined graph identities and states.", parameterName);
            }

            copy.Add(pair.Key, pair.Value);
        }

        return new ReadOnlyDictionary<Guid, TState>(copy);
    }
}

/// <summary>A project- and graph-bound pure scheduling input.</summary>
public sealed class WorkGraphSchedulingRequest
{
    public WorkGraphSchedulingRequest(
        Guid projectId,
        WorkGraphReference graphReference,
        WorkGraph graph,
        IReadOnlyList<WorkGraphCompletionEvidence> completionEvidence,
        SchedulingGateSnapshot gates)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        GraphReference = graphReference ?? throw new ArgumentNullException(nameof(graphReference));
        Graph = graph ?? throw new ArgumentNullException(nameof(graph));
        ArgumentNullException.ThrowIfNull(completionEvidence);
        Gates = gates ?? throw new ArgumentNullException(nameof(gates));

        if (completionEvidence.Count > WorkGraphLimits.MaxNodes)
        {
            throw new ArgumentException(
                $"Completion evidence cannot exceed {WorkGraphLimits.MaxNodes} entries.",
                nameof(completionEvidence));
        }

        ProjectId = projectId;
        CompletionEvidence = Array.AsReadOnly(completionEvidence.ToArray());
    }

    public Guid ProjectId { get; }

    public WorkGraphReference GraphReference { get; }

    public WorkGraph Graph { get; }

    public IReadOnlyList<WorkGraphCompletionEvidence> CompletionEvidence { get; }

    public SchedulingGateSnapshot Gates { get; }
}

public enum WorkGraphDecisionState
{
    TerminalSucceeded,
    TerminalFailed,
    TerminalSkipped,
    Eligible,
    Blocked
}

public enum WorkGraphSelectionDisposition
{
    NotApplicable,
    Selected,
    AlreadyActive,
    CapacityBlocked
}

public enum WorkGraphBlockedReasonKind
{
    PrerequisiteIncomplete,
    PrerequisiteFailed,
    PrerequisiteSkipped,
    BudgetUnavailable,
    BudgetUnknown,
    ApprovalRequired,
    ApprovalUnknown
}

public sealed class WorkGraphBlockedReason
{
    public WorkGraphBlockedReason(
        WorkGraphBlockedReasonKind kind,
        Guid? prerequisiteNodeId = null)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentException("Blocked reason kind is undefined.", nameof(kind));
        }

        if (kind is WorkGraphBlockedReasonKind.PrerequisiteIncomplete or
            WorkGraphBlockedReasonKind.PrerequisiteFailed or
            WorkGraphBlockedReasonKind.PrerequisiteSkipped)
        {
            if (prerequisiteNodeId is null || prerequisiteNodeId == Guid.Empty)
            {
                throw new ArgumentException("Dependency blockers must identify a prerequisite node.", nameof(prerequisiteNodeId));
            }
        }
        else if (prerequisiteNodeId is not null)
        {
            throw new ArgumentException("Gate blockers cannot identify a prerequisite node.", nameof(prerequisiteNodeId));
        }

        Kind = kind;
        PrerequisiteNodeId = prerequisiteNodeId;
    }

    public WorkGraphBlockedReasonKind Kind { get; }

    public Guid? PrerequisiteNodeId { get; }
}

public sealed class WorkGraphNodeDecision
{
    internal WorkGraphNodeDecision(
        Guid nodeId,
        WorkGraphDecisionState state,
        WorkGraphSelectionDisposition selection,
        IReadOnlyList<WorkGraphBlockedReason> blockedReasons)
    {
        NodeId = nodeId;
        State = state;
        Selection = selection;
        BlockedReasons = blockedReasons;
    }

    public Guid NodeId { get; }

    public WorkGraphDecisionState State { get; }

    public WorkGraphSelectionDisposition Selection { get; }

    public IReadOnlyList<WorkGraphBlockedReason> BlockedReasons { get; }

    public bool IsSelected => Selection == WorkGraphSelectionDisposition.Selected;
}

public enum WorkGraphEvaluationStatus
{
    Valid,
    InvalidGraph,
    InvalidEvidence,
    InvalidSchedulingSnapshot,
    Unavailable
}

public sealed class WorkGraphEvaluationResult
{
    internal WorkGraphEvaluationResult(
        WorkGraphEvaluationStatus status,
        IReadOnlyList<WorkGraphNodeDecision> decisions,
        IReadOnlyList<Guid> selectedNodeIds,
        string? errorMessage = null)
    {
        Status = status;
        Decisions = decisions;
        SelectedNodeIds = selectedNodeIds;
        ErrorMessage = errorMessage;
    }

    public WorkGraphEvaluationStatus Status { get; }

    public bool IsValid => Status == WorkGraphEvaluationStatus.Valid;

    public IReadOnlyList<WorkGraphNodeDecision> Decisions { get; }

    public IReadOnlyList<Guid> SelectedNodeIds { get; }

    public string? ErrorMessage { get; }
}

/// <summary>
/// Pure dependency/gate evaluator. It never launches processes, calls providers or trackers, or
/// persists execution state.
/// </summary>
public sealed class WorkGraphScheduler
{
    public WorkGraphEvaluationResult Evaluate(WorkGraphSchedulingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var graph = request.Graph;
        if (request.ProjectId != graph.ProjectId || !SameReference(request.GraphReference, graph.Reference))
        {
            return Invalid(WorkGraphEvaluationStatus.InvalidGraph, "Scheduling input does not match graph authority.");
        }

        if (!string.Equals(WorkGraphIntegrity.ComputeContentHash(graph), graph.ContentHash, StringComparison.OrdinalIgnoreCase))
        {
            return Invalid(WorkGraphEvaluationStatus.InvalidGraph, "Graph content integrity validation failed.");
        }

        var nodesById = graph.Nodes.ToDictionary(static node => node.NodeId);
        var evidenceByNode = new Dictionary<Guid, WorkGraphCompletionEvidence>();
        foreach (var evidence in request.CompletionEvidence)
        {
            if (evidence is null ||
                evidence.ProjectId != request.ProjectId ||
                !SameReference(evidence.GraphReference, graph.Reference))
            {
                return Invalid(WorkGraphEvaluationStatus.InvalidEvidence, "Completion evidence does not belong to the requested graph.");
            }

            if (!nodesById.TryGetValue(evidence.NodeId, out var node) ||
                !SameReference(evidence.ContractReference, node.ContractReference))
            {
                return Invalid(WorkGraphEvaluationStatus.InvalidEvidence, "Completion evidence does not match a graph node contract.");
            }

            if (!evidenceByNode.TryAdd(evidence.NodeId, evidence))
            {
                return Invalid(WorkGraphEvaluationStatus.InvalidEvidence, "Conflicting terminal evidence exists for a graph node.");
            }
        }

        var activeNodeIds = request.Gates.ActiveNodeIds.ToHashSet();
        if (activeNodeIds.Any(nodeId => !nodesById.ContainsKey(nodeId)))
        {
            return Invalid(WorkGraphEvaluationStatus.InvalidSchedulingSnapshot, "Active node identities must belong to the requested graph.");
        }

        if (activeNodeIds.Any(nodeId => evidenceByNode.ContainsKey(nodeId)))
        {
            return Invalid(WorkGraphEvaluationStatus.InvalidSchedulingSnapshot, "Terminal nodes cannot also be active.");
        }

        if (request.Gates.BudgetStates.Keys.Any(nodeId => !nodesById.ContainsKey(nodeId)) ||
            request.Gates.ApprovalStates.Keys.Any(nodeId => !nodesById.ContainsKey(nodeId)))
        {
            return Invalid(WorkGraphEvaluationStatus.InvalidSchedulingSnapshot, "Scheduling gate identities must belong to the requested graph.");
        }

        var incoming = graph.Nodes.ToDictionary(static node => node.NodeId, static _ => new List<Guid>());
        foreach (var edge in graph.Edges)
        {
            incoming[edge.DependentNodeId].Add(edge.PrerequisiteNodeId);
        }

        foreach (var prerequisites in incoming.Values)
        {
            prerequisites.Sort();
        }

        var decisions = new Dictionary<Guid, WorkGraphNodeDecision>();
        foreach (var nodeId in graph.TopologicalOrder)
        {
            if (evidenceByNode.TryGetValue(nodeId, out var evidence))
            {
                decisions[nodeId] = new WorkGraphNodeDecision(
                    nodeId,
                    evidence.State switch
                    {
                        WorkGraphCompletionState.Succeeded => WorkGraphDecisionState.TerminalSucceeded,
                        WorkGraphCompletionState.Failed => WorkGraphDecisionState.TerminalFailed,
                        WorkGraphCompletionState.Skipped => WorkGraphDecisionState.TerminalSkipped,
                        _ => throw new InvalidOperationException("Completion state was not validated.")
                    },
                    WorkGraphSelectionDisposition.NotApplicable,
                    Array.Empty<WorkGraphBlockedReason>());
                continue;
            }

            var blockers = new List<WorkGraphBlockedReason>();
            foreach (var prerequisiteId in incoming[nodeId])
            {
                if (!evidenceByNode.TryGetValue(prerequisiteId, out var prerequisiteEvidence))
                {
                    blockers.Add(new WorkGraphBlockedReason(
                        WorkGraphBlockedReasonKind.PrerequisiteIncomplete,
                        prerequisiteId));
                    continue;
                }

                if (prerequisiteEvidence.State == WorkGraphCompletionState.Failed)
                {
                    blockers.Add(new WorkGraphBlockedReason(
                        WorkGraphBlockedReasonKind.PrerequisiteFailed,
                        prerequisiteId));
                }
                else if (prerequisiteEvidence.State == WorkGraphCompletionState.Skipped)
                {
                    blockers.Add(new WorkGraphBlockedReason(
                        WorkGraphBlockedReasonKind.PrerequisiteSkipped,
                        prerequisiteId));
                }
            }

            var budgetState = request.Gates.BudgetStates.TryGetValue(nodeId, out var suppliedBudget)
                ? suppliedBudget
                : SchedulingBudgetState.NotProvided;
            if (budgetState is SchedulingBudgetState.Unavailable)
            {
                blockers.Add(new WorkGraphBlockedReason(WorkGraphBlockedReasonKind.BudgetUnavailable));
            }
            else if (budgetState is SchedulingBudgetState.Unknown or SchedulingBudgetState.NotProvided)
            {
                blockers.Add(new WorkGraphBlockedReason(WorkGraphBlockedReasonKind.BudgetUnknown));
            }

            var approvalState = request.Gates.ApprovalStates.TryGetValue(nodeId, out var suppliedApproval)
                ? suppliedApproval
                : SchedulingApprovalState.NotProvided;
            if (approvalState == SchedulingApprovalState.RequiredButNotApproved)
            {
                blockers.Add(new WorkGraphBlockedReason(WorkGraphBlockedReasonKind.ApprovalRequired));
            }
            else if (approvalState is SchedulingApprovalState.Unknown or SchedulingApprovalState.NotProvided)
            {
                blockers.Add(new WorkGraphBlockedReason(WorkGraphBlockedReasonKind.ApprovalUnknown));
            }

            blockers = blockers
                .OrderBy(static reason => reason.Kind)
                .ThenBy(static reason => reason.PrerequisiteNodeId ?? Guid.Empty)
                .ToList();

            decisions[nodeId] = blockers.Count == 0
                ? new WorkGraphNodeDecision(
                    nodeId,
                    WorkGraphDecisionState.Eligible,
                    activeNodeIds.Contains(nodeId)
                        ? WorkGraphSelectionDisposition.AlreadyActive
                        : WorkGraphSelectionDisposition.NotApplicable,
                    Array.Empty<WorkGraphBlockedReason>())
                : new WorkGraphNodeDecision(
                    nodeId,
                    WorkGraphDecisionState.Blocked,
                    WorkGraphSelectionDisposition.NotApplicable,
                    blockers.AsReadOnly());
        }

        var remainingCapacity = request.Gates.MaxConcurrency - activeNodeIds.Count;
        var selected = 0;
        foreach (var nodeId in graph.TopologicalOrder)
        {
            var decision = decisions[nodeId];
            if (decision.State != WorkGraphDecisionState.Eligible ||
                decision.Selection == WorkGraphSelectionDisposition.AlreadyActive)
            {
                continue;
            }

            var selection = selected < remainingCapacity
                ? WorkGraphSelectionDisposition.Selected
                : WorkGraphSelectionDisposition.CapacityBlocked;
            if (selection == WorkGraphSelectionDisposition.Selected)
            {
                selected++;
            }

            decisions[nodeId] = new WorkGraphNodeDecision(
                nodeId,
                WorkGraphDecisionState.Eligible,
                selection,
                Array.Empty<WorkGraphBlockedReason>());
        }

        var orderedDecisions = graph.TopologicalOrder.Select(nodeId => decisions[nodeId]).ToArray();
        var selectedIds = orderedDecisions
            .Where(static decision => decision.IsSelected)
            .Select(static decision => decision.NodeId)
            .ToArray();
        return new WorkGraphEvaluationResult(
            WorkGraphEvaluationStatus.Valid,
            Array.AsReadOnly(orderedDecisions),
            Array.AsReadOnly(selectedIds));
    }

    private static WorkGraphEvaluationResult Invalid(WorkGraphEvaluationStatus status, string message) =>
        new(status, Array.Empty<WorkGraphNodeDecision>(), Array.Empty<Guid>(), message);

    private static bool SameReference(
        WorkGraphReference left,
        WorkGraphReference right) =>
        left.GraphId == right.GraphId &&
        left.SchemaVersion == right.SchemaVersion &&
        string.Equals(left.ContentHash, right.ContentHash, StringComparison.OrdinalIgnoreCase);

    private static bool SameReference(
        PlanningExecutionContractReference left,
        PlanningExecutionContractReference right) =>
        left.ContractId == right.ContractId &&
        left.Revision == right.Revision &&
        left.SchemaVersion == right.SchemaVersion &&
        string.Equals(left.ContentHash, right.ContentHash, StringComparison.OrdinalIgnoreCase);
}
