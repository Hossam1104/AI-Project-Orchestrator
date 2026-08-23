using AIUsageMonitor.Application.Routing;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

/// <summary>
/// Versioned JSON storage for one global policy and optional project-scoped overrides. Project
/// override paths are derived from the GUID and never from a value loaded from the registry.
/// </summary>
public sealed class JsonRoutingPolicyStore : IRoutingPolicyRepository
{
    private readonly ApplicationDataPaths _paths;
    private readonly JsonFileStore _files;
    private readonly ILogger<JsonRoutingPolicyStore> _logger;

    public JsonRoutingPolicyStore(
        ApplicationDataPaths paths,
        JsonFileStore files,
        ILogger<JsonRoutingPolicyStore> logger)
    {
        _paths = paths;
        _files = files;
        _logger = logger;
    }

    public async Task<RoutingPolicy?> GetGlobalAsync(CancellationToken cancellationToken = default)
    {
        var result = await _files.ReadAsync<RoutingPolicyRecord>(
                _paths.RoutingPolicyFile,
                cancellationToken)
            .ConfigureAwait(false);
        return result.IsUsable ? TryMap(result.Value!) : null;
    }

    public Task SaveGlobalAsync(
        RoutingPolicy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return _files.WriteAsync(
            _paths.RoutingPolicyFile,
            RoutingPolicyRecord.FromApplication(policy),
            cancellationToken);
    }

    public async Task<RoutingPolicy?> GetProjectOverrideAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var path = _paths.GetProjectRoutingPolicyFile(projectId);
        var result = await _files.ReadAsync<RoutingPolicyRecord>(path, cancellationToken).ConfigureAwait(false);
        return result.IsUsable ? TryMap(result.Value!) : null;
    }

    public async Task SaveProjectOverrideAsync(
        Guid projectId,
        RoutingPolicy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);
        await _paths.EnsureProjectDirectoriesAsync(projectId, cancellationToken).ConfigureAwait(false);
        await _files.WriteAsync(
                _paths.GetProjectRoutingPolicyFile(projectId),
                RoutingPolicyRecord.FromApplication(policy),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private RoutingPolicy? TryMap(RoutingPolicyRecord record)
    {
        try
        {
            return record.ToApplication();
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Skipping invalid routing policy document");
            return null;
        }
    }
}
