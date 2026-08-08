namespace AIUsageMonitor.Application.Settings;

/// <summary>
/// Local, non-sensitive application preferences only (refresh interval, theme, enabled
/// providers, thresholds, etc.) — never a place for secrets (AGENTS.md §13).
/// </summary>
public interface ISettingsService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default);
}
