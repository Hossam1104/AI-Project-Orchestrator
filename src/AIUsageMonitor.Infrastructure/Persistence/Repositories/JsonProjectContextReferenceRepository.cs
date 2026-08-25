using AIUsageMonitor.Application.Projects;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

/// <summary>
/// Versioned, atomic, project-GUID-scoped persistence for the single APO-39 context reference.
/// Corrupt, mismatched, and unsupported records never resolve as Ready.
/// </summary>
public sealed class JsonProjectContextReferenceRepository : IProjectContextReferenceRepository
{
    private readonly ApplicationDataPaths _paths;
    private readonly JsonFileStore _files;
    private readonly ILogger<JsonProjectContextReferenceRepository> _logger;

    public JsonProjectContextReferenceRepository(
        ApplicationDataPaths paths,
        JsonFileStore files,
        ILogger<JsonProjectContextReferenceRepository> logger)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProjectContextReadResult> GetAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        ValidateProjectId(projectId);
        var path = _paths.GetProjectContextReferenceFile(projectId);
        var result = await _files.ReadAsync<ProjectContextReferenceRecord>(path, cancellationToken)
            .ConfigureAwait(false);

        return result.Status switch
        {
            FileReadStatus.Missing or FileReadStatus.Empty =>
                new(ProjectContextReadState.Missing, ErrorMessage: "Project context reference is missing."),
            FileReadStatus.IoFailure or FileReadStatus.PermissionFailure =>
                new(ProjectContextReadState.Unavailable, ErrorMessage: "Project context reference is unavailable."),
            FileReadStatus.UnsupportedSchema or FileReadStatus.Corrupt =>
                new(ProjectContextReadState.Invalid, ErrorMessage: "Project context reference is invalid."),
            FileReadStatus.Valid => MapValid(projectId, result.Value, path),
            _ => new(ProjectContextReadState.Invalid, ErrorMessage: "Project context reference is invalid.")
        };
    }

    public async Task UpsertAsync(
        ProjectContextReference context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ValidateProjectId(context.ProjectId);
        if (context.ContractVersion != ProjectContextContract.CurrentVersion)
        {
            throw new ArgumentException("Only the current project context contract can be written.", nameof(context));
        }

        await _paths.EnsureProjectDirectoriesAsync(context.ProjectId, cancellationToken).ConfigureAwait(false);
        await _files.WriteAsync(
                _paths.GetProjectContextReferenceFile(context.ProjectId),
                ProjectContextReferenceRecord.FromApplication(context),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private ProjectContextReadResult MapValid(
        Guid requestedProjectId,
        ProjectContextReferenceRecord? record,
        string path)
    {
        if (record is null)
        {
            return new(ProjectContextReadState.Invalid, ErrorMessage: "Project context payload is missing.");
        }

        if (record.ProjectId != requestedProjectId)
        {
            _logger.LogWarning(
                "Rejected project context with mismatched project id {EmbeddedProjectId} at {Path}; requested {RequestedProjectId}",
                record.ProjectId,
                path,
                requestedProjectId);
            return new(ProjectContextReadState.Invalid, ErrorMessage: "Project context belongs to another project.");
        }

        if (record.ContractVersion > ProjectContextContract.CurrentVersion)
        {
            return new(
                ProjectContextReadState.UnsupportedVersion,
                ErrorMessage: $"Project context contract version {record.ContractVersion} is newer than supported version {ProjectContextContract.CurrentVersion}.");
        }

        if (record.ContractVersion != ProjectContextContract.CurrentVersion)
        {
            return new(ProjectContextReadState.Invalid, ErrorMessage: "Project context contract version is unsupported.");
        }

        try
        {
            return new(ProjectContextReadState.Valid, record.ToApplication());
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            InvalidOperationException or
            NullReferenceException)
        {
            _logger.LogWarning(exception, "Rejected invalid project context at {Path}", path);
            return new(ProjectContextReadState.Invalid, ErrorMessage: "Project context record is invalid.");
        }
    }

    private static void ValidateProjectId(Guid projectId)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }
    }
}
