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
        string? summary = null,
        IReadOnlyList<string>? relatedRequirementReferences = null)
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
        RelatedRequirementReferences = CopyReferences(relatedRequirementReferences);
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

    public IReadOnlyList<string> RelatedRequirementReferences { get; }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
                    "Related requirement references cannot contain blank values.",
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
