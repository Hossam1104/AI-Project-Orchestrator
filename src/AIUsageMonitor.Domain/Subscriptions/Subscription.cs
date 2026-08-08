using AIUsageMonitor.Domain.Common;

namespace AIUsageMonitor.Domain.Subscriptions;

/// <summary>
/// Subscription/billing facts for a provider (BRD §14-15). Every field is optional except
/// what identifies the record — missing values must be displayed as "Not available", never
/// inferred or fabricated.
/// </summary>
public sealed class Subscription
{
    public Guid Id { get; }
    public Guid ProviderId { get; }
    public string? PlanName { get; }
    public DateTimeOffset? OriginalStartDate { get; }
    public DateTimeOffset? BillingPeriodStart { get; }
    public DateTimeOffset? BillingPeriodEnd { get; }
    public DateTimeOffset? RenewalDate { get; }
    public DateTimeOffset? CancelledDate { get; }
    public bool? AutoRenew { get; }
    public decimal? Price { get; }
    public string? Currency { get; }
    public BillingCadence? Cadence { get; }
    public DataSource Source { get; }
    public DateTimeOffset? LastVerifiedAt { get; }

    public Subscription(
        Guid id,
        Guid providerId,
        string? planName,
        DateTimeOffset? originalStartDate,
        DateTimeOffset? billingPeriodStart,
        DateTimeOffset? billingPeriodEnd,
        DateTimeOffset? renewalDate,
        DateTimeOffset? cancelledDate,
        bool? autoRenew,
        decimal? price,
        string? currency,
        BillingCadence? cadence,
        DataSource source,
        DateTimeOffset? lastVerifiedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Subscription id cannot be empty.", nameof(id));
        }

        if (providerId == Guid.Empty)
        {
            throw new ArgumentException("Provider id cannot be empty.", nameof(providerId));
        }

        if (billingPeriodStart.HasValue && billingPeriodEnd.HasValue && billingPeriodEnd.Value < billingPeriodStart.Value)
        {
            throw new ArgumentException("Billing period end cannot precede billing period start.", nameof(billingPeriodEnd));
        }

        if (price.HasValue && price.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), price, "Price cannot be negative.");
        }

        if (price.HasValue && string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required when a price is specified.", nameof(currency));
        }

        Id = id;
        ProviderId = providerId;
        PlanName = planName;
        OriginalStartDate = originalStartDate;
        BillingPeriodStart = billingPeriodStart;
        BillingPeriodEnd = billingPeriodEnd;
        RenewalDate = renewalDate;
        CancelledDate = cancelledDate;
        AutoRenew = autoRenew;
        Price = price;
        Currency = currency;
        Cadence = cadence;
        Source = source;
        LastVerifiedAt = lastVerifiedAt;
    }
}
