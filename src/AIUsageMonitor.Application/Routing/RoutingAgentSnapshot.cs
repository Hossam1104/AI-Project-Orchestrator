using AIUsageMonitor.Application.Agents;

namespace AIUsageMonitor.Application.Routing;

/// <summary>Bounded immutable copy of trusted effective registry truth used by one decision.</summary>
public sealed class RoutingAgentSnapshot
{
    public RoutingAgentSnapshot(
        Guid projectId,
        Guid agentId,
        AgentIdentity identity,
        DateTimeOffset registryUpdatedAt,
        bool enabled,
        IReadOnlyList<AgentRole> roleCapabilities,
        IReadOnlyList<string> capabilities,
        IReadOnlyList<string> limitations,
        AgentConnectionMode connectionMode,
        IReadOnlyList<AgentConnectionMode> supportedConnectionModes,
        AgentAvailability availability,
        AgentAuthenticationState authenticationState,
        AgentEntitlementState entitlementState)
    {
        if (projectId == Guid.Empty || agentId == Guid.Empty)
        {
            throw new ArgumentException("Routing agent project and agent ids are required.");
        }

        ArgumentNullException.ThrowIfNull(identity);
        if (identity.Id != agentId)
        {
            throw new ArgumentException("Routing agent identity does not match its agent id.", nameof(identity));
        }

        if (registryUpdatedAt == default)
        {
            throw new ArgumentException("Registry update time is required.", nameof(registryUpdatedAt));
        }

        ValidateEnums(roleCapabilities, nameof(roleCapabilities));
        ValidateEnums(supportedConnectionModes, nameof(supportedConnectionModes));
        if (!Enum.IsDefined(connectionMode) ||
            !Enum.IsDefined(availability) ||
            !Enum.IsDefined(authenticationState) ||
            !Enum.IsDefined(entitlementState))
        {
            throw new ArgumentException("Routing agent state contains an undefined value.");
        }

        ProjectId = projectId;
        AgentId = agentId;
        Identity = identity;
        RegistryUpdatedAt = registryUpdatedAt;
        Enabled = enabled;
        RoleCapabilities = roleCapabilities.Distinct().OrderBy(static value => value).ToArray();
        Capabilities = RoutingTagNormalization.Normalize(capabilities, nameof(capabilities));
        Limitations = NormalizeLimitations(limitations);
        ConnectionMode = connectionMode;
        SupportedConnectionModes = supportedConnectionModes.Distinct().OrderBy(static value => value).ToArray();
        Availability = availability;
        AuthenticationState = authenticationState;
        EntitlementState = entitlementState;
    }

    public Guid ProjectId { get; }
    public Guid AgentId { get; }
    public AgentIdentity Identity { get; }
    public DateTimeOffset RegistryUpdatedAt { get; }
    public bool Enabled { get; }
    public IReadOnlyList<AgentRole> RoleCapabilities { get; }
    public IReadOnlyList<string> Capabilities { get; }
    public IReadOnlyList<string> Limitations { get; }
    public AgentConnectionMode ConnectionMode { get; }
    public IReadOnlyList<AgentConnectionMode> SupportedConnectionModes { get; }
    public AgentAvailability Availability { get; }
    public AgentAuthenticationState AuthenticationState { get; }
    public AgentEntitlementState EntitlementState { get; }

    public static RoutingAgentSnapshot FromEffective(EffectiveAgentDefinition agent) => new(
        agent.ProjectId,
        agent.Id,
        new AgentIdentity(agent.Id, agent.Name, agent.Provider, agent.ModelIdentifier),
        agent.GlobalDefinition.UpdatedAt,
        agent.Enabled,
        agent.RoleCapabilities,
        agent.Capabilities,
        agent.Limitations,
        agent.ConnectionMode,
        agent.SupportedConnectionModes,
        agent.Availability,
        agent.AuthenticationState,
        agent.EntitlementState);

    public RoutingAgentSnapshot WithLimitations(IReadOnlyList<string> limitations) => new(
        ProjectId,
        AgentId,
        Identity,
        RegistryUpdatedAt,
        Enabled,
        RoleCapabilities,
        Capabilities,
        limitations,
        ConnectionMode,
        SupportedConnectionModes,
        Availability,
        AuthenticationState,
        EntitlementState);

    private static IReadOnlyList<string> NormalizeLimitations(IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count > 32)
        {
            throw new ArgumentException("Routing agent limitations cannot exceed 32 values.", nameof(values));
        }

        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Routing agent limitations cannot be blank.", nameof(values));
            }

            var normalized = value.Trim();
            if (normalized.Length > 1_000)
            {
                throw new ArgumentException("Routing agent limitations cannot exceed 1000 characters.", nameof(values));
            }

            result.Add(normalized);
        }

        return result.OrderBy(static value => value, StringComparer.Ordinal).ToArray();
    }

    private static void ValidateEnums<T>(IReadOnlyList<T> values, string parameterName)
        where T : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count > 64 || values.Any(value => !Enum.IsDefined(value)))
        {
            throw new ArgumentException("Routing agent enum capabilities are invalid or unbounded.", parameterName);
        }
    }
}
