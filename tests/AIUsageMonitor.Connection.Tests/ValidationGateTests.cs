using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Validation;

namespace AIUsageMonitor.Connection.Tests;

public sealed class ValidationGateTests
{
    [Fact]
    public void ExecutorCompletionWithoutIndependentEvidenceCannotAdvanceValidationGate()
    {
        var plan = CreatePlan(new ValidationRequirement("tests", ValidationEvidenceKind.Test, true, ValidationCoverageScope.Targeted, ValidationBaselineRelation.Standalone, "collector"));
        var adapterResult = new ExecutionAdapterResult(ExecutionAdapterOutcome.Succeeded, "All tests pass.", evidenceReferences: ["executor-self-report"]);

        var decision = ValidationGateEvaluator.Evaluate(plan, [], plan.CreatedAt.AddMinutes(1));

        Assert.Equal(ExecutionAdapterOutcome.Succeeded, adapterResult.Outcome);
        Assert.Equal(ValidationGateDecisionState.Pending, decision.State);
        Assert.Equal(ValidationRequirementDecisionState.Missing, decision.RequirementDecisions.Single().State);
    }

    [Fact]
    public void MatchingIndependentEvidenceSatisfiesRequiredGate()
    {
        var plan = CreatePlan(new ValidationRequirement("tests", ValidationEvidenceKind.Test, true, ValidationCoverageScope.Targeted, ValidationBaselineRelation.Standalone, "collector"));
        var evidence = CreateEvidence(plan, plan.Requirements[0]);

        var decision = ValidationGateEvaluator.Evaluate(plan, [evidence], evidence.CapturedAt.AddMinutes(1));

        Assert.Equal(ValidationGateDecisionState.Satisfied, decision.State);
        Assert.Equal(ValidationRequirementDecisionState.Satisfied, decision.RequirementDecisions.Single().State);
    }

    [Fact]
    public void TargetedEvidenceCannotSatisfyFullRequirement()
    {
        var requirement = new ValidationRequirement("tests", ValidationEvidenceKind.Test, true, ValidationCoverageScope.Full, ValidationBaselineRelation.Standalone, "collector");
        var plan = CreatePlan(requirement);
        var evidence = CreateEvidence(plan, requirement, coverage: ValidationCoverageScope.Targeted);

        var decision = ValidationGateEvaluator.Evaluate(plan, [evidence], evidence.CapturedAt.AddMinutes(1));

        Assert.Equal(ValidationGateDecisionState.Blocked, decision.State);
        Assert.Equal(ValidationReasonCodes.TargetedEvidenceForFullRequirement, decision.RequirementDecisions.Single().ReasonCode);
    }

    [Fact]
    public void StaleEvidenceDoesNotSatisfyGate()
    {
        var requirement = new ValidationRequirement("tests", ValidationEvidenceKind.Test, true, ValidationCoverageScope.Targeted, ValidationBaselineRelation.Standalone, "collector", maxAge: TimeSpan.FromMinutes(5));
        var plan = CreatePlan(requirement);
        var evidence = CreateEvidence(plan, requirement, capturedAt: plan.CreatedAt.AddMinutes(1));

        var decision = ValidationGateEvaluator.Evaluate(plan, [evidence], plan.CreatedAt.AddMinutes(7));

        Assert.Equal(ValidationGateDecisionState.Stale, decision.State);
        Assert.Equal(ValidationReasonCodes.EvidenceStale, decision.RequirementDecisions.Single().ReasonCode);
    }

    [Fact]
    public void RegressionRequiresMatchingBaselineEvidence()
    {
        var requirement = new ValidationRequirement("tests", ValidationEvidenceKind.Test, true, ValidationCoverageScope.Targeted, ValidationBaselineRelation.Regression, "collector");
        var plan = CreatePlan(requirement);
        var regression = CreateEvidence(plan, requirement, baselineRelation: ValidationBaselineRelation.Regression);

        var decision = ValidationGateEvaluator.Evaluate(plan, [regression], regression.CapturedAt.AddMinutes(1));

        Assert.Equal(ValidationGateDecisionState.Blocked, decision.State);
        Assert.Equal(ValidationReasonCodes.BaselineMissing, decision.RequirementDecisions.Single().ReasonCode);
    }

    [Fact]
    public void FailedEvidenceDominatesMissingAndSatisfiedRequirements()
    {
        var failingRequirement = new ValidationRequirement("build", ValidationEvidenceKind.Build, true, ValidationCoverageScope.Targeted, ValidationBaselineRelation.Standalone, "build");
        var passingRequirement = new ValidationRequirement("tests", ValidationEvidenceKind.Test, true, ValidationCoverageScope.Targeted, ValidationBaselineRelation.Standalone, "tests");
        var plan = CreatePlan(failingRequirement, passingRequirement);
        var evidence = CreateEvidence(plan, failingRequirement, outcome: ValidationOutcome.Failed);

        var decision = ValidationGateEvaluator.Evaluate(plan, [evidence], evidence.CapturedAt.AddMinutes(1));

        Assert.Equal(ValidationGateDecisionState.Failed, decision.State);
        Assert.Equal(ValidationRequirementDecisionState.Failed, decision.RequirementDecisions.Single(value => value.RequirementId == "build").State);
        Assert.Equal(ValidationRequirementDecisionState.Missing, decision.RequirementDecisions.Single(value => value.RequirementId == "tests").State);
    }

    [Fact]
    public void PartialEvidenceDoesNotSatisfyRequiredGate()
    {
        var requirement = new ValidationRequirement("tests", ValidationEvidenceKind.Test, true, ValidationCoverageScope.Targeted, ValidationBaselineRelation.Standalone, "collector");
        var plan = CreatePlan(requirement);
        var evidence = CreateEvidence(plan, requirement, state: ValidationEvidenceState.Partial, outcome: ValidationOutcome.Unknown);

        var decision = ValidationGateEvaluator.Evaluate(plan, [evidence], evidence.CapturedAt.AddMinutes(1));

        Assert.Equal(ValidationGateDecisionState.Blocked, decision.State);
        Assert.Equal(ValidationEvidenceState.Partial, decision.RequirementDecisions.Single().ObservedState);
    }

    [Fact]
    public void OptionalMissingEvidenceDoesNotBlockRequiredGate()
    {
        var optional = new ValidationRequirement("security", ValidationEvidenceKind.Security, false, ValidationCoverageScope.Targeted, ValidationBaselineRelation.Standalone, "security");
        var required = new ValidationRequirement("tests", ValidationEvidenceKind.Test, true, ValidationCoverageScope.Targeted, ValidationBaselineRelation.Standalone, "collector");
        var plan = CreatePlan(optional, required);

        var decision = ValidationGateEvaluator.Evaluate(plan, [CreateEvidence(plan, required)], plan.CreatedAt.AddMinutes(1));

        Assert.Equal(ValidationGateDecisionState.Satisfied, decision.State);
        Assert.Equal(ValidationRequirementDecisionState.NotApplicable, decision.RequirementDecisions.Single(value => value.RequirementId == "security").State);
    }

    [Fact]
    public void FullEvidenceMaySatisfyTargetedRequirementWhenExplicitlyAllowed()
    {
        var requirement = new ValidationRequirement("tests", ValidationEvidenceKind.Test, true, ValidationCoverageScope.Targeted, ValidationBaselineRelation.Standalone, "collector", allowFullEvidenceForTargeted: true);
        var plan = CreatePlan(requirement);
        var evidence = CreateEvidence(plan, requirement, coverage: ValidationCoverageScope.Full);

        var decision = ValidationGateEvaluator.Evaluate(plan, [evidence], evidence.CapturedAt.AddMinutes(1));

        Assert.Equal(ValidationGateDecisionState.Satisfied, decision.State);
    }

    [Fact]
    public void EvidenceFromAnotherRunCannotSatisfyTheCurrentPlan()
    {
        var requirement = new ValidationRequirement("tests", ValidationEvidenceKind.Test, true, ValidationCoverageScope.Targeted, ValidationBaselineRelation.Standalone, "collector");
        var plan = CreatePlan(requirement);
        var otherAuthority = new ExecutionRunAuthorityReference(Guid.NewGuid(), 1, Hash('e'));
        var evidence = CreateEvidence(plan, requirement, authorityReference: otherAuthority);

        var decision = ValidationGateEvaluator.Evaluate(plan, [evidence], evidence.CapturedAt.AddMinutes(1));

        Assert.Equal(ValidationGateDecisionState.Pending, decision.State);
        Assert.Equal(ValidationRequirementDecisionState.Missing, decision.RequirementDecisions.Single().State);
    }

    [Fact]
    public void EvidenceFromAnotherWorkspaceCannotSatisfyTheCurrentPlan()
    {
        var requirement = new ValidationRequirement("tests", ValidationEvidenceKind.Test, true, ValidationCoverageScope.Targeted, ValidationBaselineRelation.Standalone, "collector");
        var plan = CreatePlan(requirement);
        var evidence = CreateEvidence(plan, requirement, workspaceId: Guid.NewGuid(), workspacePath: Path.Combine(Path.GetTempPath(), "other-workspace"));

        var decision = ValidationGateEvaluator.Evaluate(plan, [evidence], evidence.CapturedAt.AddMinutes(1));

        Assert.Equal(ValidationGateDecisionState.Pending, decision.State);
    }

    [Fact]
    public void ValidBaselineAndRegressionEvidenceSatisfyRegressionRequirement()
    {
        var requirement = new ValidationRequirement("tests", ValidationEvidenceKind.Test, true, ValidationCoverageScope.Targeted, ValidationBaselineRelation.Regression, "collector");
        var plan = CreatePlan(requirement);
        var baseline = CreateEvidence(plan, requirement, baselineRelation: ValidationBaselineRelation.Baseline);
        var regression = CreateEvidence(plan, requirement, baselineEvidenceReference: baseline.Reference);

        var decision = ValidationGateEvaluator.Evaluate(plan, [baseline, regression], regression.CapturedAt.AddMinutes(1));

        Assert.Equal(ValidationGateDecisionState.Satisfied, decision.State);
    }

    private static ValidationPlan CreatePlan(params ValidationRequirement[] requirements)
    {
        var projectId = Guid.NewGuid();
        var graphReference = new WorkGraphReference(Guid.NewGuid(), 1, Hash('e'));
        return new(
            projectId,
            Guid.NewGuid(),
            1,
            new DateTimeOffset(2026, 8, 31, 10, 0, 0, TimeSpan.Zero),
            new ExecutionRunAuthorityReference(Guid.NewGuid(), 1, Hash('a')),
            new PlanningExecutionContractReference(Guid.NewGuid(), 1, 1, Hash('b')),
            graphReference,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Path.GetFullPath(Path.Combine(Path.GetTempPath(), "apo-validation-tests")),
            Hash('c'),
            new RecoveryCheckpointReference(Guid.NewGuid(), 1, Hash('d')),
            requirements);
    }

    private static ValidationEvidence CreateEvidence(
        ValidationPlan plan,
        ValidationRequirement requirement,
        ValidationOutcome outcome = ValidationOutcome.Passed,
        ValidationEvidenceState state = ValidationEvidenceState.Available,
        ValidationCoverageScope? coverage = null,
        ValidationBaselineRelation? baselineRelation = null,
        DateTimeOffset? capturedAt = null,
        ValidationEvidenceReference? baselineEvidenceReference = null,
        ExecutionRunAuthorityReference? authorityReference = null,
        Guid? workspaceId = null,
        string? workspacePath = null) =>
        new(
            plan.ProjectId,
            Guid.NewGuid(),
            plan.Reference,
            requirement.RequirementId,
            (authorityReference ?? plan.ExecutionRunAuthorityReference).RunId,
            authorityReference ?? plan.ExecutionRunAuthorityReference,
            plan.PlanningContractReference,
            plan.WorkGraphReference,
            plan.WorkGraphNodeId,
            plan.CurrentRecoveryCheckpointReference,
            workspaceId ?? plan.WorkspaceId,
            workspacePath ?? plan.WorkspacePath,
            plan.WorkspaceReceiptContentHash,
            requirement.CollectorIdentifier,
            requirement.EvidenceKind,
            state,
            outcome,
            coverage ?? requirement.Coverage,
            baselineRelation ?? requirement.BaselineRelation,
            capturedAt ?? plan.CreatedAt.AddMinutes(1),
            independentlyCaptured: true,
            securityBoundaryValid: true,
            baselineEvidenceReference: baselineEvidenceReference);

    private static string Hash(char value) => new(value, 64);
}
