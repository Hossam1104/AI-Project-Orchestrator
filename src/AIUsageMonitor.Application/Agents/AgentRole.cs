namespace AIUsageMonitor.Application.Agents;

/// <summary>
/// Provider-independent orchestration roles an agent/model may be capable of performing.
/// These are capabilities and metadata only; they do not select or invoke an agent.
/// </summary>
public enum AgentRole
{
    Planner,
    Architect,
    AcceptanceAuthority,
    Executor,
    Reviewer,
    SecuritySpecialist,
    AuxiliaryExecutor
}
