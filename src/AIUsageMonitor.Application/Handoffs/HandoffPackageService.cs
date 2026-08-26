using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Projects;

namespace AIUsageMonitor.Application.Handoffs;

/// <summary>
/// Resolves exact planner and graph authority, validates lifecycle lineage, and persists one
/// immutable structured handoff. It deliberately contains no model, prompt, execution, or review
/// engine behavior.
/// </summary>
public sealed class HandoffPackageService : IHandoffPackageService
{
    private readonly IProjectRepository _projects;
    private readonly IPlanningExecutionContractRepository _contracts;
    private readonly IWorkGraphRepository _graphs;
    private readonly IHandoffPackageRepository _packages;
    private readonly HandoffPackageBuilder _builder;

    public HandoffPackageService(
        IProjectRepository projects,
        IPlanningExecutionContractRepository contracts,
        IWorkGraphRepository graphs,
        IHandoffPackageRepository packages,
        IHandoffRedactionService redaction)
    {
        _projects = projects ?? throw new ArgumentNullException(nameof(projects));
        _contracts = contracts ?? throw new ArgumentNullException(nameof(contracts));
        _graphs = graphs ?? throw new ArgumentNullException(nameof(graphs));
        _packages = packages ?? throw new ArgumentNullException(nameof(packages));
        _builder = new HandoffPackageBuilder(redaction ?? throw new ArgumentNullException(nameof(redaction)));
    }

    public async Task<HandoffPackageCreationResult> CreateAsync(
        HandoffPackageCreationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Invalid("A handoff creation request is required.");
        }

        var requestValidation = ValidateRequest(request);
        if (requestValidation is not null)
        {
            return requestValidation;
        }

        try
        {
            var project = await _projects.GetByIdAsync(request.ProjectId, cancellationToken).ConfigureAwait(false);
            if (project is null)
            {
                return new(HandoffPackageCreationStatus.ProjectNotFound);
            }

            var contractReference = request.PlanningContractReference!;
            var contractRead = await _contracts.GetAsync(
                    request.ProjectId,
                    contractReference.ContractId,
                    contractReference.Revision,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!contractRead.IsValid || contractRead.Contract is null)
            {
                return MapContractFailure(contractRead);
            }

            var contract = contractRead.Contract;
            if (contract.ProjectId != request.ProjectId || !SameContractReference(contract.Reference, contractReference))
            {
                return new(
                    HandoffPackageCreationStatus.ContractMismatch,
                    ContractState: contractRead.State,
                    ErrorMessage: "The exact requested planning contract reference does not match the persisted contract.");
            }

            WorkGraph? graph = null;
            if (request.WorkGraphReference is not null)
            {
                var graphRead = await _graphs.GetAsync(
                        request.ProjectId,
                        request.WorkGraphReference.GraphId,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (!graphRead.IsValid || graphRead.Graph is null)
                {
                    return MapGraphFailure(graphRead);
                }

                graph = graphRead.Graph;
                var graphNode = graph.Nodes.FirstOrDefault(node => node.NodeId == request.WorkGraphNodeId!.Value);
                if (!SameWorkGraphReference(graph.Reference, request.WorkGraphReference) ||
                    graph.ProjectId != request.ProjectId ||
                    request.WorkGraphNodeId is null ||
                    graphNode is null ||
                    !SameContractReference(graphNode.ContractReference, contractReference))
                {
                    return new(
                        HandoffPackageCreationStatus.GraphNodeMismatch,
                        ContractState: contractRead.State,
                        GraphState: graphRead.State,
                        ErrorMessage: "The graph binding or graph node is not bound to the exact planning contract.");
                }
            }

            HandoffPackage? predecessor = null;
            if (request.PreviousPackageReference is not null)
            {
                var predecessorRead = await _packages.GetAsync(
                        request.ProjectId,
                        request.PreviousPackageReference.PackageId,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (!predecessorRead.IsValid || predecessorRead.Package is null)
                {
                    return MapPredecessorFailure(predecessorRead, contractRead.State, graph is null ? null : WorkGraphReadState.Valid);
                }

                predecessor = predecessorRead.Package;
                var lineageFailure = ValidatePredecessor(request, contractReference, predecessor);
                if (lineageFailure is not null)
                {
                    return new(
                        HandoffPackageCreationStatus.InvalidLineage,
                        ContractState: contractRead.State,
                        GraphState: graph is null ? null : WorkGraphReadState.Valid,
                        PredecessorState: predecessorRead.State,
                        ErrorMessage: lineageFailure);
                }
            }

            if (!HandoffTransitionPolicy.TryGetRoles(request.Transition, out var sourceRole, out var targetRole))
            {
                return new(HandoffPackageCreationStatus.UnsupportedTransition);
            }

            var build = _builder.Build(
                request,
                contract,
                sourceRole,
                targetRole,
                predecessor?.Reference,
                graph?.Reference,
                request.WorkGraphNodeId);

            if (build.Status != HandoffPackageBuildStatus.Created || build.Package is null)
            {
                return build.Status switch
                {
                    HandoffPackageBuildStatus.PackageTooLarge => new(
                        HandoffPackageCreationStatus.PackageTooLarge,
                        ContractState: contractRead.State,
                        GraphState: graph is null ? null : WorkGraphReadState.Valid,
                        PredecessorState: predecessor is null ? null : HandoffPackageReadState.Valid,
                        ErrorMessage: build.ErrorMessage),
                    HandoffPackageBuildStatus.RedactionRejected => new(
                        HandoffPackageCreationStatus.RedactionRejected,
                        ContractState: contractRead.State,
                        GraphState: graph is null ? null : WorkGraphReadState.Valid,
                        PredecessorState: predecessor is null ? null : HandoffPackageReadState.Valid,
                        ErrorMessage: build.ErrorMessage),
                    _ => new(
                        HandoffPackageCreationStatus.RequiredContextMissing,
                        ContractState: contractRead.State,
                        GraphState: graph is null ? null : WorkGraphReadState.Valid,
                        PredecessorState: predecessor is null ? null : HandoffPackageReadState.Valid,
                        ErrorMessage: build.ErrorMessage)
                };
            }

            var write = await _packages.CreateAsync(build.Package, cancellationToken).ConfigureAwait(false);
            return write.Status switch
            {
                HandoffPackageRepositoryWriteStatus.Created => new(
                    HandoffPackageCreationStatus.Created,
                    build.Package,
                    contractRead.State,
                    graph is null ? null : WorkGraphReadState.Valid,
                    predecessor is null ? null : HandoffPackageReadState.Valid),
                HandoffPackageRepositoryWriteStatus.PackageConflict => new(
                    HandoffPackageCreationStatus.PackageConflict,
                    ContractState: contractRead.State,
                    GraphState: graph is null ? null : WorkGraphReadState.Valid,
                    PredecessorState: predecessor is null ? null : HandoffPackageReadState.Valid,
                    ErrorMessage: write.ErrorMessage),
                _ => new(
                    HandoffPackageCreationStatus.PersistenceUnavailable,
                    ContractState: contractRead.State,
                    GraphState: graph is null ? null : WorkGraphReadState.Valid,
                    PredecessorState: predecessor is null ? null : HandoffPackageReadState.Valid,
                    ErrorMessage: write.ErrorMessage ?? "Handoff persistence is unavailable.")
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            return new(HandoffPackageCreationStatus.PersistenceUnavailable, ErrorMessage: "Handoff persistence is unavailable.");
        }
        catch (IOException)
        {
            return new(HandoffPackageCreationStatus.PersistenceUnavailable, ErrorMessage: "Handoff persistence is unavailable.");
        }
    }

    private static HandoffPackageCreationResult? ValidateRequest(HandoffPackageCreationRequest request)
    {
        if (request.ProjectId == Guid.Empty || request.PackageId == Guid.Empty || request.CreatedAt == default)
        {
            return Invalid("Project, package, and creation identifiers are required.");
        }

        if (!HandoffTransitionPolicy.TryGetRoles(request.Transition, out _, out _))
        {
            return new(HandoffPackageCreationStatus.UnsupportedTransition);
        }

        if (request.SchemaVersion != HandoffPackageSchema.CurrentVersion)
        {
            return Invalid("Only the current handoff package schema can be created.");
        }

        if (request.PlanningContractReference is null)
        {
            return new(HandoffPackageCreationStatus.RequiredContextMissing, ErrorMessage: "An exact planning contract reference is required.");
        }

        if ((request.WorkGraphReference is null) != (request.WorkGraphNodeId is null) ||
            request.WorkGraphNodeId == Guid.Empty)
        {
            return Invalid("A graph binding requires both a graph reference and a non-empty node id.");
        }

        if (request.Transition == HandoffTransition.PlannerToExecutor && request.PreviousPackageReference is not null)
        {
            return new(HandoffPackageCreationStatus.InvalidLineage, ErrorMessage: "Planner-to-executor is the root handoff and cannot have a predecessor.");
        }

        if (HandoffTransitionPolicy.RequiresPredecessor(request.Transition) && request.PreviousPackageReference is null)
        {
            return new(HandoffPackageCreationStatus.PredecessorMissing, ErrorMessage: "This transition requires an exact predecessor package reference.");
        }

        if (request.EvidenceReferences.Count > HandoffPackageLimits.MaxEvidenceReferences ||
            request.FindingReferences.Count > HandoffPackageLimits.MaxFindingReferences ||
            request.ChangedArtifactReferences.Count > HandoffPackageLimits.MaxChangedArtifactReferences ||
            request.Limitations.Count > HandoffPackageLimits.MaxLimitations ||
            request.EvidenceReferences.Any(static value => value is null) ||
            request.FindingReferences.Any(static value => value is null) ||
            request.ChangedArtifactReferences.Any(static value => value is null) ||
            request.Limitations.Any(static value => value is null))
        {
            return Invalid("The handoff request contains invalid or excessive bounded collections.");
        }

        if (request.NextAction is not null && request.NextAction.Length > HandoffPackageLimits.MaxTextLength)
        {
            return Invalid("The next action exceeds its bounded length.");
        }

        return null;
    }

    private static HandoffPackageCreationResult MapContractFailure(PlanningContractReadResult read) =>
        read.State switch
        {
            PlanningContractReadState.Missing => new(HandoffPackageCreationStatus.ContractMissing, ContractState: read.State, ErrorMessage: read.ErrorMessage),
            PlanningContractReadState.Unavailable => new(HandoffPackageCreationStatus.PersistenceUnavailable, ContractState: read.State, ErrorMessage: read.ErrorMessage),
            _ => new(HandoffPackageCreationStatus.ContractInvalid, ContractState: read.State, ErrorMessage: read.ErrorMessage)
        };

    private static HandoffPackageCreationResult MapGraphFailure(WorkGraphReadResult read) =>
        read.State switch
        {
            WorkGraphReadState.Missing => new(HandoffPackageCreationStatus.GraphMissing, GraphState: read.State, ErrorMessage: read.ErrorMessage),
            WorkGraphReadState.Unavailable => new(HandoffPackageCreationStatus.PersistenceUnavailable, GraphState: read.State, ErrorMessage: read.ErrorMessage),
            _ => new(HandoffPackageCreationStatus.GraphInvalid, GraphState: read.State, ErrorMessage: read.ErrorMessage)
        };

    private static HandoffPackageCreationResult MapPredecessorFailure(
        HandoffPackageReadResult read,
        PlanningContractReadState contractState,
        WorkGraphReadState? graphState) =>
        read.State switch
        {
            HandoffPackageReadState.Missing => new(HandoffPackageCreationStatus.PredecessorMissing, ContractState: contractState, GraphState: graphState, PredecessorState: read.State, ErrorMessage: read.ErrorMessage),
            HandoffPackageReadState.Unavailable => new(HandoffPackageCreationStatus.PersistenceUnavailable, ContractState: contractState, GraphState: graphState, PredecessorState: read.State, ErrorMessage: read.ErrorMessage),
            _ => new(HandoffPackageCreationStatus.PredecessorInvalid, ContractState: contractState, GraphState: graphState, PredecessorState: read.State, ErrorMessage: read.ErrorMessage)
        };

    private static HandoffPackageCreationResult Invalid(string message) =>
        new(HandoffPackageCreationStatus.InvalidRequest, ErrorMessage: message);

    private static string? ValidatePredecessor(
        HandoffPackageCreationRequest request,
        PlanningExecutionContractReference contractReference,
        HandoffPackage predecessor)
    {
        if (predecessor.ProjectId != request.ProjectId || predecessor.PackageId == request.PackageId ||
            request.PreviousPackageReference is null ||
            !SamePackageReference(predecessor.Reference, request.PreviousPackageReference))
        {
            return "The predecessor reference does not match the exact persisted predecessor package.";
        }

        if (!HandoffTransitionPolicy.IsAllowedPredecessor(predecessor.Transition, request.Transition))
        {
            return "The predecessor transition is not an allowed lifecycle predecessor.";
        }

        if (request.Transition == HandoffTransition.AcceptanceToPlanner)
        {
            if (predecessor.PlanningContractReference.ContractId != contractReference.ContractId ||
                predecessor.PlanningContractReference.SchemaVersion != contractReference.SchemaVersion ||
                contractReference.Revision < predecessor.PlanningContractReference.Revision ||
                contractReference.Revision > predecessor.PlanningContractReference.Revision + 1)
            {
                return "Acceptance-to-planner must retain the contract identity and use the same or immediate next revision.";
            }
        }
        else if (!SameContractReference(predecessor.PlanningContractReference, contractReference))
        {
            return "The handoff lineage must remain bound to the exact planning contract revision.";
        }

        if (request.Transition == HandoffTransition.RemediationToReviewer)
        {
            var predecessorFindingIds = predecessor.FindingReferences
                .Select(static finding => finding.FindingId)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray();
            var requestFindingIds = request.FindingReferences
                .Select(static finding => finding.FindingId)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray();
            if (!predecessorFindingIds.SequenceEqual(requestFindingIds, StringComparer.Ordinal))
            {
                return "Remediation-to-reviewer must carry the exact predecessor finding identities.";
            }
        }

        return null;
    }

    private static bool SameContractReference(
        PlanningExecutionContractReference left,
        PlanningExecutionContractReference right) =>
        left.ContractId == right.ContractId &&
        left.Revision == right.Revision &&
        left.SchemaVersion == right.SchemaVersion &&
        string.Equals(left.ContentHash, right.ContentHash, StringComparison.OrdinalIgnoreCase);

    private static bool SameWorkGraphReference(WorkGraphReference left, WorkGraphReference right) =>
        left.GraphId == right.GraphId &&
        left.SchemaVersion == right.SchemaVersion &&
        string.Equals(left.ContentHash, right.ContentHash, StringComparison.OrdinalIgnoreCase);

    private static bool SamePackageReference(HandoffPackageReference left, HandoffPackageReference right) =>
        left.PackageId == right.PackageId &&
        left.SchemaVersion == right.SchemaVersion &&
        string.Equals(left.ContentHash, right.ContentHash, StringComparison.OrdinalIgnoreCase);
}
