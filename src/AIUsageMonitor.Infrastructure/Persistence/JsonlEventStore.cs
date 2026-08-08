using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence;

/// <summary>
/// Shared append/read mechanics for monthly JSONL streams. Readers parse one line at a time and
/// isolate malformed records so a bad optional history partition cannot block startup or other
/// months.
/// </summary>
public sealed class JsonlEventStore<TRecord>
    where TRecord : class
{
    private readonly ApplicationDataPaths _paths;
    private readonly JsonFileStore _files;
    private readonly ILogger<JsonlEventStore<TRecord>> _logger;

    public JsonlEventStore(
        ApplicationDataPaths paths,
        JsonFileStore files,
        ILogger<JsonlEventStore<TRecord>> logger)
    {
        _paths = paths;
        _files = files;
        _logger = logger;
    }

    public async Task AppendAsync(
        string directory,
        DateTimeOffset timestamp,
        TRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        await _paths.EnsureDirectoriesAsync(cancellationToken).ConfigureAwait(false);

        var path = _paths.GetMonthlyPartition(directory, timestamp);
        await _files.ExecuteExclusiveAsync(path, async () =>
        {
            Directory.CreateDirectory(directory);
            await using var stream = new FileStream(
                path,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.Read,
                bufferSize: 16 * 1024,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);

            stream.Position = stream.Length;
            if (HasUnterminatedTail(stream))
            {
                _logger.LogWarning(
                    "Isolating an unterminated JSONL tail before appending to {FilePath}",
                    path);
                await stream.WriteAsync("\n"u8.ToArray(), cancellationToken).ConfigureAwait(false);
            }

            var line = JsonSerializer.Serialize(record, JsonFileStore.JsonlSerializerOptions);
            await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(line), cancellationToken)
                .ConfigureAwait(false);
            await stream.WriteAsync("\n"u8.ToArray(), cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            stream.Flush(flushToDisk: true);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TResult?> ReadLatestAsync<TResult>(
        string directory,
        Func<TRecord, DateTimeOffset> timestampSelector,
        Func<TRecord, TResult?> map,
        Func<TResult, DateTimeOffset> mappedTimestampSelector,
        CancellationToken cancellationToken = default)
        where TResult : class
    {
        ArgumentNullException.ThrowIfNull(timestampSelector);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(mappedTimestampSelector);

        if (!Directory.Exists(directory))
        {
            return null;
        }

        foreach (var path in EnumerateMonthlyPathsDescending(directory))
        {
            var records = await ReadFileAsync(path, timestampSelector, cancellationToken)
                .ConfigureAwait(false);
            TResult? latest = null;
            DateTimeOffset latestTimestamp = default;

            foreach (var record in records)
            {
                var mapped = map(record);
                if (mapped is null)
                {
                    continue;
                }

                var candidateTimestamp = mappedTimestampSelector(mapped);
                if (latest is null || candidateTimestamp >= latestTimestamp)
                {
                    latest = mapped;
                    latestTimestamp = candidateTimestamp;
                }
            }

            // JSONL partitions are named by the UTC month of the captured event. Once a valid
            // matching value exists in the newest partition, older partitions cannot supersede it.
            if (latest is not null)
            {
                return latest;
            }
        }

        return null;
    }

    public async IAsyncEnumerable<TRecord> ReadRangeAsync(
        string directory,
        DateTimeOffset from,
        DateTimeOffset to,
        Func<TRecord, DateTimeOffset> timestampSelector,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (from > to || !Directory.Exists(directory))
        {
            yield break;
        }

        foreach (var path in EnumerateMonthlyPaths(directory, from, to))
        {
            var records = await ReadFileAsync(path, timestampSelector, cancellationToken).ConfigureAwait(false);
            foreach (var record in records)
            {
                var timestamp = timestampSelector(record);
                if (timestamp >= from && timestamp <= to)
                {
                    yield return record;
                }
            }
        }
    }

    public async IAsyncEnumerable<TRecord> ReadAllAsync(
        string directory,
        Func<TRecord, DateTimeOffset> timestampSelector,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        foreach (var path in Directory.EnumerateFiles(directory, "*.jsonl", SearchOption.TopDirectoryOnly)
                     .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
        {
            var records = await ReadFileAsync(path, timestampSelector, cancellationToken).ConfigureAwait(false);
            foreach (var record in records)
            {
                yield return record;
            }
        }
    }

    private Task<IReadOnlyList<TRecord>> ReadFileAsync(
        string path,
        Func<TRecord, DateTimeOffset> timestampSelector,
        CancellationToken cancellationToken) =>
        _files.ExecuteExclusiveAsync(
            path,
            () => ReadFileCoreAsync(path, timestampSelector, cancellationToken),
            cancellationToken);

    private static bool HasUnterminatedTail(FileStream stream)
    {
        if (stream.Length == 0)
        {
            return false;
        }

        stream.Position = stream.Length - 1;
        var lastByte = stream.ReadByte();
        stream.Position = stream.Length;
        return lastByte != '\n';
    }

    private async Task<IReadOnlyList<TRecord>> ReadFileCoreAsync(
        string path,
        Func<TRecord, DateTimeOffset> timestampSelector,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        var records = new List<TRecord>();
        try
        {
            if (new FileInfo(path).Length == 0)
            {
                return records;
            }

            await foreach (var line in File.ReadLinesAsync(path, cancellationToken).ConfigureAwait(false))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                TRecord? record;
                try
                {
                    using var document = JsonDocument.Parse(line);
                    if (!document.RootElement.TryGetProperty("schemaVersion", out var schemaElement) ||
                        !schemaElement.TryGetInt32(out var schemaVersion) ||
                        schemaVersion != JsonFileStore.CurrentSchemaVersion)
                    {
                        _logger.LogWarning("Skipping unsupported JSONL record in {FilePath}", path);
                        continue;
                    }

                    record = JsonSerializer.Deserialize<TRecord>(line, JsonFileStore.SerializerOptions);
                }
                catch (JsonException exception)
                {
                    _logger.LogWarning(exception, "Skipping corrupt JSONL record in {FilePath}", path);
                    continue;
                }

                if (record is null)
                {
                    _logger.LogWarning("Skipping empty JSONL record in {FilePath}", path);
                    continue;
                }

                try
                {
                    _ = timestampSelector(record);
                }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                {
                    _logger.LogWarning(exception, "Skipping invalid JSONL record in {FilePath}", path);
                    continue;
                }

                records.Add(record);
            }
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Permission denied while reading JSONL partition {FilePath}", path);
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "I/O failure while reading JSONL partition {FilePath}", path);
        }

        return records;
    }

    private static IEnumerable<string> EnumerateMonthlyPathsDescending(string directory) =>
        Directory.EnumerateFiles(directory, "*.jsonl", SearchOption.TopDirectoryOnly)
            .OrderByDescending(
                static path => Path.GetFileName(path),
                StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<string> EnumerateMonthlyPaths(string directory, DateTimeOffset from, DateTimeOffset to)
    {
        var month = new DateTime(from.UtcDateTime.Year, from.UtcDateTime.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var finalMonth = new DateTime(to.UtcDateTime.Year, to.UtcDateTime.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        while (month <= finalMonth)
        {
            var path = Path.Combine(directory, $"{month:yyyy-MM}.jsonl");
            if (File.Exists(path))
            {
                yield return path;
            }

            month = month.AddMonths(1);
        }
    }
}
