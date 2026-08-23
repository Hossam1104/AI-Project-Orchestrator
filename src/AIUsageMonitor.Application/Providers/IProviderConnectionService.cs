using AIUsageMonitor.Domain.Providers;

namespace AIUsageMonitor.Application.Providers;

public interface IProviderConnectionService
{
    Task<ProviderConnection?> GetAsync(
        ProviderCode code,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProviderConnection>> LoadAllAsync(
        CancellationToken cancellationToken = default);

    Task<ProviderConnection> SaveAsync(
        ProviderConnectionEdit edit,
        CancellationToken cancellationToken = default);
}
