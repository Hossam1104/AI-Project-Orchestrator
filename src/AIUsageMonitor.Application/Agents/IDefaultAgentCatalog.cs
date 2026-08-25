namespace AIUsageMonitor.Application.Agents;

/// <summary>
/// Centralized owner-approved default role catalog. Catalog entries describe identity and role
/// metadata only; they are not persisted or treated as live provider connectivity evidence.
/// </summary>
public interface IDefaultAgentCatalog
{
    IReadOnlyList<AgentDefinition> GetDefaults();
}
