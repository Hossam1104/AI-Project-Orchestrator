namespace AIUsageMonitor.Application.Orchestration;

public enum WorkGraphCompletionEvidenceWriteStatus
{
    Created,
    AlreadyRecorded,
    Conflict,
    Unavailable
}

public sealed record WorkGraphCompletionEvidenceWriteResult(
    WorkGraphCompletionEvidenceWriteStatus Status,
    string? ErrorMessage = null)
{
    public bool Succeeded => Status is
        WorkGraphCompletionEvidenceWriteStatus.Created or
        WorkGraphCompletionEvidenceWriteStatus.AlreadyRecorded;
}

public enum WorkGraphCompletionEvidenceReadState
{
    Missing,
    Valid,
    UnsupportedFutureVersion,
    MigrationRequired,
    Invalid,
    IntegrityFailure,
    Conflict,
    Unavailable
}

public sealed record WorkGraphCompletionEvidenceReadResult(
    WorkGraphCompletionEvidenceReadState State,
    IReadOnlyList<WorkGraphCompletionEvidence> Evidence,
    string? ErrorMessage = null)
{
    public bool IsValid => State is
        WorkGraphCompletionEvidenceReadState.Valid or
        WorkGraphCompletionEvidenceReadState.Missing;
}

/// <summary>
/// Dedicated create-once persistence boundary for terminal node truth. It is separate from the
/// append-oriented orchestration history stream.
/// </summary>
public interface IWorkGraphCompletionEvidenceRepository
{
    Task<WorkGraphCompletionEvidenceWriteResult> CreateAsync(
        WorkGraphCompletionEvidence evidence,
        CancellationToken cancellationToken = default);

    Task<WorkGraphCompletionEvidenceReadResult> ReadForGraphAsync(
        Guid projectId,
        WorkGraphReference graphReference,
        CancellationToken cancellationToken = default);
}
