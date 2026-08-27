using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Handoffs;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Projects;
using AIUsageMonitor.Application.Time;

namespace AIUsageMonitor.Application.Routing;

public sealed class RoutingDecisionRequest
{
    public RoutingDecisionRequest(
        Guid projectId,
        PlanningExecutionContractReference planningContractReference,
        RoutingTaskClassification classification,
        RoutingPolicySnapshot policy,
        IReadOnlyList<RoutingCapacityEvidence>? capacityEvidence = null,
        RoutingOwnerOverrideRequest? ownerOverride = null)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Routing project id cannot be empty.", nameof(projectId));
        }

        ProjectId = projectId;
        PlanningContractReference = planningContractReference ?? throw new ArgumentNullException(nameof(planningContractReference));
        Classification = classification ?? throw new ArgumentNullException(nameof(classification));
        Policy = policy ?? throw new ArgumentNullException(nameof(policy));
        CapacityEvidence = capacityEvidence ?? Array.Empty<RoutingCapacityEvidence>();
        OwnerOverride = ownerOverride;
    }

    public Guid ProjectId { get; }
    public PlanningExecutionContractReference PlanningContractReference { get; }
    public RoutingTaskClassification Classification { get; }
    public RoutingPolicySnapshot Policy { get; }
    public IReadOnlyList<RoutingCapacityEvidence> CapacityEvidence { get; }
    public RoutingOwnerOverrideRequest? OwnerOverride { get; }
}

public enum RoutingInputAssemblyStatus
{
    Ready,
    InvalidRequest,
    ProjectNotFound,
    ContextInsufficient,
    ContractMissing,
    ContractUnsupported,
    ContractInvalid,
    ContractUnavailable,
    ContractMismatch,
    RedactionRejected
}

public sealed record RoutingInputAssemblyResult(
    RoutingInputAssemblyStatus Status,
    RoutingInputSnapshot? Input = null,
    PlanningContractReadState? ContractState = null,
    string? ErrorMessage = null)
{
    public bool Succeeded => Status == RoutingInputAssemblyStatus.Ready && Input is not null;
}

public interface IRoutingInputAssembler
{
    Task<RoutingInputAssemblyResult> AssembleAsync(
        RoutingDecisionRequest request,
        DateTimeOffset evaluatedAt,
        CancellationToken cancellationToken = default);
}

public sealed class RoutingRedactionRejectedException : InvalidOperationException
{
    public RoutingRedactionRejectedException(string fieldName)
        : base($"Secret-shaped content is not permitted in routing field '{fieldName}'.")
    {
        FieldName = fieldName;
    }

    public string FieldName { get; }
}

/// <summary>
/// Assembles a routing snapshot from exact project/context/contract authority. It performs no
/// provider refresh and does not consult the latest contract as a fallback.
/// </summary>
public sealed class RoutingInputAssembler : IRoutingInputAssembler
{
    private readonly IProjectContextResolver _contexts;
    private readonly IPlanningExecutionContractRepository _contracts;
    private readonly IHandoffRedactionService _redaction;

    public RoutingInputAssembler(
        IProjectContextResolver contexts,
        IPlanningExecutionContractRepository contracts,
        IHandoffRedactionService redaction)
    {
        _contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
        _contracts = contracts ?? throw new ArgumentNullException(nameof(contracts));
        _redaction = redaction ?? throw new ArgumentNullException(nameof(redaction));
    }

    public async Task<RoutingInputAssemblyResult> AssembleAsync(
        RoutingDecisionRequest request,
        DateTimeOffset evaluatedAt,
        CancellationToken cancellationToken = default)
    {
        if (request is null || evaluatedAt == default)
        {
            return new(RoutingInputAssemblyStatus.InvalidRequest, ErrorMessage: "A valid routing request and evaluation time are required.");
        }

        if (request.Classification.RequiredRole != request.Policy.RequiredRole)
        {
            return new(RoutingInputAssemblyStatus.InvalidRequest, ErrorMessage: "Classification and executable routing policy roles must match.");
        }

        if (request.CapacityEvidence.Any(value => value is not null && value.ObservedAt > evaluatedAt))
        {
            return new(
                RoutingInputAssemblyStatus.InvalidRequest,
                ErrorMessage: "Routing capacity evidence cannot be observed after the routing evaluation time.");
        }

        if (request.OwnerOverride is not null && request.OwnerOverride.RequestedAt > evaluatedAt)
        {
            return new(
                RoutingInputAssemblyStatus.InvalidRequest,
                ErrorMessage: "An owner override cannot be requested after the routing evaluation time.");
        }

        try
        {
            foreach (var capability in request.Classification.RequiredCapabilities)
            {
                ValidateAuthority(capability, "classification.requiredCapability");
            }

            foreach (var tag in request.Classification.PolicyTags)
            {
                ValidateAuthority(tag, "classification.policyTag");
            }

            var contextResolution = await _contexts.ResolveAsync(request.ProjectId, cancellationToken).ConfigureAwait(false);
            if (contextResolution.State != ProjectContextResolutionState.Ready || contextResolution.View is null)
            {
                return MapContextFailure(contextResolution);
            }

            var view = contextResolution.View;
            if (view.Context.ContractVersion != ProjectContextContract.CurrentVersion)
            {
                return new(
                    RoutingInputAssemblyStatus.ContextInsufficient,
                    ErrorMessage: "The project context contract version is not supported for routing.");
            }

            var contractReference = request.PlanningContractReference;
            var contractRead = await _contracts.GetAsync(
                    request.ProjectId,
                    contractReference.ContractId,
                    contractReference.Revision,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!contractRead.IsValid || contractRead.Contract is null)
            {
                return MapContractFailure(contractRead);
            }

            var contract = contractRead.Contract;
            if (contract.ProjectId != request.ProjectId || !SameContractReference(contract.Reference, contractReference))
            {
                return new(
                    RoutingInputAssemblyStatus.ContractMismatch,
                    ContractState: contractRead.State,
                    ErrorMessage: "The exact requested planning contract reference does not match the persisted contract.");
            }

            if (contract.Context.ProjectContextId != view.Context.ContextId ||
                contract.Context.ProjectContextContractVersion != view.Context.ContractVersion)
            {
                return new(
                    RoutingInputAssemblyStatus.ContractMismatch,
                    ContractState: contractRead.State,
                    ErrorMessage: "The planning contract is not bound to the current project context identity and version.");
            }

            var sanitizedPolicy = SanitizePolicy(request.Policy);
            var sanitizedCandidates = view.EffectiveAgents
                .Select(agent => SanitizeCandidate(RoutingAgentSnapshot.FromEffective(agent)))
                .ToArray();
            var sanitizedEvidence = request.CapacityEvidence.Select(SanitizeEvidence).ToArray();
            var sanitizedOverride = request.OwnerOverride is null ? null : SanitizeOverride(request.OwnerOverride);
            var input = new RoutingInputSnapshot(
                request.ProjectId,
                contract.Reference,
                new RoutingContextReference(view.Context.ContextId, view.Context.ContractVersion, view.Context.UpdatedAt),
                request.Classification,
                sanitizedPolicy,
                sanitizedCandidates,
                sanitizedEvidence,
                sanitizedOverride,
                evaluatedAt);
            return new(RoutingInputAssemblyStatus.Ready, input, contractRead.State);
        }
        catch (RoutingRedactionRejectedException exception)
        {
            return new(RoutingInputAssemblyStatus.RedactionRejected, ErrorMessage: exception.Message);
        }
        catch (ArgumentException exception)
        {
            return new(RoutingInputAssemblyStatus.InvalidRequest, ErrorMessage: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return new(RoutingInputAssemblyStatus.ContractInvalid, ErrorMessage: exception.Message);
        }
    }

    private RoutingPolicySnapshot SanitizePolicy(RoutingPolicySnapshot policy)
    {
        ValidateAuthority(policy.PolicyId, "policyId");
        if (policy.PolicyReference is not null)
        {
            ValidateAuthority(policy.PolicyReference, "policyReference");
        }

        var reason = policy.Reason is null ? null : _redaction.Redact(policy.Reason).Value;
        return policy.WithDescriptions(policy.PolicyReference, reason);
    }

    private RoutingAgentSnapshot SanitizeCandidate(RoutingAgentSnapshot candidate)
    {
        ValidateAuthority(candidate.Identity.DisplayName, "agentIdentity.displayName");
        if (candidate.Identity.Provider is not null)
        {
            ValidateAuthority(candidate.Identity.Provider, "agentIdentity.provider");
        }

        if (candidate.Identity.ModelIdentifier is not null)
        {
            ValidateAuthority(candidate.Identity.ModelIdentifier, "agentIdentity.modelIdentifier");
        }

        foreach (var capability in candidate.Capabilities)
        {
            ValidateAuthority(capability, "agent.capability");
        }

        var limitations = candidate.Limitations.Select(value => _redaction.Redact(value).Value).ToArray();
        return candidate.WithLimitations(limitations);
    }

    private RoutingCapacityEvidence SanitizeEvidence(RoutingCapacityEvidence evidence)
    {
        if (evidence.EvidenceReference is not null)
        {
            ValidateAuthority(evidence.EvidenceReference, "capacityEvidence.evidenceReference");
        }

        return new(
            evidence.AgentId,
            evidence.CapacityState,
            evidence.ObservedAt,
            evidence.ValidUntil,
            evidence.EvidenceReference,
            evidence.ProviderId,
            evidence.QuotaDefinitionId,
            evidence.RemainingFraction,
            evidence.Source);
    }

    private RoutingOwnerOverrideRequest SanitizeOverride(RoutingOwnerOverrideRequest ownerOverride)
    {
        ValidateAuthority(ownerOverride.ActorReference, "ownerOverride.actorReference");
        return ownerOverride.WithReason(_redaction.Redact(ownerOverride.Reason).Value);
    }

    private void ValidateAuthority(string value, string fieldName)
    {
        if (_redaction.ValidateIdentityText(value).RequiresRedaction)
        {
            throw new RoutingRedactionRejectedException(fieldName);
        }
    }

    private static RoutingInputAssemblyResult MapContextFailure(ProjectContextResolution resolution) =>
        resolution.State switch
        {
            ProjectContextResolutionState.ProjectNotFound => new(RoutingInputAssemblyStatus.ProjectNotFound, ErrorMessage: resolution.ErrorMessage),
            _ => new(RoutingInputAssemblyStatus.ContextInsufficient, ErrorMessage: resolution.ErrorMessage ?? "Project context is not sufficient for routing.")
        };

    private static RoutingInputAssemblyResult MapContractFailure(PlanningContractReadResult result) =>
        result.State switch
        {
            PlanningContractReadState.Missing => new(RoutingInputAssemblyStatus.ContractMissing, ContractState: result.State, ErrorMessage: result.ErrorMessage),
            PlanningContractReadState.UnsupportedFutureVersion or PlanningContractReadState.MigrationRequired => new(RoutingInputAssemblyStatus.ContractUnsupported, ContractState: result.State, ErrorMessage: result.ErrorMessage),
            PlanningContractReadState.Unavailable => new(RoutingInputAssemblyStatus.ContractUnavailable, ContractState: result.State, ErrorMessage: result.ErrorMessage),
            _ => new(RoutingInputAssemblyStatus.ContractInvalid, ContractState: result.State, ErrorMessage: result.ErrorMessage)
        };

    private static bool SameContractReference(
        PlanningExecutionContractReference left,
        PlanningExecutionContractReference right) =>
        left.ContractId == right.ContractId &&
        left.Revision == right.Revision &&
        left.SchemaVersion == right.SchemaVersion &&
        string.Equals(left.ContentHash, right.ContentHash, StringComparison.OrdinalIgnoreCase);
}

public enum RoutingDecisionCreationStatus
{
    Created,
    InvalidRequest,
    ProjectNotFound,
    ContextInsufficient,
    ContractMissing,
    ContractUnsupported,
    ContractInvalid,
    ContractUnavailable,
    ContractMismatch,
    RedactionRejected,
    PersistenceUnavailable,
    DecisionConflict
}

public sealed record RoutingDecisionCreationResult(
    RoutingDecisionCreationStatus Status,
    RoutingDecision? Decision = null,
    RoutingInputAssemblyStatus? AssemblyStatus = null,
    string? ErrorMessage = null)
{
    public bool Succeeded => Status == RoutingDecisionCreationStatus.Created && Decision is not null;
}

public interface IRoutingDecisionService
{
    Task<RoutingDecisionCreationResult> CreateAsync(
        RoutingDecisionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class RoutingDecisionService : IRoutingDecisionService
{
    private readonly IRoutingInputAssembler _assembler;
    private readonly IRoutingDecisionEngine _engine;
    private readonly IRoutingDecisionRepository _decisions;
    private readonly IClock _clock;

    public RoutingDecisionService(
        IRoutingInputAssembler assembler,
        IRoutingDecisionEngine engine,
        IRoutingDecisionRepository decisions,
        IClock clock)
    {
        _assembler = assembler ?? throw new ArgumentNullException(nameof(assembler));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _decisions = decisions ?? throw new ArgumentNullException(nameof(decisions));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<RoutingDecisionCreationResult> CreateAsync(
        RoutingDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return new(RoutingDecisionCreationStatus.InvalidRequest, ErrorMessage: "A routing request is required.");
        }

        var assembly = await _assembler.AssembleAsync(request, _clock.UtcNow, cancellationToken).ConfigureAwait(false);
        if (!assembly.Succeeded || assembly.Input is null)
        {
            return MapAssemblyFailure(assembly);
        }

        var evaluation = _engine.Evaluate(assembly.Input);
        var decision = new RoutingDecision(
            request.ProjectId,
            Guid.NewGuid(),
            RoutingDecisionSchema.CurrentVersion,
            _clock.UtcNow,
            evaluation);
        var write = await _decisions.CreateAsync(decision, cancellationToken).ConfigureAwait(false);
        return write.Status switch
        {
            RoutingDecisionRepositoryWriteStatus.Created => new(RoutingDecisionCreationStatus.Created, decision, assembly.Status),
            RoutingDecisionRepositoryWriteStatus.DecisionConflict => new(RoutingDecisionCreationStatus.DecisionConflict, AssemblyStatus: assembly.Status, ErrorMessage: write.ErrorMessage),
            _ => new(RoutingDecisionCreationStatus.PersistenceUnavailable, AssemblyStatus: assembly.Status, ErrorMessage: write.ErrorMessage ?? "Routing decision persistence is unavailable.")
        };
    }

    private static RoutingDecisionCreationResult MapAssemblyFailure(RoutingInputAssemblyResult assembly) =>
        assembly.Status switch
        {
            RoutingInputAssemblyStatus.InvalidRequest => new(RoutingDecisionCreationStatus.InvalidRequest, AssemblyStatus: assembly.Status, ErrorMessage: assembly.ErrorMessage),
            RoutingInputAssemblyStatus.ProjectNotFound => new(RoutingDecisionCreationStatus.ProjectNotFound, AssemblyStatus: assembly.Status, ErrorMessage: assembly.ErrorMessage),
            RoutingInputAssemblyStatus.ContextInsufficient => new(RoutingDecisionCreationStatus.ContextInsufficient, AssemblyStatus: assembly.Status, ErrorMessage: assembly.ErrorMessage),
            RoutingInputAssemblyStatus.ContractMissing => new(RoutingDecisionCreationStatus.ContractMissing, AssemblyStatus: assembly.Status, ErrorMessage: assembly.ErrorMessage),
            RoutingInputAssemblyStatus.ContractUnsupported => new(RoutingDecisionCreationStatus.ContractUnsupported, AssemblyStatus: assembly.Status, ErrorMessage: assembly.ErrorMessage),
            RoutingInputAssemblyStatus.ContractUnavailable => new(RoutingDecisionCreationStatus.ContractUnavailable, AssemblyStatus: assembly.Status, ErrorMessage: assembly.ErrorMessage),
            RoutingInputAssemblyStatus.ContractMismatch => new(RoutingDecisionCreationStatus.ContractMismatch, AssemblyStatus: assembly.Status, ErrorMessage: assembly.ErrorMessage),
            RoutingInputAssemblyStatus.RedactionRejected => new(RoutingDecisionCreationStatus.RedactionRejected, AssemblyStatus: assembly.Status, ErrorMessage: assembly.ErrorMessage),
            _ => new(RoutingDecisionCreationStatus.ContractInvalid, AssemblyStatus: assembly.Status, ErrorMessage: assembly.ErrorMessage)
        };
}
