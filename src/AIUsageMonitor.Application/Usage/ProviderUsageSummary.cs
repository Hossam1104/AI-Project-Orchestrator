using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Quotas;

namespace AIUsageMonitor.Application.Usage;

/// <summary>
/// A dashboard-ready view of one provider's current quota state. Aggregation/scoring logic
/// is implemented in later sessions (History/Recommendation) — this is the shared shape they
/// will consume and produce.
/// </summary>
public sealed class ProviderUsageSummary
{
    public ProviderCode Code { get; }
    public ProviderConnectionStatus Status { get; }
    public IReadOnlyList<QuotaWindow> QuotaWindows { get; }
    public double? BestRemainingPercentage { get; }
    public DateTimeOffset? LastUpdated { get; }

    public ProviderUsageSummary(
        ProviderCode code,
        ProviderConnectionStatus status,
        IReadOnlyList<QuotaWindow> quotaWindows,
        double? bestRemainingPercentage,
        DateTimeOffset? lastUpdated)
    {
        Code = code;
        Status = status;
        QuotaWindows = quotaWindows;
        BestRemainingPercentage = bestRemainingPercentage;
        LastUpdated = lastUpdated;
    }
}
