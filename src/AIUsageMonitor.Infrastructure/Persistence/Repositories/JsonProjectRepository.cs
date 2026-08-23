using AIUsageMonitor.Application.Projects;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

/// <summary>
/// Versioned JSON project registry. The registry stores metadata only; project files and
/// repository contents remain in the registered local workspace.
/// </summary>
public sealed class JsonProjectRepository : IProjectRepository
{
    private readonly ApplicationDataPaths _paths;
    private readonly VersionedJsonCollectionStore<ProjectRecord> _records;
    private readonly ILogger<JsonProjectRepository> _logger;

    public JsonProjectRepository(
        ApplicationDataPaths paths,
        JsonFileStore files,
        ILogger<JsonProjectRepository> logger)
    {
        _paths = paths;
        _records = new VersionedJsonCollectionStore<ProjectRecord>(files);
        _logger = logger;
    }

    public async Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var records = await _records.ReadAsync(_paths.ProjectsFile, cancellationToken).ConfigureAwait(false);
        return records
            .Select(TryMap)
            .Where(static project => project is not null)
            .Select(static project => project!)
            .OrderBy(static project => project.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static project => project.Id)
            .ToArray();
    }

    public async Task<Project?> GetByIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        var records = await _records.ReadAsync(_paths.ProjectsFile, cancellationToken).ConfigureAwait(false);
        var record = records.FirstOrDefault(project => project.Id == projectId);
        return record is null ? null : TryMap(record);
    }

    public Task UpsertAsync(Project project, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        var record = ProjectRecord.FromApplication(project);

        return _records.UpdateAsync(_paths.ProjectsFile, records =>
        {
            var index = records.FindIndex(existing => existing.Id == record.Id);
            if (index >= 0)
            {
                records[index] = record;
            }
            else
            {
                records.Add(record);
            }

            return records;
        }, cancellationToken);
    }

    private Project? TryMap(ProjectRecord record)
    {
        try
        {
            return record.ToApplication();
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(exception, "Skipping invalid project record {ProjectId}", record.Id);
            return null;
        }
    }
}
