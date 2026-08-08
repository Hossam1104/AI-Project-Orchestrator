using AIUsageMonitor.Domain.Quotas;

namespace AIUsageMonitor.Application.Quotas;

/// <summary>
/// Persistence for the stable per-provider quota identities a provider exposes (BRD §30
/// QuotaDefinitions), separate from the individual <see cref="Domain.Usage.UsageSnapshot"/>
/// readings taken over time.
/// </summary>
public interface IQuotaDefinitionRepository
{
    Task<IReadOnlyList<QuotaDefinition>> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default);

    Task<QuotaDefinition?> GetByExternalKeyAsync(Guid providerId, string externalKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the existing definition for (providerId, externalKey) if one exists, otherwise
    /// persists and returns a new one. Quota identities are provider-discovered, not
    /// pre-seeded, so callers resolve them lazily on first sighting.
    /// </summary>
    Task<QuotaDefinition> GetOrCreateAsync(
        Guid providerId,
        string externalKey,
        string name,
        QuotaType type,
        QuotaUnit unit,
        int sortOrder,
        CancellationToken cancellationToken = default);
}
