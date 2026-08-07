namespace AIUsageMonitor.Domain.Providers;

public sealed class ProviderConnection
{
    public Guid Id { get; }
    public Guid ProviderId { get; }
    public ProviderConnectionType ConnectionType { get; }
    public ProviderConnectionStatus Status { get; }
    public string? AccountDisplayName { get; }
    public DateTimeOffset? LastSuccessfulSync { get; }
    public DateTimeOffset? LastAttempt { get; }
    public string? LastErrorCode { get; }
    public string? LastErrorMessage { get; }

    public ProviderConnection(
        Guid id,
        Guid providerId,
        ProviderConnectionType connectionType,
        ProviderConnectionStatus status,
        string? accountDisplayName,
        DateTimeOffset? lastSuccessfulSync,
        DateTimeOffset? lastAttempt,
        string? lastErrorCode,
        string? lastErrorMessage)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Provider connection id cannot be empty.", nameof(id));
        }

        if (providerId == Guid.Empty)
        {
            throw new ArgumentException("Provider id cannot be empty.", nameof(providerId));
        }

        if (lastSuccessfulSync.HasValue && lastAttempt.HasValue && lastSuccessfulSync.Value > lastAttempt.Value)
        {
            throw new ArgumentException("Last successful sync cannot be later than the last attempt.", nameof(lastSuccessfulSync));
        }

        Id = id;
        ProviderId = providerId;
        ConnectionType = connectionType;
        Status = status;
        AccountDisplayName = accountDisplayName;
        LastSuccessfulSync = lastSuccessfulSync;
        LastAttempt = lastAttempt;
        LastErrorCode = lastErrorCode;
        LastErrorMessage = lastErrorMessage;
    }
}
