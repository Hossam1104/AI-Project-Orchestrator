using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Handoffs;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Routing;
using AIUsageMonitor.Application.Workspaces;

namespace AIUsageMonitor.Application.Orchestration;

/// <summary>Semantic version of the immutable one-run anti-replay authority.</summary>
public static class ExecutionRunAuthoritySchema
{
    public const int CurrentVersion = 1;
}

public static class ExecutionRunAuthorityLimits
{
    public const int MaxCanonicalPayloadBytes = 128 * 1024;
    public const int MaxAdapterIdentifierLength = 200;
    public const int MaxProviderLength = 300;
    public const int MaxModelIdentifierLength = 500;
    public const int MaxWorkspacePathLength = 2_000;
}

/// <summary>
/// Exact bounded budget values copied from an immutable planning contract. This is a data
/// envelope only; it does not imply that a metric can be self-certified by an adapter.
/// </summary>
public sealed class ExecutionBudgetEnvelope
{
    public ExecutionBudgetEnvelope(
        long attempts,
        long elapsedMinutes,
        long? changedFiles = null,
        long? changedLines = null,
        long? toolInvocations = null,
        long? modelTurns = null)
    {
        ValidatePositive(attempts, nameof(attempts));
        ValidatePositive(elapsedMinutes, nameof(elapsedMinutes));
        ValidateOptional(changedFiles, nameof(changedFiles));
        ValidateOptional(changedLines, nameof(changedLines));
        ValidateOptional(toolInvocations, nameof(toolInvocations));
        ValidateOptional(modelTurns, nameof(modelTurns));

        Attempts = attempts;
        ElapsedMinutes = elapsedMinutes;
        ChangedFiles = changedFiles;
        ChangedLines = changedLines;
        ToolInvocations = toolInvocations;
        ModelTurns = modelTurns;
    }

    public long Attempts { get; }
    public long ElapsedMinutes { get; }
    public long? ChangedFiles { get; }
    public long? ChangedLines { get; }
    public long? ToolInvocations { get; }
    public long? ModelTurns { get; }

    public bool TryGetLimit(PlanningBudgetKind kind, out long limit)
    {
        limit = kind switch
        {
            PlanningBudgetKind.Attempts => Attempts,
            PlanningBudgetKind.ElapsedMinutes => ElapsedMinutes,
            PlanningBudgetKind.ChangedFiles => ChangedFiles ?? 0,
            PlanningBudgetKind.ChangedLines => ChangedLines ?? 0,
            PlanningBudgetKind.ToolInvocations => ToolInvocations ?? 0,
            PlanningBudgetKind.ModelTurns => ModelTurns ?? 0,
            _ => 0
        };
        return kind switch
        {
            PlanningBudgetKind.Attempts or PlanningBudgetKind.ElapsedMinutes => true,
            PlanningBudgetKind.ChangedFiles => ChangedFiles.HasValue,
            PlanningBudgetKind.ChangedLines => ChangedLines.HasValue,
            PlanningBudgetKind.ToolInvocations => ToolInvocations.HasValue,
            PlanningBudgetKind.ModelTurns => ModelTurns.HasValue,
            _ => false
        };
    }

    public IReadOnlyDictionary<PlanningBudgetKind, long> ToDictionary()
    {
        var values = new Dictionary<PlanningBudgetKind, long>
        {
            [PlanningBudgetKind.Attempts] = Attempts,
            [PlanningBudgetKind.ElapsedMinutes] = ElapsedMinutes
        };
        AddIfPresent(values, PlanningBudgetKind.ChangedFiles, ChangedFiles);
        AddIfPresent(values, PlanningBudgetKind.ChangedLines, ChangedLines);
        AddIfPresent(values, PlanningBudgetKind.ToolInvocations, ToolInvocations);
        AddIfPresent(values, PlanningBudgetKind.ModelTurns, ModelTurns);
        return values;
    }

    public static bool TryCreate(
        IReadOnlyList<PlanningExecutionBudget> budgets,
        out ExecutionBudgetEnvelope? envelope,
        out string? errorMessage)
    {
        envelope = null;
        errorMessage = null;
        if (budgets is null)
        {
            errorMessage = "Execution budgets are required.";
            return false;
        }

        var values = new Dictionary<PlanningBudgetKind, long>();
        foreach (var budget in budgets)
        {
            if (budget is null || !values.TryAdd(budget.Kind, budget.Limit))
            {
                errorMessage = "Execution budgets must be unique and valid.";
                return false;
            }
        }

        if (!values.TryGetValue(PlanningBudgetKind.Attempts, out var attempts) || attempts < 1)
        {
            errorMessage = "An Attempts budget of at least one is required.";
            return false;
        }

        if (!values.TryGetValue(PlanningBudgetKind.ElapsedMinutes, out var elapsedMinutes) || elapsedMinutes < 1)
        {
            errorMessage = "An ElapsedMinutes budget is required.";
            return false;
        }

        envelope = new ExecutionBudgetEnvelope(
            attempts,
            elapsedMinutes,
            OptionalValue(PlanningBudgetKind.ChangedFiles),
            OptionalValue(PlanningBudgetKind.ChangedLines),
            OptionalValue(PlanningBudgetKind.ToolInvocations),
            OptionalValue(PlanningBudgetKind.ModelTurns));
        return true;

        long? OptionalValue(PlanningBudgetKind kind) => values.TryGetValue(kind, out var value) ? value : null;
    }

    private static void AddIfPresent(
        IDictionary<PlanningBudgetKind, long> values,
        PlanningBudgetKind kind,
        long? value)
    {
        if (value.HasValue)
        {
            values[kind] = value.Value;
        }
    }

    private static void ValidatePositive(long value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Budget limits must be positive.");
        }
    }

    private static void ValidateOptional(long? value, string parameterName)
    {
        if (value is <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Budget limits must be positive when supplied.");
        }
    }
}

/// <summary>Content-integrity reference for one immutable execution-run authority.</summary>
public sealed class ExecutionRunAuthorityReference
{
    public ExecutionRunAuthorityReference(Guid runId, int schemaVersion, string contentHash)
    {
        if (runId == Guid.Empty)
        {
            throw new ArgumentException("Run id cannot be empty.", nameof(runId));
        }

        if (schemaVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        }

        if (!IsSha256(contentHash))
        {
            throw new ArgumentException("Run-authority content hash must be SHA-256 evidence.", nameof(contentHash));
        }

        RunId = runId;
        SchemaVersion = schemaVersion;
        ContentHash = contentHash.ToLowerInvariant();
    }

    public Guid RunId { get; }
    public int SchemaVersion { get; }
    public string ContentHash { get; }

    public override string ToString() =>
        $"execution-run:{RunId:D}/schema:{SchemaVersion}/sha256:{ContentHash}";

    public static bool IsSha256(string? value) =>
        value is { Length: 64 } && value.All(Uri.IsHexDigit);
}

/// <summary>
/// Immutable authority for exactly one bounded adapter invocation. It contains references and
/// bounded identity/budget metadata only; it never contains prompts, source, output, or secrets.
/// </summary>
public sealed class ExecutionRunAuthority
{
    public ExecutionRunAuthority(
        Guid projectId,
        Guid runId,
        DateTimeOffset createdAt,
        PlanningExecutionContractReference planningContractReference,
        WorkGraphReference workGraphReference,
        Guid workGraphNodeId,
        HandoffPackageReference handoffPackageReference,
        RoutingDecisionReference routingDecisionReference,
        WorkspacePreparationPlanReference workspacePreparationPlanReference,
        Guid workspaceId,
        string workspacePath,
        string workspaceReceiptContentHash,
        RecoveryCheckpointReference inputRecoveryCheckpointReference,
        Guid agentId,
        string provider,
        string modelIdentifier,
        AgentConnectionMode connectionMode,
        string adapterIdentifier,
        ExecutionBudgetEnvelope budgets,
        int schemaVersion = ExecutionRunAuthoritySchema.CurrentVersion,
        string? contentHash = null)
    {
        if (projectId == Guid.Empty || runId == Guid.Empty || workspaceId == Guid.Empty || agentId == Guid.Empty)
        {
            throw new ArgumentException("Project, run, workspace, and agent identifiers are required.");
        }

        if (schemaVersion != ExecutionRunAuthoritySchema.CurrentVersion)
        {
            throw new ArgumentException(
                $"Only run-authority schema {ExecutionRunAuthoritySchema.CurrentVersion} is supported.",
                nameof(schemaVersion));
        }

        if (createdAt == default)
        {
            throw new ArgumentException("Run-authority creation time is required.", nameof(createdAt));
        }

        PlanningContractReferenceRequired(planningContractReference);
        WorkGraphReference = workGraphReference ?? throw new ArgumentNullException(nameof(workGraphReference));
        if (workGraphNodeId == Guid.Empty)
        {
            throw new ArgumentException("Work-graph node id cannot be empty.", nameof(workGraphNodeId));
        }

        HandoffPackageReference = handoffPackageReference ?? throw new ArgumentNullException(nameof(handoffPackageReference));
        RoutingDecisionReference = routingDecisionReference ?? throw new ArgumentNullException(nameof(routingDecisionReference));
        WorkspacePreparationPlanReference = workspacePreparationPlanReference ?? throw new ArgumentNullException(nameof(workspacePreparationPlanReference));
        if (workspacePreparationPlanReference.ProjectId != Guid.Empty && workspacePreparationPlanReference.ProjectId != projectId)
        {
            throw new ArgumentException("Workspace plan reference belongs to another project.", nameof(workspacePreparationPlanReference));
        }

        WorkspacePath = RequiredText(workspacePath, nameof(workspacePath), ExecutionRunAuthorityLimits.MaxWorkspacePathLength);
        if (!ExecutionRunAuthorityReference.IsSha256(workspaceReceiptContentHash))
        {
            throw new ArgumentException("Workspace receipt content hash must be SHA-256 evidence.", nameof(workspaceReceiptContentHash));
        }

        WorkspaceReceiptContentHash = workspaceReceiptContentHash.ToLowerInvariant();
        InputRecoveryCheckpointReference = inputRecoveryCheckpointReference ?? throw new ArgumentNullException(nameof(inputRecoveryCheckpointReference));
        Provider = RequiredText(provider, nameof(provider), ExecutionRunAuthorityLimits.MaxProviderLength);
        ModelIdentifier = RequiredText(modelIdentifier, nameof(modelIdentifier), ExecutionRunAuthorityLimits.MaxModelIdentifierLength);
        AdapterIdentifier = RequiredText(adapterIdentifier, nameof(adapterIdentifier), ExecutionRunAuthorityLimits.MaxAdapterIdentifierLength);
        if (!Enum.IsDefined(connectionMode))
        {
            throw new ArgumentException("Connection mode is undefined.", nameof(connectionMode));
        }

        Budgets = budgets ?? throw new ArgumentNullException(nameof(budgets));
        ProjectId = projectId;
        RunId = runId;
        CreatedAt = createdAt;
        SchemaVersion = schemaVersion;
        PlanningContractReference = planningContractReference;
        WorkGraphNodeId = workGraphNodeId;
        WorkspaceId = workspaceId;
        AgentId = agentId;
        ConnectionMode = connectionMode;

        if (ExecutionRunAuthorityIntegrity.ComputeCanonicalPayloadBytes(this) > ExecutionRunAuthorityLimits.MaxCanonicalPayloadBytes)
        {
            throw new ArgumentException("Run-authority canonical payload exceeds its supported bound.", nameof(budgets));
        }

        var calculatedHash = ExecutionRunAuthorityIntegrity.ComputeContentHash(this);
        if (contentHash is not null &&
            (!ExecutionRunAuthorityReference.IsSha256(contentHash) ||
             !string.Equals(calculatedHash, contentHash, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Run-authority content hash does not match the payload.", nameof(contentHash));
        }

        ContentHash = calculatedHash;
        Reference = new ExecutionRunAuthorityReference(RunId, SchemaVersion, ContentHash);
    }

    public Guid ProjectId { get; }
    public Guid RunId { get; }
    public int SchemaVersion { get; }
    public DateTimeOffset CreatedAt { get; }
    public PlanningExecutionContractReference PlanningContractReference { get; }
    public WorkGraphReference WorkGraphReference { get; }
    public Guid WorkGraphNodeId { get; }
    public HandoffPackageReference HandoffPackageReference { get; }
    public RoutingDecisionReference RoutingDecisionReference { get; }
    public WorkspacePreparationPlanReference WorkspacePreparationPlanReference { get; }
    public Guid WorkspaceId { get; }
    public string WorkspacePath { get; }
    public string WorkspaceReceiptContentHash { get; }
    public RecoveryCheckpointReference InputRecoveryCheckpointReference { get; }
    public Guid AgentId { get; }
    public string Provider { get; }
    public string ModelIdentifier { get; }
    public AgentConnectionMode ConnectionMode { get; }
    public string AdapterIdentifier { get; }
    public ExecutionBudgetEnvelope Budgets { get; }
    public string ContentHash { get; private set; }
    public ExecutionRunAuthorityReference Reference { get; private set; }

    private static void PlanningContractReferenceRequired(PlanningExecutionContractReference? value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!ExecutionRunAuthorityReference.IsSha256(value.ContentHash) || value.ContractId == Guid.Empty || value.Revision <= 0)
        {
            throw new ArgumentException("A complete planning contract reference is required.", nameof(value));
        }
    }

    private static string RequiredText(string value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A bounded text value is required.", parameterName);
        }

        var normalized = value.Trim();
        return normalized.Length <= maximumLength
            ? normalized
            : throw new ArgumentException($"The value cannot exceed {maximumLength} characters.", parameterName);
    }
}

public static class ExecutionRunAuthorityIntegrity
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string ComputeContentHash(ExecutionRunAuthority authority)
    {
        ArgumentNullException.ThrowIfNull(authority);
        var json = JsonSerializer.Serialize(CreateCanonicalPayload(authority), Options);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    public static int ComputeCanonicalPayloadBytes(ExecutionRunAuthority authority)
    {
        ArgumentNullException.ThrowIfNull(authority);
        return JsonSerializer.SerializeToUtf8Bytes(CreateCanonicalPayload(authority), Options).Length;
    }

    internal static object CreateCanonicalPayload(ExecutionRunAuthority authority) => new
    {
        authority.ProjectId,
        authority.RunId,
        authority.SchemaVersion,
        authority.CreatedAt,
        planningContractReference = ContractReferencePayload(authority.PlanningContractReference),
        workGraphReference = new
        {
            authority.WorkGraphReference.GraphId,
            authority.WorkGraphReference.SchemaVersion,
            authority.WorkGraphReference.ContentHash
        },
        authority.WorkGraphNodeId,
        handoffPackageReference = new
        {
            authority.HandoffPackageReference.PackageId,
            authority.HandoffPackageReference.SchemaVersion,
            authority.HandoffPackageReference.ContentHash
        },
        routingDecisionReference = new
        {
            authority.RoutingDecisionReference.DecisionId,
            authority.RoutingDecisionReference.SchemaVersion,
            authority.RoutingDecisionReference.ContentHash
        },
        workspacePreparationPlanReference = new
        {
            authority.WorkspacePreparationPlanReference.ProjectId,
            authority.WorkspacePreparationPlanReference.PlanId,
            authority.WorkspacePreparationPlanReference.SchemaVersion,
            authority.WorkspacePreparationPlanReference.ContentHash
        },
        authority.WorkspaceId,
        authority.WorkspacePath,
        authority.WorkspaceReceiptContentHash,
        inputRecoveryCheckpointReference = new
        {
            authority.InputRecoveryCheckpointReference.CheckpointId,
            authority.InputRecoveryCheckpointReference.SchemaVersion,
            authority.InputRecoveryCheckpointReference.ContentHash
        },
        authority.AgentId,
        authority.Provider,
        authority.ModelIdentifier,
        authority.ConnectionMode,
        authority.AdapterIdentifier,
        budgets = authority.Budgets.ToDictionary()
            .OrderBy(static pair => pair.Key)
            .ToDictionary(static pair => pair.Key, static pair => pair.Value)
    };

    private static object ContractReferencePayload(PlanningExecutionContractReference value) => new
    {
        value.ContractId,
        value.Revision,
        value.SchemaVersion,
        value.ContentHash
    };
}
