namespace AIUsageMonitor.Application.Providers;

/// <summary>
/// Runs read-only local detection across all registered providers (BRD FR-001). Detecting an
/// installation does not imply authenticated usage data is available.
/// </summary>
public interface IProviderDiscoveryService
{
    Task<IReadOnlyList<ProviderDetectionResult>> DiscoverAsync(CancellationToken cancellationToken = default);
}
