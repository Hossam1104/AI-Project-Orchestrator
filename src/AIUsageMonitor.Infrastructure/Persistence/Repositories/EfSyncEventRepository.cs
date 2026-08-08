using AIUsageMonitor.Application.Sync;
using AIUsageMonitor.Domain.Sync;
using Microsoft.EntityFrameworkCore;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

public sealed class EfSyncEventRepository : ISyncEventRepository
{
    private readonly AIUsageMonitorDbContext _dbContext;

    public EfSyncEventRepository(AIUsageMonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(SyncEvent syncEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(syncEvent);

        _dbContext.SyncEvents.Add(syncEvent);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<SyncEvent?> GetLatestAsync(Guid providerId, CancellationToken cancellationToken = default) =>
        await _dbContext.SyncEvents
            .AsNoTracking()
            .Where(e => e.ProviderId == providerId)
            .OrderByDescending(e => e.StartedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<SyncEvent>> GetHistoryAsync(Guid providerId, int maxCount, CancellationToken cancellationToken = default) =>
        await _dbContext.SyncEvents
            .AsNoTracking()
            .Where(e => e.ProviderId == providerId)
            .OrderByDescending(e => e.StartedAt)
            .Take(maxCount)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
