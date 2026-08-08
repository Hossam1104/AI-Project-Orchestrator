using System.Text.Json;
using AIUsageMonitor.Application.Settings;
using AIUsageMonitor.Application.Time;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

public sealed class JsonSettingsService : ISettingsService
{
    private readonly ApplicationDataPaths _paths;
    private readonly JsonFileStore _files;
    private readonly IClock _clock;
    private readonly ILogger<JsonSettingsService> _logger;

    public JsonSettingsService(
        ApplicationDataPaths paths,
        JsonFileStore files,
        IClock clock,
        ILogger<JsonSettingsService> logger)
    {
        _paths = paths;
        _files = files;
        _clock = clock;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var result = await _files.ReadAsync<SettingsDocument>(_paths.SettingsFile, cancellationToken)
            .ConfigureAwait(false);
        if (!result.IsUsable || result.Value!.Values is null || !result.Value.Values.TryGetValue(key, out var value))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(value.Value.GetRawText(), JsonFileStore.SerializerOptions);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Setting {SettingKey} contains an invalid typed value", key);
            return default;
        }
    }

    public Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var serializedValue = JsonSerializer.SerializeToElement(value, JsonFileStore.SerializerOptions).Clone();

        return _files.ExecuteExclusiveAsync(_paths.SettingsFile, async () =>
        {
            var result = await _files.ReadAsync<SettingsDocument>(_paths.SettingsFile, cancellationToken)
                .ConfigureAwait(false);
            var values = result.IsUsable && result.Value!.Values is not null
                ? new Dictionary<string, SettingsValueRecord>(result.Value!.Values, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, SettingsValueRecord>(StringComparer.OrdinalIgnoreCase);

            values[key] = new SettingsValueRecord
            {
                Value = serializedValue,
                UpdatedAt = _clock.UtcNow
            };

            await _files.WriteCoreAsync(
                    _paths.SettingsFile,
                    new SettingsDocument { Values = values },
                    cancellationToken)
                .ConfigureAwait(false);
        }, cancellationToken);
    }
}
