using AIUsageMonitor.Domain.Sync;

namespace AIUsageMonitor.Application.Sync;

/// <summary>
/// Persistence for provider refresh-attempt history (BRD §30 SyncEvents) — drives stale/error
/// UI state and refresh diagnostics.
/// </summary>
public interface ISyncEventRepository
{
    Task AddAsync(SyncEvent syncEvent, CancellationToken cancellationToken = default);

    Task<SyncEvent?> GetLatestAsync(Guid providerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SyncEvent>> GetHistoryAsync(Guid providerId, int maxCount, CancellationToken cancellationToken = default);
}
