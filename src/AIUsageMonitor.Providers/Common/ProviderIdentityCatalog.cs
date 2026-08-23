using AIUsageMonitor.Application.Providers;
using AIUsageMonitor.Domain.Providers;

namespace AIUsageMonitor.Providers.Common;

public sealed class ProviderIdentityCatalog : IProviderIdentityCatalog
{
    public Guid GetProviderId(ProviderCode code) => ProviderIdentity.ForProvider(code);
}
