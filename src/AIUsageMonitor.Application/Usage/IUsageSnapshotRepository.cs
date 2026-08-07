using AIUsageMonitor.Domain.Usage;

namespace AIUsageMonitor.Application.Usage;

/// <summary>
/// Persistence abstraction for usage history. Implemented against EF Core/LocalDB in Session 03;
/// the Application layer must not know that.
/// </summary>
public interface IUsageSnapshotRepository
{
    Task AddAsync(UsageSnapshot snapshot, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UsageSnapshot>> GetHistoryAsync(
        Guid providerId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

    Task<UsageSnapshot?> GetLatestAsync(
        Guid providerId, Guid quotaDefinitionId, CancellationToken cancellationToken = default);
}
