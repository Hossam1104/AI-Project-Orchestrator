using AIUsageMonitor.Application.Routing;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

/// <summary>Immutable create-once, GUID-isolated routing decision persistence.</summary>
public sealed class JsonRoutingDecisionRepository : IRoutingDecisionRepository
{
    private const string ExpectedRecordType = "routing-decision";

    private readonly ApplicationDataPaths _paths;
    private readonly JsonFileStore _files;
    private readonly ILogger<JsonRoutingDecisionRepository> _logger;

    public JsonRoutingDecisionRepository(
        ApplicationDataPaths paths,
        JsonFileStore files,
        ILogger<JsonRoutingDecisionRepository> logger)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RoutingDecisionRepositoryWriteResult> CreateAsync(
        RoutingDecision decision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ValidateForWrite(decision);

        var directory = _paths.GetRoutingDecisionDirectory(decision.ProjectId, decision.DecisionId);
        var path = _paths.GetRoutingDecisionFile(decision.ProjectId, decision.DecisionId);
        try
        {
            await _paths.EnsureProjectDirectoriesAsync(decision.ProjectId, cancellationToken).ConfigureAwait(false);
            Directory.CreateDirectory(directory);
            await _files.CreateNewAsync(path, RoutingDecisionRecord.FromApplication(decision), cancellationToken)
                .ConfigureAwait(false);
            return new(RoutingDecisionRepositoryWriteStatus.Created);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Permission denied while creating routing decision {DecisionId}", decision.DecisionId);
            return new(RoutingDecisionRepositoryWriteStatus.Unavailable, "Routing decision persistence is unavailable.");
        }
        catch (IOException) when (File.Exists(path))
        {
            _logger.LogInformation("Rejected immutable routing decision overwrite for {DecisionId}", decision.DecisionId);
            return new(RoutingDecisionRepositoryWriteStatus.DecisionConflict, "The immutable routing decision already exists.");
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "I/O failure while creating routing decision {DecisionId}", decision.DecisionId);
            return new(RoutingDecisionRepositoryWriteStatus.Unavailable, "Routing decision persistence is unavailable.");
        }
    }

    public async Task<RoutingDecisionReadResult> GetAsync(
        Guid projectId,
        Guid decisionId,
        CancellationToken cancellationToken = default)
    {
        ValidateGuid(projectId, nameof(projectId));
        ValidateGuid(decisionId, nameof(decisionId));

        var path = _paths.GetRoutingDecisionFile(projectId, decisionId);
        var result = await _files.ReadPreservingAsync<RoutingDecisionRecord>(path, cancellationToken)
            .ConfigureAwait(false);
        return result.Status switch
        {
            FileReadStatus.Missing => new(RoutingDecisionReadState.Missing, ErrorMessage: "Routing decision is missing."),
            FileReadStatus.Empty => new(RoutingDecisionReadState.Invalid, ErrorMessage: "Routing decision JSON is empty."),
            FileReadStatus.IoFailure or FileReadStatus.PermissionFailure => new(RoutingDecisionReadState.Unavailable, ErrorMessage: "Routing decision is unavailable."),
            FileReadStatus.UnsupportedSchema => new(RoutingDecisionReadState.UnsupportedFutureVersion, ErrorMessage: "Routing decision storage schema is newer than supported."),
            FileReadStatus.Corrupt => new(RoutingDecisionReadState.Invalid, ErrorMessage: "Routing decision JSON is invalid."),
            FileReadStatus.Valid => MapValid(projectId, decisionId, path, result.Value),
            _ => new(RoutingDecisionReadState.Invalid, ErrorMessage: "Routing decision is invalid.")
        };
    }

    private RoutingDecisionReadResult MapValid(
        Guid projectId,
        Guid decisionId,
        string path,
        RoutingDecisionRecord? record)
    {
        if (record is null)
        {
            return new(RoutingDecisionReadState.Invalid, ErrorMessage: "Routing decision payload is missing.");
        }

        if (!string.Equals(record.RecordType, ExpectedRecordType, StringComparison.Ordinal))
        {
            return new(RoutingDecisionReadState.Invalid, ErrorMessage: "Routing decision record type is invalid.");
        }

        if (record.SchemaVersion > RoutingDecisionSchema.CurrentVersion)
        {
            return new(RoutingDecisionReadState.UnsupportedFutureVersion, ErrorMessage: "Routing decision schema is newer than supported.");
        }

        if (record.SchemaVersion < RoutingDecisionSchema.CurrentVersion)
        {
            return new(RoutingDecisionReadState.MigrationRequired, ErrorMessage: "An explicit routing decision migrator is required before this decision can be read.");
        }

        if (record.ProjectId != projectId || record.DecisionId != decisionId)
        {
            return new(RoutingDecisionReadState.Invalid, ErrorMessage: "Routing decision identity does not match its GUID-derived path.");
        }

        if (!RoutingDecisionReference.IsSha256(record.ContentHash) ||
            !RoutingDecisionReference.IsSha256(record.InputFingerprint))
        {
            return new(RoutingDecisionReadState.IntegrityFailure, ErrorMessage: "Routing decision integrity evidence is invalid.");
        }

        if (record.Input is null ||
            !string.Equals(record.InputFingerprint, record.Input.InputFingerprint, StringComparison.OrdinalIgnoreCase) ||
            record.SelectedAgentId != record.Recommendation?.SelectedAgentId)
        {
            return new(RoutingDecisionReadState.IntegrityFailure, ErrorMessage: "Routing decision duplicate integrity fields do not agree.");
        }

        try
        {
            var withoutHash = record.ToApplicationForIntegrityValidation();
            if (RoutingDecisionIntegrity.ComputeCanonicalPayloadBytes(withoutHash).Length > RoutingDecisionLimits.MaxCanonicalPayloadBytes)
            {
                return new(RoutingDecisionReadState.Invalid, ErrorMessage: "Routing decision exceeds its canonical payload size bound.");
            }

            var calculatedHash = RoutingDecisionIntegrity.ComputeContentHash(withoutHash);
            if (!string.Equals(calculatedHash, record.ContentHash, StringComparison.OrdinalIgnoreCase))
            {
                return new(RoutingDecisionReadState.IntegrityFailure, ErrorMessage: "Routing decision content hash does not match its payload.");
            }

            return new(RoutingDecisionReadState.Valid, record.ToApplication());
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or NullReferenceException)
        {
            _logger.LogWarning(exception, "Rejected invalid or tampered routing decision at {Path}", path);
            return new(RoutingDecisionReadState.IntegrityFailure, ErrorMessage: "Routing decision content does not match its integrity evidence.");
        }
    }

    private static void ValidateForWrite(RoutingDecision decision)
    {
        if (decision.SchemaVersion != RoutingDecisionSchema.CurrentVersion ||
            RoutingDecisionIntegrity.ComputeCanonicalPayloadBytes(decision).Length > RoutingDecisionLimits.MaxCanonicalPayloadBytes ||
            !string.Equals(RoutingDecisionIntegrity.ComputeContentHash(decision), decision.ContentHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only a current, internally consistent routing decision can be written.", nameof(decision));
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
