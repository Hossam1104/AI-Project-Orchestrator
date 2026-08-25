namespace AIUsageMonitor.Application.Agents;

public enum AgentRegistryResolutionStatus
{
    Found,
    NotFound
}

/// <summary>
/// Explicit result for effective registry lookup. A missing model is not fabricated into a
/// placeholder definition.
/// </summary>
public sealed class AgentRegistryResolution
{
    private AgentRegistryResolution(
        AgentRegistryResolutionStatus status,
        EffectiveAgentDefinition? agent)
    {
        Status = status;
        Agent = agent;
    }

    public AgentRegistryResolutionStatus Status { get; }

    public bool Found => Status == AgentRegistryResolutionStatus.Found;

    public EffectiveAgentDefinition? Agent { get; }

    public static AgentRegistryResolution FoundResult(EffectiveAgentDefinition agent)
    {
        ArgumentNullException.ThrowIfNull(agent);
        return new(AgentRegistryResolutionStatus.Found, agent);
    }

    public static AgentRegistryResolution NotFoundResult() =>
        new(AgentRegistryResolutionStatus.NotFound, null);
}
