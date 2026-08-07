using AIUsageMonitor.Domain.Quotas;

namespace AIUsageMonitor.Domain.Usage;

/// <summary>
/// A persisted point-in-time reading of a <see cref="QuotaWindow"/>, tied to a provider and
/// quota definition. Reuses <see cref="QuotaWindow"/>'s normalization/validation rather than
/// duplicating them.
/// </summary>
public sealed class UsageSnapshot
{
    public Guid Id { get; }
    public Guid ProviderId { get; }
    public Guid QuotaDefinitionId { get; }
    public QuotaWindow Quota { get; }

    public DateTimeOffset CapturedAt => Quota.CapturedAt;

    public UsageSnapshot(Guid id, Guid providerId, Guid quotaDefinitionId, QuotaWindow quota)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Usage snapshot id cannot be empty.", nameof(id));
        }

        if (providerId == Guid.Empty)
        {
            throw new ArgumentException("Provider id cannot be empty.", nameof(providerId));
        }

        if (quotaDefinitionId == Guid.Empty)
        {
            throw new ArgumentException("Quota definition id cannot be empty.", nameof(quotaDefinitionId));
        }

        Id = id;
        ProviderId = providerId;
        QuotaDefinitionId = quotaDefinitionId;
        Quota = quota ?? throw new ArgumentNullException(nameof(quota));
    }
}
