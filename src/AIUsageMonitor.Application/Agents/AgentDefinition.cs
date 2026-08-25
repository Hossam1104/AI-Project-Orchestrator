using AIUsageMonitor.Application.Common;

namespace AIUsageMonitor.Application.Agents;

/// <summary>
/// Small-state registry entry for an AI agent/model. Connection metadata is descriptive only;
/// credentials and authenticated provider payloads never belong here.
/// </summary>
public sealed class AgentDefinition
{
    public AgentDefinition(
        Guid id,
        string name,
        string role,
        AgentConnectionMode connectionMode,
        AgentAvailability availability,
        bool enabled,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string? provider = null,
        IReadOnlyList<string>? capabilities = null,
        IReadOnlyList<string>? limitations = null,
        IReadOnlyDictionary<string, string?>? costAndQuotaMetadata = null,
        IReadOnlyList<AgentRole>? roleCapabilities = null,
        IReadOnlyList<AgentConnectionMode>? supportedConnectionModes = null,
        AgentAuthenticationState authenticationState = AgentAuthenticationState.Unknown,
        AgentEntitlementState entitlementState = AgentEntitlementState.Unknown,
        string? modelIdentifier = null,
        IReadOnlyList<AgentRolePolicyMetadata>? rolePolicyMetadata = null,
        AgentConnectionResult? lastConnectionResult = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Agent name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Agent role is required.", nameof(role));
        }

        if (updatedAt < createdAt)
        {
            throw new ArgumentException("Agent UpdatedAt cannot precede CreatedAt.", nameof(updatedAt));
        }

        if (!Enum.IsDefined(typeof(AgentConnectionMode), connectionMode))
        {
            throw new ArgumentException("Agent connection mode is undefined.", nameof(connectionMode));
        }

        if (!Enum.IsDefined(typeof(AgentAvailability), availability))
        {
            throw new ArgumentException("Agent availability is undefined.", nameof(availability));
        }

        if (!Enum.IsDefined(authenticationState))
        {
            throw new ArgumentException("Agent authentication state is undefined.", nameof(authenticationState));
        }

        if (!Enum.IsDefined(entitlementState))
        {
            throw new ArgumentException("Agent entitlement state is undefined.", nameof(entitlementState));
        }

        Id = id;
        Name = name.Trim();
        Role = role.Trim();
        Provider = AgentContractValidation.NormalizeOptional(provider, nameof(provider), 200);
        ModelIdentifier = AgentContractValidation.NormalizeOptional(modelIdentifier, nameof(modelIdentifier), 300);
        ConnectionMode = connectionMode;
        Availability = availability;
        Enabled = enabled;
        Capabilities = CopyList(capabilities);
        Limitations = CopyList(limitations);
        CostAndQuotaMetadata = MetadataValidation.Copy(costAndQuotaMetadata);
        RoleCapabilities = NormalizeRoleCapabilities(roleCapabilities, Role);
        var isLegacyConnectionModeCompatibility = supportedConnectionModes is null;
        SupportedConnectionModes = isLegacyConnectionModeCompatibility &&
            connectionMode == AgentConnectionMode.Unknown
            ? Array.Empty<AgentConnectionMode>()
            : AgentContractValidation.CopyDistinctEnums(
                supportedConnectionModes,
                nameof(supportedConnectionModes),
                isLegacyConnectionModeCompatibility ? connectionMode : null);
        if (!isLegacyConnectionModeCompatibility)
        {
            AgentContractValidation.RejectUnverifiedOrUnsupportedSupportedModes(
                SupportedConnectionModes,
                nameof(supportedConnectionModes));
        }

        if (connectionMode is not AgentConnectionMode.Unknown and not AgentConnectionMode.Unsupported &&
            !SupportedConnectionModes.Contains(connectionMode))
        {
            throw new ArgumentException(
                "The legacy connection mode must remain represented in supported connection modes.",
                nameof(supportedConnectionModes));
        }

        if (availability == AgentAvailability.Available &&
            SupportedConnectionModes.Count == 1 &&
            SupportedConnectionModes[0] == AgentConnectionMode.Unsupported)
        {
            throw new ArgumentException(
                "Unsupported invocation truth cannot be presented as available.",
                nameof(availability));
        }

        if (availability == AgentAvailability.AuthenticationRequired &&
            authenticationState == AgentAuthenticationState.Authenticated)
        {
            throw new ArgumentException(
                "Authentication-required availability cannot be presented as authenticated.",
                nameof(authenticationState));
        }

        AuthenticationState = authenticationState;
        EntitlementState = entitlementState;
        RolePolicyMetadata = CopyRolePolicyMetadata(rolePolicyMetadata, RoleCapabilities);
        if (lastConnectionResult is not null && lastConnectionResult.Identity.Id != id)
        {
            throw new ArgumentException(
                "The last connection result must refer to this agent identity.",
                nameof(lastConnectionResult));
        }

        LastConnectionResult = lastConnectionResult;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string Role { get; }

    public string? Provider { get; }

    /// <summary>
    /// Stable provider/product/model identifier when one is known. It is never a credential.
    /// </summary>
    public string? ModelIdentifier { get; }

    public AgentIdentity Identity => new(Id, Name, Provider, ModelIdentifier);

    public AgentConnectionMode ConnectionMode { get; }

    public AgentAvailability Availability { get; }

    public bool Enabled { get; }

    public IReadOnlyList<string> Capabilities { get; }

    public IReadOnlyList<string> Limitations { get; }

    public IReadOnlyDictionary<string, string?> CostAndQuotaMetadata { get; }

    public IReadOnlyList<AgentRole> RoleCapabilities { get; }

    public IReadOnlyList<AgentConnectionMode> SupportedConnectionModes { get; }

    public AgentAuthenticationState AuthenticationState { get; }

    public AgentEntitlementState EntitlementState { get; }

    public IReadOnlyList<AgentRolePolicyMetadata> RolePolicyMetadata { get; }

    public AgentConnectionResult? LastConnectionResult { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }

    private static IReadOnlyList<string> CopyList(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<string>();
        }

        return AgentContractValidation.CopyStrings(
            values,
            nameof(values),
            maximumLength: 500);
    }

    private static IReadOnlyList<AgentRole> NormalizeRoleCapabilities(
        IReadOnlyList<AgentRole>? values,
        string legacyRole)
    {
        if (values is not null)
        {
            return AgentContractValidation.CopyDistinctEnums(values, nameof(values));
        }

        return TryMapLegacyRole(legacyRole, out var role)
            ? [role]
            : Array.Empty<AgentRole>();
    }

    private static IReadOnlyList<AgentRolePolicyMetadata> CopyRolePolicyMetadata(
        IReadOnlyList<AgentRolePolicyMetadata>? values,
        IReadOnlyList<AgentRole> roleCapabilities)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<AgentRolePolicyMetadata>();
        }

        var result = new List<AgentRolePolicyMetadata>(values.Count);
        foreach (var value in values)
        {
            ArgumentNullException.ThrowIfNull(value);
            if (result.Any(existing => existing.Role == value.Role))
            {
                throw new ArgumentException(
                    "Role policy metadata cannot contain duplicate roles.",
                    nameof(values));
            }

            if (!roleCapabilities.Contains(value.Role))
            {
                throw new ArgumentException(
                    "Role policy metadata cannot describe a role that is not a capability.",
                    nameof(values));
            }

            result.Add(value);
        }

        return result.AsReadOnly();
    }

    private static bool TryMapLegacyRole(string value, out AgentRole role)
    {
        role = value.Trim().ToLowerInvariant() switch
        {
            "planner" => AgentRole.Planner,
            "architect" => AgentRole.Architect,
            "acceptance authority" => AgentRole.AcceptanceAuthority,
            "executor" => AgentRole.Executor,
            "reviewer" => AgentRole.Reviewer,
            "security specialist" => AgentRole.SecuritySpecialist,
            "auxiliary executor" => AgentRole.AuxiliaryExecutor,
            _ => default
        };

        return value.Trim().ToLowerInvariant() is
            "planner" or
            "architect" or
            "acceptance authority" or
            "executor" or
            "reviewer" or
            "security specialist" or
            "auxiliary executor";
    }
}
