using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIUsageMonitor.Application.Agents;

namespace AIUsageMonitor.Application.Planning;

/// <summary>Version of the persisted planning/execution contract data format.</summary>
public static class PlanningExecutionContractSchema
{
    public const int CurrentVersion = 1;
}

public enum PlanningWorkItemSource
{
    Manual,
    Jira,
    AzureBoards,
    Other
}

public enum PlanningRepositoryMode
{
    None,
    LocalGit
}

public enum PlanningValidationKind
{
    Build,
    Test,
    StaticCheck,
    SecurityCheck,
    ManualInspection,
    Custom
}

public enum PlanningBudgetKind
{
    Attempts,
    ElapsedMinutes,
    ChangedFiles,
    ChangedLines,
    ToolInvocations,
    ModelTurns
}

public enum PlanningStopConditionKind
{
    ImmutableTargetMoved,
    ScopeViolation,
    ValidationFailure,
    BudgetExceeded,
    CredentialRequired,
    OwnerApprovalRequired,
    ExternalDependencyUnavailable,
    ContextInsufficient,
    UnresolvedAmbiguity,
    SecurityBoundaryReached
}

public sealed class PlanningContextBinding
{
    public PlanningContextBinding(Guid projectContextId, int projectContextContractVersion)
    {
        if (projectContextId == Guid.Empty)
        {
            throw new ArgumentException("Project context id cannot be empty.", nameof(projectContextId));
        }

        if (projectContextContractVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(projectContextContractVersion));
        }

        ProjectContextId = projectContextId;
        ProjectContextContractVersion = projectContextContractVersion;
    }

    public Guid ProjectContextId { get; }

    public int ProjectContextContractVersion { get; }
}

public sealed class PlanningWorkItem
{
    public PlanningWorkItem(
        PlanningWorkItemSource source,
        string reference,
        string title)
    {
        if (!Enum.IsDefined(source))
        {
            throw new ArgumentException("Work-item source is undefined.", nameof(source));
        }

        Source = source;
        Reference = RequiredText(reference, nameof(reference), 200);
        Title = RequiredText(title, nameof(title), 500);
    }

    public PlanningWorkItemSource Source { get; }

    public string Reference { get; }

    public string Title { get; }

    internal static bool SameIdentity(PlanningWorkItem left, PlanningWorkItem right) =>
        left.Source == right.Source &&
        string.Equals(left.Reference, right.Reference, StringComparison.Ordinal) &&
        string.Equals(left.Title, right.Title, StringComparison.Ordinal);

    private static string RequiredText(string value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A bounded text value is required.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }
}

public sealed class PlanningRepositoryTarget
{
    public PlanningRepositoryTarget(
        PlanningRepositoryMode mode,
        string? registeredLocalPath = null,
        string? expectedBranch = null,
        string? expectedHeadCommit = null)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentException("Repository mode is undefined.", nameof(mode));
        }

        Mode = mode;
        RegisteredLocalPath = NormalizeOptional(registeredLocalPath, nameof(registeredLocalPath), 2_000);
        ExpectedBranch = NormalizeOptional(expectedBranch, nameof(expectedBranch), 300);
        ExpectedHeadCommit = NormalizeOptional(expectedHeadCommit, nameof(expectedHeadCommit), 64);

        if (mode == PlanningRepositoryMode.None)
        {
            if (RegisteredLocalPath is not null || ExpectedBranch is not null || ExpectedHeadCommit is not null)
            {
                throw new ArgumentException("A repository-free target cannot carry repository identity.", nameof(mode));
            }

            return;
        }

        if (RegisteredLocalPath is null || ExpectedBranch is null || ExpectedHeadCommit is null)
        {
            throw new ArgumentException(
                "A LocalGit target requires a registered path, branch, and exact HEAD commit.",
                nameof(mode));
        }

        if (!IsGitObjectId(ExpectedHeadCommit))
        {
            throw new ArgumentException(
                "Expected HEAD commit must be a full SHA-1 or SHA-256 Git object id.",
                nameof(expectedHeadCommit));
        }
    }

    public PlanningRepositoryMode Mode { get; }

    public string? RegisteredLocalPath { get; }

    public string? ExpectedBranch { get; }

    public string? ExpectedHeadCommit { get; }

    private static string? NormalizeOptional(string? value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static bool IsGitObjectId(string value) =>
        (value.Length is 40 or 64) && value.All(static character => Uri.IsHexDigit(character));
}

public sealed class PlanningScopeClause
{
    public PlanningScopeClause(string id, string statement)
    {
        Id = RequiredText(id, nameof(id), 120);
        Statement = RequiredText(statement, nameof(statement), 4_000);
    }

    public string Id { get; }

    public string Statement { get; }

    private static string RequiredText(string value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A bounded text value is required.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }
}

public sealed class PlanningDeliverable
{
    public PlanningDeliverable(string deliverableId, string description, bool required)
    {
        DeliverableId = RequiredText(deliverableId, nameof(deliverableId), 120);
        Description = RequiredText(description, nameof(description), 4_000);
        Required = required;
    }

    public string DeliverableId { get; }

    public string Description { get; }

    public bool Required { get; }

    private static string RequiredText(string value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A bounded text value is required.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }
}

public sealed class PlanningValidationRequirement
{
    public PlanningValidationRequirement(
        string validationId,
        PlanningValidationKind kind,
        string description,
        bool required,
        string? commandOrReference = null)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentException("Validation kind is undefined.", nameof(kind));
        }

        ValidationId = RequiredText(validationId, nameof(validationId), 120);
        Description = RequiredText(description, nameof(description), 4_000);
        Required = required;
        CommandOrReference = NormalizeOptional(commandOrReference, nameof(commandOrReference), 1_000);
    }

    public string ValidationId { get; }

    public PlanningValidationKind Kind { get; }

    public string Description { get; }

    public bool Required { get; }

    /// <summary>Data-only command/reference metadata. APO-40 never executes this value.</summary>
    public string? CommandOrReference { get; }

    private static string RequiredText(string value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A bounded text value is required.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }
}

public sealed class PlanningAcceptanceCriterion
{
    public PlanningAcceptanceCriterion(string criterionId, string statement, bool required)
    {
        CriterionId = RequiredText(criterionId, nameof(criterionId), 120);
        Statement = RequiredText(statement, nameof(statement), 4_000);
        Required = required;
    }

    public string CriterionId { get; }

    public string Statement { get; }

    public bool Required { get; }

    private static string RequiredText(string value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A bounded text value is required.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }
}

public sealed class PlanningExecutionBudget
{
    public PlanningExecutionBudget(PlanningBudgetKind kind, long limit)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentException("Budget kind is undefined.", nameof(kind));
        }

        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Budget limit must be positive.");
        }

        Kind = kind;
        Limit = limit;
    }

    public PlanningBudgetKind Kind { get; }

    public long Limit { get; }
}

public sealed class PlanningStopCondition
{
    public PlanningStopCondition(
        string conditionId,
        PlanningStopConditionKind kind,
        string description)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentException("Stop-condition kind is undefined.", nameof(kind));
        }

        ConditionId = RequiredText(conditionId, nameof(conditionId), 120);
        Kind = kind;
        Description = RequiredText(description, nameof(description), 4_000);
    }

    public string ConditionId { get; }

    public PlanningStopConditionKind Kind { get; }

    public string Description { get; }

    private static string RequiredText(string value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A bounded text value is required.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }
}

/// <summary>Typed content-integrity reference for one immutable contract revision.</summary>
public sealed class PlanningExecutionContractReference
{
    public PlanningExecutionContractReference(
        Guid contractId,
        int revision,
        int schemaVersion,
        string contentHash)
    {
        if (contractId == Guid.Empty)
        {
            throw new ArgumentException("Contract id cannot be empty.", nameof(contractId));
        }

        if (revision <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(revision));
        }

        if (schemaVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        }

        if (!IsSha256(contentHash))
        {
            throw new ArgumentException("Content hash must be a SHA-256 hexadecimal value.", nameof(contentHash));
        }

        ContractId = contractId;
        Revision = revision;
        SchemaVersion = schemaVersion;
        ContentHash = contentHash.ToLowerInvariant();
    }

    public Guid ContractId { get; }

    public int Revision { get; }

    public int SchemaVersion { get; }

    public string ContentHash { get; }

    public override string ToString() =>
        $"contract:{ContractId:D}/revision:{Revision:D6}/schema:{SchemaVersion}/sha256:{ContentHash}";

    internal static bool IsSha256(string? value) =>
        value is not null &&
        value.Length == 64 &&
        value.All(static character => Uri.IsHexDigit(character));
}

/// <summary>
/// Immutable authority for one bounded planner-authored work item. This type contains data and
/// validation only; it does not invoke agents, execute commands, schedule work, or mutate Git.
/// </summary>
public sealed class PlanningExecutionContract
{
    public PlanningExecutionContract(
        Guid projectId,
        Guid contractId,
        int schemaVersion,
        int revision,
        DateTimeOffset createdAt,
        string ownerReference,
        Guid plannerAgentId,
        PlanningContextBinding context,
        PlanningWorkItem workItem,
        PlanningRepositoryTarget repositoryTarget,
        IReadOnlyList<PlanningScopeClause> includedScope,
        IReadOnlyList<PlanningScopeClause> constraints,
        IReadOnlyList<PlanningScopeClause> forbiddenScope,
        IReadOnlyList<PlanningDeliverable> deliverables,
        IReadOnlyList<PlanningValidationRequirement> validationRequirements,
        IReadOnlyList<PlanningAcceptanceCriterion> acceptanceCriteria,
        IReadOnlyList<PlanningExecutionBudget> executionBudgets,
        IReadOnlyList<PlanningStopCondition> stopConditions,
        IReadOnlyList<string>? governanceReferences,
        string? routingPolicyReference,
        string? safetyPolicyReference,
        int? previousRevision = null,
        string? previousContentHash = null,
        string? contentHash = null)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        if (contractId == Guid.Empty)
        {
            throw new ArgumentException("Contract id cannot be empty.", nameof(contractId));
        }

        if (schemaVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        }

        if (revision <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(revision));
        }

        if (createdAt == default)
        {
            throw new ArgumentException("Contract creation time is required.", nameof(createdAt));
        }

        OwnerReference = RequiredText(ownerReference, nameof(ownerReference), 300);
        if (plannerAgentId == Guid.Empty)
        {
            throw new ArgumentException("Planner agent id cannot be empty.", nameof(plannerAgentId));
        }

        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(workItem);
        ArgumentNullException.ThrowIfNull(repositoryTarget);

        ProjectId = projectId;
        ContractId = contractId;
        SchemaVersion = schemaVersion;
        Revision = revision;
        CreatedAt = createdAt;
        PlannerAgentId = plannerAgentId;
        Context = context;
        WorkItem = workItem;
        RepositoryTarget = repositoryTarget;
        IncludedScope = NormalizeClauses(includedScope, nameof(includedScope), requireAtLeastOne: true);
        Constraints = NormalizeClauses(constraints, nameof(constraints), requireAtLeastOne: false);
        ForbiddenScope = NormalizeClauses(forbiddenScope, nameof(forbiddenScope), requireAtLeastOne: true);
        Deliverables = NormalizeDeliverables(deliverables);
        ValidationRequirements = NormalizeValidations(validationRequirements);
        AcceptanceCriteria = NormalizeAcceptanceCriteria(acceptanceCriteria);
        ExecutionBudgets = NormalizeBudgets(executionBudgets);
        StopConditions = NormalizeStopConditions(stopConditions);
        GovernanceReferences = NormalizeReferences(governanceReferences);
        RoutingPolicyReference = NormalizeOptional(routingPolicyReference, nameof(routingPolicyReference), 500);
        SafetyPolicyReference = NormalizeOptional(safetyPolicyReference, nameof(safetyPolicyReference), 500);

        if (revision == 1)
        {
            if (previousRevision is not null || previousContentHash is not null)
            {
                throw new ArgumentException("Revision 1 cannot carry predecessor evidence.", nameof(previousRevision));
            }
        }
        else
        {
            if (previousRevision != revision - 1 || !PlanningExecutionContractReference.IsSha256(previousContentHash))
            {
                throw new ArgumentException(
                    "A revised contract must reference the immediate predecessor and its SHA-256 hash.",
                    nameof(previousRevision));
            }
        }

        PreviousRevision = previousRevision;
        PreviousContentHash = previousContentHash?.ToLowerInvariant();
        ContentHash = string.Empty;
        var calculatedHash = PlanningExecutionContractIntegrity.ComputeContentHash(this);
        if (contentHash is not null &&
            (!PlanningExecutionContractReference.IsSha256(contentHash) ||
             !string.Equals(contentHash, calculatedHash, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("The supplied content hash does not match the contract payload.", nameof(contentHash));
        }

        ContentHash = calculatedHash;
        Reference = new PlanningExecutionContractReference(
            ContractId,
            Revision,
            SchemaVersion,
            ContentHash);
    }

    public Guid ProjectId { get; }

    public Guid ContractId { get; }

    /// <summary>Software/data format version; distinct from <see cref="Revision"/>.</summary>
    public int SchemaVersion { get; }

    /// <summary>Planner-authorized immutable revision of the same logical contract.</summary>
    public int Revision { get; }

    public DateTimeOffset CreatedAt { get; }

    public string OwnerReference { get; }

    public Guid PlannerAgentId { get; }

    public PlanningContextBinding Context { get; }

    public PlanningWorkItem WorkItem { get; }

    public PlanningRepositoryTarget RepositoryTarget { get; }

    public IReadOnlyList<PlanningScopeClause> IncludedScope { get; }

    public IReadOnlyList<PlanningScopeClause> Constraints { get; }

    public IReadOnlyList<PlanningScopeClause> ForbiddenScope { get; }

    public IReadOnlyList<PlanningDeliverable> Deliverables { get; }

    public IReadOnlyList<PlanningValidationRequirement> ValidationRequirements { get; }

    public IReadOnlyList<PlanningAcceptanceCriterion> AcceptanceCriteria { get; }

    public IReadOnlyList<PlanningExecutionBudget> ExecutionBudgets { get; }

    public IReadOnlyList<PlanningStopCondition> StopConditions { get; }

    public IReadOnlyList<string> GovernanceReferences { get; }

    public string? RoutingPolicyReference { get; }

    public string? SafetyPolicyReference { get; }

    public int? PreviousRevision { get; }

    public string? PreviousContentHash { get; }

    /// <summary>SHA-256 content-integrity evidence, not a signature or authentication proof.</summary>
    public string ContentHash { get; private set; }

    public PlanningExecutionContractReference Reference { get; private set; }

    private static IReadOnlyList<PlanningScopeClause> NormalizeClauses(
        IReadOnlyList<PlanningScopeClause> values,
        string parameterName,
        bool requireAtLeastOne)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        if (requireAtLeastOne && values.Count == 0)
        {
            throw new ArgumentException("At least one scope clause is required.", parameterName);
        }

        var result = new List<PlanningScopeClause>(values.Count);
        foreach (var value in values)
        {
            if (value is null)
            {
                throw new ArgumentException("Scope clauses cannot contain null entries.", parameterName);
            }

            if (result.Any(existing => string.Equals(existing.Id, value.Id, StringComparison.Ordinal)))
            {
                throw new ArgumentException("Scope clause identifiers must be unique.", parameterName);
            }

            result.Add(value);
        }

        result.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));
        return result.AsReadOnly();
    }

    private static IReadOnlyList<PlanningDeliverable> NormalizeDeliverables(
        IReadOnlyList<PlanningDeliverable> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0)
        {
            throw new ArgumentException("At least one deliverable is required.", nameof(values));
        }

        var result = new List<PlanningDeliverable>(values.Count);
        foreach (var value in values)
        {
            if (value is null)
            {
                throw new ArgumentException("Deliverables cannot contain null entries.", nameof(values));
            }

            if (result.Any(existing => string.Equals(existing.DeliverableId, value.DeliverableId, StringComparison.Ordinal)))
            {
                throw new ArgumentException("Deliverable identifiers must be unique.", nameof(values));
            }

            result.Add(value);
        }

        if (!result.Any(static value => value.Required))
        {
            throw new ArgumentException("At least one deliverable must be required.", nameof(values));
        }

        result.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.DeliverableId, right.DeliverableId));
        return result.AsReadOnly();
    }

    private static IReadOnlyList<PlanningValidationRequirement> NormalizeValidations(
        IReadOnlyList<PlanningValidationRequirement> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0)
        {
            throw new ArgumentException("At least one validation requirement is required.", nameof(values));
        }

        var result = new List<PlanningValidationRequirement>(values.Count);
        foreach (var value in values)
        {
            if (value is null)
            {
                throw new ArgumentException("Validation requirements cannot contain null entries.", nameof(values));
            }

            if (result.Any(existing => string.Equals(existing.ValidationId, value.ValidationId, StringComparison.Ordinal)))
            {
                throw new ArgumentException("Validation identifiers must be unique.", nameof(values));
            }

            result.Add(value);
        }

        result.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.ValidationId, right.ValidationId));
        return result.AsReadOnly();
    }

    private static IReadOnlyList<PlanningAcceptanceCriterion> NormalizeAcceptanceCriteria(
        IReadOnlyList<PlanningAcceptanceCriterion> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0)
        {
            throw new ArgumentException("At least one acceptance criterion is required.", nameof(values));
        }

        var result = new List<PlanningAcceptanceCriterion>(values.Count);
        foreach (var value in values)
        {
            if (value is null)
            {
                throw new ArgumentException("Acceptance criteria cannot contain null entries.", nameof(values));
            }

            if (result.Any(existing => string.Equals(existing.CriterionId, value.CriterionId, StringComparison.Ordinal)))
            {
                throw new ArgumentException("Acceptance criterion identifiers must be unique.", nameof(values));
            }

            result.Add(value);
        }

        if (!result.Any(static value => value.Required))
        {
            throw new ArgumentException("At least one acceptance criterion must be required.", nameof(values));
        }

        result.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.CriterionId, right.CriterionId));
        return result.AsReadOnly();
    }

    private static IReadOnlyList<PlanningExecutionBudget> NormalizeBudgets(
        IReadOnlyList<PlanningExecutionBudget> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0)
        {
            throw new ArgumentException("At least one execution budget is required.", nameof(values));
        }

        var result = new List<PlanningExecutionBudget>(values.Count);
        foreach (var value in values)
        {
            if (value is null)
            {
                throw new ArgumentException("Execution budgets cannot contain null entries.", nameof(values));
            }

            if (result.Any(existing => existing.Kind == value.Kind))
            {
                throw new ArgumentException("Budget kinds must be unique.", nameof(values));
            }

            result.Add(value);
        }

        result.Sort(static (left, right) => left.Kind.CompareTo(right.Kind));
        return result.AsReadOnly();
    }

    private static IReadOnlyList<PlanningStopCondition> NormalizeStopConditions(
        IReadOnlyList<PlanningStopCondition> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var result = new List<PlanningStopCondition>(values.Count);
        foreach (var value in values)
        {
            if (value is null)
            {
                throw new ArgumentException("Stop conditions cannot contain null entries.", nameof(values));
            }

            if (result.Any(existing => string.Equals(existing.ConditionId, value.ConditionId, StringComparison.Ordinal)))
            {
                throw new ArgumentException("Stop-condition identifiers must be unique.", nameof(values));
            }

            result.Add(value);
        }

        var requiredKinds = new[]
        {
            PlanningStopConditionKind.ImmutableTargetMoved,
            PlanningStopConditionKind.ScopeViolation,
            PlanningStopConditionKind.BudgetExceeded
        };
        if (requiredKinds.Any(kind => result.All(value => value.Kind != kind)))
        {
            throw new ArgumentException(
                "Stop conditions must represent immutable-target, scope, and budget boundaries.",
                nameof(values));
        }

        result.Sort(static (left, right) =>
        {
            var kindComparison = left.Kind.CompareTo(right.Kind);
            return kindComparison != 0
                ? kindComparison
                : StringComparer.Ordinal.Compare(left.ConditionId, right.ConditionId);
        });
        return result.AsReadOnly();
    }

    private static IReadOnlyList<string> NormalizeReferences(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<string>();
        }

        var result = new List<string>(values.Count);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Governance references cannot contain blank values.", nameof(values));
            }

            var normalized = value.Trim();
            if (normalized.Length > 500)
            {
                throw new ArgumentException("Governance references cannot exceed 500 characters.", nameof(values));
            }

            if (!result.Contains(normalized, StringComparer.Ordinal))
            {
                result.Add(normalized);
            }
        }

        result.Sort(StringComparer.Ordinal);
        return result.AsReadOnly();
    }

    private static string? NormalizeOptional(string? value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static string RequiredText(string value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A bounded text value is required.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }
}

/// <summary>Computes SHA-256 content integrity over the contract payload, excluding the hash.</summary>
public static class PlanningExecutionContractIntegrity
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static string ComputeContentHash(PlanningExecutionContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var payload = new
        {
            contract.ProjectId,
            contract.ContractId,
            contract.SchemaVersion,
            contract.Revision,
            contract.CreatedAt,
            contract.OwnerReference,
            contract.PlannerAgentId,
            context = new
            {
                contract.Context.ProjectContextId,
                contract.Context.ProjectContextContractVersion
            },
            workItem = new
            {
                contract.WorkItem.Source,
                contract.WorkItem.Reference,
                contract.WorkItem.Title
            },
            repositoryTarget = new
            {
                contract.RepositoryTarget.Mode,
                contract.RepositoryTarget.RegisteredLocalPath,
                contract.RepositoryTarget.ExpectedBranch,
                contract.RepositoryTarget.ExpectedHeadCommit
            },
            includedScope = contract.IncludedScope.Select(static value => new { value.Id, value.Statement }).ToArray(),
            constraints = contract.Constraints.Select(static value => new { value.Id, value.Statement }).ToArray(),
            forbiddenScope = contract.ForbiddenScope.Select(static value => new { value.Id, value.Statement }).ToArray(),
            deliverables = contract.Deliverables.Select(static value => new { value.DeliverableId, value.Description, value.Required }).ToArray(),
            validationRequirements = contract.ValidationRequirements.Select(static value => new
            {
                value.ValidationId,
                value.Kind,
                value.Description,
                value.Required,
                value.CommandOrReference
            }).ToArray(),
            acceptanceCriteria = contract.AcceptanceCriteria.Select(static value => new { value.CriterionId, value.Statement, value.Required }).ToArray(),
            executionBudgets = contract.ExecutionBudgets.Select(static value => new { value.Kind, value.Limit }).ToArray(),
            stopConditions = contract.StopConditions.Select(static value => new { value.ConditionId, value.Kind, value.Description }).ToArray(),
            contract.GovernanceReferences,
            contract.RoutingPolicyReference,
            contract.SafetyPolicyReference,
            contract.PreviousRevision,
            contract.PreviousContentHash
        };

        var json = JsonSerializer.Serialize(payload, Options);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

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
