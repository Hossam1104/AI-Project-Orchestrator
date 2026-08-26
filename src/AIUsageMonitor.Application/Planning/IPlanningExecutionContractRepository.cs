namespace AIUsageMonitor.Application.Planning;

public enum PlanningContractRepositoryWriteStatus
{
    Created,
    RevisionConflict,
    PredecessorMissing,
    InvalidLineage,
    Unavailable
}

public sealed record PlanningContractRepositoryWriteResult(
    PlanningContractRepositoryWriteStatus Status,
    string? ErrorMessage = null)
{
    public bool Succeeded => Status == PlanningContractRepositoryWriteStatus.Created;
}

public enum PlanningContractReadState
{
    Missing,
    Valid,
    UnsupportedFutureVersion,
    MigrationRequired,
    Invalid,
    IntegrityFailure,
    Unavailable
}

public sealed record PlanningContractReadResult(
    PlanningContractReadState State,
    PlanningExecutionContract? Contract = null,
    string? ErrorMessage = null)
{
    public bool IsValid => State == PlanningContractReadState.Valid && Contract is not null;
}

public sealed record PlanningContractRevisionListResult(
    PlanningContractReadState State,
    IReadOnlyList<PlanningExecutionContract> Revisions,
    string? ErrorMessage = null)
{
    public bool IsValid => State == PlanningContractReadState.Valid;
}

/// <summary>
/// Dedicated immutable storage boundary for planner-authorized contract revisions. It is not the
/// orchestration JSONL store and intentionally has no update or delete operation.
/// </summary>
public interface IPlanningExecutionContractRepository
{
    Task<PlanningContractRepositoryWriteResult> CreateAsync(
        PlanningExecutionContract contract,
        CancellationToken cancellationToken = default);

    Task<PlanningContractReadResult> GetAsync(
        Guid projectId,
        Guid contractId,
        int revision,
        CancellationToken cancellationToken = default);

    Task<PlanningContractReadResult> GetLatestAsync(
        Guid projectId,
        Guid contractId,
        CancellationToken cancellationToken = default);

    Task<PlanningContractRevisionListResult> ListRevisionsAsync(
        Guid projectId,
        Guid contractId,
        CancellationToken cancellationToken = default);
}
