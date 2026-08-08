using AIUsageMonitor.Application.Subscriptions;
using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core-backed subscription persistence (BRD §14). Full subscription business logic
/// (manual-vs-provider precedence, editing UI) is Session 13 scope — this session only
/// provides the persistence primitives.
/// </summary>
public sealed class EfSubscriptionService : ISubscriptionService
{
    private readonly AIUsageMonitorDbContext _dbContext;

    public EfSubscriptionService(AIUsageMonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Subscription?> GetSubscriptionAsync(ProviderCode code, CancellationToken cancellationToken = default)
    {
        var provider = await _dbContext.Providers
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Code == code, cancellationToken)
            .ConfigureAwait(false);

        if (provider is null)
        {
            return null;
        }

        return await _dbContext.Subscriptions
            .AsNoTracking()
            .Where(s => s.ProviderId == provider.Id)
            .OrderByDescending(s => s.LastVerifiedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SaveManualSubscriptionAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        var existing = await _dbContext.Subscriptions
            .SingleOrDefaultAsync(s => s.Id == subscription.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            _dbContext.Subscriptions.Add(subscription);
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(subscription);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
