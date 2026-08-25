namespace AIUsageMonitor.Application.Agents;

/// <summary>
/// Project GUID-scoped persistence for agent configuration overrides.
/// </summary>
public interface IAgentProjectOverrideRepository
{
    Task<IReadOnlyList<AgentProjectOverride>> GetAllAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<AgentProjectOverride?> GetAsync(
        Guid projectId,
        Guid agentId,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        AgentProjectOverride projectOverride,
        CancellationToken cancellationToken = default);
}
