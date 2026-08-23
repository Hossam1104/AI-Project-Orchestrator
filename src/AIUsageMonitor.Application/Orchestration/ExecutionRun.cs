namespace AIUsageMonitor.Application.Orchestration;

/// <summary>
/// Non-secret execution-run metadata. This is an append-oriented checkpoint record, not an
/// executor runtime or a persisted prompt/working-tree snapshot.
/// </summary>
public sealed class ExecutionRun
{
    public ExecutionRun(
        Guid projectId,
        Guid runId,
        ExecutionRunStatus status,
        DateTimeOffset startedAt,
        DateTimeOffset? completedAt = null,
        string? workItemReference = null,
        string? taskTitle = null,
        Guid? agentId = null,
        string? modelReference = null,
        string? outcome = null,
        string? stopReason = null,
        string? contractReference = null,
        Guid? recordId = null,
        DateTimeOffset? recordedAt = null)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Execution project id cannot be empty.", nameof(projectId));
        }

        if (runId == Guid.Empty)
        {
            throw new ArgumentException("Execution run id cannot be empty.", nameof(runId));
        }

        if (completedAt.HasValue && completedAt.Value < startedAt)
        {
            throw new ArgumentException("Execution completed time cannot precede started time.", nameof(completedAt));
        }

        if (recordId == Guid.Empty)
        {
            throw new ArgumentException("Execution record id cannot be empty.", nameof(recordId));
        }

        var effectiveRecordedAt = recordedAt ?? startedAt;
        if (effectiveRecordedAt < startedAt)
        {
            throw new ArgumentException("Execution recorded time cannot precede started time.", nameof(recordedAt));
        }

        if (!Enum.IsDefined(typeof(ExecutionRunStatus), status))
        {
            throw new ArgumentException("Execution run status is undefined.", nameof(status));
        }

        ProjectId = projectId;
        RunId = runId;
        RecordId = recordId ?? Guid.NewGuid();
        RecordedAt = effectiveRecordedAt;
        Status = status;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        WorkItemReference = NormalizeOptional(workItemReference);
        TaskTitle = NormalizeOptional(taskTitle);
        AgentId = agentId;
        ModelReference = NormalizeOptional(modelReference);
        Outcome = NormalizeOptional(outcome);
        StopReason = NormalizeOptional(stopReason);
        ContractReference = NormalizeOptional(contractReference);
    }

    public Guid ProjectId { get; }

    public Guid RunId { get; }

    /// <summary>
    /// Identifies one durable checkpoint appended to the execution history stream.
    /// </summary>
    public Guid RecordId { get; }

    /// <summary>
    /// History timestamp for this checkpoint. It is independent from the lifecycle start time.
    /// </summary>
    public DateTimeOffset RecordedAt { get; }

    public ExecutionRunStatus Status { get; }

    public DateTimeOffset StartedAt { get; }

    public DateTimeOffset? CompletedAt { get; }

    public string? WorkItemReference { get; }

    public string? TaskTitle { get; }

    public Guid? AgentId { get; }

    public string? ModelReference { get; }

    public string? Outcome { get; }

    public string? StopReason { get; }

    public string? ContractReference { get; }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
