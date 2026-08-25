using AIUsageMonitor.Application.Time;

namespace AIUsageMonitor.Application.Projects;

/// <summary>
/// Coordinates project registration create/edit semantics over <see cref="IProjectRepository"/>.
/// The service is deliberately metadata-only: it does not inspect local paths, repositories, or
/// tracker systems.
/// </summary>
public sealed class ProjectRegistryService : IProjectRegistryService
{
    private readonly IProjectRepository _repository;
    private readonly IClock _clock;

    public ProjectRegistryService(IProjectRepository repository, IClock clock)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public Task<IReadOnlyList<Project>> GetProjectsAsync(
        CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public async Task<Project> CreateProjectAsync(
        ProjectEdit edit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(edit);

        var now = _clock.UtcNow;
        var project = BuildProject(
            Guid.NewGuid(),
            edit,
            now,
            now,
            repositoryMetadata: edit.RepositoryMetadata,
            trackerMetadata: edit.TrackerMetadata);

        await _repository.UpsertAsync(project, cancellationToken).ConfigureAwait(false);
        return project;
    }

    public async Task<Project> UpdateProjectAsync(
        Guid projectId,
        ProjectEdit edit,
        CancellationToken cancellationToken = default)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        ArgumentNullException.ThrowIfNull(edit);

        // Load first so the immutable identity, creation time, and fields not exposed by the
        // editor survive the visible-field update.
        var existing = await _repository.GetByIdAsync(projectId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException("The project no longer exists.");

        var updated = BuildProject(
            existing.Id,
            edit,
            existing.CreatedAt,
            _clock.UtcNow,
            edit.RepositoryMetadata ?? existing.RepositoryMetadata,
            edit.TrackerMetadata ?? existing.TrackerMetadata);

        await _repository.UpsertAsync(updated, cancellationToken).ConfigureAwait(false);
        return updated;
    }

    private static Project BuildProject(
        Guid id,
        ProjectEdit edit,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        IReadOnlyDictionary<string, string?>? repositoryMetadata,
        IReadOnlyDictionary<string, string?>? trackerMetadata) =>
        new(
            id,
            edit.Name,
            edit.LocalPath.Trim(),
            edit.DefaultBranch,
            edit.Status,
            createdAt,
            updatedAt,
            edit.RepositoryProvider,
            edit.RepositoryUrl,
            edit.RepositoryId,
            repositoryMetadata,
            edit.TrackerType,
            edit.TrackerId,
            trackerMetadata,
            NormalizeGovernanceReferences(edit.GovernanceReferences),
            edit.RoutingPolicyReference,
            edit.SafetyPolicyReference);

    private static IReadOnlyList<string> NormalizeGovernanceReferences(
        IReadOnlyList<string>? references)
    {
        if (references is null || references.Count == 0)
        {
            return Array.Empty<string>();
        }

        return references
            .Where(static reference => !string.IsNullOrWhiteSpace(reference))
            .Select(static reference => reference.Trim())
            .ToArray();
    }
}
