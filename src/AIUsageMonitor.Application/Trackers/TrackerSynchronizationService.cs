using AIUsageMonitor.Application.Projects;

namespace AIUsageMonitor.Application.Trackers;

/// <summary>
/// Exact tracker adapter lookup. It never ranks providers or treats an unknown tracker as Jira.
/// </summary>
public sealed class WorkItemTrackerAdapterResolver : IWorkItemTrackerAdapterResolver
{
    private readonly IReadOnlyList<IWorkItemTrackerAdapter> _adapters;

    public WorkItemTrackerAdapterResolver(IEnumerable<IWorkItemTrackerAdapter> adapters)
    {
        ArgumentNullException.ThrowIfNull(adapters);
        _adapters = adapters.ToArray();
    }

    public TrackerAdapterResolution Resolve(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!TrackerConfiguration.TryCreate(project, out var configuration, out var state, out var errorMessage))
        {
            var resolutionStatus = state switch
            {
                TrackerEvidenceState.Unsupported => TrackerAdapterResolutionStatus.Unsupported,
                TrackerEvidenceState.Partial => TrackerAdapterResolutionStatus.ConfigurationConflict,
                _ => TrackerAdapterResolutionStatus.NotConfigured
            };
            return new(resolutionStatus, ErrorMessage: errorMessage);
        }

        var matches = _adapters
            .Where(adapter => adapter is not null && adapter.Provider == configuration!.Identity.Provider)
            .ToArray();
        return matches.Length switch
        {
            0 => new(TrackerAdapterResolutionStatus.Unsupported, Configuration: configuration, ErrorMessage: "No exact tracker adapter is available for the configured provider."),
            1 => new(TrackerAdapterResolutionStatus.Resolved, matches[0], configuration),
            _ => new(TrackerAdapterResolutionStatus.ConfigurationConflict, Configuration: configuration, ErrorMessage: "More than one exact tracker adapter matches the configured provider.")
        };
    }
}

/// <summary>
/// Pure deterministic planner plus a project-isolated, caller-authorized execution seam. It does
/// not poll, mutate during planning, infer approval, or create authorities.
/// </summary>
public sealed class TrackerSynchronizationService : ITrackerSynchronizationService
{
    private readonly IProjectRepository _projects;
    private readonly IWorkItemTrackerAdapterResolver _resolver;
    public TrackerSynchronizationService(
        IProjectRepository projects,
        IWorkItemTrackerAdapterResolver resolver)
    {
        _projects = projects ?? throw new ArgumentNullException(nameof(projects));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public TrackerSynchronizationPlan CreatePlan(TrackerSynchronizationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ProjectId != request.CurrentEvidence.ProjectId)
        {
            return new(
                request.ProjectId,
                request.CurrentEvidence.State,
                null,
                conflicts: ["Tracker evidence local project provenance does not match the requested local project."]);
        }

        if (request.CurrentEvidence.Project is null || request.CurrentEvidence.Target is null ||
            request.CurrentEvidence.Project.ProjectId != request.CurrentEvidence.Target.ProjectId)
        {
            return new(
                request.ProjectId,
                request.CurrentEvidence.State,
                null,
                blockers: ["Tracker evidence does not contain a complete project and target identity."]);
        }

        if (request.CurrentEvidence.Project is { } project &&
            request.CurrentEvidence.Target is { } target &&
            (target.Provider != project.Provider || !string.Equals(target.ProjectId, project.ProjectId, StringComparison.OrdinalIgnoreCase)))
        {
            return new(
                request.ProjectId,
                request.CurrentEvidence.State,
                null,
                blockers: ["Tracker evidence target does not belong to its project identity."]);
        }

        if (request.CurrentEvidence.State != TrackerEvidenceState.Available)
        {
            return new(
                request.ProjectId,
                request.CurrentEvidence.State,
                request.CurrentEvidence.Value?.StateFingerprint,
                blockers: [$"Fresh complete tracker evidence is required; current state is {request.CurrentEvidence.State}."]);
        }

        var current = request.CurrentEvidence.Value;
        if (request.ProjectId == Guid.Empty || current is null)
        {
            return new(
                request.ProjectId,
                request.CurrentEvidence.State,
                null,
                blockers: ["A complete current tracker snapshot is required before planning."]);
        }

        if (request.Direction != TrackerSynchronizationDirection.TrackerAuthoritative)
        {
            return new(
                request.ProjectId,
                request.CurrentEvidence.State,
                current.StateFingerprint,
                unsupportedChanges: ["The requested synchronization direction is unsupported."]);
        }

        var operations = new List<TrackerSynchronizationOperation>();
        var conflicts = new List<string>();
        var unsupported = request.Desired.UnsupportedChanges.ToList();

        if (request.Desired.StatusId is not null &&
            !string.Equals(current.Status.Id, request.Desired.StatusId, StringComparison.OrdinalIgnoreCase))
        {
            operations.Add(new(
                TrackerMutationKind.TransitionStatus,
                new TrackerMutationTarget(current.Identity),
                current.StateFingerprint,
                statusId: request.Desired.StatusId));
        }

        if (request.Desired.CommentBody is not null)
        {
            var bodyHash = TrackerCommentMetadata.ComputeBodyHash(request.Desired.CommentBody);
            if (!current.Comments.Any(comment => string.Equals(comment.BodyHash, bodyHash, StringComparison.OrdinalIgnoreCase)))
            {
                operations.Add(new(
                    TrackerMutationKind.AddComment,
                    new TrackerMutationTarget(current.Identity),
                    current.StateFingerprint,
                    commentBody: request.Desired.CommentBody));
            }
        }

        foreach (var link in request.Desired.LinksToAdd
                     .OrderBy(static value => value.CanonicalIdentity, StringComparer.Ordinal))
        {
            if (link.RemoteTypeId is null && link.RemoteTypeName is null)
            {
                conflicts.Add("A desired dependency link does not contain an exact remote link type identity.");
                continue;
            }

            if (link.Source.Provider != current.Identity.Provider || link.Target.Provider != current.Identity.Provider ||
                !string.Equals(link.Source.ProjectId, current.Project.ProjectId, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(link.Target.ProjectId, current.Project.ProjectId, StringComparison.OrdinalIgnoreCase))
            {
                conflicts.Add("A desired dependency link does not belong to the current tracker project.");
                continue;
            }

            if (current.Links.All(existing => !SameRelationship(existing, link)))
            {
                operations.Add(new(
                    TrackerMutationKind.AddDependencyLink,
                    new TrackerMutationTarget(
                        link.Source,
                        link.Target,
                        link.RemoteTypeName,
                        link.Direction,
                        link.RemoteTypeId,
                        link.Relationship),
                    current.StateFingerprint));
            }
        }

        operations = operations
            .OrderBy(static operation => operation.Kind)
            .ThenBy(static operation => operation.Target.CanonicalIdentity, StringComparer.Ordinal)
            .ToList();

        var blockers = new List<string>();
        if (operations.Count > TrackerLimits.MaxPlanOperations)
        {
            operations.Clear();
            blockers.Add("Synchronization plan exceeds its supported operation bound.");
        }

        return new(
            request.ProjectId,
            request.CurrentEvidence.State,
            current.StateFingerprint,
            operations,
            conflicts,
            unsupported,
            blockers);
    }

    private static bool SameRelationship(TrackerDependencyLink left, TrackerDependencyLink right) =>
        left.Direction == right.Direction &&
        string.Equals(left.Relationship, right.Relationship, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.Source.CanonicalIdentity, right.Source.CanonicalIdentity, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.Target.CanonicalIdentity, right.Target.CanonicalIdentity, StringComparison.OrdinalIgnoreCase) &&
        SameRemoteType(left, right);

    private static bool SameRemoteType(TrackerDependencyLink left, TrackerDependencyLink right) =>
        !(left.RemoteTypeId is not null && right.RemoteTypeId is not null &&
          !string.Equals(left.RemoteTypeId, right.RemoteTypeId, StringComparison.OrdinalIgnoreCase)) &&
        !(left.RemoteTypeName is not null && right.RemoteTypeName is not null &&
          !string.Equals(left.RemoteTypeName, right.RemoteTypeName, StringComparison.OrdinalIgnoreCase)) &&
        (left.RemoteTypeId is not null && right.RemoteTypeId is not null ||
         left.RemoteTypeName is not null && right.RemoteTypeName is not null);

    public async Task<TrackerMutationResult> ExecuteAsync(
        TrackerSynchronizationPlan plan,
        TrackerSynchronizationOperation operation,
        TrackerMutationAuthority authority,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(authority);

        if (!plan.IsExecutable || !plan.Operations.Contains(operation))
        {
            return new(TrackerMutationOutcome.InvalidAuthority, "The operation is not executable in this synchronization plan.");
        }

        if (authority.ProjectId != plan.ProjectId ||
            !string.Equals(authority.ExpectedStateIdentity, operation.ExpectedStateIdentity, StringComparison.OrdinalIgnoreCase))
        {
            return new(TrackerMutationOutcome.InvalidAuthority, "The mutation authority does not bind the exact planned operation state.");
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return new(TrackerMutationOutcome.Cancelled, "Tracker synchronization was cancelled before the remote mutation request.", verificationState: TrackerEvidenceState.Cancelled);
        }

        var project = await _projects.GetByIdAsync(plan.ProjectId, cancellationToken).ConfigureAwait(false);
        if (project is null || project.Id != plan.ProjectId)
        {
            return new(TrackerMutationOutcome.InvalidAuthority, "The project identity could not be resolved.");
        }

        var resolution = _resolver.Resolve(project);
        if (!resolution.Succeeded)
        {
            return new(
                resolution.Status == TrackerAdapterResolutionStatus.NotConfigured
                    ? TrackerMutationOutcome.AuthenticationRequired
                    : TrackerMutationOutcome.Unsupported,
                resolution.ErrorMessage);
        }

        if (project.Id != plan.ProjectId ||
            authority.ProjectId != project.Id ||
            resolution.Configuration!.ProjectId != project.Id ||
            operation.Target.WorkItem.Provider != resolution.Configuration.Identity.Provider ||
            !string.Equals(operation.Target.WorkItem.ProjectId, resolution.Configuration.Identity.ProjectId, StringComparison.OrdinalIgnoreCase))
        {
            return new(TrackerMutationOutcome.InvalidAuthority, "The synchronization operation does not belong to the resolved project tracker.");
        }

        var request = new TrackerMutationRequest(
            plan.ProjectId,
            resolution.Configuration!.Identity,
            operation.Kind,
            operation.Target,
            authority,
            operation.CommentBody,
            operation.StatusId);
        return await resolution.Adapter!.MutateAsync(resolution.Configuration!, request, cancellationToken).ConfigureAwait(false);
    }
}
