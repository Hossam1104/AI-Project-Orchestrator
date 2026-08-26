using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Projects;
using AIUsageMonitor.Application.Time;

namespace AIUsageMonitor.Application.Planning;

/// <summary>
/// Turns a bounded request into one immutable, context-bound planning contract. This coordinator
/// performs identity and semantic validation only; it never invokes a model or executes a command.
/// </summary>
public sealed class PlanningExecutionContractService : IPlanningExecutionContractService
{
    private readonly IProjectRepository _projects;
    private readonly IProjectContextResolver _contexts;
    private readonly IAgentRegistryService _agents;
    private readonly IPlanningExecutionContractRepository _contracts;
    private readonly IClock _clock;

    public PlanningExecutionContractService(
        IProjectRepository projects,
        IProjectContextResolver contexts,
        IAgentRegistryService agents,
        IPlanningExecutionContractRepository contracts,
        IClock clock)
    {
        _projects = projects ?? throw new ArgumentNullException(nameof(projects));
        _contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
        _agents = agents ?? throw new ArgumentNullException(nameof(agents));
        _contracts = contracts ?? throw new ArgumentNullException(nameof(contracts));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<PlanningExecutionContractCreationResult> CreateAsync(
        PlanningExecutionContractRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Invalid("A planning contract request is required.");
        }

        var requestValidation = ValidateRequestIdentity(request);
        if (requestValidation is not null)
        {
            return Invalid(requestValidation);
        }

        try
        {
            var project = await _projects
                .GetByIdAsync(request.ProjectId, cancellationToken)
                .ConfigureAwait(false);
            if (project is null)
            {
                return new(PlanningExecutionContractCreationStatus.ProjectNotFound, ErrorMessage: "Project was not found.");
            }

            var contextResolution = await _contexts
                .ResolveAsync(request.ProjectId, cancellationToken)
                .ConfigureAwait(false);
            if (contextResolution.State != ProjectContextResolutionState.Ready || contextResolution.View is null)
            {
                return new(
                    PlanningExecutionContractCreationStatus.ContextNotReady,
                    ContextState: contextResolution.State,
                    ErrorMessage: contextResolution.ErrorMessage ?? "The project context is not Ready.");
            }

            if (contextResolution.View.Project.Id != request.ProjectId ||
                contextResolution.View.Context.ProjectId != request.ProjectId)
            {
                return new(
                    PlanningExecutionContractCreationStatus.ContextNotReady,
                    ContextState: ProjectContextResolutionState.Incomplete,
                    ErrorMessage: "The resolved context does not belong to the requested project.");
            }

            var plannerResolution = await _agents
                .ResolveAsync(request.ProjectId, request.PlannerAgentId, cancellationToken)
                .ConfigureAwait(false);
            if (!plannerResolution.Found || plannerResolution.Agent is null)
            {
                return new(
                    PlanningExecutionContractCreationStatus.PlannerNotFound,
                    ErrorMessage: "The requested planner agent identity was not found.");
            }

            if (!plannerResolution.Agent.Enabled ||
                !plannerResolution.Agent.RoleCapabilities.Contains(AgentRole.Planner))
            {
                return new(
                    PlanningExecutionContractCreationStatus.PlannerNotAuthorized,
                    ErrorMessage: "The planner agent is not enabled for this project with Planner capability.");
            }

            PlanningExecutionContract? predecessor = null;
            if (request.Revision > 1)
            {
                var predecessorResult = await _contracts
                    .GetAsync(request.ProjectId, request.ContractId, request.Revision - 1, cancellationToken)
                    .ConfigureAwait(false);
                switch (predecessorResult.State)
                {
                    case PlanningContractReadState.Missing:
                        return new(
                            PlanningExecutionContractCreationStatus.PredecessorMissing,
                            ErrorMessage: "The immediate predecessor revision is missing.");
                    case PlanningContractReadState.Unavailable:
                        return new(
                            PlanningExecutionContractCreationStatus.PersistenceUnavailable,
                            ErrorMessage: predecessorResult.ErrorMessage ?? "The predecessor could not be read safely.");
                    case PlanningContractReadState.Valid when predecessorResult.Contract is not null:
                        predecessor = predecessorResult.Contract;
                        break;
                    default:
                        return new(
                            PlanningExecutionContractCreationStatus.PredecessorMismatch,
                            ErrorMessage: predecessorResult.ErrorMessage ?? "The predecessor is not a valid contract revision.");
                }

                if (!string.IsNullOrWhiteSpace(request.PreviousContentHash) &&
                    !string.Equals(
                        request.PreviousContentHash,
                        predecessor.ContentHash,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return new(
                        PlanningExecutionContractCreationStatus.PredecessorMismatch,
                        ErrorMessage: "The supplied predecessor content hash does not match durable evidence.");
                }

                if (predecessor.ProjectId != request.ProjectId ||
                    predecessor.ContractId != request.ContractId ||
                    predecessor.Revision != request.Revision - 1)
                {
                    return new(
                        PlanningExecutionContractCreationStatus.PredecessorMismatch,
                        ErrorMessage: "The predecessor identity does not match the requested contract lineage.");
                }

                if (!PlanningWorkItem.SameIdentity(predecessor.WorkItem, request.WorkItem!) ||
                    !string.Equals(predecessor.OwnerReference, request.OwnerReference.Trim(), StringComparison.Ordinal))
                {
                    return new(
                        PlanningExecutionContractCreationStatus.PredecessorMismatch,
                        ErrorMessage: "A revision cannot silently change logical work-item or owner identity.");
                }
            }
            else if (!string.IsNullOrWhiteSpace(request.PreviousContentHash))
            {
                return new(
                    PlanningExecutionContractCreationStatus.PredecessorMismatch,
                    ErrorMessage: "Revision 1 cannot carry predecessor evidence.");
            }

            PlanningExecutionContract contract;
            try
            {
                contract = new PlanningExecutionContract(
                    request.ProjectId,
                    request.ContractId,
                    request.SchemaVersion,
                    request.Revision,
                    _clock.UtcNow,
                    request.OwnerReference,
                    request.PlannerAgentId,
                    new PlanningContextBinding(
                        contextResolution.View.Context.ContextId,
                        contextResolution.View.Context.ContractVersion),
                    request.WorkItem!,
                    request.RepositoryTarget!,
                    request.IncludedScope!,
                    request.Constraints ?? Array.Empty<PlanningScopeClause>(),
                    request.ForbiddenScope!,
                    request.Deliverables!,
                    request.ValidationRequirements!,
                    request.AcceptanceCriteria!,
                    request.ExecutionBudgets!,
                    request.StopConditions!,
                    contextResolution.View.Context.GovernanceReferences,
                    contextResolution.View.Context.RoutingPolicyReference,
                    contextResolution.View.Context.SafetyPolicyReference,
                    predecessor?.Revision,
                    predecessor?.ContentHash);
            }
            catch (ArgumentException exception)
            {
                return Invalid(exception.Message);
            }

            var writeResult = await _contracts
                .CreateAsync(contract, cancellationToken)
                .ConfigureAwait(false);
            return writeResult.Status switch
            {
                PlanningContractRepositoryWriteStatus.Created =>
                    new(PlanningExecutionContractCreationStatus.Created, contract),
                PlanningContractRepositoryWriteStatus.RevisionConflict =>
                    new(
                        PlanningExecutionContractCreationStatus.RevisionConflict,
                        ErrorMessage: writeResult.ErrorMessage ?? "The immutable revision already exists."),
                _ => new(
                    PlanningExecutionContractCreationStatus.PersistenceUnavailable,
                    ErrorMessage: writeResult.ErrorMessage ?? "The contract could not be persisted safely.")
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException exception)
        {
            return new(
                PlanningExecutionContractCreationStatus.PersistenceUnavailable,
                ErrorMessage: exception.Message);
        }
        catch (IOException exception)
        {
            return new(
                PlanningExecutionContractCreationStatus.PersistenceUnavailable,
                ErrorMessage: exception.Message);
        }
    }

    private static string? ValidateRequestIdentity(PlanningExecutionContractRequest request)
    {
        if (request.ProjectId == Guid.Empty)
        {
            return "Project id cannot be empty.";
        }

        if (request.ContractId == Guid.Empty)
        {
            return "Contract id cannot be empty.";
        }

        if (request.SchemaVersion != PlanningExecutionContractSchema.CurrentVersion)
        {
            return $"Only contract schema version {PlanningExecutionContractSchema.CurrentVersion} can be created.";
        }

        if (request.Revision <= 0)
        {
            return "Contract revision must be positive.";
        }

        if (string.IsNullOrWhiteSpace(request.OwnerReference))
        {
            return "Owner reference is required.";
        }

        if (request.PlannerAgentId == Guid.Empty)
        {
            return "Planner agent id cannot be empty.";
        }

        if (request.WorkItem is null)
        {
            return "Work-item identity is required.";
        }

        if (request.RepositoryTarget is null)
        {
            return "Repository target identity is required.";
        }

        if (request.IncludedScope is null || request.ForbiddenScope is null)
        {
            return "Included and forbidden scope are required.";
        }

        if (request.Deliverables is null ||
            request.ValidationRequirements is null ||
            request.AcceptanceCriteria is null ||
            request.ExecutionBudgets is null ||
            request.StopConditions is null)
        {
            return "Deliverables, validation, acceptance, budgets, and stop conditions are required.";
        }

        return null;
    }

    private static PlanningExecutionContractCreationResult Invalid(string message) =>
        new(PlanningExecutionContractCreationStatus.InvalidContract, ErrorMessage: message);
}
