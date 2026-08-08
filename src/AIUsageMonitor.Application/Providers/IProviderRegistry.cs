using AIUsageMonitor.Domain.Providers;

namespace AIUsageMonitor.Application.Providers;

/// <summary>
/// The set of provider integrations known to the application, independent of whether each
/// one is currently detected, connected, or enabled.
/// </summary>
public interface IProviderRegistry
{
    IReadOnlyList<IAiUsageProvider> GetAll();

    IAiUsageProvider? Find(ProviderCode code);
}
