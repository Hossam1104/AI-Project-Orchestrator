namespace AIUsageMonitor.Application.Projects;

public sealed class ProjectRepositoryStateService : IProjectRepositoryStateService
{
    private readonly ILocalRepositoryInspector _inspector;

    public ProjectRepositoryStateService(ILocalRepositoryInspector inspector)
    {
        _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
    }

    public async Task<RepositoryStateSnapshot> VerifyAsync(
        Project project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        var inspection = await _inspector
            .InspectAsync(project.LocalPath, project.RepositoryUrl, cancellationToken)
            .ConfigureAwait(false);

        return new RepositoryStateSnapshot(project.Id, project.LocalPath, inspection);
    }
}
