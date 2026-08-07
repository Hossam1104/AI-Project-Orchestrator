using AIUsageMonitor.Domain.Common;

namespace AIUsageMonitor.Domain.Quotas;

/// <summary>
/// A single normalized quota window (BRD §9). Providers expose zero to many of these per
/// account, of arbitrary <see cref="QuotaType"/> — this type intentionally has no
/// provider-specific properties (no "FiveHourUsage", no "WeeklyUsage").
///
/// Percentages are always normalized to remaining-capacity-safe values here, once, so
/// nothing downstream can accidentally treat a used percentage as a remaining percentage
/// (AGENTS.md §8).
/// </summary>
public sealed class QuotaWindow
{
    private const double PercentageTolerance = 0.5;

    public string ExternalKey { get; }
    public QuotaType Type { get; }
    public QuotaUnit Unit { get; }
    public double? UsedValue { get; }
    public double? RemainingValue { get; }
    public double? LimitValue { get; }
    public double? UsedPercentage { get; }
    public double? RemainingPercentage { get; }
    public DateTimeOffset? WindowStart { get; }
    public DateTimeOffset? ResetAt { get; }
    public DataSource Source { get; }
    public ConfidenceLevel Confidence { get; }
    public DateTimeOffset CapturedAt { get; }

    private QuotaWindow(
        string externalKey,
        QuotaType type,
        QuotaUnit unit,
        double? usedValue,
        double? remainingValue,
        double? limitValue,
        double? usedPercentage,
        double? remainingPercentage,
        DateTimeOffset? windowStart,
        DateTimeOffset? resetAt,
        DataSource source,
        ConfidenceLevel confidence,
        DateTimeOffset capturedAt)
    {
        ExternalKey = externalKey;
        Type = type;
        Unit = unit;
        UsedValue = usedValue;
        RemainingValue = remainingValue;
        LimitValue = limitValue;
        UsedPercentage = usedPercentage;
        RemainingPercentage = remainingPercentage;
        WindowStart = windowStart;
        ResetAt = resetAt;
        Source = source;
        Confidence = confidence;
        CapturedAt = capturedAt;
    }

    public static QuotaWindow Create(
        string externalKey,
        QuotaType type,
        QuotaUnit unit,
        double? usedValue,
        double? remainingValue,
        double? limitValue,
        double? usedPercentage,
        double? remainingPercentage,
        DateTimeOffset? windowStart,
        DateTimeOffset? resetAt,
        DataSource source,
        ConfidenceLevel confidence,
        DateTimeOffset capturedAt)
    {
        if (string.IsNullOrWhiteSpace(externalKey))
        {
            throw new ArgumentException("A quota window requires a non-empty provider external key.", nameof(externalKey));
        }

        if (usedValue is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(usedValue), usedValue, "Used value cannot be negative.");
        }

        if (remainingValue is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(remainingValue), remainingValue, "Remaining value cannot be negative.");
        }

        if (limitValue is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limitValue), limitValue, "Limit value cannot be negative.");
        }

        if (windowStart.HasValue && resetAt.HasValue && resetAt.Value < windowStart.Value)
        {
            throw new ArgumentException("Reset time cannot precede the quota window start time.", nameof(resetAt));
        }

        var (normalizedUsed, normalizedRemaining) = NormalizePercentages(
            usedValue, remainingValue, limitValue, usedPercentage, remainingPercentage);

        return new QuotaWindow(
            externalKey, type, unit, usedValue, remainingValue, limitValue,
            normalizedUsed, normalizedRemaining, windowStart, resetAt, source, confidence, capturedAt);
    }

    private static (double? Used, double? Remaining) NormalizePercentages(
        double? usedValue,
        double? remainingValue,
        double? limitValue,
        double? usedPercentage,
        double? remainingPercentage)
    {
        if (usedPercentage.HasValue && (usedPercentage < 0 || usedPercentage > 100))
        {
            throw new ArgumentOutOfRangeException(nameof(usedPercentage), usedPercentage, "Used percentage must be between 0 and 100.");
        }

        if (remainingPercentage.HasValue && (remainingPercentage < 0 || remainingPercentage > 100))
        {
            throw new ArgumentOutOfRangeException(nameof(remainingPercentage), remainingPercentage, "Remaining percentage must be between 0 and 100.");
        }

        // Explicit percentages take priority, but must agree with each other — this is the
        // guard against a provider (or a future caller) silently double-inverting used/remaining.
        if (usedPercentage.HasValue && remainingPercentage.HasValue)
        {
            if (Math.Abs(usedPercentage.Value + remainingPercentage.Value - 100.0) > PercentageTolerance)
            {
                throw new ArgumentException(
                    $"Used percentage ({usedPercentage}) and remaining percentage ({remainingPercentage}) must sum to 100.",
                    nameof(remainingPercentage));
            }

            return (usedPercentage, remainingPercentage);
        }

        if (usedPercentage.HasValue)
        {
            return (usedPercentage, 100.0 - usedPercentage);
        }

        if (remainingPercentage.HasValue)
        {
            return (100.0 - remainingPercentage, remainingPercentage);
        }

        if (limitValue is > 0)
        {
            if (usedValue.HasValue)
            {
                var usedPct = Math.Clamp(usedValue.Value / limitValue.Value * 100.0, 0, 100);
                return (usedPct, 100.0 - usedPct);
            }

            if (remainingValue.HasValue)
            {
                var remainingPct = Math.Clamp(remainingValue.Value / limitValue.Value * 100.0, 0, 100);
                return (100.0 - remainingPct, remainingPct);
            }
        }

        if (usedValue.HasValue && remainingValue.HasValue)
        {
            var total = usedValue.Value + remainingValue.Value;
            if (total > 0)
            {
                var usedPct = usedValue.Value / total * 100.0;
                return (usedPct, 100.0 - usedPct);
            }
        }

        // Not enough data to normalize a percentage — leave it unset rather than inventing one.
        return (null, null);
    }
}
