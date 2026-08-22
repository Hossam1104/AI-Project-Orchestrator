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

    /// <summary>
    /// An opaque lookup key into secure storage (e.g. "GitHub:Copilot:Primary"), resolved
    /// through <c>ISecureCredentialStore</c>. This must NEVER contain the actual secret —
    /// no password, token, cookie, or OAuth secret. It is case-insensitive: casing must not
    /// be used to distinguish two credentials. Actual credential storage/retrieval is
    /// implemented against Windows Credential Manager (BRD §31).
    /// </summary>
    public string? CredentialReference { get; }

    public ProviderConnection(
        Guid id,
        Guid providerId,
        ProviderConnectionType connectionType,
        ProviderConnectionStatus status,
        string? accountDisplayName,
        DateTimeOffset? lastSuccessfulSync,
        DateTimeOffset? lastAttempt,
        string? lastErrorCode,
        string? lastErrorMessage,
        string? credentialReference = null)
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

        if (credentialReference is not null && string.IsNullOrWhiteSpace(credentialReference))
        {
            throw new ArgumentException("Credential reference cannot be whitespace-only.", nameof(credentialReference));
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
        CredentialReference = credentialReference;
    }
}
