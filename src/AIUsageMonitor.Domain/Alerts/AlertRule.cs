namespace AIUsageMonitor.Domain.Alerts;

/// <summary>
/// A configurable remaining-capacity threshold rule for a provider, optionally scoped to a
/// single quota definition (BRD §27, default warning 30% / critical 15%).
/// </summary>
public sealed class AlertRule
{
    public Guid Id { get; }
    public Guid ProviderId { get; }
    public Guid? QuotaDefinitionId { get; }
    public double WarningThreshold { get; }
    public double CriticalThreshold { get; }
    public bool Enabled { get; }

    public AlertRule(
        Guid id,
        Guid providerId,
        Guid? quotaDefinitionId,
        double warningThreshold,
        double criticalThreshold,
        bool enabled)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Alert rule id cannot be empty.", nameof(id));
        }

        if (providerId == Guid.Empty)
        {
            throw new ArgumentException("Provider id cannot be empty.", nameof(providerId));
        }

        if (warningThreshold is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(warningThreshold), warningThreshold, "Warning threshold must be between 0 and 100.");
        }

        if (criticalThreshold is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(criticalThreshold), criticalThreshold, "Critical threshold must be between 0 and 100.");
        }

        if (criticalThreshold > warningThreshold)
        {
            throw new ArgumentException("Critical threshold must not exceed the warning threshold.", nameof(criticalThreshold));
        }

        Id = id;
        ProviderId = providerId;
        QuotaDefinitionId = quotaDefinitionId;
        WarningThreshold = warningThreshold;
        CriticalThreshold = criticalThreshold;
        Enabled = enabled;
    }
}
