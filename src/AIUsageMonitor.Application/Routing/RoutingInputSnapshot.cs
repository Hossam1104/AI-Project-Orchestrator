using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIUsageMonitor.Application.Planning;

namespace AIUsageMonitor.Application.Routing;

public sealed class RoutingContextReference
{
    public RoutingContextReference(Guid contextId, int contextContractVersion, DateTimeOffset updatedAt)
    {
        if (contextId == Guid.Empty)
        {
            throw new ArgumentException("Routing context id cannot be empty.", nameof(contextId));
        }

        if (contextContractVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(contextContractVersion));
        }

        if (updatedAt == default)
        {
            throw new ArgumentException("Routing context update time is required.", nameof(updatedAt));
        }

        ContextId = contextId;
        ContextContractVersion = contextContractVersion;
        UpdatedAt = updatedAt;
    }

    public Guid ContextId { get; }
    public int ContextContractVersion { get; }
    public DateTimeOffset UpdatedAt { get; }
}

/// <summary>Immutable, bounded effective inputs used by the pure routing engine.</summary>
public sealed class RoutingInputSnapshot
{
    public const int MaximumCandidates = 128;
    public const int MaximumCapacityEvidence = 128;

    public RoutingInputSnapshot(
        Guid projectId,
        PlanningExecutionContractReference planningContractReference,
        RoutingContextReference context,
        RoutingTaskClassification classification,
        RoutingPolicySnapshot policy,
        IReadOnlyList<RoutingAgentSnapshot> candidates,
        IReadOnlyList<RoutingCapacityEvidence>? capacityEvidence,
        RoutingOwnerOverrideRequest? ownerOverride,
        DateTimeOffset evaluatedAt,
        string? inputFingerprint = null)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Routing project id cannot be empty.", nameof(projectId));
        }

        ArgumentNullException.ThrowIfNull(planningContractReference);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(classification);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(candidates);
        if (evaluatedAt == default)
        {
            throw new ArgumentException("Routing evaluation time is required.", nameof(evaluatedAt));
        }

        if (planningContractReference.ContractId == Guid.Empty ||
            planningContractReference.SchemaVersion <= 0 ||
            !PlanningExecutionContractReference.IsSha256(planningContractReference.ContentHash))
        {
            throw new ArgumentException("Routing requires a complete planning contract reference.", nameof(planningContractReference));
        }

        if (classification.RequiredRole != policy.RequiredRole)
        {
            throw new ArgumentException("Classification and executable routing policy roles must match.", nameof(policy));
        }

        var candidateCopies = candidates.ToArray();
        if (candidateCopies.Length > MaximumCandidates)
        {
            throw new ArgumentException($"Routing candidates cannot exceed {MaximumCandidates} entries.", nameof(candidates));
        }

        if (candidateCopies.Any(static value => value is null))
        {
            throw new ArgumentException("Routing candidates cannot contain null entries.", nameof(candidates));
        }

        if (candidateCopies.Any(value => value.ProjectId != projectId))
        {
            throw new ArgumentException("Routing candidates must belong to the requested project.", nameof(candidates));
        }

        if (candidateCopies.Select(static value => value.AgentId).Distinct().Count() != candidateCopies.Length)
        {
            throw new ArgumentException("Routing candidates cannot contain duplicate agent ids.", nameof(candidates));
        }

        var evidenceCopies = (capacityEvidence ?? Array.Empty<RoutingCapacityEvidence>()).ToArray();
        if (evidenceCopies.Length > MaximumCapacityEvidence)
        {
            throw new ArgumentException($"Routing capacity evidence cannot exceed {MaximumCapacityEvidence} entries.", nameof(capacityEvidence));
        }
        if (evidenceCopies.Any(static value => value is null))
        {
            throw new ArgumentException("Routing capacity evidence cannot contain null entries.", nameof(capacityEvidence));
        }

        if (evidenceCopies.Select(static value => value.AgentId).Distinct().Count() != evidenceCopies.Length)
        {
            throw new ArgumentException("Routing capacity evidence must be pre-aggregated to one exact observation per agent.", nameof(capacityEvidence));
        }

        var candidateIds = candidateCopies.Select(static value => value.AgentId).ToHashSet();
        if (evidenceCopies.Any(value => !candidateIds.Contains(value.AgentId)))
        {
            throw new ArgumentException("Capacity evidence must refer to a considered candidate.", nameof(capacityEvidence));
        }

        if (evidenceCopies.Any(value => value.ObservedAt > evaluatedAt))
        {
            throw new ArgumentException(
                "Routing capacity evidence cannot be observed after the routing evaluation time.",
                nameof(capacityEvidence));
        }

        if (ownerOverride is not null && ownerOverride.RequestedAt > evaluatedAt)
        {
            throw new ArgumentException(
                "An owner override cannot be requested after the routing evaluation time.",
                nameof(ownerOverride));
        }

        ProjectId = projectId;
        PlanningContractReference = planningContractReference;
        Context = context;
        Classification = classification;
        Policy = policy;
        Candidates = candidateCopies.OrderBy(static value => value.AgentId).ToArray();
        CapacityEvidence = evidenceCopies.OrderBy(static value => value.AgentId).ToArray();
        OwnerOverride = ownerOverride;
        EvaluatedAt = evaluatedAt;
        InputFingerprint = ComputeInputFingerprint(this);
        if (inputFingerprint is not null &&
            !string.Equals(InputFingerprint, inputFingerprint, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Routing input fingerprint does not match the normalized input.", nameof(inputFingerprint));
        }
    }

    public Guid ProjectId { get; }
    public PlanningExecutionContractReference PlanningContractReference { get; }
    public RoutingContextReference Context { get; }
    public RoutingTaskClassification Classification { get; }
    public RoutingPolicySnapshot Policy { get; }
    public IReadOnlyList<RoutingAgentSnapshot> Candidates { get; }
    public IReadOnlyList<RoutingCapacityEvidence> CapacityEvidence { get; }
    public RoutingOwnerOverrideRequest? OwnerOverride { get; }
    public DateTimeOffset EvaluatedAt { get; }
    public string InputFingerprint { get; }

    public static string ComputeInputFingerprint(RoutingInputSnapshot input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var json = JsonSerializer.Serialize(CreateCanonicalPayload(input), Options);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    internal static object CreateCanonicalPayload(RoutingInputSnapshot input) => new
    {
        input.ProjectId,
        planningContractReference = new
        {
            input.PlanningContractReference.ContractId,
            input.PlanningContractReference.Revision,
            input.PlanningContractReference.SchemaVersion,
            input.PlanningContractReference.ContentHash
        },
        context = new
        {
            input.Context.ContextId,
            input.Context.ContextContractVersion,
            input.Context.UpdatedAt
        },
        classification = new
        {
            input.Classification.ScopeScale,
            input.Classification.Risk,
            input.Classification.BlastRadius,
            input.Classification.ValidationCost,
            input.Classification.RequiredRole,
            requiredCapabilities = input.Classification.RequiredCapabilities.ToArray(),
            policyTags = input.Classification.PolicyTags.ToArray(),
            input.Classification.CapacityRequirement,
            input.Classification.IndependentReviewRequired,
            input.Classification.SecurityReviewRequired,
            input.Classification.OwnerApprovalRequired,
            input.Classification.RequiresSupportedConnection,
            input.Classification.RequiresVerifiedAvailability,
            input.Classification.RequiresAuthenticatedAccess,
            input.Classification.RequiresVerifiedEntitlement
        },
        policy = new
        {
            input.Policy.PolicyId,
            input.Policy.RequiredRole,
            preferredAgentIds = input.Policy.PreferredAgentIds.ToArray(),
            prohibitedAgentIds = input.Policy.ProhibitedAgentIds.ToArray(),
            input.Policy.CapacityRequirement,
            input.Policy.MinimumCapacityState,
            input.Policy.IndependentReviewRequired,
            input.Policy.SecurityReviewRequired,
            input.Policy.OwnerApprovalRequired,
            input.Policy.RequireSupportedConnection,
            input.Policy.RequireVerifiedAvailability,
            input.Policy.RequireAuthenticatedAccess,
            input.Policy.RequireVerifiedEntitlement,
            input.Policy.PolicyReference,
            input.Policy.Reason
        },
        candidates = input.Candidates.Select(static candidate => new
        {
            candidate.ProjectId,
            candidate.AgentId,
            identity = new
            {
                candidate.Identity.Id,
                candidate.Identity.DisplayName,
                candidate.Identity.Provider,
                candidate.Identity.ModelIdentifier
            },
            candidate.RegistryUpdatedAt,
            candidate.Enabled,
            roleCapabilities = candidate.RoleCapabilities.ToArray(),
            capabilities = candidate.Capabilities.ToArray(),
            limitations = candidate.Limitations.ToArray(),
            candidate.ConnectionMode,
            supportedConnectionModes = candidate.SupportedConnectionModes.ToArray(),
            candidate.Availability,
            candidate.AuthenticationState,
            candidate.EntitlementState
        }).ToArray(),
        capacityEvidence = input.CapacityEvidence.Select(static evidence => new
        {
            evidence.AgentId,
            evidence.CapacityState,
            evidence.ObservedAt,
            evidence.ValidUntil,
            evidence.EvidenceReference,
            evidence.ProviderId,
            evidence.QuotaDefinitionId,
            evidence.RemainingFraction,
            evidence.Source
        }).ToArray(),
        ownerOverride = input.OwnerOverride is null
            ? null
            : new
            {
                input.OwnerOverride.RequestedAgentId,
                input.OwnerOverride.ActorReference,
                input.OwnerOverride.Reason,
                input.OwnerOverride.RequestedAt
            },
        input.EvaluatedAt
    };

    private static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
