using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Common;

namespace AIUsageMonitor.Application.Routing;

/// <summary>
/// Explicit executable routing policy input. This is deliberately separate from the descriptive
/// <see cref="AgentRolePolicyMetadata"/> carried by the agent registry.
/// </summary>
public sealed class RoutingPolicySnapshot
{
    public const int MaximumPreferredAgents = 64;
    public const int MaximumProhibitedAgents = 64;

    public RoutingPolicySnapshot(
        string policyId,
        AgentRole requiredRole,
        IReadOnlyList<Guid>? preferredAgentIds = null,
        IReadOnlyList<Guid>? prohibitedAgentIds = null,
        RoutingCapacityRequirement capacityRequirement = RoutingCapacityRequirement.Required,
        RoutingCapacityState? minimumCapacityState = null,
        bool independentReviewRequired = false,
        bool securityReviewRequired = false,
        bool ownerApprovalRequired = false,
        bool requireSupportedConnection = true,
        bool requireVerifiedAvailability = false,
        bool requireAuthenticatedAccess = false,
        bool requireVerifiedEntitlement = false,
        string? policyReference = null,
        string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(policyId))
        {
            throw new ArgumentException("Routing policy id is required.", nameof(policyId));
        }

        if (!Enum.IsDefined(requiredRole))
        {
            throw new ArgumentException("Routing policy role is undefined.", nameof(requiredRole));
        }

        if (!Enum.IsDefined(capacityRequirement))
        {
            throw new ArgumentException("Routing policy capacity requirement is undefined.", nameof(capacityRequirement));
        }

        if (minimumCapacityState is not null && !Enum.IsDefined(minimumCapacityState.Value))
        {
            throw new ArgumentException("Routing policy minimum capacity state is undefined.", nameof(minimumCapacityState));
        }

        PolicyId = Normalize(policyId, nameof(policyId), 200);
        RequiredRole = requiredRole;
        PreferredAgentIds = NormalizeIds(preferredAgentIds, MaximumPreferredAgents, nameof(preferredAgentIds), preserveOrder: true);
        ProhibitedAgentIds = NormalizeIds(prohibitedAgentIds, MaximumProhibitedAgents, nameof(prohibitedAgentIds), preserveOrder: false);
        if (PreferredAgentIds.Any(ProhibitedAgentIds.Contains))
        {
            throw new ArgumentException("An agent cannot be both preferred and prohibited.", nameof(preferredAgentIds));
        }

        CapacityRequirement = capacityRequirement;
        MinimumCapacityState = minimumCapacityState;
        IndependentReviewRequired = independentReviewRequired;
        SecurityReviewRequired = securityReviewRequired;
        OwnerApprovalRequired = ownerApprovalRequired;
        RequireSupportedConnection = requireSupportedConnection;
        RequireVerifiedAvailability = requireVerifiedAvailability;
        RequireAuthenticatedAccess = requireAuthenticatedAccess;
        RequireVerifiedEntitlement = requireVerifiedEntitlement;
        PolicyReference = NormalizeOptional(policyReference, nameof(policyReference), 300);
        Reason = NormalizeOptional(reason, nameof(reason), 1_000);
    }

    public string PolicyId { get; }
    public AgentRole RequiredRole { get; }
    public IReadOnlyList<Guid> PreferredAgentIds { get; }
    public IReadOnlyList<Guid> ProhibitedAgentIds { get; }
    public RoutingCapacityRequirement CapacityRequirement { get; }
    public RoutingCapacityState? MinimumCapacityState { get; }
    public bool IndependentReviewRequired { get; }
    public bool SecurityReviewRequired { get; }
    public bool OwnerApprovalRequired { get; }
    public bool RequireSupportedConnection { get; }
    public bool RequireVerifiedAvailability { get; }
    public bool RequireAuthenticatedAccess { get; }
    public bool RequireVerifiedEntitlement { get; }
    public string? PolicyReference { get; }
    public string? Reason { get; }

    public RoutingPolicySnapshot WithDescriptions(string? policyReference, string? reason) => new(
        PolicyId,
        RequiredRole,
        PreferredAgentIds,
        ProhibitedAgentIds,
        CapacityRequirement,
        MinimumCapacityState,
        IndependentReviewRequired,
        SecurityReviewRequired,
        OwnerApprovalRequired,
        RequireSupportedConnection,
        RequireVerifiedAvailability,
        RequireAuthenticatedAccess,
        RequireVerifiedEntitlement,
        policyReference,
        reason);

    private static IReadOnlyList<Guid> NormalizeIds(
        IReadOnlyList<Guid>? values,
        int maximum,
        string parameterName,
        bool preserveOrder)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<Guid>();
        }

        if (values.Count > maximum)
        {
            throw new ArgumentException($"Routing policy contains too many agent ids.", parameterName);
        }

        var result = new List<Guid>(values.Count);
        foreach (var value in values)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException("Routing policy agent ids cannot be empty.", parameterName);
            }

            if (result.Contains(value))
            {
                throw new ArgumentException("Routing policy agent ids cannot repeat.", parameterName);
            }

            result.Add(value);
        }

        if (!preserveOrder)
        {
            result.Sort();
        }

        return result.AsReadOnly();
    }

    private static string Normalize(string value, string parameterName, int maximumLength)
    {
        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException($"The value cannot exceed {maximumLength} characters.", parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Normalize(value, parameterName, maximumLength);
    }
}
