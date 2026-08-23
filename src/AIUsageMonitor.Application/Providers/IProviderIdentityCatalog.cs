using AIUsageMonitor.Domain.Providers;

namespace AIUsageMonitor.Application.Providers;

/// <summary>
/// Resolves the one authoritative provider identity mapping used by persistence and UI services.
/// Desktop code must not carry a parallel table of provider identifiers.
/// </summary>
public interface IProviderIdentityCatalog
{
    Guid GetProviderId(ProviderCode code);
}
