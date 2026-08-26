using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Projects;

namespace AIUsageMonitor.Application.Orchestration;

public sealed class WorkGraphCreationRequest
{
    public WorkGraphCreationRequest(
        Guid projectId,
        Guid graphId,
        int schemaVersion,
        DateTimeOffset createdAt,
        IReadOnlyList<WorkGraphNode> nodes,
        IReadOnlyList<WorkGraphEdge> edges)
    {
        ProjectId = projectId;
        GraphId = graphId;
        SchemaVersion = schemaVersion;
        CreatedAt = createdAt;
        Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        Edges = edges ?? throw new ArgumentNullException(nameof(edges));
    }

    public Guid ProjectId { get; }
    public Guid GraphId { get; }
    public int SchemaVersion { get; }
    public DateTimeOffset CreatedAt { get; }
    public IReadOnlyList<WorkGraphNode> Nodes { get; }
    public IReadOnlyList<WorkGraphEdge> Edges { get; }
}

public enum WorkGraphCreationStatus
{
    Created,
    ProjectNotFound,
    InvalidGraph,
    ContractMissing,
    ContractInvalid,
    ContractProjectMismatch,
    ContractIdentityMismatch,
    ContractRevisionMismatch,
    ContractSchemaMismatch,
    ContractHashMismatch,
    GraphConflict,
    PersistenceUnavailable
}

public sealed record WorkGraphCreationResult(
    WorkGraphCreationStatus Status,
    WorkGraph? Graph = null,
    Guid? NodeId = null,
    PlanningContractReadState? ContractState = null,
    string? ErrorMessage = null)
{
    public bool Succeeded => Status == WorkGraphCreationStatus.Created && Graph is not null;
}

/// <summary>
/// Resolves every node's exact APO-40 reference before creating immutable graph authority.
/// </summary>
public interface IWorkGraphService
{
    Task<WorkGraphCreationResult> CreateAsync(
        WorkGraphCreationRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class WorkGraphService : IWorkGraphService
{
    private readonly IProjectRepository _projects;
    private readonly IPlanningExecutionContractRepository _contracts;
    private readonly IWorkGraphRepository _graphs;

    public WorkGraphService(
        IProjectRepository projects,
        IPlanningExecutionContractRepository contracts,
        IWorkGraphRepository graphs)
    {
        _projects = projects ?? throw new ArgumentNullException(nameof(projects));
        _contracts = contracts ?? throw new ArgumentNullException(nameof(contracts));
        _graphs = graphs ?? throw new ArgumentNullException(nameof(graphs));
    }

    public async Task<WorkGraphCreationResult> CreateAsync(
        WorkGraphCreationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var project = await _projects.GetByIdAsync(request.ProjectId, cancellationToken).ConfigureAwait(false);
            if (project is null)
            {
                return new(WorkGraphCreationStatus.ProjectNotFound, ErrorMessage: "Project was not found.");
            }

            WorkGraph graph;
            try
            {
                graph = new WorkGraph(
                    request.ProjectId,
                    request.GraphId,
                    request.SchemaVersion,
                    request.CreatedAt,
                    request.Nodes,
                    request.Edges);
            }
            catch (ArgumentException exception)
            {
                return new(WorkGraphCreationStatus.InvalidGraph, ErrorMessage: exception.Message);
            }

            foreach (var node in graph.Nodes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var reference = node.ContractReference;
                var read = await _contracts.GetAsync(
                        graph.ProjectId,
                        reference.ContractId,
                        reference.Revision,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (read.State == PlanningContractReadState.Missing)
                {
                    return new(
                        WorkGraphCreationStatus.ContractMissing,
                        NodeId: node.NodeId,
                        ContractState: read.State,
                        ErrorMessage: "The referenced planning contract revision is missing.");
                }

                if (!read.IsValid || read.Contract is null)
                {
                    return new(
                        WorkGraphCreationStatus.ContractInvalid,
                        NodeId: node.NodeId,
                        ContractState: read.State,
                        ErrorMessage: "The referenced planning contract revision is not valid.");
                }

                var contract = read.Contract;
                if (contract.ProjectId != graph.ProjectId)
                {
                    return new(
                        WorkGraphCreationStatus.ContractProjectMismatch,
                        NodeId: node.NodeId,
                        ContractState: read.State,
                        ErrorMessage: "The planning contract belongs to another project.");
                }

                if (contract.ContractId != reference.ContractId)
                {
                    return new(
                        WorkGraphCreationStatus.ContractIdentityMismatch,
                        NodeId: node.NodeId,
                        ContractState: read.State,
                        ErrorMessage: "The planning contract id does not match the requested reference.");
                }

                if (contract.Revision != reference.Revision)
                {
                    return new(
                        WorkGraphCreationStatus.ContractRevisionMismatch,
                        NodeId: node.NodeId,
                        ContractState: read.State,
                        ErrorMessage: "The planning contract revision does not match the requested reference.");
                }

                if (contract.SchemaVersion != reference.SchemaVersion)
                {
                    return new(
                        WorkGraphCreationStatus.ContractSchemaMismatch,
                        NodeId: node.NodeId,
                        ContractState: read.State,
                        ErrorMessage: "The planning contract schema does not match the requested reference.");
                }

                if (!string.Equals(contract.ContentHash, reference.ContentHash, StringComparison.OrdinalIgnoreCase))
                {
                    return new(
                        WorkGraphCreationStatus.ContractHashMismatch,
                        NodeId: node.NodeId,
                        ContractState: read.State,
                        ErrorMessage: "The planning contract content hash does not match the requested reference.");
                }
            }

            var write = await _graphs.CreateAsync(graph, cancellationToken).ConfigureAwait(false);
            return write.Status switch
            {
                WorkGraphRepositoryWriteStatus.Created => new(WorkGraphCreationStatus.Created, graph),
                WorkGraphRepositoryWriteStatus.GraphConflict => new(
                    WorkGraphCreationStatus.GraphConflict,
                    ErrorMessage: write.ErrorMessage ?? "The immutable graph already exists."),
                _ => new(
                    WorkGraphCreationStatus.PersistenceUnavailable,
                    ErrorMessage: write.ErrorMessage ?? "Graph persistence is unavailable.")
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            return new(WorkGraphCreationStatus.PersistenceUnavailable, ErrorMessage: "Graph persistence is unavailable.");
        }
        catch (IOException)
        {
            return new(WorkGraphCreationStatus.PersistenceUnavailable, ErrorMessage: "Graph persistence is unavailable.");
        }
    }
}

public enum WorkGraphEvidenceRecordStatus
{
    Created,
    AlreadyRecorded,
    Conflict,
    GraphMissing,
    GraphInvalid,
    NodeNotFound,
    ProjectMismatch,
    GraphReferenceMismatch,
    ContractReferenceMismatch,
    PersistenceUnavailable
}

public sealed record WorkGraphEvidenceRecordResult(
    WorkGraphEvidenceRecordStatus Status,
    string? ErrorMessage = null)
{
    public bool Succeeded => Status is
        WorkGraphEvidenceRecordStatus.Created or
        WorkGraphEvidenceRecordStatus.AlreadyRecorded;
}

/// <summary>Validates evidence belonging before handing it to the create-once repository.</summary>
public interface IWorkGraphCompletionEvidenceService
{
    Task<WorkGraphEvidenceRecordResult> RecordAsync(
        WorkGraphCompletionEvidence evidence,
        CancellationToken cancellationToken = default);
}

public sealed class WorkGraphCompletionEvidenceService : IWorkGraphCompletionEvidenceService
{
    private readonly IWorkGraphRepository _graphs;
    private readonly IWorkGraphCompletionEvidenceRepository _evidence;

    public WorkGraphCompletionEvidenceService(
        IWorkGraphRepository graphs,
        IWorkGraphCompletionEvidenceRepository evidence)
    {
        _graphs = graphs ?? throw new ArgumentNullException(nameof(graphs));
        _evidence = evidence ?? throw new ArgumentNullException(nameof(evidence));
    }

    public async Task<WorkGraphEvidenceRecordResult> RecordAsync(
        WorkGraphCompletionEvidence evidence,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        var graphRead = await _graphs.GetAsync(
                evidence.ProjectId,
                evidence.GraphReference.GraphId,
                cancellationToken)
            .ConfigureAwait(false);
        if (graphRead.State == WorkGraphReadState.Missing)
        {
            return new(WorkGraphEvidenceRecordStatus.GraphMissing, "The work graph was not found.");
        }

        if (!graphRead.IsValid || graphRead.Graph is null)
        {
            return new(WorkGraphEvidenceRecordStatus.GraphInvalid, "The work graph is not valid.");
        }

        var graph = graphRead.Graph;
        if (graph.ProjectId != evidence.ProjectId)
        {
            return new(WorkGraphEvidenceRecordStatus.ProjectMismatch, "The graph belongs to another project.");
        }

        if (!SameGraphReference(evidence.GraphReference, graph.Reference))
        {
            return new(WorkGraphEvidenceRecordStatus.GraphReferenceMismatch, "The graph integrity reference does not match durable authority.");
        }

        var node = graph.Nodes.FirstOrDefault(candidate => candidate.NodeId == evidence.NodeId);
        if (node is null)
        {
            return new(WorkGraphEvidenceRecordStatus.NodeNotFound, "The node does not belong to the work graph.");
        }

        if (!SameContractReference(evidence.ContractReference, node.ContractReference))
        {
            return new(WorkGraphEvidenceRecordStatus.ContractReferenceMismatch, "The completion evidence contract does not match the graph node.");
        }

        try
        {
            var result = await _evidence.CreateAsync(evidence, cancellationToken).ConfigureAwait(false);
            return result.Status switch
            {
                WorkGraphCompletionEvidenceWriteStatus.Created => new(WorkGraphEvidenceRecordStatus.Created),
                WorkGraphCompletionEvidenceWriteStatus.AlreadyRecorded => new(WorkGraphEvidenceRecordStatus.AlreadyRecorded),
                WorkGraphCompletionEvidenceWriteStatus.Conflict => new(
                    WorkGraphEvidenceRecordStatus.Conflict,
                    result.ErrorMessage ?? "Conflicting terminal evidence already exists."),
                _ => new(
                    WorkGraphEvidenceRecordStatus.PersistenceUnavailable,
                    result.ErrorMessage ?? "Completion evidence persistence is unavailable.")
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            return new(WorkGraphEvidenceRecordStatus.PersistenceUnavailable, "Completion evidence persistence is unavailable.");
        }
        catch (IOException)
        {
            return new(WorkGraphEvidenceRecordStatus.PersistenceUnavailable, "Completion evidence persistence is unavailable.");
        }
    }

    private static bool SameGraphReference(WorkGraphReference left, WorkGraphReference right) =>
        left.GraphId == right.GraphId &&
        left.SchemaVersion == right.SchemaVersion &&
        string.Equals(left.ContentHash, right.ContentHash, StringComparison.OrdinalIgnoreCase);

    private static bool SameContractReference(PlanningExecutionContractReference left, PlanningExecutionContractReference right) =>
        left.ContractId == right.ContractId &&
        left.Revision == right.Revision &&
        left.SchemaVersion == right.SchemaVersion &&
        string.Equals(left.ContentHash, right.ContentHash, StringComparison.OrdinalIgnoreCase);
}
