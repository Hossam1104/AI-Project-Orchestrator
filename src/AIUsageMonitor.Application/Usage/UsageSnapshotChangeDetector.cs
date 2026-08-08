using AIUsageMonitor.Domain.Quotas;

namespace AIUsageMonitor.Application.Usage;

/// <summary>
/// Decides whether a newly read <see cref="QuotaWindow"/> differs materially enough from the
/// most recently persisted one to justify writing a new <see cref="Domain.Usage.UsageSnapshot"/>
/// history row (BRD §16: "Do not write duplicate snapshots every minute when nothing changes.").
/// Pure/deterministic so it can be unit tested without a database.
/// </summary>
public static class UsageSnapshotChangeDetector
{
    /// <summary>Percentage-point tolerance below which a used/remaining percentage move is noise, not a material change.</summary>
    public const double PercentageChangeThreshold = 1.0;

    /// <summary>Absolute-value tolerance below which a used/remaining/limit move is noise, not a material change.</summary>
    public const double ValueChangeThreshold = 0.5;

    /// <summary>
    /// True when <paramref name="current"/> should be persisted as a new history row relative
    /// to <paramref name="previous"/>. A window boundary change (new <c>WindowStart</c>/
    /// <c>ResetAt</c>) always counts as material, even if the percentages happen to match,
    /// because it represents a distinct quota period.
    /// </summary>
    public static bool HasMaterialChange(QuotaWindow previous, QuotaWindow current)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(current);

        if (previous.WindowStart != current.WindowStart)
        {
            return true;
        }

        if (previous.ResetAt != current.ResetAt)
        {
            return true;
        }

        if (!AreClose(previous.UsedPercentage, current.UsedPercentage, PercentageChangeThreshold))
        {
            return true;
        }

        if (!AreClose(previous.RemainingPercentage, current.RemainingPercentage, PercentageChangeThreshold))
        {
            return true;
        }

        if (!AreClose(previous.UsedValue, current.UsedValue, ValueChangeThreshold))
        {
            return true;
        }

        if (!AreClose(previous.RemainingValue, current.RemainingValue, ValueChangeThreshold))
        {
            return true;
        }

        if (!AreClose(previous.LimitValue, current.LimitValue, ValueChangeThreshold))
        {
            return true;
        }

        return false;
    }

    private static bool AreClose(double? a, double? b, double threshold)
    {
        if (a is null && b is null)
        {
            return true;
        }

        // A value newly appearing or disappearing is itself a material change — never treat
        // "no data" and "some data" as equivalent.
        if (a is null || b is null)
        {
            return false;
        }

        return Math.Abs(a.Value - b.Value) <= threshold;
    }
}
