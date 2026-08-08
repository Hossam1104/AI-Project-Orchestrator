using AIUsageMonitor.Application.Alerts;
using AIUsageMonitor.Domain.Alerts;
using Microsoft.EntityFrameworkCore;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

public sealed class EfAlertRuleRepository : IAlertRuleRepository
{
    private readonly AIUsageMonitorDbContext _dbContext;

    public EfAlertRuleRepository(AIUsageMonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AlertRule>> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default) =>
        await _dbContext.AlertRules
            .AsNoTracking()
            .Where(r => r.ProviderId == providerId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task UpsertAsync(AlertRule rule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rule);

        var existing = await _dbContext.AlertRules
            .SingleOrDefaultAsync(r => r.Id == rule.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            _dbContext.AlertRules.Add(rule);
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(rule);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
