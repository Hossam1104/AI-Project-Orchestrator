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
        string? summary = null)
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

        ProjectId = projectId;
        ActivityId = activityId;
        OccurredAt = occurredAt;
        ActorReference = actorReference.Trim();
        Action = action.Trim();
        Outcome = outcome.Trim();
        RunId = runId;
        EvidenceId = evidenceId;
        Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();
    }

    public Guid ProjectId { get; }

    public Guid ActivityId { get; }

    public Guid? RunId { get; }

    public Guid? EvidenceId { get; }

    public DateTimeOffset OccurredAt { get; }

    public string ActorReference { get; }

    public string Action { get; }

    public string Outcome { get; }

    public string? Summary { get; }
}
