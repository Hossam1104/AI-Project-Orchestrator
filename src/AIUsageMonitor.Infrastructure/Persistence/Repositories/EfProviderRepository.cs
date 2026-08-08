using AIUsageMonitor.Application.Providers;
using AIUsageMonitor.Domain.Providers;
using Microsoft.EntityFrameworkCore;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

public sealed class EfProviderRepository : IProviderRepository
{
    private readonly AIUsageMonitorDbContext _dbContext;

    public EfProviderRepository(AIUsageMonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Provider>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Providers
            .AsNoTracking()
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<Provider?> GetByCodeAsync(ProviderCode code, CancellationToken cancellationToken = default) =>
        await _dbContext.Providers
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Code == code, cancellationToken)
            .ConfigureAwait(false);

    public async Task UpsertAsync(Provider provider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);

        var existing = await _dbContext.Providers
            .SingleOrDefaultAsync(p => p.Id == provider.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            _dbContext.Providers.Add(provider);
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(provider);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
