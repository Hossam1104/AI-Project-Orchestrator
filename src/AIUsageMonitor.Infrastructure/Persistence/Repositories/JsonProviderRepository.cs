using AIUsageMonitor.Application.Providers;
using AIUsageMonitor.Domain.Providers;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

public sealed class JsonProviderRepository : IProviderRepository
{
    private readonly ApplicationDataPaths _paths;
    private readonly VersionedJsonCollectionStore<ProviderRecord> _records;
    private readonly ILogger<JsonProviderRepository> _logger;

    public JsonProviderRepository(
        ApplicationDataPaths paths,
        JsonFileStore files,
        ILogger<JsonProviderRepository> logger)
    {
        _paths = paths;
        _records = new VersionedJsonCollectionStore<ProviderRecord>(files);
        _logger = logger;
    }

    public async Task<IReadOnlyList<Provider>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var records = await _records.ReadAsync(_paths.ProvidersFile, cancellationToken).ConfigureAwait(false);
        return records
            .Select(TryMap)
            .Where(static provider => provider is not null)
            .Select(static provider => provider!)
            .OrderBy(static provider => provider.SortOrder)
            .ToArray();
    }

    public async Task<Provider?> GetByCodeAsync(ProviderCode code, CancellationToken cancellationToken = default)
    {
        var records = await _records.ReadAsync(_paths.ProvidersFile, cancellationToken).ConfigureAwait(false);
        var record = records.FirstOrDefault(provider => provider.Code == code);
        return record is null ? null : TryMap(record);
    }

    public Task UpsertAsync(Provider provider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        var record = ProviderRecord.FromDomain(provider);

        return _records.UpdateAsync(_paths.ProvidersFile, records =>
        {
            var index = records.FindIndex(existing => existing.Id == record.Id || existing.Code == record.Code);
            if (index >= 0)
            {
                records[index] = record;
            }
            else
            {
                records.Add(record);
            }

            return records;
        }, cancellationToken);
    }

    private Provider? TryMap(ProviderRecord record)
    {
        try
        {
            return record.ToDomain();
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Skipping invalid provider record {ProviderId}", record.Id);
            return null;
        }
    }
}
