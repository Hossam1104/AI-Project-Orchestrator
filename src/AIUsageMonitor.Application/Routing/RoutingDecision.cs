using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIUsageMonitor.Application.Agents;

namespace AIUsageMonitor.Application.Routing;

public sealed class RoutingDecision
{
    public RoutingDecision(
        Guid projectId,
        Guid decisionId,
        int schemaVersion,
        DateTimeOffset createdAt,
        RoutingEvaluation evaluation,
        string? contentHash = null)
    {
        if (projectId == Guid.Empty || decisionId == Guid.Empty)
        {
            throw new ArgumentException("Routing decision project and decision ids are required.");
        }

        if (schemaVersion != RoutingDecisionSchema.CurrentVersion)
        {
            throw new ArgumentException("Only the current routing decision schema can be constructed.", nameof(schemaVersion));
        }

        if (createdAt == default)
        {
            throw new ArgumentException("Routing decision creation time is required.", nameof(createdAt));
        }

        ArgumentNullException.ThrowIfNull(evaluation);
        if (evaluation.Input.ProjectId != projectId)
        {
            throw new ArgumentException("Routing evaluation does not belong to the decision project.", nameof(evaluation));
        }

        if (evaluation.Assessments.Any(assessment => assessment.Candidate.ProjectId != projectId))
        {
            throw new ArgumentException("Routing assessments do not belong to the decision project.", nameof(evaluation));
        }

        if (evaluation.Assessments.Count > RoutingInputSnapshot.MaximumCandidates ||
            evaluation.Assessments.Select(static assessment => assessment.AgentId).Distinct().Count() != evaluation.Assessments.Count)
        {
            throw new ArgumentException("Routing assessments are duplicated or unbounded.", nameof(evaluation));
        }

        var inputCandidates = evaluation.Input.Candidates.ToDictionary(static candidate => candidate.AgentId);
        if (evaluation.Assessments.Any(assessment =>
            !inputCandidates.TryGetValue(assessment.AgentId, out var candidate) ||
            !SameCandidate(candidate, assessment.Candidate)))
        {
            throw new ArgumentException("Routing assessments must preserve the exact input candidate snapshots.", nameof(evaluation));
        }

        if ((evaluation.Recommendation is null) != (evaluation.SelectedAgentId is null) ||
            (evaluation.Recommendation is not null && evaluation.Recommendation.SelectedAgentId != evaluation.SelectedAgentId))
        {
            throw new ArgumentException("Routing recommendation and selected-agent identity disagree.", nameof(evaluation));
        }

        ProjectId = projectId;
        DecisionId = decisionId;
        SchemaVersion = schemaVersion;
        CreatedAt = createdAt;
        Input = evaluation.Input;
        Outcome = evaluation.Outcome;
        CandidateAssessments = evaluation.Assessments.ToArray();
        OriginalRecommendation = evaluation.OriginalRecommendation;
        Recommendation = evaluation.Recommendation;
        SelectedAgentId = evaluation.SelectedAgentId;
        OwnerOverrideDisposition = evaluation.OwnerOverrideDisposition;
        Confidence = evaluation.Confidence;
        Limitations = evaluation.Limitations.ToArray();
        ReasonCodes = evaluation.Reasons.ToArray();
        InputFingerprint = evaluation.Input.InputFingerprint;

        var calculatedHash = RoutingDecisionIntegrity.ComputeContentHash(this);
        if (contentHash is not null && !string.Equals(calculatedHash, contentHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Routing decision content hash does not match the payload.", nameof(contentHash));
        }

        ContentHash = calculatedHash;
        Reference = new RoutingDecisionReference(DecisionId, SchemaVersion, ContentHash);
    }

    private static bool SameCandidate(RoutingAgentSnapshot left, RoutingAgentSnapshot right) =>
        left.ProjectId == right.ProjectId &&
        left.AgentId == right.AgentId &&
        left.RegistryUpdatedAt == right.RegistryUpdatedAt &&
        left.Enabled == right.Enabled &&
        left.ConnectionMode == right.ConnectionMode &&
        left.Availability == right.Availability &&
        left.AuthenticationState == right.AuthenticationState &&
        left.EntitlementState == right.EntitlementState &&
        left.Identity.Id == right.Identity.Id &&
        string.Equals(left.Identity.DisplayName, right.Identity.DisplayName, StringComparison.Ordinal) &&
        string.Equals(left.Identity.Provider, right.Identity.Provider, StringComparison.Ordinal) &&
        string.Equals(left.Identity.ModelIdentifier, right.Identity.ModelIdentifier, StringComparison.Ordinal) &&
        left.RoleCapabilities.SequenceEqual(right.RoleCapabilities) &&
        left.Capabilities.SequenceEqual(right.Capabilities, StringComparer.Ordinal) &&
        left.Limitations.SequenceEqual(right.Limitations, StringComparer.Ordinal) &&
        left.SupportedConnectionModes.SequenceEqual(right.SupportedConnectionModes);

    public Guid ProjectId { get; }
    public Guid DecisionId { get; }
    public int SchemaVersion { get; }
    public DateTimeOffset CreatedAt { get; }
    public RoutingInputSnapshot Input { get; }
    public RoutingDecisionOutcome Outcome { get; }
    public IReadOnlyList<RoutingCandidateAssessment> CandidateAssessments { get; }
    public RoutingRecommendation? OriginalRecommendation { get; }
    public RoutingRecommendation? Recommendation { get; }
    public Guid? SelectedAgentId { get; }
    public RoutingOverrideDisposition OwnerOverrideDisposition { get; }
    public RoutingConfidence Confidence { get; }
    public IReadOnlyList<string> Limitations { get; }
    public IReadOnlyList<RoutingReasonCode> ReasonCodes { get; }
    public string InputFingerprint { get; }
    public string ContentHash { get; }
    public RoutingDecisionReference Reference { get; }
}

public static class RoutingDecisionIntegrity
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static string ComputeContentHash(RoutingDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        return Convert.ToHexString(SHA256.HashData(ComputeCanonicalPayloadBytes(decision))).ToLowerInvariant();
    }

    public static byte[] ComputeCanonicalPayloadBytes(RoutingDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        var json = JsonSerializer.Serialize(CreateCanonicalPayload(decision), Options);
        return Encoding.UTF8.GetBytes(json);
    }

    internal static object CreateCanonicalPayload(RoutingDecision decision) => new
    {
        decision.ProjectId,
        decision.DecisionId,
        decision.SchemaVersion,
        decision.CreatedAt,
        input = RoutingInputSnapshot.CreateCanonicalPayload(decision.Input),
        decision.Outcome,
        candidates = decision.CandidateAssessments.Select(static assessment => new
        {
            candidate = new
            {
                assessment.Candidate.ProjectId,
                assessment.Candidate.AgentId,
                identity = IdentityPayload(assessment.Candidate.Identity),
                assessment.Candidate.RegistryUpdatedAt,
                assessment.Candidate.Enabled,
                roleCapabilities = assessment.Candidate.RoleCapabilities.ToArray(),
                capabilities = assessment.Candidate.Capabilities.ToArray(),
                limitations = assessment.Candidate.Limitations.ToArray(),
                assessment.Candidate.ConnectionMode,
                supportedConnectionModes = assessment.Candidate.SupportedConnectionModes.ToArray(),
                assessment.Candidate.Availability,
                assessment.Candidate.AuthenticationState,
                assessment.Candidate.EntitlementState
            },
            assessment.CapacityState,
            assessment.PreferencePosition,
            assessment.IsHardEligible,
            assessment.IsEligible,
            missingCapabilities = assessment.MissingCapabilities.ToArray(),
            reasons = assessment.Reasons.ToArray()
        }).ToArray(),
        originalRecommendation = RecommendationPayload(decision.OriginalRecommendation),
        recommendation = RecommendationPayload(decision.Recommendation),
        decision.SelectedAgentId,
        decision.OwnerOverrideDisposition,
        decision.Confidence,
        limitations = decision.Limitations.ToArray(),
        reasonCodes = decision.ReasonCodes.ToArray(),
        decision.InputFingerprint
    };

    private static object? RecommendationPayload(RoutingRecommendation? recommendation) => recommendation is null
        ? null
        : new
        {
            recommendation.SelectedAgentId,
            identity = IdentityPayload(recommendation.SelectedAgentIdentity),
            recommendation.RequestedRole,
            reasons = recommendation.Reasons.ToArray(),
            recommendation.PreferencePosition,
            recommendation.CapacityState,
            recommendation.IndependentReviewRequired,
            recommendation.SecurityReviewRequired,
            recommendation.OwnerApprovalRequired,
            recommendation.Confidence,
            limitations = recommendation.Limitations.ToArray(),
            recommendation.NextSafeAction
        };

    private static object IdentityPayload(AgentIdentity identity) => new
    {
        identity.Id,
        identity.DisplayName,
        identity.Provider,
        identity.ModelIdentifier
    };

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
