using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Quotas;
using AIUsageMonitor.Domain.Subscriptions;

namespace AIUsageMonitor.Application.Providers;

/// <summary>
/// The contract every provider integration implements (BRD §32). The Desktop/UI layer must
/// only ever consume this abstraction — never a provider-specific type or raw payload.
/// </summary>
public interface IAiUsageProvider
{
    ProviderCode Code { get; }

    Task<ProviderDetectionResult> DetectAsync(CancellationToken cancellationToken = default);

    Task<ProviderConnectionStatus> GetConnectionStatusAsync(CancellationToken cancellationToken = default);

    Task<ProviderAccount?> GetAccountAsync(CancellationToken cancellationToken = default);

    Task<Subscription?> GetSubscriptionAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QuotaWindow>> GetQuotasAsync(CancellationToken cancellationToken = default);

    Task<ProviderRefreshResult> RefreshAsync(CancellationToken cancellationToken = default);
}
