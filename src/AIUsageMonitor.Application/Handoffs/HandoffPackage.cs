using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Planning;

namespace AIUsageMonitor.Application.Handoffs;

/// <summary>Semantic version of the immutable APO-42 handoff package authority.</summary>
public static class HandoffPackageSchema
{
    public const int CurrentVersion = 1;
}

/// <summary>Conservative bounds for one local handoff package.</summary>
public static class HandoffPackageLimits
{
    public const int MaxCanonicalPayloadBytes = 128 * 1024;
    public const int MaxEvidenceReferences = 128;
    public const int MaxFindingReferences = 64;
    public const int MaxChangedArtifactReferences = 128;
    public const int MaxLimitations = 64;
    public const int MaxScopeItemsPerSection = 128;
    public const int MaxTextLength = 4_000;
    public const int MaxReferenceLength = 1_000;
    public const int MaxFindingEvidenceIds = 32;
}

public enum HandoffRole
{
    Planner,
    Executor,
    Reviewer,
    Remediation,
    Acceptance
}

public enum HandoffTransition
{
    PlannerToExecutor,
    ExecutorToReviewer,
    ReviewerToRemediation,
    RemediationToReviewer,
    ReviewerToAcceptance,
    AcceptanceToPlanner
}

public enum HandoffEvidenceKind
{
    Build,
    Test,
    StaticCheck,
    SecurityCheck,
    Repository,
    Tracker,
    Review,
    Delivery,
    Other
}

public enum HandoffEvidenceFreshness
{
    PointInTime,
    Stale,
    Unknown,
    NotApplicable
}

public enum HandoffFindingCategory
{
    Requirements,
    Architecture,
    Correctness,
    Security,
    Reliability,
    Compatibility,
    Validation,
    Scope,
    Other
}

public enum HandoffFindingSeverity
{
    Blocker,
    High,
    Medium,
    Low
}

/// <summary>Finding state is review truth; Addressed is only a remediation claim, not closure.</summary>
public enum HandoffFindingState
{
    Open,
    Unresolved,
    Addressed,
    Accepted,
    Rejected,
    Waived
}

public enum HandoffOutcomeState
{
    Succeeded,
    Failed,
    Blocked,
    Passed,
    ChangesRequired,
    Accepted,
    Rejected,
    InsufficientEvidence,
    NotApplicable
}

public enum HandoffRedactionCategory
{
    PasswordAssignment,
    ApiKeyAssignment,
    BearerToken,
    PersonalAccessToken,
    ConnectionStringPassword,
    AuthorizationHeader
}

/// <summary>Defines the finite supported lifecycle transitions and their roles.</summary>
public static class HandoffTransitionPolicy
{
    public static bool TryGetRoles(
        HandoffTransition transition,
        out HandoffRole sourceRole,
        out HandoffRole targetRole)
    {
        (sourceRole, targetRole) = transition switch
        {
            HandoffTransition.PlannerToExecutor => (HandoffRole.Planner, HandoffRole.Executor),
            HandoffTransition.ExecutorToReviewer => (HandoffRole.Executor, HandoffRole.Reviewer),
            HandoffTransition.ReviewerToRemediation => (HandoffRole.Reviewer, HandoffRole.Remediation),
            HandoffTransition.RemediationToReviewer => (HandoffRole.Remediation, HandoffRole.Reviewer),
            HandoffTransition.ReviewerToAcceptance => (HandoffRole.Reviewer, HandoffRole.Acceptance),
            HandoffTransition.AcceptanceToPlanner => (HandoffRole.Acceptance, HandoffRole.Planner),
            _ => default
        };

        return Enum.IsDefined(transition);
    }

    public static bool RequiresPredecessor(HandoffTransition transition) =>
        transition != HandoffTransition.PlannerToExecutor;

    public static bool IsAllowedPredecessor(
        HandoffTransition previous,
        HandoffTransition current) =>
        (previous, current) switch
        {
            (HandoffTransition.PlannerToExecutor, HandoffTransition.ExecutorToReviewer) => true,
            (HandoffTransition.ExecutorToReviewer, HandoffTransition.ReviewerToRemediation) => true,
            (HandoffTransition.ExecutorToReviewer, HandoffTransition.ReviewerToAcceptance) => true,
            (HandoffTransition.ReviewerToRemediation, HandoffTransition.RemediationToReviewer) => true,
            (HandoffTransition.RemediationToReviewer, HandoffTransition.ReviewerToRemediation) => true,
            (HandoffTransition.RemediationToReviewer, HandoffTransition.ReviewerToAcceptance) => true,
            (HandoffTransition.ReviewerToAcceptance, HandoffTransition.AcceptanceToPlanner) => true,
            _ => false
        };
}

/// <summary>Content-integrity reference for one immutable handoff package.</summary>
public sealed class HandoffPackageReference
{
    public HandoffPackageReference(Guid packageId, int schemaVersion, string contentHash)
    {
        if (packageId == Guid.Empty)
        {
            throw new ArgumentException("Package id cannot be empty.", nameof(packageId));
        }

        if (schemaVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        }

        if (!IsSha256(contentHash))
        {
            throw new ArgumentException("Content hash must be a SHA-256 hexadecimal value.", nameof(contentHash));
        }

        PackageId = packageId;
        SchemaVersion = schemaVersion;
        ContentHash = contentHash.ToLowerInvariant();
    }

    public Guid PackageId { get; }

    public int SchemaVersion { get; }

    /// <summary>SHA-256 content-integrity evidence, not a signature or authenticity proof.</summary>
    public string ContentHash { get; }

    public override string ToString() =>
        $"handoff:{PackageId:D}/schema:{SchemaVersion}/sha256:{ContentHash}";

    public static bool IsSha256(string? value) =>
        value is not null &&
        value.Length == 64 &&
        value.All(static character => Uri.IsHexDigit(character));
}

/// <summary>Point-in-time context identity carried from the exact planning contract binding.</summary>
public sealed class HandoffContextReference
{
    public HandoffContextReference(
        Guid contextId,
        int contextContractVersion,
        DateTimeOffset? capturedAt = null,
        DateTimeOffset? updatedAt = null)
    {
        if (contextId == Guid.Empty)
        {
            throw new ArgumentException("Context id cannot be empty.", nameof(contextId));
        }

        if (contextContractVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(contextContractVersion));
        }

        if (capturedAt is null && updatedAt is not null)
        {
            throw new ArgumentException("An updated context time requires a captured time.", nameof(updatedAt));
        }

        if (capturedAt is not null && updatedAt is not null && updatedAt < capturedAt)
        {
            throw new ArgumentException("Context updated time cannot precede captured time.", nameof(updatedAt));
        }

        ContextId = contextId;
        ContextContractVersion = contextContractVersion;
        CapturedAt = capturedAt;
        UpdatedAt = updatedAt;
    }

    public Guid ContextId { get; }

    public int ContextContractVersion { get; }

    public DateTimeOffset? CapturedAt { get; }

    public DateTimeOffset? UpdatedAt { get; }
}

/// <summary>Bounded reference to independently captured evidence; no payload is embedded.</summary>
public sealed class HandoffEvidenceReference
{
    public HandoffEvidenceReference(
        Guid evidenceId,
        HandoffEvidenceKind kind,
        string reference,
        DateTimeOffset? capturedAt = null,
        HandoffEvidenceFreshness freshness = HandoffEvidenceFreshness.Unknown,
        string? contentHash = null)
    {
        if (evidenceId == Guid.Empty)
        {
            throw new ArgumentException("Evidence id cannot be empty.", nameof(evidenceId));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentException("Evidence kind is undefined.", nameof(kind));
        }

        if (!Enum.IsDefined(freshness))
        {
            throw new ArgumentException("Evidence freshness is undefined.", nameof(freshness));
        }

        EvidenceId = evidenceId;
        Kind = kind;
        Reference = RequiredText(reference, nameof(reference), HandoffPackageLimits.MaxReferenceLength);
        CapturedAt = capturedAt;
        Freshness = freshness;
        if (contentHash is not null && !HandoffPackageReference.IsSha256(contentHash))
        {
            throw new ArgumentException("Evidence content hash must be a SHA-256 hexadecimal value.", nameof(contentHash));
        }

        ContentHash = contentHash?.ToLowerInvariant();
    }

    public Guid EvidenceId { get; }

    public HandoffEvidenceKind Kind { get; }

    public string Reference { get; }

    public DateTimeOffset? CapturedAt { get; }

    public HandoffEvidenceFreshness Freshness { get; }

    public string? ContentHash { get; }

    private static string RequiredText(string value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A bounded text value is required.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException($"The value cannot exceed {maximumLength} characters.", parameterName);
        }

        return normalized;
    }
}

/// <summary>Bounded finding metadata used to carry review traceability without a transcript.</summary>
public sealed class HandoffFindingReference
{
    public HandoffFindingReference(
        string findingId,
        HandoffFindingCategory category,
        HandoffFindingSeverity severity,
        HandoffFindingState state,
        string? summary = null,
        string? sourceReference = null,
        IReadOnlyList<Guid>? evidenceIds = null)
    {
        if (!Enum.IsDefined(category))
        {
            throw new ArgumentException("Finding category is undefined.", nameof(category));
        }

        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentException("Finding severity is undefined.", nameof(severity));
        }

        if (!Enum.IsDefined(state))
        {
            throw new ArgumentException("Finding state is undefined.", nameof(state));
        }

        FindingId = RequiredText(findingId, nameof(findingId), 120);
        Category = category;
        Severity = severity;
        State = state;
        Summary = OptionalText(summary, nameof(summary), HandoffPackageLimits.MaxTextLength);
        SourceReference = OptionalText(sourceReference, nameof(sourceReference), HandoffPackageLimits.MaxReferenceLength);

        var values = evidenceIds ?? Array.Empty<Guid>();
        if (values.Count > HandoffPackageLimits.MaxFindingEvidenceIds)
        {
            throw new ArgumentException("A finding contains too many evidence references.", nameof(evidenceIds));
        }

        var normalized = new List<Guid>(values.Count);
        foreach (var evidenceId in values)
        {
            if (evidenceId == Guid.Empty)
            {
                throw new ArgumentException("Finding evidence ids cannot be empty.", nameof(evidenceIds));
            }

            if (!normalized.Contains(evidenceId))
            {
                normalized.Add(evidenceId);
            }
        }

        normalized.Sort();
        EvidenceIds = normalized.AsReadOnly();
    }

    public string FindingId { get; }

    public HandoffFindingCategory Category { get; }

    public HandoffFindingSeverity Severity { get; }

    public HandoffFindingState State { get; }

    public string? Summary { get; }

    public string? SourceReference { get; }

    public IReadOnlyList<Guid> EvidenceIds { get; }

    public bool IsUnresolved => State is HandoffFindingState.Open or HandoffFindingState.Unresolved;

    private static string RequiredText(string value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A bounded text value is required.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException($"The value cannot exceed {maximumLength} characters.", parameterName);
        }

        return normalized;
    }

    private static string? OptionalText(string? value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException($"The value cannot exceed {maximumLength} characters.", parameterName);
        }

        return normalized;
    }
}

/// <summary>Reference-only changed-artifact identity. It never contains file contents or diffs.</summary>
public sealed class HandoffChangedArtifactReference
{
    public HandoffChangedArtifactReference(
        string? repositoryRelativePath = null,
        string? commitSha = null,
        string? externalReference = null)
    {
        RepositoryRelativePath = NormalizeRelativePath(repositoryRelativePath);
        CommitSha = NormalizeCommitSha(commitSha);
        ExternalReference = OptionalText(externalReference, nameof(externalReference), HandoffPackageLimits.MaxReferenceLength);

        if (RepositoryRelativePath is null && CommitSha is null && ExternalReference is null)
        {
            throw new ArgumentException("A changed artifact needs a path, commit, or external reference.");
        }
    }

    public string? RepositoryRelativePath { get; }

    public string? CommitSha { get; }

    public string? ExternalReference { get; }

    private static string? NormalizeRelativePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().Replace('\\', '/');
        if (normalized.StartsWith('/') || normalized.Contains(':') || Path.IsPathRooted(value))
        {
            throw new ArgumentException("Changed artifact paths must be repository-relative.", nameof(value));
        }

        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(static segment => segment is ".." or "."))
        {
            throw new ArgumentException("Changed artifact paths cannot contain traversal segments.", nameof(value));
        }

        var result = string.Join('/', segments);
        return result.Length <= HandoffPackageLimits.MaxReferenceLength
            ? result
            : throw new ArgumentException(
                $"The value cannot exceed {HandoffPackageLimits.MaxReferenceLength} characters.",
                nameof(value));
    }

    private static string? NormalizeCommitSha(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length is not (40 or 64) || normalized.Any(static character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException("Changed artifact commit must be a full Git object id.", nameof(value));
        }

        return normalized;
    }

    private static string? OptionalText(string? value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException($"The value cannot exceed {maximumLength} characters.", parameterName);
        }

        return normalized;
    }
}

/// <summary>Bounded outcome/result metadata; it is not a reviewer or acceptance decision engine.</summary>
public sealed class HandoffOutcomeMetadata
{
    public HandoffOutcomeMetadata(HandoffOutcomeState state, string? summary = null, string? resultReference = null)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentException("Outcome state is undefined.", nameof(state));
        }

        State = state;
        Summary = OptionalText(summary, nameof(summary), HandoffPackageLimits.MaxTextLength);
        ResultReference = OptionalText(resultReference, nameof(resultReference), HandoffPackageLimits.MaxReferenceLength);
    }

    public HandoffOutcomeState State { get; }

    public string? Summary { get; }

    public string? ResultReference { get; }

    private static string? OptionalText(string? value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException($"The value cannot exceed {maximumLength} characters.", parameterName);
        }

        return normalized;
    }
}

public sealed class HandoffExecutionScope
{
    public HandoffExecutionScope(
        IReadOnlyList<PlanningScopeClause> includedScope,
        IReadOnlyList<PlanningScopeClause> constraints,
        IReadOnlyList<PlanningScopeClause> forbiddenScope,
        IReadOnlyList<PlanningDeliverable> deliverables,
        IReadOnlyList<PlanningValidationRequirement> validationRequirements,
        IReadOnlyList<PlanningExecutionBudget> executionBudgets,
        IReadOnlyList<PlanningStopCondition> stopConditions,
        IReadOnlyList<string> governanceReferences,
        string? routingPolicyReference,
        string? safetyPolicyReference)
    {
        IncludedScope = Copy(includedScope, nameof(includedScope));
        Constraints = Copy(constraints, nameof(constraints));
        ForbiddenScope = Copy(forbiddenScope, nameof(forbiddenScope));
        Deliverables = Copy(deliverables, nameof(deliverables));
        ValidationRequirements = Copy(validationRequirements, nameof(validationRequirements));
        ExecutionBudgets = Copy(executionBudgets, nameof(executionBudgets));
        StopConditions = Copy(stopConditions, nameof(stopConditions));
        GovernanceReferences = CopyStrings(governanceReferences, nameof(governanceReferences));
        RoutingPolicyReference = routingPolicyReference;
        SafetyPolicyReference = safetyPolicyReference;
    }

    public IReadOnlyList<PlanningScopeClause> IncludedScope { get; }
    public IReadOnlyList<PlanningScopeClause> Constraints { get; }
    public IReadOnlyList<PlanningScopeClause> ForbiddenScope { get; }
    public IReadOnlyList<PlanningDeliverable> Deliverables { get; }
    public IReadOnlyList<PlanningValidationRequirement> ValidationRequirements { get; }
    public IReadOnlyList<PlanningExecutionBudget> ExecutionBudgets { get; }
    public IReadOnlyList<PlanningStopCondition> StopConditions { get; }
    public IReadOnlyList<string> GovernanceReferences { get; }
    public string? RoutingPolicyReference { get; }
    public string? SafetyPolicyReference { get; }

    private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        if (values.Count > HandoffPackageLimits.MaxScopeItemsPerSection)
        {
            throw new ArgumentException("A handoff scope section contains too many items.", parameterName);
        }

        return values.ToArray();
    }

    private static IReadOnlyList<string> CopyStrings(IReadOnlyList<string> values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        if (values.Count > HandoffPackageLimits.MaxScopeItemsPerSection)
        {
            throw new ArgumentException("A handoff scope section contains too many items.", parameterName);
        }

        return values.ToArray();
    }
}

public sealed class HandoffReviewScope
{
    public HandoffReviewScope(
        IReadOnlyList<PlanningScopeClause> includedScope,
        IReadOnlyList<PlanningScopeClause> constraints,
        IReadOnlyList<PlanningScopeClause> forbiddenScope,
        IReadOnlyList<PlanningAcceptanceCriterion> acceptanceCriteria)
    {
        IncludedScope = Copy(includedScope, nameof(includedScope));
        Constraints = Copy(constraints, nameof(constraints));
        ForbiddenScope = Copy(forbiddenScope, nameof(forbiddenScope));
        AcceptanceCriteria = Copy(acceptanceCriteria, nameof(acceptanceCriteria));
    }

    public IReadOnlyList<PlanningScopeClause> IncludedScope { get; }
    public IReadOnlyList<PlanningScopeClause> Constraints { get; }
    public IReadOnlyList<PlanningScopeClause> ForbiddenScope { get; }
    public IReadOnlyList<PlanningAcceptanceCriterion> AcceptanceCriteria { get; }

    private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        if (values.Count > HandoffPackageLimits.MaxScopeItemsPerSection)
        {
            throw new ArgumentException("A handoff review section contains too many items.", parameterName);
        }

        return values.ToArray();
    }
}

public sealed class HandoffRemediationScope
{
    public HandoffRemediationScope(
        IReadOnlyList<PlanningScopeClause> includedScope,
        IReadOnlyList<PlanningScopeClause> constraints,
        IReadOnlyList<PlanningScopeClause> forbiddenScope,
        IReadOnlyList<PlanningStopCondition> stopConditions)
    {
        IncludedScope = Copy(includedScope, nameof(includedScope));
        Constraints = Copy(constraints, nameof(constraints));
        ForbiddenScope = Copy(forbiddenScope, nameof(forbiddenScope));
        StopConditions = Copy(stopConditions, nameof(stopConditions));
    }

    public IReadOnlyList<PlanningScopeClause> IncludedScope { get; }
    public IReadOnlyList<PlanningScopeClause> Constraints { get; }
    public IReadOnlyList<PlanningScopeClause> ForbiddenScope { get; }
    public IReadOnlyList<PlanningStopCondition> StopConditions { get; }

    private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        if (values.Count > HandoffPackageLimits.MaxScopeItemsPerSection)
        {
            throw new ArgumentException("A handoff remediation section contains too many items.", parameterName);
        }

        return values.ToArray();
    }
}

public sealed class HandoffAcceptanceScope
{
    public HandoffAcceptanceScope(IReadOnlyList<PlanningAcceptanceCriterion> acceptanceCriteria)
    {
        ArgumentNullException.ThrowIfNull(acceptanceCriteria);
        if (acceptanceCriteria.Count > HandoffPackageLimits.MaxScopeItemsPerSection)
        {
            throw new ArgumentException("A handoff acceptance section contains too many items.", nameof(acceptanceCriteria));
        }

        AcceptanceCriteria = acceptanceCriteria.ToArray();
    }

    public IReadOnlyList<PlanningAcceptanceCriterion> AcceptanceCriteria { get; }
}

public sealed class HandoffRedactionMetadata
{
    public HandoffRedactionMetadata(bool applied, int count, IReadOnlyList<HandoffRedactionCategory>? categories)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        var values = (categories ?? Array.Empty<HandoffRedactionCategory>()).Distinct().ToArray();
        if (count == 0 && values.Length != 0)
        {
            throw new ArgumentException("Redaction categories require a positive redaction count.", nameof(categories));
        }

        if (count > 0 && values.Length == 0)
        {
            throw new ArgumentException("A positive redaction count requires categories.", nameof(categories));
        }

        RedactionApplied = applied || count > 0;
        RedactionCount = count;
        Categories = values.OrderBy(static category => category).ToArray();
    }

    public bool RedactionApplied { get; }
    public int RedactionCount { get; }
    public IReadOnlyList<HandoffRedactionCategory> Categories { get; }
}

public sealed class HandoffPackageSizeMetadata
{
    public HandoffPackageSizeMetadata(
        int maxPayloadBytes,
        int canonicalPayloadBytes,
        int evidenceReferenceCount,
        int findingReferenceCount,
        int changedArtifactReferenceCount,
        int limitationCount,
        int scopeItemCount)
    {
        if (maxPayloadBytes <= 0 || maxPayloadBytes > HandoffPackageLimits.MaxCanonicalPayloadBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPayloadBytes));
        }

        if (canonicalPayloadBytes < 0 || canonicalPayloadBytes > maxPayloadBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(canonicalPayloadBytes));
        }

        if (evidenceReferenceCount < 0 || evidenceReferenceCount > HandoffPackageLimits.MaxEvidenceReferences ||
            findingReferenceCount < 0 || findingReferenceCount > HandoffPackageLimits.MaxFindingReferences ||
            changedArtifactReferenceCount < 0 || changedArtifactReferenceCount > HandoffPackageLimits.MaxChangedArtifactReferences ||
            limitationCount < 0 || limitationCount > HandoffPackageLimits.MaxLimitations ||
            scopeItemCount < 0 || scopeItemCount > HandoffPackageLimits.MaxScopeItemsPerSection * 8)
        {
            throw new ArgumentOutOfRangeException(nameof(evidenceReferenceCount));
        }

        MaxPayloadBytes = maxPayloadBytes;
        CanonicalPayloadBytes = canonicalPayloadBytes;
        EvidenceReferenceCount = evidenceReferenceCount;
        FindingReferenceCount = findingReferenceCount;
        ChangedArtifactReferenceCount = changedArtifactReferenceCount;
        LimitationCount = limitationCount;
        ScopeItemCount = scopeItemCount;
    }

    public int MaxPayloadBytes { get; }
    public int CanonicalPayloadBytes { get; }
    public int EvidenceReferenceCount { get; }
    public int FindingReferenceCount { get; }
    public int ChangedArtifactReferenceCount { get; }
    public int LimitationCount { get; }
    public int ScopeItemCount { get; }
}

/// <summary>
/// Immutable structured lifecycle context. It contains references and bounded role-specific
/// sections, never a prompt, transcript, source file, diff, or complete log.
/// </summary>
public sealed class HandoffPackage
{
    public HandoffPackage(
        Guid projectId,
        Guid packageId,
        int schemaVersion,
        DateTimeOffset createdAt,
        HandoffTransition transition,
        HandoffRole sourceRole,
        HandoffRole targetRole,
        PlanningExecutionContractReference planningContractReference,
        PlanningWorkItem workItem,
        HandoffContextReference context,
        PlanningRepositoryTarget repositoryTarget,
        WorkGraphReference? workGraphReference,
        Guid? workGraphNodeId,
        HandoffPackageReference? previousPackageReference,
        HandoffExecutionScope? executionScope,
        HandoffReviewScope? reviewScope,
        HandoffRemediationScope? remediationScope,
        HandoffAcceptanceScope? acceptanceScope,
        IReadOnlyList<HandoffEvidenceReference> evidenceReferences,
        IReadOnlyList<HandoffFindingReference> findingReferences,
        IReadOnlyList<HandoffChangedArtifactReference> changedArtifactReferences,
        HandoffOutcomeMetadata? outcome,
        IReadOnlyList<string> limitations,
        string nextAction,
        HandoffRedactionMetadata redaction,
        HandoffPackageSizeMetadata size,
        string? contentHash = null)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        if (packageId == Guid.Empty)
        {
            throw new ArgumentException("Package id cannot be empty.", nameof(packageId));
        }

        if (schemaVersion != HandoffPackageSchema.CurrentVersion)
        {
            throw new ArgumentException(
                $"Only handoff package schema {HandoffPackageSchema.CurrentVersion} is supported.",
                nameof(schemaVersion));
        }

        if (createdAt == default)
        {
            throw new ArgumentException("Package creation time is required.", nameof(createdAt));
        }

        if (!HandoffTransitionPolicy.TryGetRoles(transition, out var expectedSource, out var expectedTarget) ||
            sourceRole != expectedSource ||
            targetRole != expectedTarget)
        {
            throw new ArgumentException("The package roles do not match its supported transition.", nameof(transition));
        }

        PlanningContractReferenceRequired(planningContractReference);
        ArgumentNullException.ThrowIfNull(workItem);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(repositoryTarget);
        ArgumentNullException.ThrowIfNull(redaction);
        ArgumentNullException.ThrowIfNull(size);

        if ((workGraphReference is null) != (workGraphNodeId is null))
        {
            throw new ArgumentException("A graph binding requires both a graph reference and node id.", nameof(workGraphReference));
        }

        if (workGraphNodeId == Guid.Empty)
        {
            throw new ArgumentException("Graph node id cannot be empty.", nameof(workGraphNodeId));
        }

        if (HandoffTransitionPolicy.RequiresPredecessor(transition) != (previousPackageReference is not null))
        {
            throw new ArgumentException("The transition has an invalid predecessor reference.", nameof(previousPackageReference));
        }

        ExecutionScope = executionScope;
        ReviewScope = reviewScope;
        RemediationScope = remediationScope;
        AcceptanceScope = acceptanceScope;
        ValidateScopeShape(transition, executionScope, reviewScope, remediationScope, acceptanceScope);

        EvidenceReferences = Normalize(evidenceReferences, HandoffPackageLimits.MaxEvidenceReferences, nameof(evidenceReferences), EvidenceSort);
        FindingReferences = Normalize(findingReferences, HandoffPackageLimits.MaxFindingReferences, nameof(findingReferences), FindingSort);
        ChangedArtifactReferences = Normalize(changedArtifactReferences, HandoffPackageLimits.MaxChangedArtifactReferences, nameof(changedArtifactReferences), ArtifactSort);
        Limitations = NormalizeStrings(limitations, HandoffPackageLimits.MaxLimitations, nameof(limitations));
        NextAction = RequiredText(nextAction, nameof(nextAction), HandoffPackageLimits.MaxTextLength);

        if (transition == HandoffTransition.PlannerToExecutor && outcome is not null)
        {
            throw new ArgumentException("A planner-to-executor package cannot carry an outcome.", nameof(outcome));
        }

        if (transition != HandoffTransition.PlannerToExecutor && outcome is null)
        {
            throw new ArgumentException("This transition requires bounded outcome metadata.", nameof(outcome));
        }

        ProjectId = projectId;
        PackageId = packageId;
        SchemaVersion = schemaVersion;
        CreatedAt = createdAt;
        Transition = transition;
        SourceRole = sourceRole;
        TargetRole = targetRole;
        PlanningContractReference = planningContractReference;
        WorkItem = workItem;
        Context = context;
        RepositoryTarget = repositoryTarget;
        WorkGraphReference = workGraphReference;
        WorkGraphNodeId = workGraphNodeId;
        PreviousPackageReference = previousPackageReference;
        Outcome = outcome;
        Redaction = redaction;
        Size = size;

        var actualScopeItemCount = CountScopeItems(executionScope, reviewScope, remediationScope, acceptanceScope);
        if (size.EvidenceReferenceCount != EvidenceReferences.Count ||
            size.FindingReferenceCount != FindingReferences.Count ||
            size.ChangedArtifactReferenceCount != ChangedArtifactReferences.Count ||
            size.LimitationCount != Limitations.Count ||
            size.ScopeItemCount != actualScopeItemCount)
        {
            throw new ArgumentException("Handoff size metadata does not match the package items.", nameof(size));
        }

        ContentHash = string.Empty;
        var calculatedHash = HandoffPackageIntegrity.ComputeContentHash(this);
        if (contentHash is not null &&
            (!HandoffPackageReference.IsSha256(contentHash) ||
             !string.Equals(contentHash, calculatedHash, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("The supplied handoff content hash does not match the package payload.", nameof(contentHash));
        }

        ContentHash = calculatedHash;
        Reference = new HandoffPackageReference(PackageId, SchemaVersion, ContentHash);
    }

    public Guid ProjectId { get; }
    public Guid PackageId { get; }
    public int SchemaVersion { get; }
    public DateTimeOffset CreatedAt { get; }
    public HandoffTransition Transition { get; }
    public HandoffRole SourceRole { get; }
    public HandoffRole TargetRole { get; }
    public PlanningExecutionContractReference PlanningContractReference { get; }
    public PlanningWorkItem WorkItem { get; }
    public HandoffContextReference Context { get; }
    public PlanningRepositoryTarget RepositoryTarget { get; }
    public WorkGraphReference? WorkGraphReference { get; }
    public Guid? WorkGraphNodeId { get; }
    public HandoffPackageReference? PreviousPackageReference { get; }
    public HandoffExecutionScope? ExecutionScope { get; }
    public HandoffReviewScope? ReviewScope { get; }
    public HandoffRemediationScope? RemediationScope { get; }
    public HandoffAcceptanceScope? AcceptanceScope { get; }
    public IReadOnlyList<HandoffEvidenceReference> EvidenceReferences { get; }
    public IReadOnlyList<HandoffFindingReference> FindingReferences { get; }
    public IReadOnlyList<HandoffChangedArtifactReference> ChangedArtifactReferences { get; }
    public HandoffOutcomeMetadata? Outcome { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string NextAction { get; }
    public HandoffRedactionMetadata Redaction { get; }
    public HandoffPackageSizeMetadata Size { get; }
    /// <summary>SHA-256 content-integrity evidence, not a signature or authentication proof.</summary>
    public string ContentHash { get; private set; }
    public HandoffPackageReference Reference { get; private set; }

    private static void PlanningContractReferenceRequired(PlanningExecutionContractReference? value)
    {
        ArgumentNullException.ThrowIfNull(value);
    }

    private static void ValidateScopeShape(
        HandoffTransition transition,
        HandoffExecutionScope? executionScope,
        HandoffReviewScope? reviewScope,
        HandoffRemediationScope? remediationScope,
        HandoffAcceptanceScope? acceptanceScope)
    {
        (bool, bool, bool, bool) expected = transition switch
        {
            HandoffTransition.PlannerToExecutor => (true, false, false, false),
            HandoffTransition.ExecutorToReviewer or HandoffTransition.RemediationToReviewer => (false, true, false, false),
            HandoffTransition.ReviewerToRemediation => (false, false, true, false),
            HandoffTransition.ReviewerToAcceptance => (false, false, false, true),
            HandoffTransition.AcceptanceToPlanner => (false, false, false, false),
            _ => default
        };

        if ((executionScope is not null) != expected.Item1 ||
            (reviewScope is not null) != expected.Item2 ||
            (remediationScope is not null) != expected.Item3 ||
            (acceptanceScope is not null) != expected.Item4)
        {
            throw new ArgumentException("The package contains a role-irrelevant scope section.", nameof(transition));
        }
    }

    private static IReadOnlyList<T> Normalize<T>(
        IReadOnlyList<T> values,
        int maximum,
        string parameterName,
        Comparison<T> comparison)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        if (values.Count > maximum)
        {
            throw new ArgumentException("The handoff contains too many references.", parameterName);
        }

        var result = values.ToList();
        if (result.Any(static value => value is null))
        {
            throw new ArgumentException("Handoff references cannot contain null entries.", parameterName);
        }

        result.Sort(comparison);
        return result.AsReadOnly();
    }

    private static IReadOnlyList<string> NormalizeStrings(IReadOnlyList<string> values, int maximum, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        if (values.Count > maximum)
        {
            throw new ArgumentException("The handoff contains too many limitations.", parameterName);
        }

        var result = new List<string>(values.Count);
        foreach (var value in values)
        {
            var normalized = RequiredText(value, parameterName, HandoffPackageLimits.MaxTextLength);
            if (!result.Contains(normalized, StringComparer.Ordinal))
            {
                result.Add(normalized);
            }
        }

        result.Sort(StringComparer.Ordinal);
        return result.AsReadOnly();
    }

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

    private static int EvidenceSort(HandoffEvidenceReference left, HandoffEvidenceReference right) => left.EvidenceId.CompareTo(right.EvidenceId);

    private static int FindingSort(HandoffFindingReference left, HandoffFindingReference right) =>
        StringComparer.Ordinal.Compare(left.FindingId, right.FindingId);

    private static int ArtifactSort(HandoffChangedArtifactReference left, HandoffChangedArtifactReference right) =>
        StringComparer.Ordinal.Compare(
            left.RepositoryRelativePath ?? left.ExternalReference ?? left.CommitSha ?? string.Empty,
            right.RepositoryRelativePath ?? right.ExternalReference ?? right.CommitSha ?? string.Empty);

    private static string RequiredText(string value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A bounded text value is required.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException($"The value cannot exceed {maximumLength} characters.", parameterName);
        }

        return normalized;
    }
}

/// <summary>Deterministic SHA-256 integrity calculation for complete package authority.</summary>
public static class HandoffPackageIntegrity
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static string ComputeContentHash(HandoffPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        var json = JsonSerializer.Serialize(CreatePayload(package, zeroCanonicalPayloadBytes: false), Options);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    /// <summary>
    /// Size convention: count UTF-8 bytes of the canonical authority payload with ContentHash
    /// omitted and CanonicalPayloadBytes set to zero. This avoids circular size metadata while
    /// retaining all other package authority in the deterministic size calculation.
    /// </summary>
    public static int ComputeCanonicalPayloadBytes(HandoffPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        var json = JsonSerializer.Serialize(CreatePayload(package, zeroCanonicalPayloadBytes: true), Options);
        return Encoding.UTF8.GetByteCount(json);
    }

    private static object CreatePayload(HandoffPackage package, bool zeroCanonicalPayloadBytes) => new
    {
        package.ProjectId,
        package.PackageId,
        package.SchemaVersion,
        package.CreatedAt,
        package.Transition,
        package.SourceRole,
        package.TargetRole,
        planningContractReference = new
        {
            package.PlanningContractReference.ContractId,
            package.PlanningContractReference.Revision,
            package.PlanningContractReference.SchemaVersion,
            package.PlanningContractReference.ContentHash
        },
        workItem = new
        {
            package.WorkItem.Source,
            package.WorkItem.Reference,
            package.WorkItem.Title
        },
        context = new
        {
            package.Context.ContextId,
            package.Context.ContextContractVersion,
            package.Context.CapturedAt,
            package.Context.UpdatedAt
        },
        repositoryTarget = new
        {
            package.RepositoryTarget.Mode,
            package.RepositoryTarget.RegisteredLocalPath,
            package.RepositoryTarget.ExpectedBranch,
            package.RepositoryTarget.ExpectedHeadCommit
        },
        workGraphReference = package.WorkGraphReference is null
            ? null
            : new
            {
                package.WorkGraphReference.GraphId,
                package.WorkGraphReference.SchemaVersion,
                package.WorkGraphReference.ContentHash
            },
        package.WorkGraphNodeId,
        previousPackageReference = package.PreviousPackageReference is null
            ? null
            : new
            {
                package.PreviousPackageReference.PackageId,
                package.PreviousPackageReference.SchemaVersion,
                package.PreviousPackageReference.ContentHash
            },
        executionScope = package.ExecutionScope is null ? null : ExecutionPayload(package.ExecutionScope),
        reviewScope = package.ReviewScope is null ? null : ReviewPayload(package.ReviewScope),
        remediationScope = package.RemediationScope is null ? null : RemediationPayload(package.RemediationScope),
        acceptanceScope = package.AcceptanceScope is null ? null : AcceptancePayload(package.AcceptanceScope),
        evidenceReferences = package.EvidenceReferences.Select(static value => new
        {
            value.EvidenceId,
            value.Kind,
            value.Reference,
            value.CapturedAt,
            value.Freshness,
            value.ContentHash
        }).ToArray(),
        findingReferences = package.FindingReferences.Select(static value => new
        {
            value.FindingId,
            value.Category,
            value.Severity,
            value.State,
            value.Summary,
            value.SourceReference,
            evidenceIds = value.EvidenceIds.ToArray()
        }).ToArray(),
        changedArtifactReferences = package.ChangedArtifactReferences.Select(static value => new
        {
            value.RepositoryRelativePath,
            value.CommitSha,
            value.ExternalReference
        }).ToArray(),
        outcome = package.Outcome is null ? null : new
        {
            package.Outcome.State,
            package.Outcome.Summary,
            package.Outcome.ResultReference
        },
        package.Limitations,
        package.NextAction,
        redaction = new
        {
            package.Redaction.RedactionApplied,
            package.Redaction.RedactionCount,
            categories = package.Redaction.Categories.ToArray()
        },
        size = new
        {
            package.Size.MaxPayloadBytes,
            CanonicalPayloadBytes = zeroCanonicalPayloadBytes ? 0 : package.Size.CanonicalPayloadBytes,
            package.Size.EvidenceReferenceCount,
            package.Size.FindingReferenceCount,
            package.Size.ChangedArtifactReferenceCount,
            package.Size.LimitationCount,
            package.Size.ScopeItemCount
        }
    };

    private static object ExecutionPayload(HandoffExecutionScope scope) => new
    {
        includedScope = scope.IncludedScope.Select(static value => new { value.Id, value.Statement }).ToArray(),
        constraints = scope.Constraints.Select(static value => new { value.Id, value.Statement }).ToArray(),
        forbiddenScope = scope.ForbiddenScope.Select(static value => new { value.Id, value.Statement }).ToArray(),
        deliverables = scope.Deliverables.Select(static value => new { value.DeliverableId, value.Description, value.Required }).ToArray(),
        validationRequirements = scope.ValidationRequirements.Select(static value => new
        {
            value.ValidationId,
            value.Kind,
            value.Description,
            value.Required,
            value.CommandOrReference
        }).ToArray(),
        executionBudgets = scope.ExecutionBudgets.Select(static value => new { value.Kind, value.Limit }).ToArray(),
        stopConditions = scope.StopConditions.Select(static value => new { value.ConditionId, value.Kind, value.Description }).ToArray(),
        scope.GovernanceReferences,
        scope.RoutingPolicyReference,
        scope.SafetyPolicyReference
    };

    private static object ReviewPayload(HandoffReviewScope scope) => new
    {
        includedScope = scope.IncludedScope.Select(static value => new { value.Id, value.Statement }).ToArray(),
        constraints = scope.Constraints.Select(static value => new { value.Id, value.Statement }).ToArray(),
        forbiddenScope = scope.ForbiddenScope.Select(static value => new { value.Id, value.Statement }).ToArray(),
        acceptanceCriteria = scope.AcceptanceCriteria.Select(static value => new { value.CriterionId, value.Statement, value.Required }).ToArray()
    };

    private static object RemediationPayload(HandoffRemediationScope scope) => new
    {
        includedScope = scope.IncludedScope.Select(static value => new { value.Id, value.Statement }).ToArray(),
        constraints = scope.Constraints.Select(static value => new { value.Id, value.Statement }).ToArray(),
        forbiddenScope = scope.ForbiddenScope.Select(static value => new { value.Id, value.Statement }).ToArray(),
        stopConditions = scope.StopConditions.Select(static value => new { value.ConditionId, value.Kind, value.Description }).ToArray()
    };

    private static object AcceptancePayload(HandoffAcceptanceScope scope) => new
    {
        acceptanceCriteria = scope.AcceptanceCriteria.Select(static value => new { value.CriterionId, value.Statement, value.Required }).ToArray()
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
