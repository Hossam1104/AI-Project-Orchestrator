namespace AIUsageMonitor.Application.Projects;

/// <summary>
/// Project-aware use case for explicitly requested local repository verification.
/// </summary>
public interface IProjectRepositoryStateService
{
    Task<RepositoryStateSnapshot> VerifyAsync(
        Project project,
        CancellationToken cancellationToken = default);
}
