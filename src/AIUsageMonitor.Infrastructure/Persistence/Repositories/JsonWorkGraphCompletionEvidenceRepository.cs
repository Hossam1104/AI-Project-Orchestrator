using AIUsageMonitor.Application.Orchestration;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

/// <summary>
/// GUID-isolated create-once JSON storage for terminal work-graph evidence. Reads are
/// observational and never quarantine immutable authority.
/// </summary>
public sealed class JsonWorkGraphCompletionEvidenceRepository : IWorkGraphCompletionEvidenceRepository
{
    private const string ExpectedRecordType = "work-graph-completion-evidence";

    private readonly ApplicationDataPaths _paths;
    private readonly JsonFileStore _files;
    private readonly ILogger<JsonWorkGraphCompletionEvidenceRepository> _logger;

    public JsonWorkGraphCompletionEvidenceRepository(
        ApplicationDataPaths paths,
        JsonFileStore files,
        ILogger<JsonWorkGraphCompletionEvidenceRepository> logger)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WorkGraphCompletionEvidenceWriteResult> CreateAsync(
        WorkGraphCompletionEvidence evidence,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        var path = _paths.GetWorkGraphCompletionEvidenceFile(
            evidence.ProjectId,
            evidence.GraphReference.GraphId,
            evidence.NodeId);

        try
        {
            await _paths.EnsureProjectDirectoriesAsync(evidence.ProjectId, cancellationToken).ConfigureAwait(false);
            Directory.CreateDirectory(_paths.GetWorkGraphCompletionEvidenceDirectory(
                evidence.ProjectId,
                evidence.GraphReference.GraphId));
            await _files.CreateNewAsync(
                    path,
                    WorkGraphCompletionEvidenceRecord.FromApplication(evidence),
                    cancellationToken)
                .ConfigureAwait(false);
            return new(WorkGraphCompletionEvidenceWriteStatus.Created);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Permission denied while creating completion evidence {Path}", path);
            return new(WorkGraphCompletionEvidenceWriteStatus.Unavailable, "Completion evidence persistence is unavailable.");
        }
        catch (IOException) when (File.Exists(path))
        {
            return await ResolveExistingAsync(path, evidence, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "I/O failure while creating completion evidence {Path}", path);
            return new(WorkGraphCompletionEvidenceWriteStatus.Unavailable, "Completion evidence persistence is unavailable.");
        }
    }

    public async Task<WorkGraphCompletionEvidenceReadResult> ReadForGraphAsync(
        Guid projectId,
        WorkGraphReference graphReference,
        CancellationToken cancellationToken = default)
    {
        ValidateGuid(projectId, nameof(projectId));
        ArgumentNullException.ThrowIfNull(graphReference);

        if (graphReference.SchemaVersion > WorkGraphSchema.CurrentVersion)
        {
            return new(
                WorkGraphCompletionEvidenceReadState.UnsupportedFutureVersion,
                Array.Empty<WorkGraphCompletionEvidence>(),
                "The requested work-graph schema is newer than supported.");
        }

        if (graphReference.SchemaVersion < WorkGraphSchema.CurrentVersion)
        {
            return new(
                WorkGraphCompletionEvidenceReadState.MigrationRequired,
                Array.Empty<WorkGraphCompletionEvidence>(),
                "An explicit work-graph migrator is required.");
        }

        var directory = _paths.GetWorkGraphCompletionEvidenceDirectory(projectId, graphReference.GraphId);
        if (!Directory.Exists(directory))
        {
            return new(WorkGraphCompletionEvidenceReadState.Missing, Array.Empty<WorkGraphCompletionEvidence>());
        }

        string[] paths;
        try
        {
            paths = Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
                .OrderBy(static path => path, StringComparer.Ordinal)
                .ToArray();
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Permission denied while enumerating completion evidence {Directory}", directory);
            return new(WorkGraphCompletionEvidenceReadState.Unavailable, Array.Empty<WorkGraphCompletionEvidence>(), "Completion evidence is unavailable.");
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "I/O failure while enumerating completion evidence {Directory}", directory);
            return new(WorkGraphCompletionEvidenceReadState.Unavailable, Array.Empty<WorkGraphCompletionEvidence>(), "Completion evidence is unavailable.");
        }

        if (paths.Length > WorkGraphLimits.MaxNodes)
        {
            return new(WorkGraphCompletionEvidenceReadState.Invalid, Array.Empty<WorkGraphCompletionEvidence>(), "Completion evidence exceeds the graph bound.");
        }

        var values = new List<WorkGraphCompletionEvidence>(paths.Length);
        var nodeIds = new HashSet<Guid>();
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileResult = await _files.ReadPreservingAsync<WorkGraphCompletionEvidenceRecord>(path, cancellationToken)
                .ConfigureAwait(false);
            if (fileResult.Status is FileReadStatus.IoFailure or FileReadStatus.PermissionFailure)
            {
                return new(WorkGraphCompletionEvidenceReadState.Unavailable, Array.Empty<WorkGraphCompletionEvidence>(), "Completion evidence is unavailable.");
            }

            if (fileResult.Status == FileReadStatus.UnsupportedSchema)
            {
                return new(WorkGraphCompletionEvidenceReadState.UnsupportedFutureVersion, Array.Empty<WorkGraphCompletionEvidence>(), "Completion evidence storage schema is unsupported.");
            }

            if (fileResult.Status is FileReadStatus.Missing or FileReadStatus.Empty or FileReadStatus.Corrupt)
            {
                return new(WorkGraphCompletionEvidenceReadState.Invalid, Array.Empty<WorkGraphCompletionEvidence>(), "Completion evidence JSON is invalid.");
            }

            if (fileResult.Value?.GraphSchemaVersion > WorkGraphSchema.CurrentVersion)
            {
                return new(WorkGraphCompletionEvidenceReadState.UnsupportedFutureVersion, Array.Empty<WorkGraphCompletionEvidence>(), "Completion evidence references a newer work-graph schema.");
            }

            if (fileResult.Value?.GraphSchemaVersion < WorkGraphSchema.CurrentVersion)
            {
                return new(WorkGraphCompletionEvidenceReadState.MigrationRequired, Array.Empty<WorkGraphCompletionEvidence>(), "An explicit work-graph migrator is required.");
            }

            var mapped = MapRecord(fileResult.Value);
            if (mapped is null)
            {
                return new(WorkGraphCompletionEvidenceReadState.Invalid, Array.Empty<WorkGraphCompletionEvidence>(), "Completion evidence is semantically invalid.");
            }

            if (mapped.ProjectId != projectId || !SameGraphReference(mapped.GraphReference, graphReference))
            {
                return new(WorkGraphCompletionEvidenceReadState.Invalid, Array.Empty<WorkGraphCompletionEvidence>(), "Completion evidence does not belong to the requested graph.");
            }

            if (!TryGetNodeIdFromPath(path, out var pathNodeId) || pathNodeId != mapped.NodeId)
            {
                return new(WorkGraphCompletionEvidenceReadState.Invalid, Array.Empty<WorkGraphCompletionEvidence>(), "Completion evidence path identity is invalid.");
            }

            if (!nodeIds.Add(mapped.NodeId))
            {
                return new(WorkGraphCompletionEvidenceReadState.Conflict, Array.Empty<WorkGraphCompletionEvidence>(), "Conflicting terminal evidence exists for a node.");
            }

            values.Add(mapped);
        }

        return new(
            WorkGraphCompletionEvidenceReadState.Valid,
            values.OrderBy(static value => value.NodeId).ThenBy(static value => value.EvidenceId).ToArray());
    }

    private async Task<WorkGraphCompletionEvidenceWriteResult> ResolveExistingAsync(
        string path,
        WorkGraphCompletionEvidence evidence,
        CancellationToken cancellationToken)
    {
        var existing = await _files.ReadPreservingAsync<WorkGraphCompletionEvidenceRecord>(path, cancellationToken)
            .ConfigureAwait(false);
        if (existing.Status is FileReadStatus.IoFailure or FileReadStatus.PermissionFailure)
        {
            return new(WorkGraphCompletionEvidenceWriteStatus.Unavailable, "Existing completion evidence is unavailable.");
        }

        var mapped = existing.Status == FileReadStatus.Valid ? MapRecord(existing.Value) : null;
        if (mapped is not null && SameEvidence(mapped, evidence))
        {
            return new(WorkGraphCompletionEvidenceWriteStatus.AlreadyRecorded);
        }

        return new(
            WorkGraphCompletionEvidenceWriteStatus.Conflict,
            "A different terminal truth already exists for this node.");
    }

    private WorkGraphCompletionEvidence? MapRecord(WorkGraphCompletionEvidenceRecord? record)
    {
        if (record is null || !string.Equals(record.RecordType, ExpectedRecordType, StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            return record.ToApplication();
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            InvalidOperationException or
            NullReferenceException)
        {
            _logger.LogWarning(exception, "Rejected invalid immutable completion evidence record");
            return null;
        }
    }

    private static bool SameEvidence(
        WorkGraphCompletionEvidence left,
        WorkGraphCompletionEvidence right) =>
        left.EvidenceId == right.EvidenceId &&
        left.ProjectId == right.ProjectId &&
        SameGraphReference(left.GraphReference, right.GraphReference) &&
        left.NodeId == right.NodeId &&
        SameContractReference(left.ContractReference, right.ContractReference) &&
        left.State == right.State &&
        string.Equals(left.EvidenceReference, right.EvidenceReference, StringComparison.Ordinal) &&
        left.RecordedAt == right.RecordedAt;

    private static bool SameGraphReference(WorkGraphReference left, WorkGraphReference right) =>
        left.GraphId == right.GraphId &&
        left.SchemaVersion == right.SchemaVersion &&
        string.Equals(left.ContentHash, right.ContentHash, StringComparison.OrdinalIgnoreCase);

    private static bool SameContractReference(
        AIUsageMonitor.Application.Planning.PlanningExecutionContractReference left,
        AIUsageMonitor.Application.Planning.PlanningExecutionContractReference right) =>
        left.ContractId == right.ContractId &&
        left.Revision == right.Revision &&
        left.SchemaVersion == right.SchemaVersion &&
        string.Equals(left.ContentHash, right.ContentHash, StringComparison.OrdinalIgnoreCase);

    private static bool TryGetNodeIdFromPath(string path, out Guid nodeId)
    {
        nodeId = Guid.Empty;
        var fileName = Path.GetFileNameWithoutExtension(path);
        const string prefix = "node-";
        return fileName.StartsWith(prefix, StringComparison.Ordinal) &&
            Guid.TryParseExact(fileName[prefix.Length..], "D", out nodeId);
    }

    private static void ValidateGuid(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier cannot be empty.", parameterName);
        }
    }
}
