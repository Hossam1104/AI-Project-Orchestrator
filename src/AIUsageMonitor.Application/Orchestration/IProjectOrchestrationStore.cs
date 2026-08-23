namespace AIUsageMonitor.Application.Orchestration;

/// <summary>
/// Project-isolated append/read boundary for orchestration metadata. Implementations must keep
/// each stream under the project GUID directory and must not expose another project's records.
/// </summary>
public interface IProjectOrchestrationStore
{
    Task AppendExecutionRunAsync(ExecutionRun run, CancellationToken cancellationToken = default);

    Task<HistoryReadResult<ExecutionRun>> ReadExecutionRunsAsync(
        Guid projectId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    Task AppendEvidenceAsync(EvidenceMetadata evidence, CancellationToken cancellationToken = default);

    Task<HistoryReadResult<EvidenceMetadata>> ReadEvidenceAsync(
        Guid projectId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    Task AppendReviewAsync(ReviewMetadata review, CancellationToken cancellationToken = default);

    Task<HistoryReadResult<ReviewMetadata>> ReadReviewsAsync(
        Guid projectId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    Task AppendActivityAsync(ActivityAuditRecord activity, CancellationToken cancellationToken = default);

    Task<HistoryReadResult<ActivityAuditRecord>> ReadActivityAsync(
        Guid projectId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);
}
