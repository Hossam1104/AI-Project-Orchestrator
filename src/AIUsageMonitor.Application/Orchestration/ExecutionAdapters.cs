using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Handoffs;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Workspaces;

namespace AIUsageMonitor.Application.Orchestration;

public enum ExecutionAdapterOutcome
{
    Succeeded,
    Failed,
    Cancelled,
    TimedOut,
    BudgetExceeded,
    Unsupported,
    AuthenticationRequired,
    AdapterUnavailable,
    InvalidResult
}

/// <summary>Bounded metrics reported by an adapter. Values are evidence, not acceptance.</summary>
public sealed class ExecutionAdapterUsageMetrics
{
    public ExecutionAdapterUsageMetrics(
        long? toolInvocations = null,
        long? modelTurns = null,
        long? changedFiles = null,
        long? changedLines = null)
    {
        Validate(toolInvocations, nameof(toolInvocations));
        Validate(modelTurns, nameof(modelTurns));
        Validate(changedFiles, nameof(changedFiles));
        Validate(changedLines, nameof(changedLines));
        ToolInvocations = toolInvocations;
        ModelTurns = modelTurns;
        ChangedFiles = changedFiles;
        ChangedLines = changedLines;
    }

    public long? ToolInvocations { get; }
    public long? ModelTurns { get; }
    public long? ChangedFiles { get; }
    public long? ChangedLines { get; }

    private static void Validate(long? value, string parameterName)
    {
        if (value is < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Usage metrics cannot be negative.");
        }
    }
}

public sealed class ExecutionAdapterResult
{
    public ExecutionAdapterResult(
        ExecutionAdapterOutcome outcome,
        string? sanitizedSummary = null,
        string? stopReason = null,
        ExecutionAdapterUsageMetrics? usage = null,
        bool mayHaveModifiedWorkspace = false,
        IReadOnlyList<string>? evidenceReferences = null)
    {
        if (!Enum.IsDefined(outcome))
        {
            throw new ArgumentException("Adapter outcome is undefined.", nameof(outcome));
        }

        Outcome = outcome;
        Summary = NormalizeOptional(sanitizedSummary, nameof(sanitizedSummary), 2_000);
        StopReason = NormalizeOptional(stopReason, nameof(stopReason), 1_000);
        Usage = usage;
        MayHaveModifiedWorkspace = mayHaveModifiedWorkspace;
        EvidenceReferences = NormalizeReferences(evidenceReferences);
    }

    public ExecutionAdapterOutcome Outcome { get; }
    public string? Summary { get; }
    public string? StopReason { get; }
    public ExecutionAdapterUsageMetrics? Usage { get; }
    public bool MayHaveModifiedWorkspace { get; }
    public IReadOnlyList<string> EvidenceReferences { get; }

    private static string? NormalizeOptional(string? value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maximumLength
            ? normalized
            : throw new ArgumentException($"The value cannot exceed {maximumLength} characters.", parameterName);
    }

    private static IReadOnlyList<string> NormalizeReferences(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<string>();
        }

        if (values.Count > 32)
        {
            throw new ArgumentException("Adapter evidence references exceed the supported bound.", nameof(values));
        }

        return values.Select(value => NormalizeOptional(value, nameof(values), 1_000)!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}

/// <summary>
/// Stable, provider-independent metadata used to decide whether an internal adapter is exact.
/// It contains no credentials and does not perform routing.
/// </summary>
public sealed class ExecutionAdapterDescriptor
{
    public ExecutionAdapterDescriptor(
        string adapterIdentifier,
        IReadOnlyList<AgentConnectionMode> supportedConnectionModes,
        IReadOnlyList<PlanningBudgetKind>? supportedBudgetMetrics = null,
        IReadOnlyList<string>? supportedProviders = null,
        IReadOnlyList<string>? supportedModels = null,
        bool supportsCancellation = true)
    {
        AdapterIdentifier = Required(adapterIdentifier, nameof(adapterIdentifier), 200);
        ArgumentNullException.ThrowIfNull(supportedConnectionModes);
        if (supportedConnectionModes.Count == 0 || supportedConnectionModes.Any(value => !Enum.IsDefined(value)))
        {
            throw new ArgumentException("At least one supported connection mode is required.", nameof(supportedConnectionModes));
        }

        SupportedConnectionModes = supportedConnectionModes.Distinct().OrderBy(static value => value).ToArray();
        SupportedBudgetMetrics = NormalizeBudgetMetrics(supportedBudgetMetrics);
        SupportedProviders = NormalizeValues(supportedProviders, nameof(supportedProviders), 300);
        SupportedModels = NormalizeValues(supportedModels, nameof(supportedModels), 500);
        SupportsCancellation = supportsCancellation;
    }

    public string AdapterIdentifier { get; }
    public IReadOnlyList<AgentConnectionMode> SupportedConnectionModes { get; }
    public IReadOnlyList<PlanningBudgetKind> SupportedBudgetMetrics { get; }
    public IReadOnlyList<string> SupportedProviders { get; }
    public IReadOnlyList<string> SupportedModels { get; }
    public bool SupportsCancellation { get; }

    public bool Matches(EffectiveAgentDefinition agent)
    {
        ArgumentNullException.ThrowIfNull(agent);
        return SupportsCancellation &&
            SupportedConnectionModes.Contains(agent.ConnectionMode) &&
            (SupportedProviders.Count == 0 || (agent.Provider is not null && SupportedProviders.Contains(agent.Provider, StringComparer.OrdinalIgnoreCase))) &&
            (SupportedModels.Count == 0 || (agent.ModelIdentifier is not null && SupportedModels.Contains(agent.ModelIdentifier, StringComparer.Ordinal)));
    }

    private static IReadOnlyList<PlanningBudgetKind> NormalizeBudgetMetrics(IReadOnlyList<PlanningBudgetKind>? values)
    {
        var normalized = (values ?? Array.Empty<PlanningBudgetKind>()).Distinct().ToArray();
        return normalized.Any(value => !Enum.IsDefined(value))
            ? throw new ArgumentException("Adapter budget metrics contain an undefined value.", nameof(values))
            : normalized;
    }

    private static IReadOnlyList<string> NormalizeValues(IReadOnlyList<string>? values, string parameterName, int maximumLength)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<string>();
        }

        return values.Select(value => Required(value, parameterName, maximumLength))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static string Required(string value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A bounded adapter value is required.", parameterName);
        }

        var normalized = value.Trim();
        return normalized.Length <= maximumLength
            ? normalized
            : throw new ArgumentException($"The value cannot exceed {maximumLength} characters.", parameterName);
    }
}

/// <summary>Exact execution context supplied to one adapter invocation.</summary>
public sealed class ExecutionAdapterRequest
{
    public ExecutionAdapterRequest(
        ExecutionRunAuthority authority,
        EffectiveAgentDefinition selectedAgent,
        PlanningExecutionContract contract,
        HandoffPackage handoff,
        WorkspacePreparationReceipt workspaceReceipt,
        CancellationToken cancellationToken)
    {
        Authority = authority ?? throw new ArgumentNullException(nameof(authority));
        SelectedAgent = selectedAgent ?? throw new ArgumentNullException(nameof(selectedAgent));
        Contract = contract ?? throw new ArgumentNullException(nameof(contract));
        Handoff = handoff ?? throw new ArgumentNullException(nameof(handoff));
        WorkspaceReceipt = workspaceReceipt ?? throw new ArgumentNullException(nameof(workspaceReceipt));
        CancellationToken = cancellationToken;
    }

    public ExecutionRunAuthority Authority { get; }
    public EffectiveAgentDefinition SelectedAgent { get; }
    public PlanningExecutionContract Contract { get; }
    public HandoffPackage Handoff { get; }
    public HandoffExecutionScope ExecutionScope => Handoff.ExecutionScope!;
    public WorkspacePreparationReceipt WorkspaceReceipt { get; }
    public string WorkspacePath => WorkspaceReceipt.WorkspacePath;
    public ExecutionBudgetEnvelope Budgets => Authority.Budgets;
    public CancellationToken CancellationToken { get; }
}

public interface IExecutionAdapter
{
    ExecutionAdapterDescriptor Descriptor { get; }

    Task<ExecutionAdapterResult> ExecuteAsync(
        ExecutionAdapterRequest request,
        CancellationToken cancellationToken = default);
}

public enum ExecutionAdapterResolutionStatus
{
    Resolved,
    Unsupported,
    ConfigurationConflict
}

public sealed record ExecutionAdapterResolution(
    ExecutionAdapterResolutionStatus Status,
    IExecutionAdapter? Adapter = null,
    string? ErrorMessage = null)
{
    public bool Succeeded => Status == ExecutionAdapterResolutionStatus.Resolved && Adapter is not null;
}

public interface IExecutionAdapterResolver
{
    ExecutionAdapterResolution Resolve(EffectiveAgentDefinition selectedAgent);
}

/// <summary>
/// Deterministic adapter lookup. It never ranks, falls back, changes the selected agent, or
/// invokes an adapter. With no registered production adapters it fails closed as Unsupported.
/// </summary>
public sealed class ExecutionAdapterResolver : IExecutionAdapterResolver
{
    private readonly IReadOnlyList<IExecutionAdapter> _adapters;

    public ExecutionAdapterResolver(IEnumerable<IExecutionAdapter> adapters)
    {
        ArgumentNullException.ThrowIfNull(adapters);
        _adapters = adapters.ToArray();
    }

    public ExecutionAdapterResolution Resolve(EffectiveAgentDefinition selectedAgent)
    {
        ArgumentNullException.ThrowIfNull(selectedAgent);
        var matches = _adapters
            .Where(adapter => adapter is not null && adapter.Descriptor.Matches(selectedAgent))
            .ToArray();
        return matches.Length switch
        {
            0 => new(ExecutionAdapterResolutionStatus.Unsupported, ErrorMessage: "No exact bounded execution adapter is available."),
            1 => new(ExecutionAdapterResolutionStatus.Resolved, matches[0]),
            _ => new(ExecutionAdapterResolutionStatus.ConfigurationConflict, ErrorMessage: "More than one exact bounded execution adapter matches the selected agent.")
        };
    }
}
