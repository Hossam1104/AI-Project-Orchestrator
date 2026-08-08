using AIUsageMonitor.Domain.Quotas;

namespace AIUsageMonitor.Domain.Usage;

/// <summary>
/// A persisted point-in-time reading of a <see cref="QuotaWindow"/>, tied to a provider and
/// quota definition. Reuses <see cref="QuotaWindow"/>'s normalization/validation rather than
/// duplicating them.
/// </summary>
public sealed class UsageSnapshot
{
    public Guid Id { get; private set; }
    public Guid ProviderId { get; private set; }
    public Guid QuotaDefinitionId { get; private set; }

    /// <summary>
    /// Private setter only so EF Core can assign the materialized owned instance after
    /// constructing this entity (see the private constructor below) — application/domain
    /// code must always go through the public validating constructor.
    /// </summary>
    public QuotaWindow Quota { get; private set; } = null!;

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

    /// <summary>
    /// EF Core materialization constructor. EF cannot constructor-bind the owned
    /// <see cref="Quota"/> navigation when it is table-split onto <c>UsageSnapshots</c> (see
    /// .ai/CURRENT_STATE.md Session 03 notes), so this constructor only binds the scalar
    /// mapped properties; EF assigns <see cref="Quota"/> afterward via its private setter once
    /// the owned instance has been materialized separately. Not for application/domain use.
    /// </summary>
    private UsageSnapshot(Guid id, Guid providerId, Guid quotaDefinitionId)
    {
        Id = id;
        ProviderId = providerId;
        QuotaDefinitionId = quotaDefinitionId;
    }
}
