using AIUsageMonitor.Application.Usage;
using AIUsageMonitor.Domain.Usage;
using Microsoft.EntityFrameworkCore;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core-backed usage history persistence. Applies the BRD §16 duplicate-snapshot rule via
/// <see cref="UsageSnapshotChangeDetector"/> before writing a new row.
/// </summary>
public sealed class EfUsageSnapshotRepository : IUsageSnapshotRepository
{
    private readonly AIUsageMonitorDbContext _dbContext;

    public EfUsageSnapshotRepository(AIUsageMonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(UsageSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var latest = await _dbContext.UsageSnapshots
            .AsNoTracking()
            .Where(s => s.ProviderId == snapshot.ProviderId && s.QuotaDefinitionId == snapshot.QuotaDefinitionId)
            .OrderByDescending(s => s.Quota.CapturedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (latest is not null && !UsageSnapshotChangeDetector.HasMaterialChange(latest.Quota, snapshot.Quota))
        {
            // Nothing materially changed since the last reading — do not write a duplicate
            // history row (BRD §16).
            return;
        }

        _dbContext.UsageSnapshots.Add(snapshot);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UsageSnapshot>> GetHistoryAsync(
        Guid providerId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UsageSnapshots
            .AsNoTracking()
            .Where(s => s.ProviderId == providerId && s.Quota.CapturedAt >= from && s.Quota.CapturedAt <= to)
            .OrderBy(s => s.Quota.CapturedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<UsageSnapshot?> GetLatestAsync(
        Guid providerId, Guid quotaDefinitionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UsageSnapshots
            .AsNoTracking()
            .Where(s => s.ProviderId == providerId && s.QuotaDefinitionId == quotaDefinitionId)
            .OrderByDescending(s => s.Quota.CapturedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
