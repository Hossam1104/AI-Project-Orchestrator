using AIUsageMonitor.Application.Providers;
using AIUsageMonitor.Domain.Providers;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

public sealed class JsonProviderConnectionRepository : IProviderConnectionRepository
{
    private readonly ApplicationDataPaths _paths;
    private readonly VersionedJsonCollectionStore<ProviderConnectionRecord> _records;
    private readonly ILogger<JsonProviderConnectionRepository> _logger;

    public JsonProviderConnectionRepository(
        ApplicationDataPaths paths,
        JsonFileStore files,
        ILogger<JsonProviderConnectionRepository> logger)
    {
        _paths = paths;
        _records = new VersionedJsonCollectionStore<ProviderConnectionRecord>(files);
        _logger = logger;
    }

    public async Task<ProviderConnection?> GetByProviderIdAsync(
        Guid providerId,
        CancellationToken cancellationToken = default)
    {
        var records = await _records.ReadAsync(_paths.ConnectionsFile, cancellationToken).ConfigureAwait(false);
        var record = records.FirstOrDefault(connection => connection.ProviderId == providerId);
        return record is null ? null : TryMap(record);
    }

    public Task UpsertAsync(ProviderConnection connection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        var record = ProviderConnectionRecord.FromDomain(connection);

        return _records.UpdateAsync(_paths.ConnectionsFile, records =>
        {
            var index = records.FindIndex(existing =>
                existing.Id == record.Id || existing.ProviderId == record.ProviderId);
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

    private ProviderConnection? TryMap(ProviderConnectionRecord record)
    {
        try
        {
            return record.ToDomain();
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Skipping invalid provider connection record {ConnectionId}", record.Id);
            return null;
        }
    }
}
