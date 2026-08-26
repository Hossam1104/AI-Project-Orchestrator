using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Planning;

namespace AIUsageMonitor.Infrastructure.Persistence;

internal sealed class WorkGraphRecord
{
    public string RecordType { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public Guid GraphId { get; set; }
    public int SchemaVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<WorkGraphNodeRecord>? Nodes { get; set; }
    public List<WorkGraphEdgeRecord>? Edges { get; set; }
    public string ContentHash { get; set; } = string.Empty;

    public static WorkGraphRecord FromApplication(WorkGraph graph) => new()
    {
        RecordType = "work-graph",
        ProjectId = graph.ProjectId,
        GraphId = graph.GraphId,
        SchemaVersion = graph.SchemaVersion,
        CreatedAt = graph.CreatedAt,
        Nodes = graph.Nodes.Select(WorkGraphNodeRecord.FromApplication).ToList(),
        Edges = graph.Edges.Select(WorkGraphEdgeRecord.FromApplication).ToList(),
        ContentHash = graph.ContentHash
    };

    public WorkGraph ToApplication() => new(
        ProjectId,
        GraphId,
        SchemaVersion,
        CreatedAt,
        (Nodes ?? throw new ArgumentException("Graph nodes are missing.")).Select(static node => node.ToApplication()).ToArray(),
        (Edges ?? throw new ArgumentException("Graph edges are missing.")).Select(static edge => edge.ToApplication()).ToArray(),
        ContentHash);
}

internal sealed class WorkGraphNodeRecord
{
    public Guid NodeId { get; set; }
    public Guid ContractId { get; set; }
    public int ContractRevision { get; set; }
    public int ContractSchemaVersion { get; set; }
    public string ContractContentHash { get; set; } = string.Empty;

    public static WorkGraphNodeRecord FromApplication(WorkGraphNode node) => new()
    {
        NodeId = node.NodeId,
        ContractId = node.ContractReference.ContractId,
        ContractRevision = node.ContractReference.Revision,
        ContractSchemaVersion = node.ContractReference.SchemaVersion,
        ContractContentHash = node.ContractReference.ContentHash
    };

    public WorkGraphNode ToApplication() => new(
        NodeId,
        new PlanningExecutionContractReference(
            ContractId,
            ContractRevision,
            ContractSchemaVersion,
            ContractContentHash));
}

internal sealed class WorkGraphEdgeRecord
{
    public Guid EdgeId { get; set; }
    public Guid PrerequisiteNodeId { get; set; }
    public Guid DependentNodeId { get; set; }

    public static WorkGraphEdgeRecord FromApplication(WorkGraphEdge edge) => new()
    {
        EdgeId = edge.EdgeId,
        PrerequisiteNodeId = edge.PrerequisiteNodeId,
        DependentNodeId = edge.DependentNodeId
    };

    public WorkGraphEdge ToApplication() => new(EdgeId, PrerequisiteNodeId, DependentNodeId);
}

internal sealed class WorkGraphCompletionEvidenceRecord
{
    public string RecordType { get; set; } = string.Empty;
    public Guid EvidenceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid GraphId { get; set; }
    public int GraphSchemaVersion { get; set; }
    public string GraphContentHash { get; set; } = string.Empty;
    public Guid NodeId { get; set; }
    public Guid ContractId { get; set; }
    public int ContractRevision { get; set; }
    public int ContractSchemaVersion { get; set; }
    public string ContractContentHash { get; set; } = string.Empty;
    public WorkGraphCompletionState State { get; set; }
    public string EvidenceReference { get; set; } = string.Empty;
    public DateTimeOffset RecordedAt { get; set; }
    public string? ContentHash { get; set; }

    public static WorkGraphCompletionEvidenceRecord FromApplication(
        WorkGraphCompletionEvidence evidence) => new()
    {
        RecordType = "work-graph-completion-evidence",
        EvidenceId = evidence.EvidenceId,
        ProjectId = evidence.ProjectId,
        GraphId = evidence.GraphReference.GraphId,
        GraphSchemaVersion = evidence.GraphReference.SchemaVersion,
        GraphContentHash = evidence.GraphReference.ContentHash,
        NodeId = evidence.NodeId,
        ContractId = evidence.ContractReference.ContractId,
        ContractRevision = evidence.ContractReference.Revision,
        ContractSchemaVersion = evidence.ContractReference.SchemaVersion,
        ContractContentHash = evidence.ContractReference.ContentHash,
        State = evidence.State,
        EvidenceReference = evidence.EvidenceReference,
        RecordedAt = evidence.RecordedAt,
        ContentHash = evidence.ContentHash
    };

    public WorkGraphCompletionEvidence ToApplication()
    {
        if (ContentHash is null)
        {
            throw new ArgumentException("Completion evidence content hash is missing.", nameof(ContentHash));
        }

        return new(
            EvidenceId,
            ProjectId,
            new WorkGraphReference(GraphId, GraphSchemaVersion, GraphContentHash),
            NodeId,
            new PlanningExecutionContractReference(
                ContractId,
                ContractRevision,
                ContractSchemaVersion,
                ContractContentHash),
            State,
            EvidenceReference,
            RecordedAt,
            ContentHash);
    }

    public WorkGraphCompletionEvidence ToApplicationForIntegrityValidation() => new(
        EvidenceId,
        ProjectId,
        new WorkGraphReference(GraphId, GraphSchemaVersion, GraphContentHash),
        NodeId,
        new PlanningExecutionContractReference(
            ContractId,
            ContractRevision,
            ContractSchemaVersion,
            ContractContentHash),
        State,
        EvidenceReference,
        RecordedAt);
}
