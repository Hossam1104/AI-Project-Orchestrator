using AIUsageMonitor.Application.Agents;

namespace AIUsageMonitor.Application.Routing;

public enum RoutingScopeScale
{
    Bounded,
    MultiFile,
    CrossCutting,
    ProjectWide
}

public enum RoutingTaskRisk
{
    Low,
    Moderate,
    High,
    Critical
}

public enum RoutingBlastRadius
{
    Local,
    Module,
    Project,
    CrossProject,
    ExternalSystem
}

public enum RoutingValidationCost
{
    Low,
    Moderate,
    High
}

public enum RoutingCapacityRequirement
{
    Required,
    Optional,
    NotApplicable
}

/// <summary>
/// Explicit caller-supplied work classification. It is data for routing, not an ML/LLM
/// classification result and it does not select or invoke an agent.
/// </summary>
public sealed class RoutingTaskClassification
{
    public RoutingTaskClassification(
        RoutingScopeScale scopeScale,
        RoutingTaskRisk risk,
        RoutingBlastRadius blastRadius,
        RoutingValidationCost validationCost,
        AgentRole requiredRole,
        IReadOnlyList<string>? requiredCapabilities = null,
        IReadOnlyList<string>? policyTags = null,
        RoutingCapacityRequirement capacityRequirement = RoutingCapacityRequirement.Required,
        bool independentReviewRequired = false,
        bool securityReviewRequired = false,
        bool ownerApprovalRequired = false,
        bool requiresSupportedConnection = true,
        bool requiresVerifiedAvailability = false,
        bool requiresAuthenticatedAccess = false,
        bool requiresVerifiedEntitlement = false)
    {
        ValidateEnum(scopeScale, nameof(scopeScale));
        ValidateEnum(risk, nameof(risk));
        ValidateEnum(blastRadius, nameof(blastRadius));
        ValidateEnum(validationCost, nameof(validationCost));
        ValidateEnum(requiredRole, nameof(requiredRole));
        ValidateEnum(capacityRequirement, nameof(capacityRequirement));

        ScopeScale = scopeScale;
        Risk = risk;
        BlastRadius = blastRadius;
        ValidationCost = validationCost;
        RequiredRole = requiredRole;
        RequiredCapabilities = RoutingTagNormalization.Normalize(requiredCapabilities, nameof(requiredCapabilities));
        PolicyTags = RoutingTagNormalization.Normalize(policyTags, nameof(policyTags));
        CapacityRequirement = capacityRequirement;
        IndependentReviewRequired = independentReviewRequired;
        SecurityReviewRequired = securityReviewRequired;
        OwnerApprovalRequired = ownerApprovalRequired;
        RequiresSupportedConnection = requiresSupportedConnection;
        RequiresVerifiedAvailability = requiresVerifiedAvailability;
        RequiresAuthenticatedAccess = requiresAuthenticatedAccess;
        RequiresVerifiedEntitlement = requiresVerifiedEntitlement;
    }

    public RoutingScopeScale ScopeScale { get; }
    public RoutingTaskRisk Risk { get; }
    public RoutingBlastRadius BlastRadius { get; }
    public RoutingValidationCost ValidationCost { get; }
    public AgentRole RequiredRole { get; }
    public IReadOnlyList<string> RequiredCapabilities { get; }
    public IReadOnlyList<string> PolicyTags { get; }
    public RoutingCapacityRequirement CapacityRequirement { get; }
    public bool IndependentReviewRequired { get; }
    public bool SecurityReviewRequired { get; }
    public bool OwnerApprovalRequired { get; }
    public bool RequiresSupportedConnection { get; }
    public bool RequiresVerifiedAvailability { get; }
    public bool RequiresAuthenticatedAccess { get; }
    public bool RequiresVerifiedEntitlement { get; }

    private static void ValidateEnum<T>(T value, string parameterName)
        where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentException("The routing classification contains an undefined value.", parameterName);
        }
    }
}

/// <summary>Deterministic, bounded, case-insensitive tag normalization for routing inputs.</summary>
public static class RoutingTagNormalization
{
    public const int MaximumCount = 64;
    public const int MaximumLength = 160;

    public static IReadOnlyList<string> Normalize(
        IReadOnlyList<string>? values,
        string parameterName)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<string>();
        }

        if (values.Count > MaximumCount)
        {
            throw new ArgumentException($"Routing tags cannot exceed {MaximumCount} values.", parameterName);
        }

        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Routing tags cannot be blank.", parameterName);
            }

            var item = value.Trim().ToLowerInvariant();
            if (item.Length > MaximumLength)
            {
                throw new ArgumentException(
                    $"Routing tags cannot exceed {MaximumLength} characters.",
                    parameterName);
            }

            normalized.Add(item);
        }

        return normalized.OrderBy(static value => value, StringComparer.Ordinal).ToArray();
    }
}
