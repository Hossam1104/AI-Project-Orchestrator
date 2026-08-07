using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Quotas;
using AIUsageMonitor.Domain.Subscriptions;

namespace AIUsageMonitor.Application.Providers;

/// <summary>
/// The single normalized result shape every <see cref="IAiUsageProvider"/> refresh returns.
/// Factory methods correspond 1:1 to <see cref="ProviderRefreshOutcome"/> so a caller cannot
/// construct an inconsistent combination (e.g. Success with no data, or an error that drops
/// the last known quota windows — AGENTS.md §20 requires retaining last-known-good values).
/// </summary>
public sealed class ProviderRefreshResult
{
    public ProviderCode Code { get; }
    public ProviderRefreshOutcome Outcome { get; }
    public ProviderAccount? Account { get; }
    public Subscription? Subscription { get; }
    public IReadOnlyList<QuotaWindow> QuotaWindows { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public DateTimeOffset CompletedAt { get; }

    private ProviderRefreshResult(
        ProviderCode code,
        ProviderRefreshOutcome outcome,
        ProviderAccount? account,
        Subscription? subscription,
        IReadOnlyList<QuotaWindow> quotaWindows,
        string? errorCode,
        string? errorMessage,
        DateTimeOffset completedAt)
    {
        Code = code;
        Outcome = outcome;
        Account = account;
        Subscription = subscription;
        QuotaWindows = quotaWindows;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        CompletedAt = completedAt;
    }

    public static ProviderRefreshResult Success(
        ProviderCode code, ProviderAccount? account, Subscription? subscription,
        IReadOnlyList<QuotaWindow> quotaWindows, DateTimeOffset completedAt) =>
        new(code, ProviderRefreshOutcome.Success, account, subscription, quotaWindows, null, null, completedAt);

    public static ProviderRefreshResult Partial(
        ProviderCode code, ProviderAccount? account, Subscription? subscription,
        IReadOnlyList<QuotaWindow> quotaWindows, string errorMessage, DateTimeOffset completedAt) =>
        new(code, ProviderRefreshOutcome.Partial, account, subscription, quotaWindows, null, errorMessage, completedAt);

    public static ProviderRefreshResult AuthenticationRequired(ProviderCode code, DateTimeOffset completedAt) =>
        new(code, ProviderRefreshOutcome.AuthenticationRequired, null, null, Array.Empty<QuotaWindow>(), null,
            "Authentication required.", completedAt);

    public static ProviderRefreshResult Unsupported(ProviderCode code, DateTimeOffset completedAt) =>
        new(code, ProviderRefreshOutcome.Unsupported, null, null, Array.Empty<QuotaWindow>(), null,
            "This capability is not supported for this provider.", completedAt);

    /// <summary>Preserves the last known good data while marking it stale — never replaces it with zero.</summary>
    public static ProviderRefreshResult Stale(
        ProviderCode code, ProviderAccount? lastKnownAccount, Subscription? lastKnownSubscription,
        IReadOnlyList<QuotaWindow> lastKnownQuotaWindows, DateTimeOffset completedAt) =>
        new(code, ProviderRefreshOutcome.Stale, lastKnownAccount, lastKnownSubscription, lastKnownQuotaWindows, null,
            "Refresh did not complete; showing last known data.", completedAt);

    public static ProviderRefreshResult Error(
        ProviderCode code, string errorCode, string errorMessage, DateTimeOffset completedAt) =>
        new(code, ProviderRefreshOutcome.ProviderError, null, null, Array.Empty<QuotaWindow>(), errorCode, errorMessage, completedAt);
}
