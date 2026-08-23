namespace AIUsageMonitor.Application.Agents;

/// <summary>
/// Persistence contract for the small global agent/model registry document.
/// </summary>
public interface IAgentRepository
{
    Task<IReadOnlyList<AgentDefinition>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AgentDefinition?> GetByIdAsync(Guid agentId, CancellationToken cancellationToken = default);

    Task UpsertAsync(AgentDefinition agent, CancellationToken cancellationToken = default);
}
