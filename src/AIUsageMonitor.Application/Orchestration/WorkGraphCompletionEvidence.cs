using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIUsageMonitor.Application.Planning;

namespace AIUsageMonitor.Application.Orchestration;

public enum WorkGraphCompletionState
{
    Succeeded,
    Failed,
    Skipped
}

/// <summary>
/// Immutable terminal truth for one graph node. A missing record is intentionally not represented
/// as a state and therefore evaluates as incomplete.
/// </summary>
public sealed class WorkGraphCompletionEvidence
{
    public WorkGraphCompletionEvidence(
        Guid evidenceId,
        Guid projectId,
        WorkGraphReference graphReference,
        Guid nodeId,
        PlanningExecutionContractReference contractReference,
        WorkGraphCompletionState state,
        string evidenceReference,
        DateTimeOffset recordedAt,
        string? contentHash = null)
    {
        if (evidenceId == Guid.Empty)
        {
            throw new ArgumentException("Evidence id cannot be empty.", nameof(evidenceId));
        }

        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        GraphReference = graphReference ?? throw new ArgumentNullException(nameof(graphReference));

        if (nodeId == Guid.Empty)
        {
            throw new ArgumentException("Node id cannot be empty.", nameof(nodeId));
        }

        ContractReference = contractReference ?? throw new ArgumentNullException(nameof(contractReference));

        if (!Enum.IsDefined(state))
        {
            throw new ArgumentException("Completion state is undefined.", nameof(state));
        }

        if (string.IsNullOrWhiteSpace(evidenceReference))
        {
            throw new ArgumentException("A bounded evidence reference is required.", nameof(evidenceReference));
        }

        var normalizedReference = evidenceReference.Trim();
        if (normalizedReference.Length > WorkGraphLimits.MaxEvidenceReferenceLength ||
            normalizedReference.Any(static character => char.IsControl(character)))
        {
            throw new ArgumentException(
                $"Evidence references must be non-control text of at most {WorkGraphLimits.MaxEvidenceReferenceLength} characters.",
                nameof(evidenceReference));
        }

        if (recordedAt == default)
        {
            throw new ArgumentException("Evidence recording time is required.", nameof(recordedAt));
        }

        EvidenceId = evidenceId;
        ProjectId = projectId;
        NodeId = nodeId;
        State = state;
        EvidenceReference = normalizedReference;
        RecordedAt = recordedAt;

        ContentHash = string.Empty;
        var calculatedHash = WorkGraphCompletionEvidenceIntegrity.ComputeContentHash(this);
        if (contentHash is not null &&
            (!WorkGraphReference.IsSha256(contentHash) ||
             !string.Equals(contentHash, calculatedHash, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException(
                "The supplied completion evidence content hash does not match the evidence payload.",
                nameof(contentHash));
        }

        ContentHash = calculatedHash;
    }

    public Guid EvidenceId { get; }

    public Guid ProjectId { get; }

    public WorkGraphReference GraphReference { get; }

    public Guid NodeId { get; }

    public PlanningExecutionContractReference ContractReference { get; }

    public WorkGraphCompletionState State { get; }

    /// <summary>Non-secret bounded identifier for independently captured evidence.</summary>
    public string EvidenceReference { get; }

    public DateTimeOffset RecordedAt { get; }

    /// <summary>SHA-256 content-integrity evidence, not a signature or authenticity proof.</summary>
    public string ContentHash { get; }
}

/// <summary>Computes deterministic SHA-256 integrity over terminal completion evidence.</summary>
public static class WorkGraphCompletionEvidenceIntegrity
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string ComputeContentHash(WorkGraphCompletionEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        var payload = new
        {
            evidence.EvidenceId,
            evidence.ProjectId,
            graphReference = new
            {
                evidence.GraphReference.GraphId,
                evidence.GraphReference.SchemaVersion,
                evidence.GraphReference.ContentHash
            },
            evidence.NodeId,
            contractReference = new
            {
                evidence.ContractReference.ContractId,
                evidence.ContractReference.Revision,
                evidence.ContractReference.SchemaVersion,
                evidence.ContractReference.ContentHash
            },
            evidence.State,
            evidence.EvidenceReference,
            evidence.RecordedAt
        };

        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }
}
