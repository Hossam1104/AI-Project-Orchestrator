namespace AIUsageMonitor.Application.Agents;

public sealed class AgentRegistryService : IAgentRegistryService
{
    private readonly IAgentRepository _agents;
    private readonly IAgentProjectOverrideRepository _overrides;

    public AgentRegistryService(
        IAgentRepository agents,
        IAgentProjectOverrideRepository overrides)
    {
        _agents = agents ?? throw new ArgumentNullException(nameof(agents));
        _overrides = overrides ?? throw new ArgumentNullException(nameof(overrides));
    }

    public async Task<IReadOnlyList<EffectiveAgentDefinition>> GetEffectiveAgentsAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        ValidateProjectId(projectId);

        var agents = await _agents.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var overrides = await _overrides.GetAllAsync(projectId, cancellationToken).ConfigureAwait(false);
        var overridesByAgent = overrides
            .GroupBy(value => value.AgentId)
            .ToDictionary(group => group.Key, group => group.First());

        return agents
            .GroupBy(value => value.Id)
            .Select(group => group.First())
            .Select(agent => new EffectiveAgentDefinition(
                projectId,
                agent,
                overridesByAgent.GetValueOrDefault(agent.Id)))
            .OrderBy(value => value.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(value => value.Id)
            .ToArray();
    }

    public async Task<AgentRegistryResolution> ResolveAsync(
        Guid projectId,
        Guid agentId,
        CancellationToken cancellationToken = default)
    {
        ValidateProjectId(projectId);
        if (agentId == Guid.Empty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(agentId));
        }

        var agent = await _agents.GetByIdAsync(agentId, cancellationToken).ConfigureAwait(false);
        if (agent is null)
        {
            return AgentRegistryResolution.NotFoundResult();
        }

        var projectOverride = await _overrides
            .GetAsync(projectId, agentId, cancellationToken)
            .ConfigureAwait(false);
        return AgentRegistryResolution.FoundResult(
            new EffectiveAgentDefinition(projectId, agent, projectOverride));
    }

    private static void ValidateProjectId(Guid projectId)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }
    }
}
