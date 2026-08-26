using AIUsageMonitor.Application.Handoffs;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

/// <summary>
/// GUID-isolated create-once JSON storage for structured handoff packages. Reads are strictly
/// observational: invalid or unsupported authority is classified without quarantine or repair.
/// </summary>
public sealed class JsonHandoffPackageRepository : IHandoffPackageRepository
{
    private const string ExpectedRecordType = "handoff-package";

    private readonly ApplicationDataPaths _paths;
    private readonly JsonFileStore _files;
    private readonly ILogger<JsonHandoffPackageRepository> _logger;

    public JsonHandoffPackageRepository(
        ApplicationDataPaths paths,
        JsonFileStore files,
        ILogger<JsonHandoffPackageRepository> logger)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HandoffPackageRepositoryWriteResult> CreateAsync(
        HandoffPackage package,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);
        var canonicalPayloadBytes = HandoffPackageIntegrity.ComputeCanonicalPayloadBytes(package);
        if (package.SchemaVersion != HandoffPackageSchema.CurrentVersion ||
            package.Size.CanonicalPayloadBytes != canonicalPayloadBytes ||
            canonicalPayloadBytes > HandoffPackageLimits.MaxCanonicalPayloadBytes ||
            !string.Equals(
                HandoffPackageIntegrity.ComputeContentHash(package),
                package.ContentHash,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only a current, internally consistent handoff package can be written.", nameof(package));
        }

        var directory = _paths.GetHandoffPackageDirectory(package.ProjectId, package.PackageId);
        var path = _paths.GetHandoffPackageFile(package.ProjectId, package.PackageId);

        try
        {
            await _paths.EnsureProjectDirectoriesAsync(package.ProjectId, cancellationToken).ConfigureAwait(false);
            Directory.CreateDirectory(directory);
            await _files.CreateNewAsync(
                    path,
                    HandoffPackageRecord.FromApplication(package),
                    cancellationToken)
                .ConfigureAwait(false);
            return new(HandoffPackageRepositoryWriteStatus.Created);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Permission denied while creating handoff package {PackageId}", package.PackageId);
            return new(HandoffPackageRepositoryWriteStatus.Unavailable, "Handoff persistence is unavailable.");
        }
        catch (IOException) when (File.Exists(path))
        {
            _logger.LogInformation("Rejected immutable handoff package overwrite for {PackageId}", package.PackageId);
            return new(HandoffPackageRepositoryWriteStatus.PackageConflict, "The immutable handoff package already exists.");
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "I/O failure while creating handoff package {PackageId}", package.PackageId);
            return new(HandoffPackageRepositoryWriteStatus.Unavailable, "Handoff persistence is unavailable.");
        }
    }

    public async Task<HandoffPackageReadResult> GetAsync(
        Guid projectId,
        Guid packageId,
        CancellationToken cancellationToken = default)
    {
        ValidateGuid(projectId, nameof(projectId));
        ValidateGuid(packageId, nameof(packageId));

        var path = _paths.GetHandoffPackageFile(projectId, packageId);
        var result = await _files.ReadPreservingAsync<HandoffPackageRecord>(path, cancellationToken)
            .ConfigureAwait(false);

        return result.Status switch
        {
            FileReadStatus.Missing or FileReadStatus.Empty =>
                new(HandoffPackageReadState.Missing, ErrorMessage: "Handoff package is missing."),
            FileReadStatus.IoFailure or FileReadStatus.PermissionFailure =>
                new(HandoffPackageReadState.Unavailable, ErrorMessage: "Handoff package is unavailable."),
            FileReadStatus.UnsupportedSchema =>
                new(HandoffPackageReadState.UnsupportedFutureVersion, ErrorMessage: "Handoff storage schema is newer than supported."),
            FileReadStatus.Corrupt =>
                new(HandoffPackageReadState.Invalid, ErrorMessage: "Handoff package JSON is invalid."),
            FileReadStatus.Valid => MapValid(projectId, packageId, path, result.Value),
            _ => new(HandoffPackageReadState.Invalid, ErrorMessage: "Handoff package is invalid.")
        };
    }

    private HandoffPackageReadResult MapValid(
        Guid projectId,
        Guid packageId,
        string path,
        HandoffPackageRecord? record)
    {
        if (record is null)
        {
            return new(HandoffPackageReadState.Invalid, ErrorMessage: "Handoff package payload is missing.");
        }

        if (!string.Equals(record.RecordType, ExpectedRecordType, StringComparison.Ordinal))
        {
            return new(HandoffPackageReadState.Invalid, ErrorMessage: "Handoff package record type is invalid.");
        }

        if (record.SchemaVersion > HandoffPackageSchema.CurrentVersion)
        {
            return new(
                HandoffPackageReadState.UnsupportedFutureVersion,
                ErrorMessage: "Handoff package schema is newer than supported.");
        }

        if (record.SchemaVersion < HandoffPackageSchema.CurrentVersion)
        {
            return new(
                HandoffPackageReadState.MigrationRequired,
                ErrorMessage: "An explicit handoff package migrator is required before this package can be read.");
        }

        if (record.ProjectId != projectId || record.PackageId != packageId)
        {
            _logger.LogWarning(
                "Rejected handoff package identity mismatch for requested project {ProjectId}, package {PackageId}",
                projectId,
                packageId);
            return new(HandoffPackageReadState.Invalid, ErrorMessage: "Handoff identity does not match its GUID-derived path.");
        }

        if (!HandoffPackageReference.IsSha256(record.ContentHash))
        {
            return new(HandoffPackageReadState.IntegrityFailure, ErrorMessage: "Handoff package content hash is invalid.");
        }

        try
        {
            var package = record.ToApplication();
            var calculatedHash = HandoffPackageIntegrity.ComputeContentHash(package);
            if (!string.Equals(calculatedHash, record.ContentHash, StringComparison.OrdinalIgnoreCase))
            {
                return new(HandoffPackageReadState.IntegrityFailure, ErrorMessage: "Handoff package content hash does not match its payload.");
            }

            if (HandoffPackageIntegrity.ComputeCanonicalPayloadBytes(package) != package.Size.CanonicalPayloadBytes)
            {
                return new(HandoffPackageReadState.IntegrityFailure, ErrorMessage: "Handoff package size metadata does not match its payload.");
            }

            return new(HandoffPackageReadState.Valid, package);
        }
        catch (ArgumentException)
        {
            return new(HandoffPackageReadState.IntegrityFailure, ErrorMessage: "Handoff package content does not match its integrity evidence.");
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            InvalidOperationException or
            NullReferenceException)
        {
            _logger.LogWarning(exception, "Rejected invalid handoff package for {PackageId}", packageId);
            return new(HandoffPackageReadState.Invalid, ErrorMessage: "Handoff package is semantically invalid.");
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
