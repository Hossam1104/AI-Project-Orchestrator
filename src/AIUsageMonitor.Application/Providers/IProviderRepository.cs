using AIUsageMonitor.Domain.Providers;

namespace AIUsageMonitor.Application.Providers;

/// <summary>
/// Persistence for the fixed V1 <see cref="Provider"/> catalog (BRD §30). Implemented against
/// EF Core/LocalDB in Session 03; the Application layer must not know that.
/// </summary>
public interface IProviderRepository
{
    Task<IReadOnlyList<Provider>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Provider?> GetByCodeAsync(ProviderCode code, CancellationToken cancellationToken = default);

    Task UpsertAsync(Provider provider, CancellationToken cancellationToken = default);
}
