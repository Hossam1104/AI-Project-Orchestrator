namespace AIUsageMonitor.Application.Orchestration;

/// <summary>
/// Append-oriented project activity metadata. It contains actor/action/outcome references only,
/// never prompts, source code, credentials, or authenticated payloads.
/// </summary>
public sealed class ActivityAuditRecord
{
    public ActivityAuditRecord(
        Guid projectId,
        Guid activityId,
        DateTimeOffset occurredAt,
        string actorReference,
        string action,
        string outcome,
        Guid? runId = null,
        Guid? evidenceId = null,
        string? summary = null,
        string? taskReference = null,
        IReadOnlyList<Guid>? evidenceIds = null)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Activity project id cannot be empty.", nameof(projectId));
        }

        if (activityId == Guid.Empty)
        {
            throw new ArgumentException("Activity id cannot be empty.", nameof(activityId));
        }

        if (string.IsNullOrWhiteSpace(actorReference))
        {
            throw new ArgumentException("Activity actor reference is required.", nameof(actorReference));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Activity action is required.", nameof(action));
        }

        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Activity outcome is required.", nameof(outcome));
        }

        if (evidenceId == Guid.Empty)
        {
            throw new ArgumentException("Activity evidence id cannot be empty.", nameof(evidenceId));
        }

        ProjectId = projectId;
        ActivityId = activityId;
        OccurredAt = occurredAt;
        ActorReference = actorReference.Trim();
        Action = action.Trim();
        Outcome = outcome.Trim();
        RunId = runId;
        EvidenceId = evidenceId;
        Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();
        TaskReference = string.IsNullOrWhiteSpace(taskReference) ? null : taskReference.Trim();
        EvidenceIds = CopyEvidenceIds(evidenceId, evidenceIds);
    }

    public Guid ProjectId { get; }

    public Guid ActivityId { get; }

    public Guid? RunId { get; }

    public Guid? EvidenceId { get; }

    public string? TaskReference { get; }

    public IReadOnlyList<Guid> EvidenceIds { get; }

    public DateTimeOffset OccurredAt { get; }

    public string ActorReference { get; }

    public string Action { get; }

    public string Outcome { get; }

    public string? Summary { get; }

    private static IReadOnlyList<Guid> CopyEvidenceIds(Guid? singularEvidenceId, IReadOnlyList<Guid>? evidenceIds)
    {
        var result = new List<Guid>();
        if (singularEvidenceId.HasValue)
        {
            result.Add(singularEvidenceId.Value);
        }

        if (evidenceIds is not null)
        {
            foreach (var evidenceId in evidenceIds)
            {
                if (evidenceId == Guid.Empty)
                {
                    throw new ArgumentException("Activity evidence ids cannot be empty.", nameof(evidenceIds));
                }

                if (!result.Contains(evidenceId))
                {
                    result.Add(evidenceId);
                }
            }
        }

        return result.AsReadOnly();
    }
}
