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
        bool blocking,
        Guid? runId = null,
        int findingCount = 0,
        string? evidenceReference = null,
        string? summary = null)
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

        if (findingCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(findingCount), findingCount, "Finding count cannot be negative.");
        }

        ProjectId = projectId;
        ReviewId = reviewId;
        RunId = runId;
        OccurredAt = occurredAt;
        ReviewerReference = reviewerReference.Trim();
        Verdict = verdict.Trim();
        Severity = severity.Trim();
        Blocking = blocking;
        FindingCount = findingCount;
        EvidenceReference = NormalizeOptional(evidenceReference);
        Summary = NormalizeOptional(summary);
    }

    public Guid ProjectId { get; }

    public Guid ReviewId { get; }

    public Guid? RunId { get; }

    public DateTimeOffset OccurredAt { get; }

    public string ReviewerReference { get; }

    public string Verdict { get; }

    public string Severity { get; }

    public bool Blocking { get; }

    public int FindingCount { get; }

    public string? EvidenceReference { get; }

    public string? Summary { get; }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
