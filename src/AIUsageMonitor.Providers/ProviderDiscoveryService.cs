using AIUsageMonitor.Application.Providers;

namespace AIUsageMonitor.Providers;

public sealed class ProviderDiscoveryService : IProviderDiscoveryService
{
    private readonly IProviderRegistry _registry;

    public ProviderDiscoveryService(IProviderRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public async Task<IReadOnlyList<ProviderDetectionResult>> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        var results = await Task.WhenAll(
                _registry.GetAll().Select(provider => provider.DetectAsync(cancellationToken)))
            .ConfigureAwait(false);
        return results.OrderBy(result => result.Code).ToArray();
    }
}
