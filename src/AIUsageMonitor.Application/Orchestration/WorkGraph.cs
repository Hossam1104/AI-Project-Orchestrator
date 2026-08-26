using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIUsageMonitor.Application.Planning;

namespace AIUsageMonitor.Application.Orchestration;

/// <summary>Semantic version of the immutable APO-41 work-graph authority.</summary>
public static class WorkGraphSchema
{
    public const int CurrentVersion = 1;
}

/// <summary>
/// Conservative desktop-local bounds for graph and scheduling inputs. These limits are part of
/// the bounded V1 contract and prevent an untrusted caller from causing unbounded validation work.
/// </summary>
public static class WorkGraphLimits
{
    public const int MaxNodes = 512;
    public const int MaxEdges = 2_048;
    public const int MaxActiveNodes = 512;
    public const int MaxConcurrency = 512;
    public const int MaxEvidenceReferenceLength = 500;
}

/// <summary>A graph node bound to one exact immutable APO-40 contract revision.</summary>
public sealed class WorkGraphNode
{
    public WorkGraphNode(
        Guid nodeId,
        PlanningExecutionContractReference contractReference)
    {
        if (nodeId == Guid.Empty)
        {
            throw new ArgumentException("Node id cannot be empty.", nameof(nodeId));
        }

        ContractReference = contractReference ?? throw new ArgumentNullException(nameof(contractReference));
        NodeId = nodeId;
    }

    public Guid NodeId { get; }

    public PlanningExecutionContractReference ContractReference { get; }
}

/// <summary>A required prerequisite edge: prerequisite node -> dependent node.</summary>
public sealed class WorkGraphEdge
{
    public WorkGraphEdge(
        Guid edgeId,
        Guid prerequisiteNodeId,
        Guid dependentNodeId)
    {
        if (edgeId == Guid.Empty)
        {
            throw new ArgumentException("Edge id cannot be empty.", nameof(edgeId));
        }

        if (prerequisiteNodeId == Guid.Empty)
        {
            throw new ArgumentException("Prerequisite node id cannot be empty.", nameof(prerequisiteNodeId));
        }

        if (dependentNodeId == Guid.Empty)
        {
            throw new ArgumentException("Dependent node id cannot be empty.", nameof(dependentNodeId));
        }

        EdgeId = edgeId;
        PrerequisiteNodeId = prerequisiteNodeId;
        DependentNodeId = dependentNodeId;
    }

    public Guid EdgeId { get; }

    public Guid PrerequisiteNodeId { get; }

    public Guid DependentNodeId { get; }
}

/// <summary>Typed integrity reference for one immutable work-graph snapshot.</summary>
public sealed class WorkGraphReference
{
    public WorkGraphReference(Guid graphId, int schemaVersion, string contentHash)
    {
        if (graphId == Guid.Empty)
        {
            throw new ArgumentException("Graph id cannot be empty.", nameof(graphId));
        }

        if (schemaVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        }

        if (!IsSha256(contentHash))
        {
            throw new ArgumentException("Content hash must be a SHA-256 hexadecimal value.", nameof(contentHash));
        }

        GraphId = graphId;
        SchemaVersion = schemaVersion;
        ContentHash = contentHash.ToLowerInvariant();
    }

    public Guid GraphId { get; }

    public int SchemaVersion { get; }

    /// <summary>SHA-256 integrity evidence, not a signature or authenticity proof.</summary>
    public string ContentHash { get; }

    public override string ToString() =>
        $"graph:{GraphId:D}/schema:{SchemaVersion}/sha256:{ContentHash}";

    public static bool IsSha256(string? value) =>
        value is not null &&
        value.Length == 64 &&
        value.All(static character => Uri.IsHexDigit(character));
}

/// <summary>
/// Immutable planning/scheduling snapshot. It contains no executable behavior and exposes no
/// update, replacement, or deletion operation.
/// </summary>
public sealed class WorkGraph
{
    public WorkGraph(
        Guid projectId,
        Guid graphId,
        int schemaVersion,
        DateTimeOffset createdAt,
        IReadOnlyList<WorkGraphNode> nodes,
        IReadOnlyList<WorkGraphEdge> edges,
        string? contentHash = null)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        if (graphId == Guid.Empty)
        {
            throw new ArgumentException("Graph id cannot be empty.", nameof(graphId));
        }

        if (schemaVersion != WorkGraphSchema.CurrentVersion)
        {
            throw new ArgumentException(
                $"Only work-graph schema {WorkGraphSchema.CurrentVersion} is supported for new graph authority.",
                nameof(schemaVersion));
        }

        if (createdAt == default)
        {
            throw new ArgumentException("Graph creation time is required.", nameof(createdAt));
        }

        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);

        if (nodes.Count > WorkGraphLimits.MaxNodes)
        {
            throw new ArgumentException(
                $"A graph cannot contain more than {WorkGraphLimits.MaxNodes} nodes.",
                nameof(nodes));
        }

        if (edges.Count > WorkGraphLimits.MaxEdges)
        {
            throw new ArgumentException(
                $"A graph cannot contain more than {WorkGraphLimits.MaxEdges} edges.",
                nameof(edges));
        }

        var normalizedNodes = nodes
            .Select(static node => node ?? throw new ArgumentException("Nodes cannot contain null entries.", nameof(nodes)))
            .OrderBy(static node => node.NodeId)
            .ToArray();
        var nodeIds = new HashSet<Guid>();
        var contractReferences = new HashSet<(Guid ContractId, int Revision, int SchemaVersion, string ContentHash)>();
        var contractRevisions = new HashSet<Guid>();

        foreach (var node in normalizedNodes)
        {
            if (!nodeIds.Add(node.NodeId))
            {
                throw new ArgumentException("Node identifiers must be unique.", nameof(nodes));
            }

            var reference = node.ContractReference;
            if (!contractReferences.Add((
                    reference.ContractId,
                    reference.Revision,
                    reference.SchemaVersion,
                    reference.ContentHash)))
            {
                throw new ArgumentException(
                    "A graph cannot contain duplicate exact planning contract references.",
                    nameof(nodes));
            }

            if (!contractRevisions.Add(reference.ContractId))
            {
                throw new ArgumentException(
                    "A graph cannot contain multiple revisions of one logical planning contract.",
                    nameof(nodes));
            }
        }

        var normalizedEdges = edges
            .Select(static edge => edge ?? throw new ArgumentException("Edges cannot contain null entries.", nameof(edges)))
            .OrderBy(static edge => edge.PrerequisiteNodeId)
            .ThenBy(static edge => edge.DependentNodeId)
            .ThenBy(static edge => edge.EdgeId)
            .ToArray();
        var edgeIds = new HashSet<Guid>();
        var logicalEdges = new HashSet<(Guid Prerequisite, Guid Dependent)>();

        foreach (var edge in normalizedEdges)
        {
            if (!edgeIds.Add(edge.EdgeId))
            {
                throw new ArgumentException("Edge identifiers must be unique.", nameof(edges));
            }

            if (edge.PrerequisiteNodeId == edge.DependentNodeId)
            {
                throw new ArgumentException("A graph cannot contain a self-edge.", nameof(edges));
            }

            if (!nodeIds.Contains(edge.PrerequisiteNodeId) || !nodeIds.Contains(edge.DependentNodeId))
            {
                throw new ArgumentException("Graph edges must reference existing nodes.", nameof(edges));
            }

            if (!logicalEdges.Add((edge.PrerequisiteNodeId, edge.DependentNodeId)))
            {
                throw new ArgumentException(
                    "A graph cannot contain duplicate prerequisite edges.",
                    nameof(edges));
            }
        }

        var topologicalOrder = ComputeTopologicalOrder(normalizedNodes, normalizedEdges);

        ProjectId = projectId;
        GraphId = graphId;
        SchemaVersion = schemaVersion;
        CreatedAt = createdAt;
        Nodes = Array.AsReadOnly(normalizedNodes);
        Edges = Array.AsReadOnly(normalizedEdges);
        TopologicalOrder = Array.AsReadOnly(topologicalOrder);
        ContentHash = string.Empty;

        var calculatedHash = WorkGraphIntegrity.ComputeContentHash(this);
        if (contentHash is not null &&
            (!WorkGraphReference.IsSha256(contentHash) ||
             !string.Equals(contentHash, calculatedHash, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("The supplied graph content hash does not match the graph payload.", nameof(contentHash));
        }

        ContentHash = calculatedHash;
        Reference = new WorkGraphReference(GraphId, SchemaVersion, ContentHash);
    }

    public Guid ProjectId { get; }

    public Guid GraphId { get; }

    public int SchemaVersion { get; }

    public DateTimeOffset CreatedAt { get; }

    public IReadOnlyList<WorkGraphNode> Nodes { get; }

    public IReadOnlyList<WorkGraphEdge> Edges { get; }

    /// <summary>Canonical Kahn order, with NodeId ordinal tie-breaking.</summary>
    public IReadOnlyList<Guid> TopologicalOrder { get; }

    /// <summary>SHA-256 graph integrity evidence, not a signature.</summary>
    public string ContentHash { get; private set; }

    public WorkGraphReference Reference { get; private set; }

    private static Guid[] ComputeTopologicalOrder(
        IReadOnlyList<WorkGraphNode> nodes,
        IReadOnlyList<WorkGraphEdge> edges)
    {
        var indegree = nodes.ToDictionary(static node => node.NodeId, static _ => 0);
        var adjacency = nodes.ToDictionary(static node => node.NodeId, static _ => new List<Guid>());

        foreach (var edge in edges)
        {
            indegree[edge.DependentNodeId]++;
            adjacency[edge.PrerequisiteNodeId].Add(edge.DependentNodeId);
        }

        foreach (var dependents in adjacency.Values)
        {
            dependents.Sort();
        }

        var ready = new SortedSet<Guid>(
            indegree
                .Where(static pair => pair.Value == 0)
                .Select(static pair => pair.Key));
        var result = new List<Guid>(nodes.Count);

        while (ready.Count > 0)
        {
            var nodeId = ready.Min;
            ready.Remove(nodeId);
            result.Add(nodeId);

            foreach (var dependentId in adjacency[nodeId])
            {
                indegree[dependentId]--;
                if (indegree[dependentId] == 0)
                {
                    ready.Add(dependentId);
                }
            }
        }

        if (result.Count != nodes.Count)
        {
            throw new ArgumentException("A work graph must be acyclic.", nameof(edges));
        }

        return result.ToArray();
    }
}

/// <summary>Computes deterministic SHA-256 integrity over canonical graph content.</summary>
public static class WorkGraphIntegrity
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string ComputeContentHash(WorkGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var payload = new
        {
            graph.ProjectId,
            graph.GraphId,
            graph.SchemaVersion,
            graph.CreatedAt,
            nodes = graph.Nodes.Select(static node => new
            {
                node.NodeId,
                contractReference = new
                {
                    node.ContractReference.ContractId,
                    node.ContractReference.Revision,
                    node.ContractReference.SchemaVersion,
                    node.ContractReference.ContentHash
                }
            }).ToArray(),
            edges = graph.Edges.Select(static edge => new
            {
                edge.EdgeId,
                edge.PrerequisiteNodeId,
                edge.DependentNodeId
            }).ToArray()
        };

        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }
}
