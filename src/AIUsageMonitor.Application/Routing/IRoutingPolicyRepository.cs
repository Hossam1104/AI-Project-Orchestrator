namespace AIUsageMonitor.Application.Routing;

/// <summary>
/// Persistence contract for one global routing policy and optional project-scoped overrides.
/// </summary>
public interface IRoutingPolicyRepository
{
    Task<RoutingPolicy?> GetGlobalAsync(CancellationToken cancellationToken = default);

    Task SaveGlobalAsync(RoutingPolicy policy, CancellationToken cancellationToken = default);

    Task<RoutingPolicy?> GetProjectOverrideAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task SaveProjectOverrideAsync(
        Guid projectId,
        RoutingPolicy policy,
        CancellationToken cancellationToken = default);
}
