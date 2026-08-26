namespace AIUsageMonitor.Application.Orchestration;

public enum WorkGraphRepositoryWriteStatus
{
    Created,
    GraphConflict,
    Unavailable
}

public sealed record WorkGraphRepositoryWriteResult(
    WorkGraphRepositoryWriteStatus Status,
    string? ErrorMessage = null)
{
    public bool Succeeded => Status == WorkGraphRepositoryWriteStatus.Created;
}

public enum WorkGraphReadState
{
    Missing,
    Valid,
    UnsupportedFutureVersion,
    MigrationRequired,
    Invalid,
    IntegrityFailure,
    Unavailable
}

public sealed record WorkGraphReadResult(
    WorkGraphReadState State,
    WorkGraph? Graph = null,
    string? ErrorMessage = null)
{
    public bool IsValid => State == WorkGraphReadState.Valid && Graph is not null;
}

/// <summary>
/// Dedicated immutable persistence boundary for work-graph authority. There is intentionally no
/// update, replacement, or delete operation.
/// </summary>
public interface IWorkGraphRepository
{
    Task<WorkGraphRepositoryWriteResult> CreateAsync(
        WorkGraph graph,
        CancellationToken cancellationToken = default);

    Task<WorkGraphReadResult> GetAsync(
        Guid projectId,
        Guid graphId,
        CancellationToken cancellationToken = default);
}
