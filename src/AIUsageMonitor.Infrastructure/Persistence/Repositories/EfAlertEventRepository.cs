using AIUsageMonitor.Application.Alerts;
using AIUsageMonitor.Domain.Alerts;
using Microsoft.EntityFrameworkCore;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

public sealed class EfAlertEventRepository : IAlertEventRepository
{
    private readonly AIUsageMonitorDbContext _dbContext;

    public EfAlertEventRepository(AIUsageMonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AlertEvent alertEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(alertEvent);

        _dbContext.AlertEvents.Add(alertEvent);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<AlertEvent?> GetLatestUnresolvedAsync(Guid alertRuleId, AlertType type, CancellationToken cancellationToken = default) =>
        await _dbContext.AlertEvents
            .AsNoTracking()
            .Where(e => e.AlertRuleId == alertRuleId && e.Type == type && e.ResolvedAt == null)
            .OrderByDescending(e => e.TriggeredAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<AlertEvent>> GetHistoryAsync(Guid alertRuleId, int maxCount, CancellationToken cancellationToken = default) =>
        await _dbContext.AlertEvents
            .AsNoTracking()
            .Where(e => e.AlertRuleId == alertRuleId)
            .OrderByDescending(e => e.TriggeredAt)
            .Take(maxCount)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
