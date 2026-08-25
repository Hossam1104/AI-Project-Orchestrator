namespace AIUsageMonitor.Application.Agents;

/// <summary>
/// Resolves global agent truth and project configuration into a read-only effective view. This
/// service deliberately contains no routing, scoring, quota comparison, or execution behavior.
/// </summary>
public interface IAgentRegistryService
{
    Task<IReadOnlyList<EffectiveAgentDefinition>> GetEffectiveAgentsAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<AgentRegistryResolution> ResolveAsync(
        Guid projectId,
        Guid agentId,
        CancellationToken cancellationToken = default);
}
