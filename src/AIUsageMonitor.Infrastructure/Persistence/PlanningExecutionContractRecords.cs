using AIUsageMonitor.Application.Planning;

namespace AIUsageMonitor.Infrastructure.Persistence;

/// <summary>Persistence-only mapping for one immutable planning contract revision.</summary>
public sealed class PlanningExecutionContractRecord
{
    public int SchemaVersion { get; set; }
    public string RecordType { get; set; } = "planning-execution-contract";
    public Guid ProjectId { get; set; }
    public Guid ContractId { get; set; }
    public int Revision { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string OwnerReference { get; set; } = string.Empty;
    public Guid PlannerAgentId { get; set; }
    public PlanningContextBindingRecord Context { get; set; } = new();
    public PlanningWorkItemRecord WorkItem { get; set; } = new();
    public PlanningRepositoryTargetRecord RepositoryTarget { get; set; } = new();
    public List<PlanningScopeClauseRecord> IncludedScope { get; set; } = [];
    public List<PlanningScopeClauseRecord> Constraints { get; set; } = [];
    public List<PlanningScopeClauseRecord> ForbiddenScope { get; set; } = [];
    public List<PlanningDeliverableRecord> Deliverables { get; set; } = [];
    public List<PlanningValidationRequirementRecord> ValidationRequirements { get; set; } = [];
    public List<PlanningAcceptanceCriterionRecord> AcceptanceCriteria { get; set; } = [];
    public List<PlanningExecutionBudgetRecord> ExecutionBudgets { get; set; } = [];
    public List<PlanningStopConditionRecord> StopConditions { get; set; } = [];
    public List<string> GovernanceReferences { get; set; } = [];
    public string? RoutingPolicyReference { get; set; }
    public string? SafetyPolicyReference { get; set; }
    public int? PreviousRevision { get; set; }
    public string? PreviousContentHash { get; set; }
    public string ContentHash { get; set; } = string.Empty;

    public static PlanningExecutionContractRecord FromApplication(PlanningExecutionContract value) => new()
    {
        SchemaVersion = value.SchemaVersion,
        ProjectId = value.ProjectId,
        ContractId = value.ContractId,
        Revision = value.Revision,
        CreatedAt = value.CreatedAt,
        OwnerReference = value.OwnerReference,
        PlannerAgentId = value.PlannerAgentId,
        Context = PlanningContextBindingRecord.FromApplication(value.Context),
        WorkItem = PlanningWorkItemRecord.FromApplication(value.WorkItem),
        RepositoryTarget = PlanningRepositoryTargetRecord.FromApplication(value.RepositoryTarget),
        IncludedScope = value.IncludedScope.Select(PlanningScopeClauseRecord.FromApplication).ToList(),
        Constraints = value.Constraints.Select(PlanningScopeClauseRecord.FromApplication).ToList(),
        ForbiddenScope = value.ForbiddenScope.Select(PlanningScopeClauseRecord.FromApplication).ToList(),
        Deliverables = value.Deliverables.Select(PlanningDeliverableRecord.FromApplication).ToList(),
        ValidationRequirements = value.ValidationRequirements.Select(PlanningValidationRequirementRecord.FromApplication).ToList(),
        AcceptanceCriteria = value.AcceptanceCriteria.Select(PlanningAcceptanceCriterionRecord.FromApplication).ToList(),
        ExecutionBudgets = value.ExecutionBudgets.Select(PlanningExecutionBudgetRecord.FromApplication).ToList(),
        StopConditions = value.StopConditions.Select(PlanningStopConditionRecord.FromApplication).ToList(),
        GovernanceReferences = value.GovernanceReferences.ToList(),
        RoutingPolicyReference = value.RoutingPolicyReference,
        SafetyPolicyReference = value.SafetyPolicyReference,
        PreviousRevision = value.PreviousRevision,
        PreviousContentHash = value.PreviousContentHash,
        ContentHash = value.ContentHash
    };

    public PlanningExecutionContract ToApplication() => new(
        ProjectId,
        ContractId,
        SchemaVersion,
        Revision,
        CreatedAt,
        OwnerReference,
        PlannerAgentId,
        Context.ToApplication(),
        WorkItem.ToApplication(),
        RepositoryTarget.ToApplication(),
        IncludedScope.Select(static value => value.ToApplication()).ToArray(),
        Constraints.Select(static value => value.ToApplication()).ToArray(),
        ForbiddenScope.Select(static value => value.ToApplication()).ToArray(),
        Deliverables.Select(static value => value.ToApplication()).ToArray(),
        ValidationRequirements.Select(static value => value.ToApplication()).ToArray(),
        AcceptanceCriteria.Select(static value => value.ToApplication()).ToArray(),
        ExecutionBudgets.Select(static value => value.ToApplication()).ToArray(),
        StopConditions.Select(static value => value.ToApplication()).ToArray(),
        GovernanceReferences,
        RoutingPolicyReference,
        SafetyPolicyReference,
        PreviousRevision,
        PreviousContentHash,
        ContentHash);
}

public sealed class PlanningContextBindingRecord
{
    public Guid ProjectContextId { get; set; }
    public int ProjectContextContractVersion { get; set; }

    public static PlanningContextBindingRecord FromApplication(PlanningContextBinding value) => new()
    {
        ProjectContextId = value.ProjectContextId,
        ProjectContextContractVersion = value.ProjectContextContractVersion
    };

    public PlanningContextBinding ToApplication() => new(ProjectContextId, ProjectContextContractVersion);
}

public sealed class PlanningWorkItemRecord
{
    public PlanningWorkItemSource Source { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    public static PlanningWorkItemRecord FromApplication(PlanningWorkItem value) => new()
    {
        Source = value.Source,
        Reference = value.Reference,
        Title = value.Title
    };

    public PlanningWorkItem ToApplication() => new(Source, Reference, Title);
}

public sealed class PlanningRepositoryTargetRecord
{
    public PlanningRepositoryMode Mode { get; set; }
    public string? RegisteredLocalPath { get; set; }
    public string? ExpectedBranch { get; set; }
    public string? ExpectedHeadCommit { get; set; }

    public static PlanningRepositoryTargetRecord FromApplication(PlanningRepositoryTarget value) => new()
    {
        Mode = value.Mode,
        RegisteredLocalPath = value.RegisteredLocalPath,
        ExpectedBranch = value.ExpectedBranch,
        ExpectedHeadCommit = value.ExpectedHeadCommit
    };

    public PlanningRepositoryTarget ToApplication() =>
        new(Mode, RegisteredLocalPath, ExpectedBranch, ExpectedHeadCommit);
}

public sealed class PlanningScopeClauseRecord
{
    public string Id { get; set; } = string.Empty;
    public string Statement { get; set; } = string.Empty;

    public static PlanningScopeClauseRecord FromApplication(PlanningScopeClause value) => new()
    {
        Id = value.Id,
        Statement = value.Statement
    };

    public PlanningScopeClause ToApplication() => new(Id, Statement);
}

public sealed class PlanningDeliverableRecord
{
    public string DeliverableId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; }

    public static PlanningDeliverableRecord FromApplication(PlanningDeliverable value) => new()
    {
        DeliverableId = value.DeliverableId,
        Description = value.Description,
        Required = value.Required
    };

    public PlanningDeliverable ToApplication() => new(DeliverableId, Description, Required);
}

public sealed class PlanningValidationRequirementRecord
{
    public string ValidationId { get; set; } = string.Empty;
    public PlanningValidationKind Kind { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; }
    public string? CommandOrReference { get; set; }

    public static PlanningValidationRequirementRecord FromApplication(PlanningValidationRequirement value) => new()
    {
        ValidationId = value.ValidationId,
        Kind = value.Kind,
        Description = value.Description,
        Required = value.Required,
        CommandOrReference = value.CommandOrReference
    };

    public PlanningValidationRequirement ToApplication() =>
        new(ValidationId, Kind, Description, Required, CommandOrReference);
}

public sealed class PlanningAcceptanceCriterionRecord
{
    public string CriterionId { get; set; } = string.Empty;
    public string Statement { get; set; } = string.Empty;
    public bool Required { get; set; }

    public static PlanningAcceptanceCriterionRecord FromApplication(PlanningAcceptanceCriterion value) => new()
    {
        CriterionId = value.CriterionId,
        Statement = value.Statement,
        Required = value.Required
    };

    public PlanningAcceptanceCriterion ToApplication() => new(CriterionId, Statement, Required);
}

public sealed class PlanningExecutionBudgetRecord
{
    public PlanningBudgetKind Kind { get; set; }
    public long Limit { get; set; }

    public static PlanningExecutionBudgetRecord FromApplication(PlanningExecutionBudget value) => new()
    {
        Kind = value.Kind,
        Limit = value.Limit
    };

    public PlanningExecutionBudget ToApplication() => new(Kind, Limit);
}

public sealed class PlanningStopConditionRecord
{
    public string ConditionId { get; set; } = string.Empty;
    public PlanningStopConditionKind Kind { get; set; }
    public string Description { get; set; } = string.Empty;

    public static PlanningStopConditionRecord FromApplication(PlanningStopCondition value) => new()
    {
        ConditionId = value.ConditionId,
        Kind = value.Kind,
        Description = value.Description
    };

    public PlanningStopCondition ToApplication() => new(ConditionId, Kind, Description);
}
