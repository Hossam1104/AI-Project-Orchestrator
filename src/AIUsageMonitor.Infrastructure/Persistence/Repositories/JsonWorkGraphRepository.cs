using AIUsageMonitor.Application.Orchestration;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

/// <summary>GUID-isolated immutable JSON storage for work-graph authority.</summary>
public sealed class JsonWorkGraphRepository : IWorkGraphRepository
{
    private const string ExpectedRecordType = "work-graph";

    private readonly ApplicationDataPaths _paths;
    private readonly JsonFileStore _files;
    private readonly ILogger<JsonWorkGraphRepository> _logger;

    public JsonWorkGraphRepository(
        ApplicationDataPaths paths,
        JsonFileStore files,
        ILogger<JsonWorkGraphRepository> logger)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WorkGraphRepositoryWriteResult> CreateAsync(
        WorkGraph graph,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (graph.SchemaVersion != WorkGraphSchema.CurrentVersion)
        {
            throw new ArgumentException("Only the current work-graph schema can be written.", nameof(graph));
        }

        if (!string.Equals(
                WorkGraphIntegrity.ComputeContentHash(graph),
                graph.ContentHash,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Work-graph content integrity is invalid.", nameof(graph));
        }

        var path = _paths.GetWorkGraphFile(graph.ProjectId, graph.GraphId);
        try
        {
            await _paths.EnsureProjectDirectoriesAsync(graph.ProjectId, cancellationToken).ConfigureAwait(false);
            Directory.CreateDirectory(_paths.GetWorkGraphDirectory(graph.ProjectId, graph.GraphId));
            await _files.CreateNewAsync(
                    path,
                    WorkGraphRecord.FromApplication(graph),
                    cancellationToken)
                .ConfigureAwait(false);
            return new(WorkGraphRepositoryWriteStatus.Created);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Permission denied while creating work graph {Path}", path);
            return new(WorkGraphRepositoryWriteStatus.Unavailable, "Graph persistence is unavailable.");
        }
        catch (IOException) when (File.Exists(path))
        {
            return new(
                WorkGraphRepositoryWriteStatus.GraphConflict,
                "The immutable work graph already exists.");
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "I/O failure while creating work graph {Path}", path);
            return new(WorkGraphRepositoryWriteStatus.Unavailable, "Graph persistence is unavailable.");
        }
    }

    public async Task<WorkGraphReadResult> GetAsync(
        Guid projectId,
        Guid graphId,
        CancellationToken cancellationToken = default)
    {
        ValidateGuid(projectId, nameof(projectId));
        ValidateGuid(graphId, nameof(graphId));

        var path = _paths.GetWorkGraphFile(projectId, graphId);
        var result = await _files.ReadPreservingAsync<WorkGraphRecord>(path, cancellationToken).ConfigureAwait(false);
        return result.Status switch
        {
            FileReadStatus.Missing => new(WorkGraphReadState.Missing, ErrorMessage: "Work graph is missing."),
            FileReadStatus.Empty or FileReadStatus.Corrupt =>
                new(WorkGraphReadState.Invalid, ErrorMessage: "Work graph JSON is invalid."),
            FileReadStatus.UnsupportedSchema =>
                new(WorkGraphReadState.UnsupportedFutureVersion, ErrorMessage: "Work graph storage schema is unsupported."),
            FileReadStatus.IoFailure or FileReadStatus.PermissionFailure =>
                new(WorkGraphReadState.Unavailable, ErrorMessage: "Work graph is unavailable."),
            FileReadStatus.Valid => MapValid(projectId, graphId, path, result.Value),
            _ => new(WorkGraphReadState.Invalid, ErrorMessage: "Work graph is invalid.")
        };
    }

    private WorkGraphReadResult MapValid(
        Guid projectId,
        Guid graphId,
        string path,
        WorkGraphRecord? record)
    {
        if (record is null)
        {
            return new(WorkGraphReadState.Invalid, ErrorMessage: "Work graph payload is missing.");
        }

        if (!string.Equals(record.RecordType, ExpectedRecordType, StringComparison.Ordinal))
        {
            return new(WorkGraphReadState.Invalid, ErrorMessage: "Work graph record type is invalid.");
        }

        if (record.SchemaVersion > WorkGraphSchema.CurrentVersion)
        {
            return new(WorkGraphReadState.UnsupportedFutureVersion, ErrorMessage: "Work graph schema is newer than supported.");
        }

        if (record.SchemaVersion < WorkGraphSchema.CurrentVersion)
        {
            return new(WorkGraphReadState.MigrationRequired, ErrorMessage: "An explicit work-graph migrator is required.");
        }

        if (record.ProjectId != projectId || record.GraphId != graphId)
        {
            _logger.LogWarning(
                "Rejected work-graph identity mismatch at {Path}; requested project {ProjectId}, graph {GraphId}",
                path,
                projectId,
                graphId);
            return new(WorkGraphReadState.Invalid, ErrorMessage: "Work graph identity does not match its GUID-derived path.");
        }

        try
        {
            var graph = record.ToApplication();
            var calculatedHash = WorkGraphIntegrity.ComputeContentHash(graph);
            if (!string.Equals(calculatedHash, record.ContentHash, StringComparison.OrdinalIgnoreCase))
            {
                return new(WorkGraphReadState.IntegrityFailure, ErrorMessage: "Work graph content hash does not match its payload.");
            }

            return new(WorkGraphReadState.Valid, graph);
        }
        catch (ArgumentException exception) when (exception.Message.Contains("hash", StringComparison.OrdinalIgnoreCase))
        {
            return new(WorkGraphReadState.IntegrityFailure, ErrorMessage: "Work graph content hash is invalid.");
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            InvalidOperationException or
            NullReferenceException)
        {
            _logger.LogWarning(exception, "Rejected invalid work graph at {Path}", path);
            return new(WorkGraphReadState.Invalid, ErrorMessage: "Work graph is semantically invalid.");
        }
    }

    private static void ValidateGuid(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier cannot be empty.", parameterName);
        }
    }
}
