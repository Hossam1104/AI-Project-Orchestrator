namespace AIUsageMonitor.Application.Projects;

/// <summary>
/// Persistence contract for the small project registry document. File paths and serialization
/// remain Infrastructure concerns.
/// </summary>
public interface IProjectRepository
{
    Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task UpsertAsync(Project project, CancellationToken cancellationToken = default);
}
