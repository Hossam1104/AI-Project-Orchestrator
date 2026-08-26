using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence;

internal sealed class VersionedDocument<T>
{
    public int SchemaVersion { get; set; }

    public T? Payload { get; set; }
}

internal static class FilePathLocks
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new(StringComparer.OrdinalIgnoreCase);

    public static async Task<T> ExecuteAsync<T>(string path, Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        var key = Path.GetFullPath(path);
        var gate = Locks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await operation().ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    public static async Task ExecuteAsync(string path, Func<Task> operation, CancellationToken cancellationToken)
    {
        await ExecuteAsync<object?>(path, async () =>
        {
            await operation().ConfigureAwait(false);
            return null;
        }, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Reads and writes versioned JSON documents. Writes use a temporary file plus replacement, and
/// all writers for a destination share a small in-process semaphore.
/// </summary>
public sealed class JsonFileStore
{
    public const int CurrentSchemaVersion = 1;

    private readonly ILogger<JsonFileStore> _logger;

    public JsonFileStore(ILogger<JsonFileStore> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Infrastructure-test seam for deterministic read-failure classification. Production code
    /// leaves this unset so reads use the real file system.
    /// </summary>
    internal Func<string, Exception?>? ReadFailureInjector { get; set; }

    public static JsonSerializerOptions SerializerOptions { get; } = CreateSerializerOptions();

    public static JsonSerializerOptions JsonlSerializerOptions { get; } = CreateSerializerOptions(writeIndented: false);

    public Task<T> ExecuteExclusiveAsync<T>(
        string path,
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default) =>
        FilePathLocks.ExecuteAsync(path, operation, cancellationToken);

    public Task ExecuteExclusiveAsync(
        string path,
        Func<Task> operation,
        CancellationToken cancellationToken = default) =>
        FilePathLocks.ExecuteAsync(path, operation, cancellationToken);

    public async Task<FileReadResult<T>> ReadAsync<T>(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            var injectedFailure = ReadFailureInjector?.Invoke(path);
            if (injectedFailure is not null)
            {
                throw injectedFailure;
            }

            if (!File.Exists(path))
            {
                return new FileReadResult<T>(FileReadStatus.Missing, default, path);
            }

            var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(content))
            {
                return new FileReadResult<T>(FileReadStatus.Empty, default, path);
            }

            using var document = JsonDocument.Parse(content);
            if (!document.RootElement.TryGetProperty("schemaVersion", out var schemaVersionElement) ||
                !schemaVersionElement.TryGetInt32(out var schemaVersion) ||
                schemaVersion != CurrentSchemaVersion)
            {
                await TryQuarantineAsync(path, "unsupported-schema").ConfigureAwait(false);
                return new FileReadResult<T>(
                    FileReadStatus.UnsupportedSchema,
                    default,
                    path,
                    $"The document schema is missing or unsupported. Expected version {CurrentSchemaVersion}.");
            }

            var envelope = JsonSerializer.Deserialize<VersionedDocument<T>>(content, SerializerOptions);
            if (envelope is null || envelope.Payload is null)
            {
                await TryQuarantineAsync(path, "corrupt").ConfigureAwait(false);
                return new FileReadResult<T>(FileReadStatus.Corrupt, default, path, "The document payload is missing.");
            }

            return new FileReadResult<T>(FileReadStatus.Valid, envelope.Payload!, path);
        }
        catch (JsonException exception)
        {
            await TryQuarantineAsync(path, "corrupt").ConfigureAwait(false);
            _logger.LogWarning(exception, "Quarantined corrupt JSON document {FilePath}", path);
            return new FileReadResult<T>(FileReadStatus.Corrupt, default, path, exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Permission denied while reading JSON document {FilePath}", path);
            return new FileReadResult<T>(FileReadStatus.PermissionFailure, default, path, exception.Message);
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "I/O failure while reading JSON document {FilePath}", path);
            return new FileReadResult<T>(FileReadStatus.IoFailure, default, path, exception.Message);
        }
    }

    public Task WriteAsync<T>(
        string path,
        T payload,
        CancellationToken cancellationToken = default) =>
        ExecuteExclusiveAsync(path, () => WriteCoreAsync(path, payload, cancellationToken), cancellationToken);

    /// <summary>
    /// Creates a versioned document without replacing an existing destination. The final move
    /// deliberately uses no-overwrite semantics so immutable stores can safely reject races.
    /// </summary>
    public Task CreateNewAsync<T>(
        string path,
        T payload,
        CancellationToken cancellationToken = default) =>
        ExecuteExclusiveAsync(path, () => CreateNewCoreAsync(path, payload, cancellationToken), cancellationToken);

    internal async Task WriteCoreAsync<T>(
        string path,
        T payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var envelope = new VersionedDocument<T>
        {
            SchemaVersion = CurrentSchemaVersion,
            Payload = payload
        };

        var json = JsonSerializer.Serialize(envelope, SerializerOptions);
        await AtomicWriteTextAsync(path, json, cancellationToken).ConfigureAwait(false);
    }

    internal async Task CreateNewCoreAsync<T>(
        string path,
        T payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var envelope = new VersionedDocument<T>
        {
            SchemaVersion = CurrentSchemaVersion,
            Payload = payload
        };

        var json = JsonSerializer.Serialize(envelope, SerializerOptions);
        await AtomicCreateTextAsync(path, json, cancellationToken).ConfigureAwait(false);
    }

    private async Task TryQuarantineAsync(string path, string reason)
    {
        try
        {
            if (!File.Exists(path))
            {
                return;
            }

            var quarantinePath = $"{path}.{reason}-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.bak";
            File.Move(path, quarantinePath);
            _logger.LogWarning("Moved unreadable JSON document {FilePath} to {QuarantinePath}", path, quarantinePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(exception, "Could not quarantine unreadable JSON document {FilePath}", path);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static async Task AtomicWriteTextAsync(string path, string content, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("The JSON destination must include a directory.", nameof(path));
        }

        Directory.CreateDirectory(directory);
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";

        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 16 * 1024,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(path))
            {
                try
                {
                    File.Replace(temporaryPath, path, destinationBackupFileName: null);
                }
                catch (PlatformNotSupportedException)
                {
                    File.Move(temporaryPath, path, overwrite: true);
                }
            }
            else
            {
                File.Move(temporaryPath, path);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static async Task AtomicCreateTextAsync(string path, string content, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("The JSON destination must include a directory.", nameof(path));
        }

        Directory.CreateDirectory(directory);
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";

        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 16 * 1024,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            // File.Move without overwrite is the create-new finalization. If another writer won
            // the race, the existing immutable destination remains untouched and this throws.
            File.Move(temporaryPath, path);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static JsonSerializerOptions CreateSerializerOptions(bool writeIndented = true)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = writeIndented,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = false
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
