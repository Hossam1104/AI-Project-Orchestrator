using AIUsageMonitor.Application.Agents;

namespace AIUsageMonitor.Application.Projects;

public sealed class ProjectContextResolver : IProjectContextResolver
{
    private readonly IProjectRepository _projects;
    private readonly IProjectContextReferenceRepository _contexts;
    private readonly IAgentRegistryService _agents;

    public ProjectContextResolver(
        IProjectRepository projects,
        IProjectContextReferenceRepository contexts,
        IAgentRegistryService agents)
    {
        _projects = projects ?? throw new ArgumentNullException(nameof(projects));
        _contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
        _agents = agents ?? throw new ArgumentNullException(nameof(agents));
    }

    public async Task<ProjectContextResolution> ResolveAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        var project = await _projects.GetByIdAsync(projectId, cancellationToken).ConfigureAwait(false);
        if (project is null)
        {
            return new(ProjectContextResolutionState.ProjectNotFound, ErrorMessage: "Project was not found.");
        }

        var stored = await _contexts.GetAsync(projectId, cancellationToken).ConfigureAwait(false);
        if (stored.State == ProjectContextReadState.Missing)
        {
            return new(ProjectContextResolutionState.ContextMissing, ErrorMessage: "Project context is missing.");
        }

        if (stored.State == ProjectContextReadState.UnsupportedVersion)
        {
            return new(ProjectContextResolutionState.UnsupportedContextVersion, ErrorMessage: stored.ErrorMessage);
        }

        if (stored.State == ProjectContextReadState.Unavailable)
        {
            return new(ProjectContextResolutionState.Unavailable, ErrorMessage: stored.ErrorMessage);
        }

        if (stored.State != ProjectContextReadState.Valid || stored.Context is null)
        {
            return new(ProjectContextResolutionState.Incomplete, ErrorMessage: stored.ErrorMessage ?? "Project context is invalid.");
        }

        if (stored.Context.ProjectId != projectId ||
            stored.Context.ContractVersion != ProjectContextContract.CurrentVersion ||
            !string.Equals(
                stored.Context.Repository.RegisteredLocalPath,
                project.LocalPath,
                StringComparison.OrdinalIgnoreCase))
        {
            return new(ProjectContextResolutionState.Incomplete, ErrorMessage: "Project context does not match the current project contract.");
        }

        if (!stored.Context.HasAcceptedReadyEvidence(project))
        {
            return new(
                ProjectContextResolutionState.Incomplete,
                ErrorMessage: "Project context does not contain the accepted v1 onboarding evidence required for planning.");
        }

        var effectiveAgents = await _agents
            .GetEffectiveAgentsAsync(projectId, cancellationToken)
            .ConfigureAwait(false);
        return new(
            ProjectContextResolutionState.Ready,
            new ProjectContextView(project, stored.Context, effectiveAgents));
    }
}
