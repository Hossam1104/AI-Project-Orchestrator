using AIUsageMonitor.Domain.Providers;

namespace AIUsageMonitor.Application.Providers;

/// <summary>
/// Persistence for a provider's current connection state (BRD §30 ProviderConnections). Never
/// stores a secret value — only <see cref="ProviderConnection.CredentialReference"/>, an
/// opaque lookup key.
/// </summary>
public interface IProviderConnectionRepository
{
    Task<ProviderConnection?> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default);

    Task UpsertAsync(ProviderConnection connection, CancellationToken cancellationToken = default);
}
