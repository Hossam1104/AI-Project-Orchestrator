using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

/// <summary>
/// GUID-isolated immutable JSON repository for planning contract revisions. It never replaces an
/// existing revision and never treats invalid, unsupported, or tampered data as valid.
/// </summary>
public sealed class JsonPlanningExecutionContractRepository : IPlanningExecutionContractRepository
{
    private const string ExpectedRecordType = "planning-execution-contract";

    private readonly ApplicationDataPaths _paths;
    private readonly JsonFileStore _files;
    private readonly ILogger<JsonPlanningExecutionContractRepository> _logger;

    public JsonPlanningExecutionContractRepository(
        ApplicationDataPaths paths,
        JsonFileStore files,
        ILogger<JsonPlanningExecutionContractRepository> logger)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PlanningContractRepositoryWriteResult> CreateAsync(
        PlanningExecutionContract contract,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ValidateGuid(contract.ProjectId, nameof(contract.ProjectId));
        ValidateGuid(contract.ContractId, nameof(contract.ContractId));
        if (contract.SchemaVersion != PlanningExecutionContractSchema.CurrentVersion)
        {
            throw new ArgumentException("Only the current contract schema can be written.", nameof(contract));
        }

        var directory = _paths.GetPlanningExecutionContractDirectory(contract.ProjectId, contract.ContractId);
        var path = _paths.GetPlanningExecutionContractRevisionFile(
            contract.ProjectId,
            contract.ContractId,
            contract.Revision);

        try
        {
            if (contract.Revision == 1)
            {
                if (contract.PreviousRevision is not null || contract.PreviousContentHash is not null)
                {
                    return new(
                        PlanningContractRepositoryWriteStatus.InvalidLineage,
                        "Revision 1 cannot carry predecessor evidence.");
                }
            }
            else
            {
                var predecessorResult = await GetAsync(
                        contract.ProjectId,
                        contract.ContractId,
                        contract.Revision - 1,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (predecessorResult.State == PlanningContractReadState.Missing)
                {
                    return new(
                        PlanningContractRepositoryWriteStatus.PredecessorMissing,
                        "The immediate predecessor revision is missing.");
                }

                if (predecessorResult.State == PlanningContractReadState.Unavailable)
                {
                    return new(
                        PlanningContractRepositoryWriteStatus.Unavailable,
                        predecessorResult.ErrorMessage ?? "The predecessor could not be read safely.");
                }

                if (!predecessorResult.IsValid || predecessorResult.Contract is null)
                {
                    return new(
                        PlanningContractRepositoryWriteStatus.InvalidLineage,
                        predecessorResult.ErrorMessage ?? "The predecessor is not a valid contract revision.");
                }

                if (!HasMatchingLineage(contract, predecessorResult.Contract))
                {
                    return new(
                        PlanningContractRepositoryWriteStatus.InvalidLineage,
                        "The contract revision does not match its durable predecessor lineage.");
                }
            }

            await _paths.EnsureProjectDirectoriesAsync(contract.ProjectId, cancellationToken).ConfigureAwait(false);
            Directory.CreateDirectory(directory);
            await _files.CreateNewAsync(
                    path,
                    PlanningExecutionContractRecord.FromApplication(contract),
                    cancellationToken)
                .ConfigureAwait(false);
            return new(PlanningContractRepositoryWriteStatus.Created);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Permission denied while creating planning contract revision {Path}", path);
            return new(PlanningContractRepositoryWriteStatus.Unavailable, "Contract persistence is unavailable.");
        }
        catch (IOException) when (File.Exists(path))
        {
            _logger.LogInformation("Rejected immutable planning contract overwrite at {Path}", path);
            return new(PlanningContractRepositoryWriteStatus.RevisionConflict, "The immutable contract revision already exists.");
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "I/O failure while creating planning contract revision {Path}", path);
            return new(PlanningContractRepositoryWriteStatus.Unavailable, "Contract persistence is unavailable.");
        }
    }

    public async Task<PlanningContractReadResult> GetAsync(
        Guid projectId,
        Guid contractId,
        int revision,
        CancellationToken cancellationToken = default)
    {
        ValidateGuid(projectId, nameof(projectId));
        ValidateGuid(contractId, nameof(contractId));
        if (revision <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(revision));
        }

        var currentResult = await ReadRevisionAsync(projectId, contractId, revision, cancellationToken)
            .ConfigureAwait(false);
        if (!currentResult.IsValid || currentResult.Contract is null || revision == 1)
        {
            return currentResult;
        }

        var current = currentResult.Contract;
        while (current.Revision > 1)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (current.PreviousRevision != current.Revision - 1 ||
                string.IsNullOrWhiteSpace(current.PreviousContentHash))
            {
                return InvalidLineage("Contract revision does not identify its immediate predecessor.");
            }

            var predecessorRevision = current.Revision - 1;
            var predecessorResult = await ReadRevisionAsync(
                    projectId,
                    contractId,
                    predecessorRevision,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!predecessorResult.IsValid || predecessorResult.Contract is null)
            {
                return MapBrokenPredecessor(predecessorRevision, predecessorResult);
            }

            if (!HasMatchingLineage(current, predecessorResult.Contract))
            {
                return InvalidLineage("Contract revision does not match its durable predecessor lineage.");
            }

            current = predecessorResult.Contract;
        }

        if (current.PreviousRevision is not null || current.PreviousContentHash is not null)
        {
            return InvalidLineage("Revision 1 cannot carry predecessor evidence.");
        }

        return currentResult;
    }

    public async Task<PlanningContractReadResult> GetLatestAsync(
        Guid projectId,
        Guid contractId,
        CancellationToken cancellationToken = default)
    {
        var revisions = await ListRevisionsAsync(projectId, contractId, cancellationToken).ConfigureAwait(false);
        if (revisions.State == PlanningContractReadState.Missing)
        {
            return new(PlanningContractReadState.Missing, ErrorMessage: revisions.ErrorMessage);
        }

        if (!revisions.IsValid || revisions.Revisions.Count == 0)
        {
            return new(revisions.State, ErrorMessage: revisions.ErrorMessage);
        }

        return new(PlanningContractReadState.Valid, revisions.Revisions[^1]);
    }

    public async Task<PlanningContractRevisionListResult> ListRevisionsAsync(
        Guid projectId,
        Guid contractId,
        CancellationToken cancellationToken = default)
    {
        ValidateGuid(projectId, nameof(projectId));
        ValidateGuid(contractId, nameof(contractId));

        var directory = _paths.GetPlanningExecutionContractDirectory(projectId, contractId);
        if (!Directory.Exists(directory))
        {
            return new(PlanningContractReadState.Missing, Array.Empty<PlanningExecutionContract>());
        }

        try
        {
            var revisionNumbers = Directory
                .EnumerateFiles(directory, "revision-*.json", SearchOption.TopDirectoryOnly)
                .Select(static path => ParseRevision(Path.GetFileName(path)))
                .Where(static revision => revision is not null)
                .Select(static revision => revision!.Value)
                .Distinct()
                .OrderBy(static revision => revision)
                .ToArray();

            if (revisionNumbers.Length == 0)
            {
                return new(PlanningContractReadState.Missing, Array.Empty<PlanningExecutionContract>());
            }

            for (var index = 0; index < revisionNumbers.Length; index++)
            {
                var expectedRevision = index + 1;
                if (revisionNumbers[index] != expectedRevision)
                {
                    return new(
                        PlanningContractReadState.Invalid,
                        Array.Empty<PlanningExecutionContract>(),
                        "Contract revision lineage contains a gap.");
                }
            }

            var contracts = new List<PlanningExecutionContract>(revisionNumbers.Length);
            foreach (var revision in revisionNumbers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await GetAsync(projectId, contractId, revision, cancellationToken).ConfigureAwait(false);
                if (!result.IsValid || result.Contract is null)
                {
                    return new(result.State, Array.Empty<PlanningExecutionContract>(), result.ErrorMessage);
                }

                contracts.Add(result.Contract);
            }

            return new(PlanningContractReadState.Valid, contracts.AsReadOnly());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException exception)
        {
            return new(PlanningContractReadState.Unavailable, Array.Empty<PlanningExecutionContract>(), exception.Message);
        }
        catch (IOException exception)
        {
            return new(PlanningContractReadState.Unavailable, Array.Empty<PlanningExecutionContract>(), exception.Message);
        }
    }

    private async Task<PlanningContractReadResult> ReadRevisionAsync(
        Guid projectId,
        Guid contractId,
        int revision,
        CancellationToken cancellationToken)
    {
        var path = _paths.GetPlanningExecutionContractRevisionFile(projectId, contractId, revision);
        var result = await _files.ReadPreservingAsync<PlanningExecutionContractRecord>(path, cancellationToken)
            .ConfigureAwait(false);
        return MapReadResult(projectId, contractId, revision, path, result);
    }

    private static PlanningContractReadResult MapBrokenPredecessor(
        int predecessorRevision,
        PlanningContractReadResult predecessorResult)
    {
        if (predecessorResult.State == PlanningContractReadState.Missing)
        {
            return InvalidLineage($"Contract predecessor revision {predecessorRevision} is missing.");
        }

        return new(
            predecessorResult.State,
            ErrorMessage: predecessorResult.ErrorMessage ??
                $"Contract predecessor revision {predecessorRevision} is not valid.");
    }

    private static PlanningContractReadResult InvalidLineage(string message) =>
        new(PlanningContractReadState.Invalid, ErrorMessage: message);

    private static bool HasMatchingLineage(
        PlanningExecutionContract contract,
        PlanningExecutionContract predecessor)
    {
        return contract.PreviousRevision == predecessor.Revision &&
            string.Equals(contract.PreviousContentHash, predecessor.ContentHash, StringComparison.OrdinalIgnoreCase) &&
            contract.ProjectId == predecessor.ProjectId &&
            contract.ContractId == predecessor.ContractId &&
            string.Equals(contract.OwnerReference, predecessor.OwnerReference, StringComparison.Ordinal) &&
            SameWorkItemIdentity(contract, predecessor);
    }

    private static bool SameWorkItemIdentity(
        PlanningExecutionContract left,
        PlanningExecutionContract right)
    {
        return left.WorkItem.Source == right.WorkItem.Source &&
            string.Equals(left.WorkItem.Reference, right.WorkItem.Reference, StringComparison.Ordinal) &&
            string.Equals(left.WorkItem.Title, right.WorkItem.Title, StringComparison.Ordinal);
    }

    private PlanningContractReadResult MapReadResult(
        Guid projectId,
        Guid contractId,
        int revision,
        string path,
        FileReadResult<PlanningExecutionContractRecord> result)
    {
        return result.Status switch
        {
            FileReadStatus.Missing or FileReadStatus.Empty =>
                new(PlanningContractReadState.Missing, ErrorMessage: "Contract revision is missing."),
            FileReadStatus.IoFailure or FileReadStatus.PermissionFailure =>
                new(PlanningContractReadState.Unavailable, ErrorMessage: "Contract revision is unavailable."),
            FileReadStatus.UnsupportedSchema =>
                new(PlanningContractReadState.UnsupportedFutureVersion, ErrorMessage: "Contract storage schema is newer than supported."),
            FileReadStatus.Corrupt =>
                new(PlanningContractReadState.Invalid, ErrorMessage: "Contract revision JSON is invalid."),
            FileReadStatus.Valid => MapValid(projectId, contractId, revision, path, result.Value),
            _ => new(PlanningContractReadState.Invalid, ErrorMessage: "Contract revision is invalid.")
        };
    }

    private PlanningContractReadResult MapValid(
        Guid projectId,
        Guid contractId,
        int revision,
        string path,
        PlanningExecutionContractRecord? record)
    {
        if (record is null)
        {
            return new(PlanningContractReadState.Invalid, ErrorMessage: "Contract payload is missing.");
        }

        if (!string.Equals(record.RecordType, ExpectedRecordType, StringComparison.Ordinal))
        {
            return new(PlanningContractReadState.Invalid, ErrorMessage: "Contract record type is invalid.");
        }

        if (record.SchemaVersion > PlanningExecutionContractSchema.CurrentVersion)
        {
            return new(
                PlanningContractReadState.UnsupportedFutureVersion,
                ErrorMessage: $"Contract schema version {record.SchemaVersion} is newer than supported version {PlanningExecutionContractSchema.CurrentVersion}.");
        }

        if (record.SchemaVersion < PlanningExecutionContractSchema.CurrentVersion)
        {
            return new(
                PlanningContractReadState.MigrationRequired,
                ErrorMessage: "An explicit contract migrator is required before this revision can be read.");
        }

        if (record.ProjectId != projectId || record.ContractId != contractId || record.Revision != revision)
        {
            _logger.LogWarning(
                "Rejected planning contract identity mismatch at {Path}; requested project {ProjectId}, contract {ContractId}, revision {Revision}",
                path,
                projectId,
                contractId,
                revision);
            return new(PlanningContractReadState.Invalid, ErrorMessage: "Contract identity does not match its GUID-derived path.");
        }

        try
        {
            var contract = record.ToApplication();
            var calculatedHash = PlanningExecutionContractIntegrity.ComputeContentHash(contract);
            if (!string.Equals(calculatedHash, record.ContentHash, StringComparison.OrdinalIgnoreCase))
            {
                return new(PlanningContractReadState.IntegrityFailure, ErrorMessage: "Contract content hash does not match its payload.");
            }

            return new(PlanningContractReadState.Valid, contract);
        }
        catch (ArgumentException exception) when (exception.Message.Contains("hash", StringComparison.OrdinalIgnoreCase))
        {
            return new(PlanningContractReadState.IntegrityFailure, ErrorMessage: "Contract content hash is invalid.");
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            InvalidOperationException or
            NullReferenceException)
        {
            _logger.LogWarning(exception, "Rejected invalid planning contract at {Path}", path);
            return new(PlanningContractReadState.Invalid, ErrorMessage: "Contract revision is semantically invalid.");
        }
    }

    private static int? ParseRevision(string fileName)
    {
        const string prefix = "revision-";
        const string suffix = ".json";
        if (!fileName.StartsWith(prefix, StringComparison.Ordinal) ||
            !fileName.EndsWith(suffix, StringComparison.Ordinal))
        {
            return null;
        }

        var value = fileName[prefix.Length..^suffix.Length];
        return int.TryParse(value, out var revision) && revision > 0 ? revision : null;
    }

    private static void ValidateGuid(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier cannot be empty.", parameterName);
        }
    }
}
