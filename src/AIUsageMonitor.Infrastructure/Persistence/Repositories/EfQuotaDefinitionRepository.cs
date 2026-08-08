using AIUsageMonitor.Application.Quotas;
using AIUsageMonitor.Domain.Quotas;
using Microsoft.EntityFrameworkCore;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

public sealed class EfQuotaDefinitionRepository : IQuotaDefinitionRepository
{
    private readonly AIUsageMonitorDbContext _dbContext;

    public EfQuotaDefinitionRepository(AIUsageMonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<QuotaDefinition>> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default) =>
        await _dbContext.QuotaDefinitions
            .AsNoTracking()
            .Where(q => q.ProviderId == providerId)
            .OrderBy(q => q.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<QuotaDefinition?> GetByExternalKeyAsync(Guid providerId, string externalKey, CancellationToken cancellationToken = default) =>
        await _dbContext.QuotaDefinitions
            .AsNoTracking()
            .SingleOrDefaultAsync(q => q.ProviderId == providerId && q.ExternalKey == externalKey, cancellationToken)
            .ConfigureAwait(false);

    public async Task<QuotaDefinition> GetOrCreateAsync(
        Guid providerId,
        string externalKey,
        string name,
        QuotaType type,
        QuotaUnit unit,
        int sortOrder,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.QuotaDefinitions
            .SingleOrDefaultAsync(q => q.ProviderId == providerId && q.ExternalKey == externalKey, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return existing;
        }

        var created = new QuotaDefinition(Guid.NewGuid(), providerId, externalKey, name, type, unit, sortOrder);
        _dbContext.QuotaDefinitions.Add(created);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return created;
    }
}
