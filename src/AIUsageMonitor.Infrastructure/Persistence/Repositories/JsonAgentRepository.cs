using AIUsageMonitor.Application.Agents;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

/// <summary>
/// Versioned JSON agent/model registry. It persists capability and access metadata but no
/// credentials or authenticated provider responses.
/// </summary>
public sealed class JsonAgentRepository : IAgentRepository
{
    private readonly ApplicationDataPaths _paths;
    private readonly VersionedJsonCollectionStore<AgentRecord> _records;
    private readonly ILogger<JsonAgentRepository> _logger;

    public JsonAgentRepository(
        ApplicationDataPaths paths,
        JsonFileStore files,
        ILogger<JsonAgentRepository> logger)
    {
        _paths = paths;
        _records = new VersionedJsonCollectionStore<AgentRecord>(files);
        _logger = logger;
    }

    public async Task<IReadOnlyList<AgentDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var records = await _records.ReadAsync(_paths.AgentsFile, cancellationToken).ConfigureAwait(false);
        return records
            .Select(TryMap)
            .Where(static agent => agent is not null)
            .Select(static agent => agent!)
            .OrderBy(static agent => agent.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static agent => agent.Id)
            .ToArray();
    }

    public async Task<AgentDefinition?> GetByIdAsync(
        Guid agentId,
        CancellationToken cancellationToken = default)
    {
        if (agentId == Guid.Empty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(agentId));
        }

        var records = await _records.ReadAsync(_paths.AgentsFile, cancellationToken).ConfigureAwait(false);
        var record = records.FirstOrDefault(agent => agent.Id == agentId);
        return record is null ? null : TryMap(record);
    }

    public Task UpsertAsync(AgentDefinition agent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        var record = AgentRecord.FromApplication(agent);

        return _records.UpdateAsync(_paths.AgentsFile, records =>
        {
            var index = records.FindIndex(existing => existing.Id == record.Id);
            if (index >= 0)
            {
                records[index] = record;
            }
            else
            {
                records.Add(record);
            }

            return records;
        }, cancellationToken);
    }

    private AgentDefinition? TryMap(AgentRecord record)
    {
        try
        {
            return record.ToApplication();
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Skipping invalid agent record {AgentId}", record.Id);
            return null;
        }
    }
}
