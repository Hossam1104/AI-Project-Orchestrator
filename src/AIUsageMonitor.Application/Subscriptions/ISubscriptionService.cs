using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Subscriptions;

namespace AIUsageMonitor.Application.Subscriptions;

/// <summary>
/// Reads/writes subscription data, distinguishing provider-sourced facts from manual entry
/// (BRD §14). Implementations must never present an inferred date as a provider-verified one.
/// </summary>
public interface ISubscriptionService
{
    Task<Subscription?> GetSubscriptionAsync(ProviderCode code, CancellationToken cancellationToken = default);

    Task SaveManualSubscriptionAsync(Subscription subscription, CancellationToken cancellationToken = default);
}
