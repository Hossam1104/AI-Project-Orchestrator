namespace AIUsageMonitor.Application.Projects;

/// <summary>
/// Project registry use cases for callers that should not know the persistence format or
/// storage location.
/// </summary>
public interface IProjectRegistryService
{
    Task<IReadOnlyList<Project>> GetProjectsAsync(CancellationToken cancellationToken = default);

    Task<Project> CreateProjectAsync(
        ProjectEdit edit,
        CancellationToken cancellationToken = default);

    Task<Project> UpdateProjectAsync(
        Guid projectId,
        ProjectEdit edit,
        CancellationToken cancellationToken = default);
}
