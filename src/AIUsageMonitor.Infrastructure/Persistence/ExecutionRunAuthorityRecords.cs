using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Handoffs;
using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Routing;
using AIUsageMonitor.Application.Workspaces;

namespace AIUsageMonitor.Infrastructure.Persistence;

/// <summary>Persistence-only DTO for one immutable execution-run authority.</summary>
public sealed class ExecutionRunAuthorityRecord
{
    public int SchemaVersion { get; set; }
    public string RecordType { get; set; } = "execution-run-authority";
    public Guid ProjectId { get; set; }
    public Guid RunId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public PlanningExecutionContractReferenceRecord PlanningContractReference { get; set; } = new();
    public Guid? WorkGraphId { get; set; }
    public int? WorkGraphSchemaVersion { get; set; }
    public string? WorkGraphContentHash { get; set; }
    public Guid? WorkGraphNodeId { get; set; }
    public Guid HandoffPackageId { get; set; }
    public int HandoffPackageSchemaVersion { get; set; }
    public string HandoffPackageContentHash { get; set; } = string.Empty;
    public Guid RoutingDecisionId { get; set; }
    public int RoutingDecisionSchemaVersion { get; set; }
    public string RoutingDecisionContentHash { get; set; } = string.Empty;
    public Guid WorkspacePlanId { get; set; }
    public Guid WorkspacePlanProjectId { get; set; }
    public int WorkspacePlanSchemaVersion { get; set; }
    public string WorkspacePlanContentHash { get; set; } = string.Empty;
    public Guid WorkspaceId { get; set; }
    public string WorkspacePath { get; set; } = string.Empty;
    public string WorkspaceReceiptContentHash { get; set; } = string.Empty;
    public Guid InputRecoveryCheckpointId { get; set; }
    public int InputRecoveryCheckpointSchemaVersion { get; set; }
    public string InputRecoveryCheckpointContentHash { get; set; } = string.Empty;
    public Guid AgentId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ModelIdentifier { get; set; } = string.Empty;
    public AgentConnectionMode ConnectionMode { get; set; }
    public string AdapterIdentifier { get; set; } = string.Empty;
    public Dictionary<PlanningBudgetKind, long> Budgets { get; set; } = [];
    public string ContentHash { get; set; } = string.Empty;

    public static ExecutionRunAuthorityRecord FromApplication(ExecutionRunAuthority value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new()
        {
            SchemaVersion = value.SchemaVersion,
            ProjectId = value.ProjectId,
            RunId = value.RunId,
            CreatedAt = value.CreatedAt,
            PlanningContractReference = new()
            {
                ContractId = value.PlanningContractReference.ContractId,
                Revision = value.PlanningContractReference.Revision,
                SchemaVersion = value.PlanningContractReference.SchemaVersion,
                ContentHash = value.PlanningContractReference.ContentHash
            },
            WorkGraphId = value.WorkGraphReference?.GraphId,
            WorkGraphSchemaVersion = value.WorkGraphReference?.SchemaVersion,
            WorkGraphContentHash = value.WorkGraphReference?.ContentHash,
            WorkGraphNodeId = value.WorkGraphNodeId,
            HandoffPackageId = value.HandoffPackageReference.PackageId,
            HandoffPackageSchemaVersion = value.HandoffPackageReference.SchemaVersion,
            HandoffPackageContentHash = value.HandoffPackageReference.ContentHash,
            RoutingDecisionId = value.RoutingDecisionReference.DecisionId,
            RoutingDecisionSchemaVersion = value.RoutingDecisionReference.SchemaVersion,
            RoutingDecisionContentHash = value.RoutingDecisionReference.ContentHash,
            WorkspacePlanId = value.WorkspacePreparationPlanReference.PlanId,
            WorkspacePlanProjectId = value.WorkspacePreparationPlanReference.ProjectId,
            WorkspacePlanSchemaVersion = value.WorkspacePreparationPlanReference.SchemaVersion,
            WorkspacePlanContentHash = value.WorkspacePreparationPlanReference.ContentHash,
            WorkspaceId = value.WorkspaceId,
            WorkspacePath = value.WorkspacePath,
            WorkspaceReceiptContentHash = value.WorkspaceReceiptContentHash,
            InputRecoveryCheckpointId = value.InputRecoveryCheckpointReference.CheckpointId,
            InputRecoveryCheckpointSchemaVersion = value.InputRecoveryCheckpointReference.SchemaVersion,
            InputRecoveryCheckpointContentHash = value.InputRecoveryCheckpointReference.ContentHash,
            AgentId = value.AgentId,
            Provider = value.Provider,
            ModelIdentifier = value.ModelIdentifier,
            ConnectionMode = value.ConnectionMode,
            AdapterIdentifier = value.AdapterIdentifier,
            Budgets = value.Budgets.ToDictionary().ToDictionary(pair => pair.Key, pair => pair.Value),
            ContentHash = value.ContentHash
        };
    }

    public ExecutionRunAuthority ToApplicationForIntegrityValidation() => ToApplication(null);

    public ExecutionRunAuthority ToApplication() => ToApplication(ContentHash);

    private ExecutionRunAuthority ToApplication(string? contentHash)
    {
        if (PlanningContractReference is null)
        {
            throw new ArgumentException("Planning contract reference is missing.");
        }

        var budget = ToBudgetEnvelope();
        var graphReference = WorkGraphId is null
            ? throw new ArgumentException("Work-graph reference is missing.")
            : new WorkGraphReference(
                WorkGraphId.Value,
                WorkGraphSchemaVersion ?? throw new ArgumentException("Work-graph schema version is missing."),
                WorkGraphContentHash ?? throw new ArgumentException("Work-graph content hash is missing."));
        var workspaceProjectId = WorkspacePlanProjectId == Guid.Empty ? ProjectId : WorkspacePlanProjectId;
        return new ExecutionRunAuthority(
            ProjectId,
            RunId,
            CreatedAt,
            PlanningContractReference.ToApplication(),
            graphReference,
            WorkGraphNodeId ?? throw new ArgumentException("Work-graph node id is missing."),
            new HandoffPackageReference(HandoffPackageId, HandoffPackageSchemaVersion, HandoffPackageContentHash),
            new RoutingDecisionReference(RoutingDecisionId, RoutingDecisionSchemaVersion, RoutingDecisionContentHash),
            new WorkspacePreparationPlanReference(WorkspacePlanId, WorkspacePlanSchemaVersion, WorkspacePlanContentHash, workspaceProjectId),
            WorkspaceId,
            WorkspacePath,
            WorkspaceReceiptContentHash,
            new RecoveryCheckpointReference(InputRecoveryCheckpointId, InputRecoveryCheckpointSchemaVersion, InputRecoveryCheckpointContentHash),
            AgentId,
            Provider,
            ModelIdentifier,
            ConnectionMode,
            AdapterIdentifier,
            budget,
            SchemaVersion,
            contentHash);
    }

    private ExecutionBudgetEnvelope ToBudgetEnvelope()
    {
        if (Budgets is null || !Budgets.TryGetValue(PlanningBudgetKind.Attempts, out var attempts) ||
            !Budgets.TryGetValue(PlanningBudgetKind.ElapsedMinutes, out var elapsedMinutes))
        {
            throw new ArgumentException("Required execution budgets are missing.");
        }

        return new ExecutionBudgetEnvelope(
            attempts,
            elapsedMinutes,
            GetOptional(PlanningBudgetKind.ChangedFiles),
            GetOptional(PlanningBudgetKind.ChangedLines),
            GetOptional(PlanningBudgetKind.ToolInvocations),
            GetOptional(PlanningBudgetKind.ModelTurns));

        long? GetOptional(PlanningBudgetKind kind) => Budgets.TryGetValue(kind, out var value) ? value : null;
    }
}
