namespace AIUsageMonitor.Application.Orchestration;

/// <summary>
/// Non-secret review metadata. Finding content and repository payloads remain outside this
/// storage foundation; only bounded summaries and traceable references are retained.
/// </summary>
public sealed class ReviewMetadata
{
    public ReviewMetadata(
        Guid projectId,
        Guid reviewId,
        DateTimeOffset occurredAt,
        string reviewerReference,
        string verdict,
        string severity,
        Guid? runId = null,
        string? evidenceReference = null,
        string? summary = null,
        IReadOnlyList<ReviewFindingMetadata>? findings = null)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Review project id cannot be empty.", nameof(projectId));
        }

        if (reviewId == Guid.Empty)
        {
            throw new ArgumentException("Review id cannot be empty.", nameof(reviewId));
        }

        if (string.IsNullOrWhiteSpace(reviewerReference))
        {
            throw new ArgumentException("Review reviewer reference is required.", nameof(reviewerReference));
        }

        if (string.IsNullOrWhiteSpace(verdict))
        {
            throw new ArgumentException("Review verdict is required.", nameof(verdict));
        }

        if (string.IsNullOrWhiteSpace(severity))
        {
            throw new ArgumentException("Review severity is required.", nameof(severity));
        }

        ProjectId = projectId;
        ReviewId = reviewId;
        RunId = runId;
        OccurredAt = occurredAt;
        ReviewerReference = reviewerReference.Trim();
        Verdict = verdict.Trim();
        Severity = severity.Trim();
        EvidenceReference = NormalizeOptional(evidenceReference);
        Summary = NormalizeOptional(summary);
        Findings = CopyFindings(findings);
    }

    public Guid ProjectId { get; }

    public Guid ReviewId { get; }

    public Guid? RunId { get; }

    public DateTimeOffset OccurredAt { get; }

    public string ReviewerReference { get; }

    public string Verdict { get; }

    public string Severity { get; }

    /// <summary>
    /// Gets whether any detailed finding is blocking. A review with no findings is non-blocking.
    /// </summary>
    public bool Blocking => Findings.Count > 0 && Findings.Any(static finding => finding.Blocking);

    /// <summary>
    /// Gets the number of detailed findings. The finding collection is the sole source of truth.
    /// </summary>
    public int FindingCount => Findings.Count;

    public string? EvidenceReference { get; }

    public string? Summary { get; }

    public IReadOnlyList<ReviewFindingMetadata> Findings { get; }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IReadOnlyList<ReviewFindingMetadata> CopyFindings(
        IReadOnlyList<ReviewFindingMetadata>? findings)
    {
        if (findings is null || findings.Count == 0)
        {
            return Array.Empty<ReviewFindingMetadata>();
        }

        var result = new List<ReviewFindingMetadata>(findings.Count);
        var findingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var finding in findings)
        {
            ArgumentNullException.ThrowIfNull(finding);
            if (!findingIds.Add(finding.FindingId))
            {
                throw new ArgumentException(
                    $"Review finding id '{finding.FindingId}' is duplicated.",
                    nameof(findings));
            }

            result.Add(finding);
        }

        return result.AsReadOnly();
    }
}
