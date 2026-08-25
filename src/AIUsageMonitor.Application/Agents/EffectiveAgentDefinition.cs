namespace AIUsageMonitor.Application.Agents;

/// <summary>
/// Read-only project-scoped view formed from global truth plus permitted project configuration.
/// It does not rank, select, invoke, or otherwise route an agent.
/// </summary>
public sealed class EffectiveAgentDefinition
{
    public EffectiveAgentDefinition(
        Guid projectId,
        AgentDefinition globalDefinition,
        AgentProjectOverride? projectOverride)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        ArgumentNullException.ThrowIfNull(globalDefinition);
        if (projectOverride is not null &&
            (projectOverride.ProjectId != projectId || projectOverride.AgentId != globalDefinition.Id))
        {
            throw new ArgumentException(
                "The project override does not belong to this effective agent view.",
                nameof(projectOverride));
        }

        ProjectId = projectId;
        GlobalDefinition = globalDefinition;
        ProjectOverride = projectOverride;
        Enabled = projectOverride?.EnabledOverride ?? globalDefinition.Enabled;
        RoleCapabilities = RestrictToGlobalCapabilities(
            globalDefinition.RoleCapabilities,
            projectOverride?.PermittedRoles);
        SupportedConnectionModes = RestrictToGlobalCapabilities(
            globalDefinition.SupportedConnectionModes,
            projectOverride?.PermittedConnectionModes);
    }

    public Guid ProjectId { get; }

    public AgentDefinition GlobalDefinition { get; }

    public AgentProjectOverride? ProjectOverride { get; }

    public Guid Id => GlobalDefinition.Id;

    public string Name => GlobalDefinition.Name;

    public string? Provider => GlobalDefinition.Provider;

    public string? ModelIdentifier => GlobalDefinition.ModelIdentifier;

    public string Role => GlobalDefinition.Role;

    public AgentConnectionMode ConnectionMode => GlobalDefinition.ConnectionMode;

    public AgentAvailability Availability => GlobalDefinition.Availability;

    public AgentAuthenticationState AuthenticationState => GlobalDefinition.AuthenticationState;

    public AgentEntitlementState EntitlementState => GlobalDefinition.EntitlementState;

    public bool Enabled { get; }

    public IReadOnlyList<AgentRole> RoleCapabilities { get; }

    public IReadOnlyList<AgentConnectionMode> SupportedConnectionModes { get; }

    public IReadOnlyList<string> Capabilities => GlobalDefinition.Capabilities;

    public IReadOnlyList<string> Limitations => GlobalDefinition.Limitations;

    public IReadOnlyList<AgentRolePolicyMetadata> RolePolicyMetadata => GlobalDefinition.RolePolicyMetadata;

    public AgentConnectionResult? LastConnectionResult => GlobalDefinition.LastConnectionResult;

    private static IReadOnlyList<T> RestrictToGlobalCapabilities<T>(
        IReadOnlyList<T> globalValues,
        IReadOnlyList<T>? requestedValues)
    {
        if (requestedValues is null)
        {
            return globalValues;
        }

        var requested = requestedValues.ToHashSet();
        return globalValues
            .Where(requested.Contains)
            .ToArray();
    }
}
