using AIUsageMonitor.Application.Agents;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

/// <summary>
/// Versioned JSON persistence for project-isolated agent configuration. The path is derived only
/// from the project GUID and every record is additionally checked against that project boundary.
/// </summary>
public sealed class JsonAgentProjectOverrideRepository : IAgentProjectOverrideRepository
{
    private readonly ApplicationDataPaths _paths;
    private readonly VersionedJsonCollectionStore<AgentProjectOverrideRecord> _records;
    private readonly ILogger<JsonAgentProjectOverrideRepository> _logger;

    public JsonAgentProjectOverrideRepository(
        ApplicationDataPaths paths,
        JsonFileStore files,
        ILogger<JsonAgentProjectOverrideRepository> logger)
    {
        _paths = paths;
        _records = new VersionedJsonCollectionStore<AgentProjectOverrideRecord>(files);
        _logger = logger;
    }

    public async Task<IReadOnlyList<AgentProjectOverride>> GetAllAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        ValidateProjectId(projectId);
        var records = await _records
            .ReadAsync(_paths.GetProjectAgentOverridesFile(projectId), cancellationToken)
            .ConfigureAwait(false);

        return records
            .Where(record => record.ProjectId == projectId)
            .Select(TryMap)
            .Where(static value => value is not null)
            .Select(static value => value!)
            .GroupBy(value => value.AgentId)
            .Select(group => group.First())
            .OrderBy(value => value.AgentId)
            .ToArray();
    }

    public async Task<AgentProjectOverride?> GetAsync(
        Guid projectId,
        Guid agentId,
        CancellationToken cancellationToken = default)
    {
        ValidateProjectId(projectId);
        ValidateAgentId(agentId);

        var records = await _records
            .ReadAsync(_paths.GetProjectAgentOverridesFile(projectId), cancellationToken)
            .ConfigureAwait(false);
        var record = records.FirstOrDefault(value =>
            value.ProjectId == projectId && value.AgentId == agentId);
        return record is null ? null : TryMap(record);
    }

    public async Task UpsertAsync(
        AgentProjectOverride projectOverride,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projectOverride);
        ValidateProjectId(projectOverride.ProjectId);
        ValidateAgentId(projectOverride.AgentId);
        await _paths
            .EnsureProjectDirectoriesAsync(projectOverride.ProjectId, cancellationToken)
            .ConfigureAwait(false);

        var record = AgentProjectOverrideRecord.FromApplication(projectOverride);
        await _records.UpdateAsync(
                _paths.GetProjectAgentOverridesFile(projectOverride.ProjectId),
                records =>
                {
                    var index = records.FindIndex(existing =>
                        existing.ProjectId == record.ProjectId && existing.AgentId == record.AgentId);
                    if (index >= 0)
                    {
                        records[index] = record;
                    }
                    else
                    {
                        records.Add(record);
                    }

                    return records;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    private AgentProjectOverride? TryMap(AgentProjectOverrideRecord record)
    {
        try
        {
            return record.ToApplication();
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(
                exception,
                "Skipping invalid agent override for project {ProjectId} and agent {AgentId}",
                record.ProjectId,
                record.AgentId);
            return null;
        }
    }

    private static void ValidateProjectId(Guid projectId)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }
    }

    private static void ValidateAgentId(Guid agentId)
    {
        if (agentId == Guid.Empty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(agentId));
        }
    }
}
