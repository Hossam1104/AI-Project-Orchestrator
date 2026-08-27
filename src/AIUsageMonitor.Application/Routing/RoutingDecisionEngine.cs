using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Common;

namespace AIUsageMonitor.Application.Routing;

public enum RoutingDecisionOutcome
{
    Recommended,
    OwnerOverrideApplied,
    OwnerOverrideRejected,
    NoEligibleCandidate,
    InsufficientCapacity,
    StaleCapacityEvidence,
    InsufficientEvidence,
    PolicyBlocked,
    ContextInsufficient
}

public enum RoutingReasonCode
{
    Eligible,
    Disabled,
    RoleUnsupported,
    MissingCapability,
    ConnectionUnsupported,
    ConnectionUnknown,
    AuthenticationUnavailable,
    EntitlementUnavailable,
    AvailabilityUnavailable,
    PolicyBlocked,
    CapacityInsufficient,
    CapacityStale,
    CapacityUnknown,
    CapacityNotMapped,
    CapacityConstrained,
    CapacityBelowMinimum,
    LowerPreference,
    Selected,
    OverrideRequested,
    OverrideRejected,
    OptionalCapacityEvidence,
    ReviewGateRequired,
    SecurityGateRequired,
    OwnerApprovalRequired
}

public enum RoutingOverrideDisposition
{
    None,
    Applied,
    Rejected
}

public enum RoutingConfidence
{
    High,
    Medium,
    Low,
    Insufficient
}

public sealed class RoutingCandidateAssessment
{
    public RoutingCandidateAssessment(
        RoutingAgentSnapshot candidate,
        RoutingCapacityState capacityState,
        int? preferencePosition,
        bool isHardEligible,
        bool isEligible,
        IReadOnlyList<string> missingCapabilities,
        IReadOnlyList<RoutingReasonCode> reasons)
    {
        Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
        if (!Enum.IsDefined(capacityState))
        {
            throw new ArgumentException("Capacity state is undefined.", nameof(capacityState));
        }

        if (preferencePosition is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(preferencePosition));
        }

        CapacityState = capacityState;
        PreferencePosition = preferencePosition;
        IsHardEligible = isHardEligible;
        IsEligible = isEligible && isHardEligible;
        MissingCapabilities = CopyStrings(missingCapabilities, nameof(missingCapabilities), 64, 160);
        Reasons = CopyReasons(reasons);
    }

    public RoutingAgentSnapshot Candidate { get; }
    public Guid AgentId => Candidate.AgentId;
    public RoutingCapacityState CapacityState { get; }
    public int? PreferencePosition { get; }
    public bool IsHardEligible { get; }
    public bool IsEligible { get; }
    public IReadOnlyList<string> MissingCapabilities { get; }
    public IReadOnlyList<RoutingReasonCode> Reasons { get; }

    private static IReadOnlyList<string> CopyStrings(
        IReadOnlyList<string> values,
        string parameterName,
        int maxCount,
        int maxLength)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count > maxCount)
        {
            throw new ArgumentException("Routing text list is unbounded.", parameterName);
        }

        return values.Select(value =>
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
            {
                throw new ArgumentException("Routing text list contains an invalid value.", parameterName);
            }

            return value.Trim();
        }).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    private static IReadOnlyList<RoutingReasonCode> CopyReasons(IReadOnlyList<RoutingReasonCode> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count > 32 || values.Any(value => !Enum.IsDefined(value)))
        {
            throw new ArgumentException("Routing reason list is invalid.", nameof(values));
        }

        return values.Distinct().OrderBy(static value => value).ToArray();
    }
}

public sealed class RoutingRecommendation
{
    public RoutingRecommendation(
        Guid selectedAgentId,
        AgentIdentity selectedAgentIdentity,
        AgentRole requestedRole,
        IReadOnlyList<RoutingReasonCode> reasons,
        int? preferencePosition,
        RoutingCapacityState capacityState,
        bool independentReviewRequired,
        bool securityReviewRequired,
        bool ownerApprovalRequired,
        RoutingConfidence confidence,
        IReadOnlyList<string>? limitations = null,
        string nextSafeAction = "Await required downstream gates before execution.")
    {
        if (selectedAgentId == Guid.Empty)
        {
            throw new ArgumentException("A recommendation requires a selected agent.", nameof(selectedAgentId));
        }

        ArgumentNullException.ThrowIfNull(selectedAgentIdentity);
        if (selectedAgentIdentity.Id != selectedAgentId)
        {
            throw new ArgumentException("Recommendation identity does not match selected agent.", nameof(selectedAgentIdentity));
        }

        if (!Enum.IsDefined(requestedRole) || !Enum.IsDefined(capacityState) || !Enum.IsDefined(confidence))
        {
            throw new ArgumentException("Recommendation contains an undefined value.");
        }

        SelectedAgentId = selectedAgentId;
        SelectedAgentIdentity = selectedAgentIdentity;
        RequestedRole = requestedRole;
        if (reasons is null || reasons.Any(value => !Enum.IsDefined(value)))
        {
            throw new ArgumentException("Recommendation reasons are invalid.", nameof(reasons));
        }

        Reasons = reasons.Distinct().OrderBy(static value => value).ToArray();
        PreferencePosition = preferencePosition;
        CapacityState = capacityState;
        IndependentReviewRequired = independentReviewRequired;
        SecurityReviewRequired = securityReviewRequired;
        OwnerApprovalRequired = ownerApprovalRequired;
        Confidence = confidence;
        Limitations = NormalizeTextList(limitations, nameof(limitations), 32, 1_000);
        NextSafeAction = RequiredText(nextSafeAction, nameof(nextSafeAction), 500);
    }

    public Guid SelectedAgentId { get; }
    public AgentIdentity SelectedAgentIdentity { get; }
    public AgentRole RequestedRole { get; }
    public IReadOnlyList<RoutingReasonCode> Reasons { get; }
    public int? PreferencePosition { get; }
    public RoutingCapacityState CapacityState { get; }
    public bool IndependentReviewRequired { get; }
    public bool SecurityReviewRequired { get; }
    public bool OwnerApprovalRequired { get; }
    public RoutingConfidence Confidence { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string NextSafeAction { get; }

    private static IReadOnlyList<string> NormalizeTextList(
        IReadOnlyList<string>? values,
        string parameterName,
        int maxCount,
        int maxLength)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<string>();
        }

        if (values.Count > maxCount)
        {
            throw new ArgumentException("Recommendation text list is unbounded.", parameterName);
        }

        return values.Select(value => RequiredText(value, parameterName, maxLength))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static string RequiredText(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A bounded recommendation text value is required.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException("Recommendation text exceeds its bound.", parameterName);
        }

        return normalized;
    }
}

public sealed class RoutingEvaluation
{
    public RoutingEvaluation(
        RoutingInputSnapshot input,
        RoutingDecisionOutcome outcome,
        IReadOnlyList<RoutingCandidateAssessment> assessments,
        RoutingRecommendation? originalRecommendation,
        RoutingRecommendation? recommendation,
        RoutingOverrideDisposition ownerOverrideDisposition,
        RoutingConfidence confidence,
        IReadOnlyList<string> limitations,
        IReadOnlyList<RoutingReasonCode> reasons)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        if (!Enum.IsDefined(outcome) || !Enum.IsDefined(ownerOverrideDisposition) || !Enum.IsDefined(confidence))
        {
            throw new ArgumentException("Routing evaluation contains an undefined value.");
        }

        Assessments = assessments?.ToArray() ?? throw new ArgumentNullException(nameof(assessments));
        OriginalRecommendation = originalRecommendation;
        Recommendation = recommendation;
        OriginalSelectedAgentId = originalRecommendation?.SelectedAgentId;
        SelectedAgentId = recommendation?.SelectedAgentId;
        Outcome = outcome;
        OwnerOverrideDisposition = ownerOverrideDisposition;
        Confidence = confidence;
        Limitations = limitations?.ToArray() ?? throw new ArgumentNullException(nameof(limitations));
        if (reasons is null || reasons.Any(value => !Enum.IsDefined(value)))
        {
            throw new ArgumentException("Routing reasons are invalid.", nameof(reasons));
        }

        Reasons = reasons.Distinct().OrderBy(static value => value).ToArray();
    }

    public RoutingInputSnapshot Input { get; }
    public RoutingDecisionOutcome Outcome { get; }
    public IReadOnlyList<RoutingCandidateAssessment> Assessments { get; }
    public RoutingRecommendation? OriginalRecommendation { get; }
    public RoutingRecommendation? Recommendation { get; }
    public Guid? OriginalSelectedAgentId { get; }
    public Guid? SelectedAgentId { get; }
    public RoutingOverrideDisposition OwnerOverrideDisposition { get; }
    public RoutingConfidence Confidence { get; }
    public IReadOnlyList<string> Limitations { get; }
    public IReadOnlyList<RoutingReasonCode> Reasons { get; }
}

public interface IRoutingDecisionEngine
{
    RoutingEvaluation Evaluate(RoutingInputSnapshot input);
}

/// <summary>
/// Pure deterministic quality-first routing. It has no clock, file, provider, network, or
/// execution dependency. Capacity is considered only after hard eligibility and policy fit.
/// </summary>
public sealed class RoutingDecisionEngine : IRoutingDecisionEngine
{
    private const string GateAction = "Await required downstream gates before execution.";

    public RoutingEvaluation Evaluate(RoutingInputSnapshot input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var rolePolicyMatches = input.Classification.RequiredRole == input.Policy.RequiredRole;
        var assessments = new List<RoutingCandidateAssessment>(input.Candidates.Count);
        foreach (var candidate in input.Candidates)
        {
            assessments.Add(Assess(input, candidate, rolePolicyMatches));
        }

        var limitations = new List<string>();
        var eligible = assessments.Where(static assessment => assessment.IsEligible).ToArray();
        var originalWinner = eligible
            .OrderBy(assessment => PreferenceRank(input.Policy, assessment.AgentId))
            .ThenByDescending(assessment => CapacityRank(assessment.CapacityState))
            .ThenBy(assessment => assessment.AgentId)
            .FirstOrDefault();

        var originalRecommendation = originalWinner is null
            ? null
            : BuildRecommendation(input, originalWinner, assessments, limitations);

        if (assessments.Any(assessment => assessment.IsHardEligible &&
            assessment.CapacityState is RoutingCapacityState.Unknown or RoutingCapacityState.Stale or RoutingCapacityState.NotMapped))
        {
            limitations.Add("Some eligible candidates lack current, exact agent-bound capacity evidence.");
        }

        var ownerDisposition = RoutingOverrideDisposition.None;
        RoutingRecommendation? recommendation = originalRecommendation;
        RoutingDecisionOutcome outcome;
        if (input.OwnerOverride is not null)
        {
            ownerDisposition = RoutingOverrideDisposition.Rejected;
            var targetAssessment = assessments.FirstOrDefault(value => value.AgentId == input.OwnerOverride.RequestedAgentId);
            if (targetAssessment is not null && targetAssessment.IsEligible)
            {
                ownerDisposition = RoutingOverrideDisposition.Applied;
                recommendation = BuildRecommendation(input, targetAssessment, assessments, limitations, isOverride: true);
                outcome = RoutingDecisionOutcome.OwnerOverrideApplied;
            }
            else
            {
                recommendation = null;
                outcome = RoutingDecisionOutcome.OwnerOverrideRejected;
                limitations.Add("The owner override could not change hard eligibility or policy constraints; no fallback was applied.");
            }
        }
        else if (originalWinner is not null)
        {
            outcome = RoutingDecisionOutcome.Recommended;
        }
        else
        {
            outcome = DetermineFailureOutcome(assessments, rolePolicyMatches);
        }

        if (recommendation is not null)
        {
            foreach (var assessment in assessments.Where(value => value.IsEligible && value.AgentId != recommendation.SelectedAgentId))
            {
                // Assessments are immutable; the deterministic lower-preference reason is carried
                // in the overall explanation and the recommendation itself.
                _ = assessment;
            }
        }

        var reasons = assessments.SelectMany(static value => value.Reasons)
            .Concat(recommendation?.Reasons ?? Array.Empty<RoutingReasonCode>())
            .Concat(originalRecommendation?.Reasons ?? Array.Empty<RoutingReasonCode>())
            .Concat(input.OwnerOverride is null ? Array.Empty<RoutingReasonCode>() : [
                RoutingReasonCode.OverrideRequested,
                ownerDisposition == RoutingOverrideDisposition.Rejected ? RoutingReasonCode.OverrideRejected : RoutingReasonCode.Selected])
            .Distinct()
            .OrderBy(static value => value)
            .ToArray();

        var confidence = recommendation is null
            ? RoutingConfidence.Insufficient
            : recommendation.Confidence;
        return new(
            input,
            outcome,
            assessments,
            originalRecommendation,
            recommendation,
            ownerDisposition,
            confidence,
            limitations.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            reasons);
    }

    private static RoutingCandidateAssessment Assess(
        RoutingInputSnapshot input,
        RoutingAgentSnapshot candidate,
        bool rolePolicyMatches)
    {
        var reasons = new List<RoutingReasonCode>();
        var missingCapabilities = input.Classification.RequiredCapabilities
            .Where(required => !candidate.Capabilities.Contains(required, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        var preference = GetPreferencePosition(input.Policy, candidate.AgentId);

        if (!rolePolicyMatches)
        {
            reasons.Add(RoutingReasonCode.PolicyBlocked);
            return new(candidate, RoutingCapacityState.Unknown, preference, false, false, missingCapabilities, reasons);
        }

        if (input.Policy.ProhibitedAgentIds.Contains(candidate.AgentId))
        {
            reasons.Add(RoutingReasonCode.PolicyBlocked);
        }

        if (!candidate.Enabled)
        {
            reasons.Add(RoutingReasonCode.Disabled);
        }

        if (!candidate.RoleCapabilities.Contains(input.Classification.RequiredRole))
        {
            reasons.Add(RoutingReasonCode.RoleUnsupported);
        }

        if (missingCapabilities.Length > 0)
        {
            reasons.Add(RoutingReasonCode.MissingCapability);
        }

        var requireConnection = input.Classification.RequiresSupportedConnection || input.Policy.RequireSupportedConnection;
        if (candidate.ConnectionMode == AgentConnectionMode.Unsupported)
        {
            reasons.Add(RoutingReasonCode.ConnectionUnsupported);
        }
        else if (requireConnection && candidate.ConnectionMode == AgentConnectionMode.Unknown)
        {
            reasons.Add(RoutingReasonCode.ConnectionUnknown);
        }
        else if (requireConnection && (candidate.SupportedConnectionModes.Count == 0 ||
            candidate.SupportedConnectionModes.All(mode => mode is AgentConnectionMode.Unsupported or AgentConnectionMode.Unknown)))
        {
            reasons.Add(RoutingReasonCode.ConnectionUnsupported);
        }

        var requireAvailability = input.Classification.RequiresVerifiedAvailability || input.Policy.RequireVerifiedAvailability;
        if (candidate.Availability is AgentAvailability.Unavailable or AgentAvailability.Disabled or AgentAvailability.Unsupported ||
            (requireAvailability && candidate.Availability != AgentAvailability.Available))
        {
            reasons.Add(RoutingReasonCode.AvailabilityUnavailable);
        }

        var requireAuth = input.Classification.RequiresAuthenticatedAccess || input.Policy.RequireAuthenticatedAccess;
        if (requireAuth && candidate.AuthenticationState is not (AgentAuthenticationState.Authenticated or AgentAuthenticationState.NotRequired))
        {
            reasons.Add(RoutingReasonCode.AuthenticationUnavailable);
        }

        var requireEntitlement = input.Classification.RequiresVerifiedEntitlement || input.Policy.RequireVerifiedEntitlement;
        if (requireEntitlement && candidate.EntitlementState is not (AgentEntitlementState.VerifiedAvailable or AgentEntitlementState.NotApplicable))
        {
            reasons.Add(RoutingReasonCode.EntitlementUnavailable);
        }

        var hardEligible = reasons.Count == 0;
        var capacity = GetCapacity(input, candidate.AgentId);
        var capacityRequirement = EffectiveCapacityRequirement(input);
        var capacityState = capacityRequirement == RoutingCapacityRequirement.NotApplicable
            ? RoutingCapacityState.NotApplicable
            : capacity?.GetStateAt(input.EvaluatedAt) ?? RoutingCapacityState.Unknown;

        if (hardEligible)
        {
            AddCapacityReason(reasons, input, capacityState);
        }

        var isEligible = hardEligible && CapacityAllowsSelection(input, capacityState, reasons);
        if (isEligible)
        {
            reasons.Add(RoutingReasonCode.Eligible);
        }

        return new(candidate, capacityState, preference, hardEligible, isEligible, missingCapabilities, reasons);
    }

    private static void AddCapacityReason(
        List<RoutingReasonCode> reasons,
        RoutingInputSnapshot input,
        RoutingCapacityState state)
    {
        switch (state)
        {
            case RoutingCapacityState.Constrained:
                reasons.Add(RoutingReasonCode.CapacityConstrained);
                break;
            case RoutingCapacityState.Insufficient:
                reasons.Add(RoutingReasonCode.CapacityInsufficient);
                break;
            case RoutingCapacityState.Stale:
                reasons.Add(RoutingReasonCode.CapacityStale);
                break;
            case RoutingCapacityState.Unknown:
                reasons.Add(RoutingReasonCode.CapacityUnknown);
                break;
            case RoutingCapacityState.NotMapped:
                reasons.Add(RoutingReasonCode.CapacityNotMapped);
                break;
        }

        if (input.Policy.MinimumCapacityState is not null &&
            state != input.Policy.MinimumCapacityState.Value &&
            CapacityRank(state) < CapacityRank(input.Policy.MinimumCapacityState.Value))
        {
            reasons.Add(RoutingReasonCode.CapacityBelowMinimum);
        }
    }

    private static bool CapacityAllowsSelection(
        RoutingInputSnapshot input,
        RoutingCapacityState state,
        List<RoutingReasonCode> reasons)
    {
        var requirement = EffectiveCapacityRequirement(input);
        if (requirement == RoutingCapacityRequirement.NotApplicable)
        {
            return true;
        }

        if (state == RoutingCapacityState.Insufficient ||
            (input.Policy.MinimumCapacityState is not null && CapacityRank(state) < CapacityRank(input.Policy.MinimumCapacityState.Value)))
        {
            return false;
        }

        if (requirement == RoutingCapacityRequirement.Required &&
            state is RoutingCapacityState.Unknown or RoutingCapacityState.Stale or RoutingCapacityState.NotMapped or RoutingCapacityState.NotApplicable)
        {
            return false;
        }

        return true;
    }

    private static RoutingRecommendation BuildRecommendation(
        RoutingInputSnapshot input,
        RoutingCandidateAssessment winner,
        IReadOnlyList<RoutingCandidateAssessment> assessments,
        List<string> limitations,
        bool isOverride = false)
    {
        var reasons = new List<RoutingReasonCode> { RoutingReasonCode.Selected };
        if (winner.PreferencePosition is null)
        {
            reasons.Add(RoutingReasonCode.LowerPreference);
        }

        if (isOverride)
        {
            reasons.Add(RoutingReasonCode.OverrideRequested);
        }

        if (EffectiveCapacityRequirement(input) == RoutingCapacityRequirement.Optional &&
            winner.CapacityState is not (RoutingCapacityState.Sufficient or RoutingCapacityState.Constrained or RoutingCapacityState.NotApplicable))
        {
            reasons.Add(RoutingReasonCode.OptionalCapacityEvidence);
            limitations.Add("Selection is allowed by optional capacity policy, but capacity evidence is not current and exact.");
        }

        if (input.Classification.IndependentReviewRequired || input.Policy.IndependentReviewRequired)
        {
            reasons.Add(RoutingReasonCode.ReviewGateRequired);
        }

        if (input.Classification.SecurityReviewRequired || input.Policy.SecurityReviewRequired)
        {
            reasons.Add(RoutingReasonCode.SecurityGateRequired);
        }

        if (input.Classification.OwnerApprovalRequired || input.Policy.OwnerApprovalRequired)
        {
            reasons.Add(RoutingReasonCode.OwnerApprovalRequired);
        }

        var confidence = DetermineConfidence(input, winner);
        var candidateLimitations = winner.Candidate.Limitations.ToList();
        if (confidence is RoutingConfidence.Low or RoutingConfidence.Medium)
        {
            candidateLimitations.Add("Routing confidence is limited by capacity or operational-state evidence.");
        }

        return new(
            winner.AgentId,
            winner.Candidate.Identity,
            input.Classification.RequiredRole,
            reasons,
            winner.PreferencePosition,
            winner.CapacityState,
            input.Classification.IndependentReviewRequired || input.Policy.IndependentReviewRequired,
            input.Classification.SecurityReviewRequired || input.Policy.SecurityReviewRequired,
            input.Classification.OwnerApprovalRequired || input.Policy.OwnerApprovalRequired,
            confidence,
            candidateLimitations,
            GateAction);
    }

    private static RoutingDecisionOutcome DetermineFailureOutcome(
        IReadOnlyList<RoutingCandidateAssessment> assessments,
        bool rolePolicyMatches)
    {
        if (!rolePolicyMatches)
        {
            return RoutingDecisionOutcome.PolicyBlocked;
        }

        var hardEligible = assessments.Where(static value => value.IsHardEligible).ToArray();
        if (hardEligible.Length == 0)
        {
            return assessments.Any(value => value.Reasons.Contains(RoutingReasonCode.PolicyBlocked))
                ? RoutingDecisionOutcome.PolicyBlocked
                : RoutingDecisionOutcome.NoEligibleCandidate;
        }

        if (hardEligible.Any(value => value.Reasons.Contains(RoutingReasonCode.CapacityInsufficient)))
        {
            return RoutingDecisionOutcome.InsufficientCapacity;
        }

        if (hardEligible.Any(value => value.Reasons.Contains(RoutingReasonCode.CapacityStale)))
        {
            return RoutingDecisionOutcome.StaleCapacityEvidence;
        }

        if (hardEligible.Any(value => value.Reasons.Contains(RoutingReasonCode.CapacityUnknown) ||
            value.Reasons.Contains(RoutingReasonCode.CapacityNotMapped)))
        {
            return RoutingDecisionOutcome.InsufficientEvidence;
        }

        return RoutingDecisionOutcome.NoEligibleCandidate;
    }

    private static RoutingCapacityEvidence? GetCapacity(RoutingInputSnapshot input, Guid agentId) =>
        input.CapacityEvidence.FirstOrDefault(value => value.AgentId == agentId);

    private static RoutingCapacityRequirement EffectiveCapacityRequirement(RoutingInputSnapshot input)
    {
        if (input.Classification.CapacityRequirement == RoutingCapacityRequirement.Required ||
            input.Policy.CapacityRequirement == RoutingCapacityRequirement.Required)
        {
            return RoutingCapacityRequirement.Required;
        }

        if (input.Classification.CapacityRequirement == RoutingCapacityRequirement.Optional ||
            input.Policy.CapacityRequirement == RoutingCapacityRequirement.Optional)
        {
            return RoutingCapacityRequirement.Optional;
        }

        return RoutingCapacityRequirement.NotApplicable;
    }

    private static int? GetPreferencePosition(RoutingPolicySnapshot policy, Guid agentId)
    {
        for (var index = 0; index < policy.PreferredAgentIds.Count; index++)
        {
            if (policy.PreferredAgentIds[index] == agentId)
            {
                return index + 1;
            }
        }

        return null;
    }

    private static int PreferenceRank(RoutingPolicySnapshot policy, Guid agentId) =>
        GetPreferencePosition(policy, agentId) ?? policy.PreferredAgentIds.Count + 1;

    private static int CapacityRank(RoutingCapacityState state) => state switch
    {
        RoutingCapacityState.Sufficient => 5,
        RoutingCapacityState.Constrained => 4,
        RoutingCapacityState.NotApplicable => 3,
        RoutingCapacityState.Unknown => 2,
        RoutingCapacityState.Stale => 1,
        RoutingCapacityState.NotMapped => 1,
        RoutingCapacityState.Insufficient => 0,
        _ => 0
    };

    private static RoutingConfidence DetermineConfidence(
        RoutingInputSnapshot input,
        RoutingCandidateAssessment winner)
    {
        if (winner.CapacityState == RoutingCapacityState.Sufficient &&
            winner.Candidate.Availability == AgentAvailability.Available &&
            winner.Candidate.ConnectionMode != AgentConnectionMode.Unknown &&
            winner.Candidate.ConnectionMode != AgentConnectionMode.Unsupported &&
            winner.Candidate.AuthenticationState is AgentAuthenticationState.Authenticated or AgentAuthenticationState.NotRequired &&
            winner.Candidate.EntitlementState is AgentEntitlementState.VerifiedAvailable or AgentEntitlementState.NotApplicable)
        {
            return RoutingConfidence.High;
        }

        if (winner.CapacityState == RoutingCapacityState.Constrained)
        {
            return RoutingConfidence.Medium;
        }

        if (EffectiveCapacityRequirement(input) == RoutingCapacityRequirement.NotApplicable &&
            winner.Candidate.Availability == AgentAvailability.Available &&
            winner.Candidate.ConnectionMode != AgentConnectionMode.Unknown)
        {
            return RoutingConfidence.Medium;
        }

        return RoutingConfidence.Low;
    }
}
