using AIUsageMonitor.Domain.Common;

namespace AIUsageMonitor.Domain.Providers;

/// <summary>
/// The connected identity for a provider, as far as it can be safely and truthfully
/// determined. Never carries credentials — only a display-safe account label.
/// </summary>
public sealed class ProviderAccount
{
    public Guid Id { get; }
    public Guid ProviderId { get; }
    public string? DisplayName { get; }
    public string? ExternalAccountId { get; }
    public DataSource Source { get; }
    public ConfidenceLevel Confidence { get; }
    public DateTimeOffset CapturedAt { get; }

    public ProviderAccount(
        Guid id,
        Guid providerId,
        string? displayName,
        string? externalAccountId,
        DataSource source,
        ConfidenceLevel confidence,
        DateTimeOffset capturedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Provider account id cannot be empty.", nameof(id));
        }

        if (providerId == Guid.Empty)
        {
            throw new ArgumentException("Provider id cannot be empty.", nameof(providerId));
        }

        Id = id;
        ProviderId = providerId;
        DisplayName = displayName;
        ExternalAccountId = externalAccountId;
        Source = source;
        Confidence = confidence;
        CapturedAt = capturedAt;
    }
}
