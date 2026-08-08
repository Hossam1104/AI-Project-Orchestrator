using AIUsageMonitor.Application.Providers;
using AIUsageMonitor.Domain.Providers;
using Microsoft.EntityFrameworkCore;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

public sealed class EfProviderConnectionRepository : IProviderConnectionRepository
{
    private readonly AIUsageMonitorDbContext _dbContext;

    public EfProviderConnectionRepository(AIUsageMonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProviderConnection?> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default) =>
        await _dbContext.ProviderConnections
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.ProviderId == providerId, cancellationToken)
            .ConfigureAwait(false);

    public async Task UpsertAsync(ProviderConnection connection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var existing = await _dbContext.ProviderConnections
            .SingleOrDefaultAsync(c => c.Id == connection.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            _dbContext.ProviderConnections.Add(connection);
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(connection);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
