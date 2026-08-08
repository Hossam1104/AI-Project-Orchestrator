using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Quotas;
using AIUsageMonitor.Domain.Subscriptions;

namespace AIUsageMonitor.Application.Providers;

/// <summary>
/// The single normalized result shape every <see cref="IAiUsageProvider"/> refresh returns.
/// Factory methods keep construction to known-safe combinations so a caller cannot build an
/// inconsistent result (e.g. Success with no data, or a failure that silently drops last-known
/// quota windows — AGENTS.md §20 requires retaining last-known-good values). <see cref="Failed"/>
/// is the one factory whose outcome depends on its arguments: it resolves to
/// <see cref="ProviderRefreshOutcome.Stale"/> when last-known data is supplied, otherwise
/// <see cref="ProviderRefreshOutcome.ProviderError"/>.
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

    /// <summary>
    /// A refresh attempt that failed. If any last-known account/subscription/quota data is
    /// supplied, it is retained and the outcome is <see cref="ProviderRefreshOutcome.Stale"/>
    /// (a failed-but-recoverable refresh over known-good data). If nothing is supplied, the
    /// outcome is <see cref="ProviderRefreshOutcome.ProviderError"/> with no data — never
    /// fabricated. <paramref name="errorCode"/>/<paramref name="errorMessage"/> are retained
    /// either way, which is what distinguishes this from a plain <see cref="Stale"/> result
    /// (which has no error code).
    /// </summary>
    public static ProviderRefreshResult Failed(
        ProviderCode code,
        string errorCode,
        string errorMessage,
        DateTimeOffset completedAt,
        ProviderAccount? lastKnownAccount = null,
        Subscription? lastKnownSubscription = null,
        IReadOnlyList<QuotaWindow>? lastKnownQuotaWindows = null)
    {
        var quotaWindows = lastKnownQuotaWindows ?? Array.Empty<QuotaWindow>();
        var hasLastKnownData = lastKnownAccount is not null || lastKnownSubscription is not null || quotaWindows.Count > 0;
        var outcome = hasLastKnownData ? ProviderRefreshOutcome.Stale : ProviderRefreshOutcome.ProviderError;

        return new(code, outcome, lastKnownAccount, lastKnownSubscription, quotaWindows, errorCode, errorMessage, completedAt);
    }
}
