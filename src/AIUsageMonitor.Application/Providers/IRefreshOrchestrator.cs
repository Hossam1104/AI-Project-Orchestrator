using AIUsageMonitor.Domain.Providers;

namespace AIUsageMonitor.Application.Providers;

/// <summary>
/// Coordinates refreshing one or all providers. A failure in one provider must never prevent
/// the others from completing (AGENTS.md §10, provider failure isolation).
/// </summary>
public interface IRefreshOrchestrator
{
    Task<IReadOnlyList<ProviderRefreshResult>> RefreshAllAsync(CancellationToken cancellationToken = default);

    Task<ProviderRefreshResult> RefreshAsync(ProviderCode code, CancellationToken cancellationToken = default);
}
