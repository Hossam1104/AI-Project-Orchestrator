using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Routing;

namespace AIUsageMonitor.Connection.Tests;

public sealed class RoutingDecisionTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly string ContractHash = new('a', 64);
    private static readonly Guid ProjectId = Guid.Parse("90000000-0000-0000-0000-000000000001");

    [Fact]
    public void CapabilityEligibilityIsEvaluatedBeforeCapacity()
    {
        var missingCapability = Agent(capabilities: ["review"], id: 1);
        var input = Input(
            [missingCapability],
            requiredCapabilities: ["code"],
            evidence: [Evidence(missingCapability.AgentId, RoutingCapacityState.Sufficient)]);

        var evaluation = new RoutingDecisionEngine().Evaluate(input);

        var assessment = Assert.Single(evaluation.Assessments);
        Assert.False(assessment.IsHardEligible);
        Assert.False(assessment.IsEligible);
        Assert.Contains("code", assessment.MissingCapabilities);
        Assert.DoesNotContain(RoutingReasonCode.CapacityInsufficient, assessment.Reasons);
        Assert.Null(evaluation.Recommendation);
    }

    [Fact]
    public void ExplicitPreferenceBeatsRawQuotaAndDoesNotComparePercentages()
    {
        var preferred = Agent(id: 1);
        var other = Agent(id: 2);
        var evaluation = new RoutingDecisionEngine().Evaluate(Input(
            [other, preferred],
            preferred: [preferred.AgentId],
            evidence: [
                Evidence(preferred.AgentId, RoutingCapacityState.Constrained, remaining: .21),
                Evidence(other.AgentId, RoutingCapacityState.Sufficient, remaining: .99)]));

        Assert.Equal(preferred.AgentId, evaluation.SelectedAgentId);
        Assert.Equal(RoutingDecisionOutcome.Recommended, evaluation.Outcome);
        Assert.Equal(RoutingCapacityState.Constrained, evaluation.Recommendation!.CapacityState);
    }

    [Fact]
    public void InsufficientPreferredAgentFallsToAnotherEligibleCandidate()
    {
        var preferred = Agent(id: 1);
        var fallback = Agent(id: 2);
        var evaluation = new RoutingDecisionEngine().Evaluate(Input(
            [preferred, fallback],
            preferred: [preferred.AgentId],
            evidence: [
                Evidence(preferred.AgentId, RoutingCapacityState.Insufficient),
                Evidence(fallback.AgentId, RoutingCapacityState.Sufficient)]));

        Assert.Equal(fallback.AgentId, evaluation.SelectedAgentId);
        Assert.Equal(RoutingDecisionOutcome.Recommended, evaluation.Outcome);
        Assert.Contains(RoutingReasonCode.CapacityInsufficient, evaluation.Assessments.Single(value => value.AgentId == preferred.AgentId).Reasons);
    }

    [Fact]
    public void RequiredUnknownStaleAndNotMappedCapacityFailClosedButOptionalDoesNot()
    {
        var unknown = Agent(id: 1);
        var required = new RoutingDecisionEngine().Evaluate(Input([unknown], evidence: []));
        Assert.Null(required.Recommendation);
        Assert.Equal(RoutingDecisionOutcome.InsufficientEvidence, required.Outcome);

        var stale = Agent(id: 2);
        var staleEvaluation = new RoutingDecisionEngine().Evaluate(Input(
            [stale], evidence: [Evidence(stale.AgentId, RoutingCapacityState.Sufficient, validUntil: Now)]));
        Assert.Null(staleEvaluation.Recommendation);
        Assert.Equal(RoutingDecisionOutcome.StaleCapacityEvidence, staleEvaluation.Outcome);

        var optional = Agent(id: 3);
        var optionalEvaluation = new RoutingDecisionEngine().Evaluate(Input(
            [optional], capacityRequirement: RoutingCapacityRequirement.Optional, evidence: []));
        Assert.Equal(optional.AgentId, optionalEvaluation.SelectedAgentId);
        Assert.Equal(RoutingConfidence.Low, optionalEvaluation.Confidence);
        Assert.Contains(RoutingReasonCode.OptionalCapacityEvidence, optionalEvaluation.Recommendation!.Reasons);
    }

    [Fact]
    public void GatesRemainRecordedAndDoNotBecomeSatisfiedBySelection()
    {
        var candidate = Agent(id: 1);
        var evaluation = new RoutingDecisionEngine().Evaluate(Input(
            [candidate],
            independentReview: true,
            securityReview: true,
            ownerApproval: true,
            evidence: [Evidence(candidate.AgentId, RoutingCapacityState.Sufficient)]));

        Assert.NotNull(evaluation.Recommendation);
        var recommendation = evaluation.Recommendation!;
        Assert.True(recommendation.IndependentReviewRequired);
        Assert.True(recommendation.SecurityReviewRequired);
        Assert.True(recommendation.OwnerApprovalRequired);
        Assert.Contains(RoutingReasonCode.ReviewGateRequired, recommendation.Reasons);
        Assert.Contains(RoutingReasonCode.SecurityGateRequired, recommendation.Reasons);
        Assert.Contains(RoutingReasonCode.OwnerApprovalRequired, recommendation.Reasons);
        Assert.Contains("Await", recommendation.NextSafeAction, StringComparison.Ordinal);
    }

    [Fact]
    public void EligibleOwnerOverrideChangesSoftSelectionAndRejectedOverrideHasNoFallback()
    {
        var first = Agent(id: 1);
        var second = Agent(id: 2);
        var applied = new RoutingDecisionEngine().Evaluate(Input(
            [first, second],
            preferred: [first.AgentId],
            ownerOverride: new RoutingOwnerOverrideRequest(second.AgentId, "owner:1", "Use the second eligible agent.", Now),
            evidence: [Evidence(first.AgentId, RoutingCapacityState.Sufficient), Evidence(second.AgentId, RoutingCapacityState.Sufficient)]));
        Assert.Equal(RoutingDecisionOutcome.OwnerOverrideApplied, applied.Outcome);
        Assert.Equal(second.AgentId, applied.SelectedAgentId);
        Assert.Equal(first.AgentId, applied.OriginalSelectedAgentId);

        var rejected = new RoutingDecisionEngine().Evaluate(Input(
            [first, second],
            preferred: [first.AgentId],
            ownerOverride: new RoutingOwnerOverrideRequest(second.AgentId, "owner:1", "Cannot bypass capacity.", Now),
            evidence: [Evidence(first.AgentId, RoutingCapacityState.Sufficient), Evidence(second.AgentId, RoutingCapacityState.Insufficient)]));
        Assert.Equal(RoutingDecisionOutcome.OwnerOverrideRejected, rejected.Outcome);
        Assert.Null(rejected.SelectedAgentId);
        Assert.NotNull(rejected.OriginalRecommendation);
        Assert.Equal(RoutingOverrideDisposition.Rejected, rejected.OwnerOverrideDisposition);
    }

    [Fact]
    public void OperationalTruthStatesAreHardEligibilityConstraintsWhenRequired()
    {
        var disabled = Agent(id: 1, enabled: false);
        var unsupported = Agent(id: 2, connection: AgentConnectionMode.Unsupported, availability: AgentAvailability.Unsupported);
        var unauthenticated = Agent(id: 3, authentication: AgentAuthenticationState.AuthenticationRequired);
        var unentitled = Agent(id: 4, entitlement: AgentEntitlementState.VerifiedUnavailable);
        var unavailable = Agent(id: 5, availability: AgentAvailability.Unavailable);
        var input = Input(
            [disabled, unsupported, unauthenticated, unentitled, unavailable],
            requireAuth: true,
            requireEntitlement: true,
            requireAvailability: true,
            evidence: [
                Evidence(disabled.AgentId, RoutingCapacityState.Sufficient),
                Evidence(unsupported.AgentId, RoutingCapacityState.Sufficient),
                Evidence(unauthenticated.AgentId, RoutingCapacityState.Sufficient),
                Evidence(unentitled.AgentId, RoutingCapacityState.Sufficient),
                Evidence(unavailable.AgentId, RoutingCapacityState.Sufficient)]);

        var evaluation = new RoutingDecisionEngine().Evaluate(input);

        Assert.All(evaluation.Assessments, assessment => Assert.False(assessment.IsHardEligible));
        Assert.Contains(RoutingReasonCode.Disabled, evaluation.Assessments.Single(value => value.AgentId == disabled.AgentId).Reasons);
        Assert.Contains(RoutingReasonCode.ConnectionUnsupported, evaluation.Assessments.Single(value => value.AgentId == unsupported.AgentId).Reasons);
        Assert.Contains(RoutingReasonCode.AuthenticationUnavailable, evaluation.Assessments.Single(value => value.AgentId == unauthenticated.AgentId).Reasons);
        Assert.Contains(RoutingReasonCode.EntitlementUnavailable, evaluation.Assessments.Single(value => value.AgentId == unentitled.AgentId).Reasons);
        Assert.Contains(RoutingReasonCode.AvailabilityUnavailable, evaluation.Assessments.Single(value => value.AgentId == unavailable.AgentId).Reasons);
    }

    [Fact]
    public void ProjectOverrideEffectiveSnapshotAndInputOrderingAreReproducible()
    {
        var first = Agent(id: 1);
        var second = Agent(id: 2);
        var a = Input([first, second], evidence: [Evidence(first.AgentId, RoutingCapacityState.Sufficient), Evidence(second.AgentId, RoutingCapacityState.Sufficient)]);
        var b = Input([second, first], evidence: [Evidence(second.AgentId, RoutingCapacityState.Sufficient), Evidence(first.AgentId, RoutingCapacityState.Sufficient)]);

        Assert.Equal(a.InputFingerprint, b.InputFingerprint);
        var engine = new RoutingDecisionEngine();
        Assert.Equal(engine.Evaluate(a).SelectedAgentId, engine.Evaluate(b).SelectedAgentId);
        Assert.Equal(engine.Evaluate(a).Reasons, engine.Evaluate(b).Reasons);
    }

    private static RoutingInputSnapshot Input(
        IReadOnlyList<RoutingAgentSnapshot> candidates,
        IReadOnlyList<Guid>? preferred = null,
        IReadOnlyList<string>? requiredCapabilities = null,
        RoutingCapacityRequirement capacityRequirement = RoutingCapacityRequirement.Required,
        bool independentReview = false,
        bool securityReview = false,
        bool ownerApproval = false,
        bool requireAvailability = false,
        bool requireAuth = false,
        bool requireEntitlement = false,
        RoutingOwnerOverrideRequest? ownerOverride = null,
        IReadOnlyList<RoutingCapacityEvidence>? evidence = null)
    {
        var projectId = ProjectId;
        var classification = new RoutingTaskClassification(
            RoutingScopeScale.Bounded,
            RoutingTaskRisk.Moderate,
            RoutingBlastRadius.Local,
            RoutingValidationCost.Low,
            AgentRole.Executor,
            requiredCapabilities,
            ["quality-first"],
            capacityRequirement,
            independentReview,
            securityReview,
            ownerApproval,
            requiresVerifiedAvailability: requireAvailability,
            requiresAuthenticatedAccess: requireAuth,
            requiresVerifiedEntitlement: requireEntitlement);
        var policy = new RoutingPolicySnapshot(
            "policy:test",
            AgentRole.Executor,
            preferred,
            capacityRequirement: capacityRequirement,
            independentReviewRequired: independentReview,
            securityReviewRequired: securityReview,
            ownerApprovalRequired: ownerApproval,
            requireVerifiedAvailability: requireAvailability,
            requireAuthenticatedAccess: requireAuth,
            requireVerifiedEntitlement: requireEntitlement);
        return new(
            projectId,
            new PlanningExecutionContractReference(Guid.Parse("10000000-0000-0000-0000-000000000001"), 1, 1, ContractHash),
            new RoutingContextReference(Guid.Parse("20000000-0000-0000-0000-000000000001"), 1, Now),
            classification,
            policy,
            candidates,
            evidence,
            ownerOverride,
            Now);
    }

    private static RoutingAgentSnapshot Agent(
        int id,
        bool enabled = true,
        AgentConnectionMode connection = AgentConnectionMode.Api,
        AgentAvailability availability = AgentAvailability.Available,
        AgentAuthenticationState authentication = AgentAuthenticationState.Authenticated,
        AgentEntitlementState entitlement = AgentEntitlementState.VerifiedAvailable,
        IReadOnlyList<string>? capabilities = null) => new(
        ProjectId,
        Guid.Parse($"30000000-0000-0000-0000-{id:D12}"),
        new AgentIdentity(Guid.Parse($"30000000-0000-0000-0000-{id:D12}"), $"Agent {id}", "provider", $"model-{id}"),
        Now,
        enabled,
        [AgentRole.Executor],
        capabilities ?? ["code"],
        [],
        connection,
        connection is AgentConnectionMode.Unsupported ? [AgentConnectionMode.Unsupported] : [connection],
        availability,
        authentication,
        entitlement);

    private static RoutingCapacityEvidence Evidence(
        Guid agentId,
        RoutingCapacityState state,
        double? remaining = null,
        DateTimeOffset? validUntil = null) => new(
        agentId,
        state,
        Now.AddMinutes(-1),
        validUntil ?? Now.AddHours(1),
        $"fixture:{agentId:D}",
        remainingFraction: remaining,
        source: RoutingCapacityEvidenceSource.Manual);
}
