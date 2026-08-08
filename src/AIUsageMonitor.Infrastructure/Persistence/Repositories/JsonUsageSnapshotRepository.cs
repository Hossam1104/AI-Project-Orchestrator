using AIUsageMonitor.Application.Usage;
using AIUsageMonitor.Domain.Usage;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

/// <summary>
/// Monthly JSONL usage history. Reads are incremental and range-aware; appends preserve the
/// application-layer material-change rule so refreshes with unchanged values do not create noise.
/// </summary>
public sealed class JsonUsageSnapshotRepository : IUsageSnapshotRepository
{
    private readonly ApplicationDataPaths _paths;
    private readonly JsonFileStore _files;
    private readonly JsonlEventStore<UsageSnapshotRecord> _snapshots;
    private readonly ILogger<JsonUsageSnapshotRepository> _logger;

    public JsonUsageSnapshotRepository(
        ApplicationDataPaths paths,
        JsonFileStore files,
        JsonlEventStore<UsageSnapshotRecord> snapshots,
        ILogger<JsonUsageSnapshotRepository> logger)
    {
        _paths = paths;
        _files = files;
        _snapshots = snapshots;
        _logger = logger;
    }

    public Task AddAsync(UsageSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        // The lock key is not a data file. It coordinates the read/compare/append sequence for a
        // provider/quota pair across repository instances without introducing a distributed lock.
        var lockPath = Path.Combine(
            _paths.HistoryDirectory,
            $".snapshot-{snapshot.ProviderId:N}-{snapshot.QuotaDefinitionId:N}.lock");

        return _files.ExecuteExclusiveAsync(lockPath, async () =>
        {
            var latest = await GetLatestAsync(snapshot.ProviderId, snapshot.QuotaDefinitionId, cancellationToken)
                .ConfigureAwait(false);
            if (latest is not null &&
                !UsageSnapshotChangeDetector.HasMaterialChange(latest.Quota, snapshot.Quota))
            {
                return;
            }

            await _snapshots.AppendAsync(
                    _paths.HistoryDirectory,
                    snapshot.CapturedAt,
                    UsageSnapshotRecord.FromDomain(snapshot),
                    cancellationToken)
                .ConfigureAwait(false);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<UsageSnapshot>> GetHistoryAsync(
        Guid providerId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        if (from > to)
        {
            return [];
        }

        var snapshots = new List<UsageSnapshot>();
        await foreach (var record in _snapshots.ReadRangeAsync(
                           _paths.HistoryDirectory,
                           from,
                           to,
                           static value => value.CapturedAt,
                           cancellationToken).ConfigureAwait(false))
        {
            if (record.ProviderId != providerId)
            {
                continue;
            }

            var mapped = TryMap(record);
            if (mapped is not null)
            {
                snapshots.Add(mapped);
            }
        }

        return snapshots
            .OrderBy(static snapshot => snapshot.CapturedAt)
            .ThenBy(static snapshot => snapshot.Id)
            .ToArray();
    }

    public async Task<UsageSnapshot?> GetLatestAsync(
        Guid providerId,
        Guid quotaDefinitionId,
        CancellationToken cancellationToken = default)
    {
        return await _snapshots.ReadLatestAsync(
                _paths.HistoryDirectory,
                static value => value.CapturedAt,
                record => record.ProviderId == providerId &&
                          record.QuotaDefinitionId == quotaDefinitionId
                    ? TryMap(record)
                    : null,
                static value => value.CapturedAt,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private UsageSnapshot? TryMap(UsageSnapshotRecord record)
    {
        try
        {
            return record.ToDomain();
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Skipping invalid usage snapshot record {UsageSnapshotId}", record.Id);
            return null;
        }
    }
}
