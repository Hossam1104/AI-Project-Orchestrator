namespace AIUsageMonitor.Domain.Sync;

/// <summary>
/// A record of one refresh attempt for a provider — used to drive stale/error UI state and
/// to avoid writing duplicate <c>UsageSnapshot</c>s when nothing changed.
/// </summary>
public sealed class SyncEvent
{
    public Guid Id { get; }
    public Guid ProviderId { get; }
    public DateTimeOffset StartedAt { get; }
    public DateTimeOffset? CompletedAt { get; }
    public bool Success { get; }
    public bool DataChanged { get; }
    public string? ErrorCode { get; }
    public string? ErrorSummary { get; }

    public SyncEvent(
        Guid id,
        Guid providerId,
        DateTimeOffset startedAt,
        DateTimeOffset? completedAt,
        bool success,
        bool dataChanged,
        string? errorCode,
        string? errorSummary)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Sync event id cannot be empty.", nameof(id));
        }

        if (providerId == Guid.Empty)
        {
            throw new ArgumentException("Provider id cannot be empty.", nameof(providerId));
        }

        if (completedAt.HasValue && completedAt.Value < startedAt)
        {
            throw new ArgumentException("Completed time cannot precede started time.", nameof(completedAt));
        }

        Id = id;
        ProviderId = providerId;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        Success = success;
        DataChanged = dataChanged;
        ErrorCode = errorCode;
        ErrorSummary = errorSummary;
    }
}
