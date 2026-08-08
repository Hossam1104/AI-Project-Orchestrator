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
    private const double ValueTolerance = 0.5;

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

        if (limitValue.HasValue && usedValue.HasValue && usedValue.Value > limitValue.Value + ValueTolerance)
        {
            throw new ArgumentException(
                $"Used value ({usedValue}) cannot exceed the limit value ({limitValue}).", nameof(usedValue));
        }

        if (limitValue.HasValue && remainingValue.HasValue && remainingValue.Value > limitValue.Value + ValueTolerance)
        {
            throw new ArgumentException(
                $"Remaining value ({remainingValue}) cannot exceed the limit value ({limitValue}).", nameof(remainingValue));
        }

        if (limitValue.HasValue && usedValue.HasValue && remainingValue.HasValue)
        {
            var total = usedValue.Value + remainingValue.Value;
            if (Math.Abs(total - limitValue.Value) > ValueTolerance)
            {
                throw new ArgumentException(
                    $"Used value ({usedValue}) plus remaining value ({remainingValue}) must approximately equal the limit value ({limitValue}).",
                    nameof(remainingValue));
            }
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

        // What the absolute used/remaining/limit values imply about the percentage, if computable.
        // Create(...) has already verified used/remaining individually fit within the limit and
        // (when all three are present) sum to it, so this is a straight derivation, not a guess.
        double? impliedUsed = null;
        if (limitValue is > 0)
        {
            if (usedValue.HasValue)
            {
                impliedUsed = usedValue.Value / limitValue.Value * 100.0;
            }
            else if (remainingValue.HasValue)
            {
                impliedUsed = 100.0 - (remainingValue.Value / limitValue.Value * 100.0);
            }
        }
        else if (usedValue.HasValue && remainingValue.HasValue)
        {
            var total = usedValue.Value + remainingValue.Value;
            if (total > 0)
            {
                impliedUsed = usedValue.Value / total * 100.0;
            }
        }

        // Explicit percentages take priority, but must agree with each other — this is the
        // guard against a provider (or a future caller) silently double-inverting used/remaining.
        double? explicitUsed = usedPercentage;
        if (usedPercentage.HasValue && remainingPercentage.HasValue)
        {
            if (Math.Abs(usedPercentage.Value + remainingPercentage.Value - 100.0) > PercentageTolerance)
            {
                throw new ArgumentException(
                    $"Used percentage ({usedPercentage}) and remaining percentage ({remainingPercentage}) must sum to 100.",
                    nameof(remainingPercentage));
            }
        }
        else if (remainingPercentage.HasValue)
        {
            explicitUsed = 100.0 - remainingPercentage.Value;
        }

        // Reject a caller supplying both explicit percentages and absolute values that disagree,
        // rather than silently letting one silently override the other.
        if (explicitUsed.HasValue && impliedUsed.HasValue && Math.Abs(explicitUsed.Value - impliedUsed.Value) > PercentageTolerance)
        {
            throw new ArgumentException(
                $"Explicit used/remaining percentage ({explicitUsed:F2}) does not match the percentage implied by the used/remaining/limit values ({impliedUsed:F2}).",
                nameof(usedPercentage));
        }

        var resolvedUsed = explicitUsed ?? impliedUsed;
        if (resolvedUsed.HasValue)
        {
            var clampedUsed = Math.Clamp(resolvedUsed.Value, 0, 100);
            return (clampedUsed, 100.0 - clampedUsed);
        }

        // Not enough data to normalize a percentage — leave it unset rather than inventing one.
        return (null, null);
    }
}
