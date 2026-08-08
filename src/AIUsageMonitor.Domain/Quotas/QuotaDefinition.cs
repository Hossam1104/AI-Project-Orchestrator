namespace AIUsageMonitor.Domain.Quotas;

/// <summary>
/// The stable identity of a quota a provider exposes (e.g. "weekly usage"), separate from
/// any particular <see cref="QuotaWindow"/> reading of it over time.
/// </summary>
public sealed class QuotaDefinition
{
    public Guid Id { get; }
    public Guid ProviderId { get; }
    public string ExternalKey { get; }
    public string Name { get; }
    public QuotaType Type { get; }
    public QuotaUnit Unit { get; }
    public int SortOrder { get; }

    public QuotaDefinition(
        Guid id,
        Guid providerId,
        string externalKey,
        string name,
        QuotaType type,
        QuotaUnit unit,
        int sortOrder)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Quota definition id cannot be empty.", nameof(id));
        }

        if (providerId == Guid.Empty)
        {
            throw new ArgumentException("Provider id cannot be empty.", nameof(providerId));
        }

        if (string.IsNullOrWhiteSpace(externalKey))
        {
            throw new ArgumentException("Quota definition requires a non-empty provider external key.", nameof(externalKey));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Quota definition requires a non-empty name.", nameof(name));
        }

        Id = id;
        ProviderId = providerId;
        ExternalKey = externalKey;
        Name = name;
        Type = type;
        Unit = unit;
        SortOrder = sortOrder;
    }
}
