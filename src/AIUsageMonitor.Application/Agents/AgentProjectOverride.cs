using AIUsageMonitor.Application.Common;

namespace AIUsageMonitor.Application.Agents;

/// <summary>
/// Project-isolated configuration override for one global agent definition. It cannot change
/// provider truth, authentication, entitlement, availability, quota, or credentials.
/// </summary>
public sealed class AgentProjectOverride
{
    public AgentProjectOverride(
        Guid projectId,
        Guid agentId,
        bool? enabledOverride = null,
        IReadOnlyList<AgentRole>? permittedRoles = null,
        IReadOnlyList<AgentConnectionMode>? permittedConnectionModes = null,
        string? policyReference = null,
        IReadOnlyDictionary<string, string?>? metadata = null)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        if (agentId == Guid.Empty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(agentId));
        }

        ProjectId = projectId;
        AgentId = agentId;
        EnabledOverride = enabledOverride;
        PermittedRoles = AgentContractValidation.CopyOptionalDistinctEnums(
            permittedRoles,
            nameof(permittedRoles));
        PermittedConnectionModes = AgentContractValidation.CopyOptionalDistinctEnums(
            permittedConnectionModes,
            nameof(permittedConnectionModes));
        if (PermittedConnectionModes is not null)
        {
            AgentContractValidation.RejectUnsupportedModeMix(
                PermittedConnectionModes,
                nameof(permittedConnectionModes));
        }

        PolicyReference = AgentContractValidation.NormalizeOptional(policyReference, nameof(policyReference), 300);
        Metadata = MetadataValidation.Copy(metadata);
    }

    public Guid ProjectId { get; }

    public Guid AgentId { get; }

    public bool? EnabledOverride { get; }

    public IReadOnlyList<AgentRole>? PermittedRoles { get; }

    public IReadOnlyList<AgentConnectionMode>? PermittedConnectionModes { get; }

    public string? PolicyReference { get; }

    public IReadOnlyDictionary<string, string?> Metadata { get; }
}
