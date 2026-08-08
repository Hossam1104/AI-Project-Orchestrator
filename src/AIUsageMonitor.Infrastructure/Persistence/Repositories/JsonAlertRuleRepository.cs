using AIUsageMonitor.Application.Alerts;
using AIUsageMonitor.Domain.Alerts;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

public sealed class JsonAlertRuleRepository : IAlertRuleRepository
{
    private readonly ApplicationDataPaths _paths;
    private readonly VersionedJsonCollectionStore<AlertRuleRecord> _records;
    private readonly ILogger<JsonAlertRuleRepository> _logger;

    public JsonAlertRuleRepository(
        ApplicationDataPaths paths,
        JsonFileStore files,
        ILogger<JsonAlertRuleRepository> logger)
    {
        _paths = paths;
        _records = new VersionedJsonCollectionStore<AlertRuleRecord>(files);
        _logger = logger;
    }

    public async Task<IReadOnlyList<AlertRule>> GetByProviderIdAsync(
        Guid providerId,
        CancellationToken cancellationToken = default)
    {
        var records = await _records.ReadAsync(_paths.AlertRulesFile, cancellationToken).ConfigureAwait(false);
        return records
            .Where(rule => rule.ProviderId == providerId)
            .Select(TryMap)
            .Where(static rule => rule is not null)
            .Select(static rule => rule!)
            .ToArray();
    }

    public Task UpsertAsync(AlertRule rule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var record = AlertRuleRecord.FromDomain(rule);

        return _records.UpdateAsync(_paths.AlertRulesFile, records =>
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

    private AlertRule? TryMap(AlertRuleRecord record)
    {
        try
        {
            return record.ToDomain();
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Skipping invalid alert rule record {AlertRuleId}", record.Id);
            return null;
        }
    }
}
