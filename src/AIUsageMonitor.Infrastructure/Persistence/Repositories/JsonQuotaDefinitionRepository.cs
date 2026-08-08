using AIUsageMonitor.Application.Quotas;
using AIUsageMonitor.Domain.Quotas;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

public sealed class JsonQuotaDefinitionRepository : IQuotaDefinitionRepository
{
    private readonly ApplicationDataPaths _paths;
    private readonly VersionedJsonCollectionStore<QuotaDefinitionRecord> _records;
    private readonly ILogger<JsonQuotaDefinitionRepository> _logger;

    public JsonQuotaDefinitionRepository(
        ApplicationDataPaths paths,
        JsonFileStore files,
        ILogger<JsonQuotaDefinitionRepository> logger)
    {
        _paths = paths;
        _records = new VersionedJsonCollectionStore<QuotaDefinitionRecord>(files);
        _logger = logger;
    }

    public async Task<IReadOnlyList<QuotaDefinition>> GetByProviderIdAsync(
        Guid providerId,
        CancellationToken cancellationToken = default)
    {
        var records = await _records.ReadAsync(_paths.QuotaDefinitionsFile, cancellationToken).ConfigureAwait(false);
        return records
            .Where(definition => definition.ProviderId == providerId)
            .Select(TryMap)
            .Where(static definition => definition is not null)
            .Select(static definition => definition!)
            .OrderBy(static definition => definition.SortOrder)
            .ToArray();
    }

    public async Task<QuotaDefinition?> GetByExternalKeyAsync(
        Guid providerId,
        string externalKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalKey);
        var records = await _records.ReadAsync(_paths.QuotaDefinitionsFile, cancellationToken).ConfigureAwait(false);
        var record = records.FirstOrDefault(definition =>
            definition.ProviderId == providerId &&
            string.Equals(definition.ExternalKey, externalKey, StringComparison.Ordinal));
        return record is null ? null : TryMap(record);
    }

    public Task<QuotaDefinition> GetOrCreateAsync(
        Guid providerId,
        string externalKey,
        string name,
        QuotaType type,
        QuotaUnit unit,
        int sortOrder,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return _records.UpdateAsync(_paths.QuotaDefinitionsFile, records =>
        {
            var existing = records.FirstOrDefault(definition =>
                definition.ProviderId == providerId &&
                string.Equals(definition.ExternalKey, externalKey, StringComparison.Ordinal));
            if (existing is not null)
            {
                return (records, existing.ToDomain());
            }

            var created = new QuotaDefinitionRecord
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                ExternalKey = externalKey,
                Name = name,
                Type = type,
                Unit = unit,
                SortOrder = sortOrder
            };
            records.Add(created);
            return (records, created.ToDomain());
        }, cancellationToken);
    }

    private QuotaDefinition? TryMap(QuotaDefinitionRecord record)
    {
        try
        {
            return record.ToDomain();
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Skipping invalid quota definition record {QuotaDefinitionId}", record.Id);
            return null;
        }
    }
}
