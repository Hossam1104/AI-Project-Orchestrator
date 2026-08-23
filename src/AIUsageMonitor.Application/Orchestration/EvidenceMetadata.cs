namespace AIUsageMonitor.Application.Orchestration;

/// <summary>
/// Metadata about independently captured evidence. Raw command output, source code, prompts,
/// credentials, and authenticated payloads are intentionally not represented.
/// </summary>
public sealed class EvidenceMetadata
{
    public EvidenceMetadata(
        Guid projectId,
        Guid evidenceId,
        DateTimeOffset capturedAt,
        string kind,
        string outcome,
        Guid? runId = null,
        string? validatorReference = null,
        string? artifactReference = null,
        string? contentHash = null,
        string? summary = null)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Evidence project id cannot be empty.", nameof(projectId));
        }

        if (evidenceId == Guid.Empty)
        {
            throw new ArgumentException("Evidence id cannot be empty.", nameof(evidenceId));
        }

        if (string.IsNullOrWhiteSpace(kind))
        {
            throw new ArgumentException("Evidence kind is required.", nameof(kind));
        }

        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Evidence outcome is required.", nameof(outcome));
        }

        ProjectId = projectId;
        EvidenceId = evidenceId;
        CapturedAt = capturedAt;
        Kind = kind.Trim();
        Outcome = outcome.Trim();
        RunId = runId;
        ValidatorReference = NormalizeOptional(validatorReference);
        ArtifactReference = NormalizeOptional(artifactReference);
        ContentHash = NormalizeOptional(contentHash);
        Summary = NormalizeOptional(summary);
    }

    public Guid ProjectId { get; }

    public Guid EvidenceId { get; }

    public Guid? RunId { get; }

    public DateTimeOffset CapturedAt { get; }

    public string Kind { get; }

    public string Outcome { get; }

    public string? ValidatorReference { get; }

    public string? ArtifactReference { get; }

    public string? ContentHash { get; }

    public string? Summary { get; }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
