using AIUsageMonitor.Application.Providers;
using AIUsageMonitor.Application.Subscriptions;
using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Subscriptions;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

public sealed class JsonSubscriptionService : ISubscriptionService
{
    private readonly ApplicationDataPaths _paths;
    private readonly IProviderRepository _providers;
    private readonly VersionedJsonCollectionStore<SubscriptionRecord> _records;
    private readonly ILogger<JsonSubscriptionService> _logger;

    public JsonSubscriptionService(
        ApplicationDataPaths paths,
        IProviderRepository providers,
        JsonFileStore files,
        ILogger<JsonSubscriptionService> logger)
    {
        _paths = paths;
        _providers = providers;
        _records = new VersionedJsonCollectionStore<SubscriptionRecord>(files);
        _logger = logger;
    }

    public async Task<Subscription?> GetSubscriptionAsync(
        ProviderCode code,
        CancellationToken cancellationToken = default)
    {
        var provider = await _providers.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
        if (provider is null)
        {
            return null;
        }

        var records = await _records.ReadAsync(_paths.SubscriptionsFile, cancellationToken).ConfigureAwait(false);
        return records
            .Where(subscription => subscription.ProviderId == provider.Id)
            .OrderByDescending(subscription => subscription.LastVerifiedAt ?? DateTimeOffset.MinValue)
            .Select(TryMap)
            .FirstOrDefault(static subscription => subscription is not null);
    }

    public Task SaveManualSubscriptionAsync(
        Subscription subscription,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        var record = SubscriptionRecord.FromDomain(subscription);

        return _records.UpdateAsync(_paths.SubscriptionsFile, records =>
        {
            var index = records.FindIndex(existing => existing.Id == record.Id);
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

    private Subscription? TryMap(SubscriptionRecord record)
    {
        try
        {
            return record.ToDomain();
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Skipping invalid subscription record {SubscriptionId}", record.Id);
            return null;
        }
    }
}
