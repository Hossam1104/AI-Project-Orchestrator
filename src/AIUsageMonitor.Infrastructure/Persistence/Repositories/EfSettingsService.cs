using System.Text.Json;
using AIUsageMonitor.Application.Settings;
using AIUsageMonitor.Application.Time;
using Microsoft.EntityFrameworkCore;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core-backed <see cref="ISettingsService"/>. Values are JSON-serialized — this is a
/// generic local-preferences store and must never be used to hold a secret (AGENTS.md §13).
/// </summary>
public sealed class EfSettingsService : ISettingsService
{
    private readonly AIUsageMonitorDbContext _dbContext;
    private readonly IClock _clock;

    public EfSettingsService(AIUsageMonitorDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var entry = await _dbContext.Settings
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Key == key, cancellationToken)
            .ConfigureAwait(false);

        return entry is null ? default : JsonSerializer.Deserialize<T>(entry.Value);
    }

    public async Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var json = JsonSerializer.Serialize(value);

        var existing = await _dbContext.Settings
            .SingleOrDefaultAsync(s => s.Key == key, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            _dbContext.Settings.Add(new SettingsEntry { Key = key, Value = json, UpdatedAt = _clock.UtcNow });
        }
        else
        {
            existing.Value = json;
            existing.UpdatedAt = _clock.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
