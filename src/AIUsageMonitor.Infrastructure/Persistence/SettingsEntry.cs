namespace AIUsageMonitor.Infrastructure.Persistence;

/// <summary>
/// The persisted row shape backing <c>ISettingsService</c> (BRD §30 Settings — "non-sensitive
/// application preferences only"). This is an Infrastructure-only persistence detail, not a
/// Domain concept, since <c>ISettingsService</c> is a generic typed key/value store.
/// </summary>
public sealed class SettingsEntry
{
    public string Key { get; set; } = string.Empty;

    /// <summary>JSON-serialized value. Never a place for secrets — see AGENTS.md §13.</summary>
    public string Value { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }
}
