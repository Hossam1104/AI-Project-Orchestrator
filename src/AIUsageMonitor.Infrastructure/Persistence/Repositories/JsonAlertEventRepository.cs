using AIUsageMonitor.Application.Alerts;
using AIUsageMonitor.Domain.Alerts;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

public sealed class JsonAlertEventRepository : IAlertEventRepository
{
    private readonly ApplicationDataPaths _paths;
    private readonly JsonlEventStore<AlertEventRecord> _events;
    private readonly ILogger<JsonAlertEventRepository> _logger;

    public JsonAlertEventRepository(
        ApplicationDataPaths paths,
        JsonlEventStore<AlertEventRecord> events,
        ILogger<JsonAlertEventRepository> logger)
    {
        _paths = paths;
        _events = events;
        _logger = logger;
    }

    public Task AddAsync(AlertEvent alertEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(alertEvent);
        return _events.AppendAsync(
            _paths.AlertsDirectory,
            alertEvent.TriggeredAt,
            AlertEventRecord.FromDomain(alertEvent),
            cancellationToken);
    }

    public async Task<AlertEvent?> GetLatestUnresolvedAsync(
        Guid alertRuleId,
        AlertType type,
        CancellationToken cancellationToken = default)
    {
        AlertEvent? latest = null;
        await foreach (var record in _events.ReadAllAsync(
                           _paths.AlertsDirectory,
                           static value => value.TriggeredAt,
                           cancellationToken).ConfigureAwait(false))
        {
            if (record.AlertRuleId != alertRuleId || record.Type != type || record.ResolvedAt is not null)
            {
                continue;
            }

            var candidate = TryMap(record);
            if (candidate is not null && (latest is null || candidate.TriggeredAt > latest.TriggeredAt))
            {
                latest = candidate;
            }
        }

        return latest;
    }

    public async Task<IReadOnlyList<AlertEvent>> GetHistoryAsync(
        Guid alertRuleId,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        if (maxCount <= 0)
        {
            return [];
        }

        var events = new List<AlertEvent>();
        await foreach (var record in _events.ReadAllAsync(
                           _paths.AlertsDirectory,
                           static value => value.TriggeredAt,
                           cancellationToken).ConfigureAwait(false))
        {
            if (record.AlertRuleId != alertRuleId)
            {
                continue;
            }

            var mapped = TryMap(record);
            if (mapped is not null)
            {
                events.Add(mapped);
            }
        }

        return events
            .OrderByDescending(static value => value.TriggeredAt)
            .Take(maxCount)
            .ToArray();
    }

    private AlertEvent? TryMap(AlertEventRecord record)
    {
        try
        {
            return record.ToDomain();
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Skipping invalid alert event record {AlertEventId}", record.Id);
            return null;
        }
    }
}
