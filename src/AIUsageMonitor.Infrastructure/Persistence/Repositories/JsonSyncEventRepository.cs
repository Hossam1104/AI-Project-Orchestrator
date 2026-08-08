using AIUsageMonitor.Application.Sync;
using AIUsageMonitor.Domain.Sync;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

public sealed class JsonSyncEventRepository : ISyncEventRepository
{
    private readonly ApplicationDataPaths _paths;
    private readonly JsonlEventStore<SyncEventRecord> _events;
    private readonly ILogger<JsonSyncEventRepository> _logger;

    public JsonSyncEventRepository(
        ApplicationDataPaths paths,
        JsonlEventStore<SyncEventRecord> events,
        ILogger<JsonSyncEventRepository> logger)
    {
        _paths = paths;
        _events = events;
        _logger = logger;
    }

    public Task AddAsync(SyncEvent syncEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(syncEvent);
        return _events.AppendAsync(
            _paths.SyncDirectory,
            syncEvent.StartedAt,
            SyncEventRecord.FromDomain(syncEvent),
            cancellationToken);
    }

    public async Task<SyncEvent?> GetLatestAsync(
        Guid providerId,
        CancellationToken cancellationToken = default)
    {
        SyncEvent? latest = null;
        await foreach (var record in _events.ReadAllAsync(
                           _paths.SyncDirectory,
                           static value => value.StartedAt,
                           cancellationToken).ConfigureAwait(false))
        {
            if (record.ProviderId != providerId)
            {
                continue;
            }

            var candidate = TryMap(record);
            if (candidate is not null && (latest is null || candidate.StartedAt > latest.StartedAt))
            {
                latest = candidate;
            }
        }

        return latest;
    }

    public async Task<IReadOnlyList<SyncEvent>> GetHistoryAsync(
        Guid providerId,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        if (maxCount <= 0)
        {
            return [];
        }

        var events = new List<SyncEvent>();
        await foreach (var record in _events.ReadAllAsync(
                           _paths.SyncDirectory,
                           static value => value.StartedAt,
                           cancellationToken).ConfigureAwait(false))
        {
            if (record.ProviderId != providerId)
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
            .OrderByDescending(static value => value.StartedAt)
            .Take(maxCount)
            .ToArray();
    }

    private SyncEvent? TryMap(SyncEventRecord record)
    {
        try
        {
            return record.ToDomain();
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Skipping invalid sync event record {SyncEventId}", record.Id);
            return null;
        }
    }
}
