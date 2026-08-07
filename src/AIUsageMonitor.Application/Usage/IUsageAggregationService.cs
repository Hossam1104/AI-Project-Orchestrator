namespace AIUsageMonitor.Application.Usage;

/// <summary>
/// Produces the dashboard's per-provider usage view from the latest known provider state.
/// </summary>
public interface IUsageAggregationService
{
    Task<IReadOnlyList<ProviderUsageSummary>> GetProviderSummariesAsync(CancellationToken cancellationToken = default);
}
