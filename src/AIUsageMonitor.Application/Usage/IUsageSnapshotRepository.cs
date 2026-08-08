using AIUsageMonitor.Domain.Usage;

namespace AIUsageMonitor.Application.Usage;

/// <summary>
/// Persistence abstraction for usage history. The Application layer does not know whether the
/// Infrastructure implementation uses JSONL or another approved local store.
/// </summary>
public interface IUsageSnapshotRepository
{
    Task AddAsync(UsageSnapshot snapshot, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UsageSnapshot>> GetHistoryAsync(
        Guid providerId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

    Task<UsageSnapshot?> GetLatestAsync(
        Guid providerId, Guid quotaDefinitionId, CancellationToken cancellationToken = default);
}
