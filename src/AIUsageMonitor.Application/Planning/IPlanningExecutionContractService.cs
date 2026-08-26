using AIUsageMonitor.Application.Projects;

namespace AIUsageMonitor.Application.Planning;

/// <summary>Caller-supplied bounded authority. Time, context policy references, and lineage are
/// resolved by the service rather than trusted from the caller.</summary>
public sealed class PlanningExecutionContractRequest
{
    public PlanningExecutionContractRequest(
        Guid projectId,
        Guid contractId,
        int revision,
        string ownerReference,
        Guid plannerAgentId,
        PlanningWorkItem? workItem,
        PlanningRepositoryTarget? repositoryTarget,
        IReadOnlyList<PlanningScopeClause>? includedScope,
        IReadOnlyList<PlanningScopeClause>? constraints,
        IReadOnlyList<PlanningScopeClause>? forbiddenScope,
        IReadOnlyList<PlanningDeliverable>? deliverables,
        IReadOnlyList<PlanningValidationRequirement>? validationRequirements,
        IReadOnlyList<PlanningAcceptanceCriterion>? acceptanceCriteria,
        IReadOnlyList<PlanningExecutionBudget>? executionBudgets,
        IReadOnlyList<PlanningStopCondition>? stopConditions,
        int schemaVersion = PlanningExecutionContractSchema.CurrentVersion,
        string? previousContentHash = null)
    {
        ProjectId = projectId;
        ContractId = contractId;
        Revision = revision;
        OwnerReference = ownerReference;
        PlannerAgentId = plannerAgentId;
        WorkItem = workItem;
        RepositoryTarget = repositoryTarget;
        IncludedScope = includedScope;
        Constraints = constraints;
        ForbiddenScope = forbiddenScope;
        Deliverables = deliverables;
        ValidationRequirements = validationRequirements;
        AcceptanceCriteria = acceptanceCriteria;
        ExecutionBudgets = executionBudgets;
        StopConditions = stopConditions;
        SchemaVersion = schemaVersion;
        PreviousContentHash = previousContentHash;
    }

    public Guid ProjectId { get; }

    public Guid ContractId { get; }

    public int Revision { get; }

    public int SchemaVersion { get; }

    public string OwnerReference { get; }

    public Guid PlannerAgentId { get; }

    public PlanningWorkItem? WorkItem { get; }

    public PlanningRepositoryTarget? RepositoryTarget { get; }

    public IReadOnlyList<PlanningScopeClause>? IncludedScope { get; }

    public IReadOnlyList<PlanningScopeClause>? Constraints { get; }

    public IReadOnlyList<PlanningScopeClause>? ForbiddenScope { get; }

    public IReadOnlyList<PlanningDeliverable>? Deliverables { get; }

    public IReadOnlyList<PlanningValidationRequirement>? ValidationRequirements { get; }

    public IReadOnlyList<PlanningAcceptanceCriterion>? AcceptanceCriteria { get; }

    public IReadOnlyList<PlanningExecutionBudget>? ExecutionBudgets { get; }

    public IReadOnlyList<PlanningStopCondition>? StopConditions { get; }

    /// <summary>Optional caller evidence; when supplied it must match the durable predecessor.</summary>
    public string? PreviousContentHash { get; }
}

public enum PlanningExecutionContractCreationStatus
{
    Created,
    ProjectNotFound,
    ContextNotReady,
    PlannerNotFound,
    PlannerNotAuthorized,
    InvalidContract,
    PredecessorMissing,
    PredecessorMismatch,
    RevisionConflict,
    PersistenceUnavailable
}

public sealed record PlanningExecutionContractCreationResult(
    PlanningExecutionContractCreationStatus Status,
    PlanningExecutionContract? Contract = null,
    ProjectContextResolutionState? ContextState = null,
    string? ErrorMessage = null)
{
    public bool Succeeded => Status == PlanningExecutionContractCreationStatus.Created && Contract is not null;
}

public interface IPlanningExecutionContractService
{
    Task<PlanningExecutionContractCreationResult> CreateAsync(
        PlanningExecutionContractRequest request,
        CancellationToken cancellationToken = default);
}
