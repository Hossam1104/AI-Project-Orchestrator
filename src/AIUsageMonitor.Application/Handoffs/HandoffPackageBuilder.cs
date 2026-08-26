using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Orchestration;

namespace AIUsageMonitor.Application.Handoffs;

internal enum HandoffPackageBuildStatus
{
    Created,
    RequiredContextMissing,
    PackageTooLarge,
    RedactionRejected
}

internal sealed record HandoffPackageBuildResult(
    HandoffPackageBuildStatus Status,
    HandoffPackage? Package = null,
    string? ErrorMessage = null);

/// <summary>
/// Pure role-inclusion and normalization policy. It derives every canonical authority field from
/// the resolved APO-40 contract and never resolves external providers or repository contents.
/// </summary>
internal sealed class HandoffPackageBuilder
{
    private readonly IHandoffRedactionService _redaction;

    public HandoffPackageBuilder(IHandoffRedactionService redaction)
    {
        _redaction = redaction ?? throw new ArgumentNullException(nameof(redaction));
    }

    public HandoffPackageBuildResult Build(
        HandoffPackageCreationRequest request,
        PlanningExecutionContract contract,
        HandoffRole sourceRole,
        HandoffRole targetRole,
        HandoffPackageReference? previousPackageReference,
        WorkGraphReference? workGraphReference,
        Guid? workGraphNodeId)
    {
        var accumulator = new RedactionAccumulator(_redaction);
        try
        {
            var workItem = new PlanningWorkItem(
                contract.WorkItem.Source,
                accumulator.Redact(contract.WorkItem.Reference),
                accumulator.Redact(contract.WorkItem.Title));
            var repositoryTarget = new PlanningRepositoryTarget(
                contract.RepositoryTarget.Mode,
                accumulator.RedactOptional(contract.RepositoryTarget.RegisteredLocalPath),
                accumulator.RedactOptional(contract.RepositoryTarget.ExpectedBranch),
                contract.RepositoryTarget.ExpectedHeadCommit);
            var context = new HandoffContextReference(
                contract.Context.ProjectContextId,
                contract.Context.ProjectContextContractVersion);

            HandoffExecutionScope? executionScope = null;
            HandoffReviewScope? reviewScope = null;
            HandoffRemediationScope? remediationScope = null;
            HandoffAcceptanceScope? acceptanceScope = null;

            switch (request.Transition)
            {
                case HandoffTransition.PlannerToExecutor:
                    executionScope = BuildExecutionScope(contract, accumulator);
                    break;
                case HandoffTransition.ExecutorToReviewer:
                case HandoffTransition.RemediationToReviewer:
                    reviewScope = BuildReviewScope(contract, accumulator);
                    break;
                case HandoffTransition.ReviewerToRemediation:
                    remediationScope = BuildRemediationScope(contract, accumulator);
                    break;
                case HandoffTransition.ReviewerToAcceptance:
                    acceptanceScope = new HandoffAcceptanceScope(
                        contract.AcceptanceCriteria.Select(value => new PlanningAcceptanceCriterion(
                            value.CriterionId,
                            accumulator.Redact(value.Statement),
                            value.Required)).ToArray());
                    break;
                case HandoffTransition.AcceptanceToPlanner:
                    break;
                default:
                    return new(HandoffPackageBuildStatus.RequiredContextMissing, ErrorMessage: "The handoff transition is unsupported.");
            }

            var evidence = IncludeEvidence(request, accumulator);
            var findings = IncludeFindings(request, accumulator);
            var changedArtifacts = IncludeChangedArtifacts(request, accumulator);
            var outcome = IncludeOutcome(request, accumulator);

            if (request.Transition is
                HandoffTransition.ExecutorToReviewer or
                HandoffTransition.ReviewerToRemediation or
                HandoffTransition.RemediationToReviewer or
                HandoffTransition.ReviewerToAcceptance)
            {
                if (evidence.Count == 0)
                {
                    return new(
                        HandoffPackageBuildStatus.RequiredContextMissing,
                        ErrorMessage: "This handoff transition requires at least one evidence reference.");
                }
            }

            if (request.Transition is HandoffTransition.ExecutorToReviewer or HandoffTransition.RemediationToReviewer)
            {
                if (changedArtifacts.Count == 0)
                {
                    return new(
                        HandoffPackageBuildStatus.RequiredContextMissing,
                        ErrorMessage: "This handoff transition requires at least one changed-artifact reference.");
                }
            }

            if (request.Transition is HandoffTransition.ReviewerToRemediation or HandoffTransition.RemediationToReviewer)
            {
                if (findings.Count == 0)
                {
                    return new(
                        HandoffPackageBuildStatus.RequiredContextMissing,
                        ErrorMessage: "This handoff transition requires relevant finding references.");
                }
            }

            if (request.Transition != HandoffTransition.PlannerToExecutor && outcome is null)
            {
                return new(
                    HandoffPackageBuildStatus.RequiredContextMissing,
                    ErrorMessage: "This handoff transition requires bounded outcome metadata.");
            }

            var limitations = request.Transition == HandoffTransition.PlannerToExecutor
                ? Array.Empty<string>()
                : request.Limitations.Select(accumulator.Redact).ToArray();
            var nextAction = accumulator.Redact(request.NextAction ?? string.Empty);

            if (string.IsNullOrWhiteSpace(nextAction))
            {
                return new(HandoffPackageBuildStatus.RequiredContextMissing, ErrorMessage: "A bounded next action is required.");
            }

            var size = new HandoffPackageSizeMetadata(
                HandoffPackageLimits.MaxCanonicalPayloadBytes,
                canonicalPayloadBytes: 0,
                evidence.Count,
                findings.Count,
                changedArtifacts.Count,
                limitations.Length,
                CountScopeItems(executionScope, reviewScope, remediationScope, acceptanceScope));

            var provisional = new HandoffPackage(
                request.ProjectId,
                request.PackageId,
                HandoffPackageSchema.CurrentVersion,
                request.CreatedAt,
                request.Transition,
                sourceRole,
                targetRole,
                contract.Reference,
                workItem,
                context,
                repositoryTarget,
                workGraphReference,
                workGraphNodeId,
                previousPackageReference,
                executionScope,
                reviewScope,
                remediationScope,
                acceptanceScope,
                evidence,
                findings,
                changedArtifacts,
                outcome,
                limitations,
                nextAction,
                new HandoffRedactionMetadata(accumulator.Count > 0, accumulator.Count, accumulator.Categories.ToArray()),
                size);

            var canonicalPayloadBytes = HandoffPackageIntegrity.ComputeCanonicalPayloadBytes(provisional);
            if (canonicalPayloadBytes > HandoffPackageLimits.MaxCanonicalPayloadBytes)
            {
                return new(
                    HandoffPackageBuildStatus.PackageTooLarge,
                    ErrorMessage: "The canonical handoff package exceeds its size budget.");
            }

            var finalSize = new HandoffPackageSizeMetadata(
                HandoffPackageLimits.MaxCanonicalPayloadBytes,
                canonicalPayloadBytes,
                evidence.Count,
                findings.Count,
                changedArtifacts.Count,
                limitations.Length,
                CountScopeItems(executionScope, reviewScope, remediationScope, acceptanceScope));

            var package = new HandoffPackage(
                request.ProjectId,
                request.PackageId,
                HandoffPackageSchema.CurrentVersion,
                request.CreatedAt,
                request.Transition,
                sourceRole,
                targetRole,
                contract.Reference,
                workItem,
                context,
                repositoryTarget,
                workGraphReference,
                workGraphNodeId,
                previousPackageReference,
                executionScope,
                reviewScope,
                remediationScope,
                acceptanceScope,
                evidence,
                findings,
                changedArtifacts,
                outcome,
                limitations,
                nextAction,
                new HandoffRedactionMetadata(accumulator.Count > 0, accumulator.Count, accumulator.Categories.ToArray()),
                finalSize);

            return new(HandoffPackageBuildStatus.Created, package);
        }
        catch (HandoffRedactionRejectedException)
        {
            return new(
                HandoffPackageBuildStatus.RedactionRejected,
                ErrorMessage: "A package text value was rejected by the redaction policy.");
        }
        catch (ArgumentException)
        {
            return new(
                HandoffPackageBuildStatus.RequiredContextMissing,
                ErrorMessage: "Required handoff context is invalid or exceeds its bounds.");
        }
    }

    private static HandoffExecutionScope BuildExecutionScope(
        PlanningExecutionContract contract,
        RedactionAccumulator accumulator) => new(
        contract.IncludedScope.Select(value => new PlanningScopeClause(value.Id, accumulator.Redact(value.Statement))).ToArray(),
        contract.Constraints.Select(value => new PlanningScopeClause(value.Id, accumulator.Redact(value.Statement))).ToArray(),
        contract.ForbiddenScope.Select(value => new PlanningScopeClause(value.Id, accumulator.Redact(value.Statement))).ToArray(),
        contract.Deliverables.Select(value => new PlanningDeliverable(value.DeliverableId, accumulator.Redact(value.Description), value.Required)).ToArray(),
        contract.ValidationRequirements.Select(value => new PlanningValidationRequirement(
            value.ValidationId,
            value.Kind,
            accumulator.Redact(value.Description),
            value.Required,
            accumulator.RedactOptional(value.CommandOrReference))).ToArray(),
        contract.ExecutionBudgets.ToArray(),
        contract.StopConditions.Select(value => new PlanningStopCondition(
            value.ConditionId,
            value.Kind,
            accumulator.Redact(value.Description))).ToArray(),
        contract.GovernanceReferences.Select(accumulator.Redact).ToArray(),
        accumulator.RedactOptional(contract.RoutingPolicyReference),
        accumulator.RedactOptional(contract.SafetyPolicyReference));

    private static HandoffReviewScope BuildReviewScope(
        PlanningExecutionContract contract,
        RedactionAccumulator accumulator) => new(
        contract.IncludedScope.Select(value => new PlanningScopeClause(value.Id, accumulator.Redact(value.Statement))).ToArray(),
        contract.Constraints.Select(value => new PlanningScopeClause(value.Id, accumulator.Redact(value.Statement))).ToArray(),
        contract.ForbiddenScope.Select(value => new PlanningScopeClause(value.Id, accumulator.Redact(value.Statement))).ToArray(),
        contract.AcceptanceCriteria.Select(value => new PlanningAcceptanceCriterion(
            value.CriterionId,
            accumulator.Redact(value.Statement),
            value.Required)).ToArray());

    private static HandoffRemediationScope BuildRemediationScope(
        PlanningExecutionContract contract,
        RedactionAccumulator accumulator) => new(
        contract.IncludedScope.Select(value => new PlanningScopeClause(value.Id, accumulator.Redact(value.Statement))).ToArray(),
        contract.Constraints.Select(value => new PlanningScopeClause(value.Id, accumulator.Redact(value.Statement))).ToArray(),
        contract.ForbiddenScope.Select(value => new PlanningScopeClause(value.Id, accumulator.Redact(value.Statement))).ToArray(),
        contract.StopConditions.Select(value => new PlanningStopCondition(
            value.ConditionId,
            value.Kind,
            accumulator.Redact(value.Description))).ToArray());

    private static IReadOnlyList<HandoffEvidenceReference> IncludeEvidence(
        HandoffPackageCreationRequest request,
        RedactionAccumulator accumulator)
    {
        var include = request.Transition is not HandoffTransition.PlannerToExecutor and
            not HandoffTransition.AcceptanceToPlanner;
        return include
            ? request.EvidenceReferences.Select(value => new HandoffEvidenceReference(
                value.EvidenceId,
                value.Kind,
                accumulator.Redact(value.Reference),
                value.CapturedAt,
                value.Freshness,
                value.ContentHash)).ToArray()
            : Array.Empty<HandoffEvidenceReference>();
    }

    private static IReadOnlyList<HandoffFindingReference> IncludeFindings(
        HandoffPackageCreationRequest request,
        RedactionAccumulator accumulator)
    {
        var include = request.Transition is
            HandoffTransition.ReviewerToRemediation or
            HandoffTransition.RemediationToReviewer or
            HandoffTransition.ReviewerToAcceptance or
            HandoffTransition.AcceptanceToPlanner;
        if (!include)
        {
            return Array.Empty<HandoffFindingReference>();
        }

        var includeAddressed = request.Transition == HandoffTransition.RemediationToReviewer;
        var values = request.FindingReferences
            .Where(finding => finding.IsUnresolved || (includeAddressed && finding.State == HandoffFindingState.Addressed))
            .Select(value => new HandoffFindingReference(
                value.FindingId,
                value.Category,
                value.Severity,
                value.State,
                accumulator.RedactOptional(value.Summary),
                accumulator.RedactOptional(value.SourceReference),
                value.EvidenceIds))
            .ToArray();
        return values;
    }

    private static IReadOnlyList<HandoffChangedArtifactReference> IncludeChangedArtifacts(
        HandoffPackageCreationRequest request,
        RedactionAccumulator accumulator)
    {
        var include = request.Transition is
            HandoffTransition.ExecutorToReviewer or
            HandoffTransition.RemediationToReviewer or
            HandoffTransition.ReviewerToAcceptance;
        return include
            ? request.ChangedArtifactReferences.Select(value => new HandoffChangedArtifactReference(
                accumulator.RedactOptional(value.RepositoryRelativePath),
                value.CommitSha,
                accumulator.RedactOptional(value.ExternalReference))).ToArray()
            : Array.Empty<HandoffChangedArtifactReference>();
    }

    private static HandoffOutcomeMetadata? IncludeOutcome(
        HandoffPackageCreationRequest request,
        RedactionAccumulator accumulator) =>
        request.Transition == HandoffTransition.PlannerToExecutor || request.Outcome is null
            ? null
            : new HandoffOutcomeMetadata(
                request.Outcome.State,
                accumulator.RedactOptional(request.Outcome.Summary),
                accumulator.RedactOptional(request.Outcome.ResultReference));

    private static int CountScopeItems(
        HandoffExecutionScope? execution,
        HandoffReviewScope? review,
        HandoffRemediationScope? remediation,
        HandoffAcceptanceScope? acceptance) =>
        execution is not null
            ? execution.IncludedScope.Count + execution.Constraints.Count + execution.ForbiddenScope.Count +
              execution.Deliverables.Count + execution.ValidationRequirements.Count + execution.ExecutionBudgets.Count +
              execution.StopConditions.Count + execution.GovernanceReferences.Count
            : review is not null
                ? review.IncludedScope.Count + review.Constraints.Count + review.ForbiddenScope.Count + review.AcceptanceCriteria.Count
                : remediation is not null
                    ? remediation.IncludedScope.Count + remediation.Constraints.Count + remediation.ForbiddenScope.Count + remediation.StopConditions.Count
                    : acceptance?.AcceptanceCriteria.Count ?? 0;

    private sealed class RedactionAccumulator
    {
        private readonly IHandoffRedactionService _service;
        private readonly HashSet<HandoffRedactionCategory> _categories = [];

        public RedactionAccumulator(IHandoffRedactionService service) => _service = service;

        public int Count { get; private set; }

        public IReadOnlyCollection<HandoffRedactionCategory> Categories => _categories;

        public string Redact(string value)
        {
            try
            {
                var result = _service.Redact(value);
                Count += result.Count;
                foreach (var category in result.Categories)
                {
                    _categories.Add(category);
                }

                return result.Value;
            }
            catch (ArgumentException)
            {
                throw new HandoffRedactionRejectedException();
            }
        }

        public string? RedactOptional(string? value) => value is null ? null : Redact(value);
    }

    private sealed class HandoffRedactionRejectedException : Exception
    {
    }
}
