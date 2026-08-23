namespace AIUsageMonitor.Application.Orchestration;

/// <summary>
/// Bounded, non-secret traceability metadata for one independent-review finding.
/// </summary>
public sealed class ReviewFindingMetadata
{
    public ReviewFindingMetadata(
        string findingId,
        string severity,
        string affectedReference,
        string disposition,
        bool blocking,
        IReadOnlyList<Guid>? evidenceIds = null,
        IReadOnlyList<string>? evidenceReferences = null,
        string? summary = null)
    {
        if (string.IsNullOrWhiteSpace(findingId))
        {
            throw new ArgumentException("Review finding id is required.", nameof(findingId));
        }

        if (string.IsNullOrWhiteSpace(severity))
        {
            throw new ArgumentException("Review finding severity is required.", nameof(severity));
        }

        if (string.IsNullOrWhiteSpace(affectedReference))
        {
            throw new ArgumentException("Review finding affected reference is required.", nameof(affectedReference));
        }

        if (string.IsNullOrWhiteSpace(disposition))
        {
            throw new ArgumentException("Review finding disposition is required.", nameof(disposition));
        }

        FindingId = findingId.Trim();
        Severity = severity.Trim();
        AffectedReference = affectedReference.Trim();
        Disposition = disposition.Trim();
        Blocking = blocking;
        EvidenceIds = CopyEvidenceIds(evidenceIds);
        EvidenceReferences = CopyReferences(evidenceReferences);
        Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();
    }

    public string FindingId { get; }

    public string Severity { get; }

    public string AffectedReference { get; }

    public string Disposition { get; }

    public bool Blocking { get; }

    public IReadOnlyList<Guid> EvidenceIds { get; }

    public IReadOnlyList<string> EvidenceReferences { get; }

    public string? Summary { get; }

    private static IReadOnlyList<Guid> CopyEvidenceIds(IReadOnlyList<Guid>? evidenceIds)
    {
        if (evidenceIds is null || evidenceIds.Count == 0)
        {
            return Array.Empty<Guid>();
        }

        var result = new List<Guid>(evidenceIds.Count);
        foreach (var evidenceId in evidenceIds)
        {
            if (evidenceId == Guid.Empty)
            {
                throw new ArgumentException("Review finding evidence ids cannot be empty.", nameof(evidenceIds));
            }

            if (!result.Contains(evidenceId))
            {
                result.Add(evidenceId);
            }
        }

        return result.AsReadOnly();
    }

    private static IReadOnlyList<string> CopyReferences(IReadOnlyList<string>? references)
    {
        if (references is null || references.Count == 0)
        {
            return Array.Empty<string>();
        }

        var result = new List<string>(references.Count);
        foreach (var reference in references)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                throw new ArgumentException(
                    "Review finding evidence references cannot contain blank values.",
                    nameof(references));
            }

            var normalized = reference.Trim();
            if (!result.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(normalized);
            }
        }

        return result.AsReadOnly();
    }
}
