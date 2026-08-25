namespace AIUsageMonitor.Application.Projects;

public enum ProjectContextResolutionState
{
    Ready,
    ProjectNotFound,
    ContextMissing,
    UnsupportedContextVersion,
    Incomplete,
    Unavailable
}

public sealed class ProjectContextView
{
    public ProjectContextView(
        Project project,
        ProjectContextReference context,
        IReadOnlyList<AIUsageMonitor.Application.Agents.EffectiveAgentDefinition> effectiveAgents)
    {
        Project = project ?? throw new ArgumentNullException(nameof(project));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        EffectiveAgents = effectiveAgents ?? throw new ArgumentNullException(nameof(effectiveAgents));
    }

    public Project Project { get; }
    public ProjectContextReference Context { get; }
    public IReadOnlyList<AIUsageMonitor.Application.Agents.EffectiveAgentDefinition> EffectiveAgents { get; }
}

public sealed record ProjectContextResolution(
    ProjectContextResolutionState State,
    ProjectContextView? View = null,
    string? ErrorMessage = null);

public interface IProjectContextResolver
{
    Task<ProjectContextResolution> ResolveAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);
}
