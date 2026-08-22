using AIUsageMonitor.Application.Providers;
using AIUsageMonitor.Domain.Providers;

namespace AIUsageMonitor.Providers;

public sealed class ProviderRegistry : IProviderRegistry
{
    private readonly IReadOnlyList<IAiUsageProvider> _providers;
    private readonly IReadOnlyDictionary<ProviderCode, IAiUsageProvider> _byCode;

    public ProviderRegistry(IEnumerable<IAiUsageProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        var materialized = providers.ToArray();
        if (materialized.Length != Enum.GetValues<ProviderCode>().Length ||
            materialized.Select(provider => provider.Code).Distinct().Count() != materialized.Length)
        {
            throw new InvalidOperationException("Provider registration must contain each V1 provider exactly once.");
        }

        var missing = Enum.GetValues<ProviderCode>().Except(materialized.Select(provider => provider.Code)).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException($"Provider registration is missing: {string.Join(", ", missing)}.");
        }

        _providers = materialized.OrderBy(provider => provider.Code).ToArray();
        _byCode = _providers.ToDictionary(provider => provider.Code);
    }

    public IReadOnlyList<IAiUsageProvider> GetAll() => _providers;

    public IAiUsageProvider? Find(ProviderCode code) => _byCode.GetValueOrDefault(code);
}
