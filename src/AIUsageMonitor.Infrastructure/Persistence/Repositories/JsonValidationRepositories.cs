using AIUsageMonitor.Application.Validation;
using AIUsageMonitor.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence.Repositories;

public sealed class JsonValidationPlanRepository : IValidationPlanRepository
{
    private readonly ApplicationDataPaths _paths;
    private readonly JsonFileStore _files;
    private readonly ILogger<JsonValidationPlanRepository> _logger;

    public JsonValidationPlanRepository(ApplicationDataPaths paths, JsonFileStore files, ILogger<JsonValidationPlanRepository> logger)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ValidationPlanRepositoryWriteResult> CreateAsync(ValidationPlan plan, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        Validate(plan);
        var path = _paths.GetValidationPlanFile(plan.ProjectId, plan.PlanId, plan.Revision);
        try
        {
            await _paths.EnsureProjectDirectoriesAsync(plan.ProjectId, cancellationToken).ConfigureAwait(false);
            Directory.CreateDirectory(_paths.GetValidationPlanDirectory(plan.ProjectId, plan.PlanId, plan.Revision));
            await _files.CreateNewAsync(path, ValidationPlanRecord.FromApplication(plan), cancellationToken).ConfigureAwait(false);
            return new(ValidationPlanRepositoryWriteStatus.Created);
        }
        catch (OperationCanceledException) { throw; }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Permission denied while creating validation plan {PlanId}", plan.PlanId);
            return new(ValidationPlanRepositoryWriteStatus.Unavailable, "Validation-plan persistence is unavailable.");
        }
        catch (IOException) when (File.Exists(path))
        {
            return new(ValidationPlanRepositoryWriteStatus.PlanConflict, "The immutable validation plan already exists.");
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "I/O failure while creating validation plan {PlanId}", plan.PlanId);
            return new(ValidationPlanRepositoryWriteStatus.Unavailable, "Validation-plan persistence is unavailable.");
        }
    }

    public async Task<ValidationPlanReadResult> GetAsync(Guid projectId, ValidationPlanReference reference, CancellationToken cancellationToken = default)
    {
        ValidateGuid(projectId, nameof(projectId));
        ArgumentNullException.ThrowIfNull(reference);
        if (reference.ProjectId != projectId) return new(ValidationPlanReadState.IntegrityFailure, ErrorMessage: "Validation-plan project does not match its exact reference.");
        var path = _paths.GetValidationPlanFile(projectId, reference.PlanId, reference.Revision);
        var result = await _files.ReadPreservingAsync<ValidationPlanRecord>(path, cancellationToken).ConfigureAwait(false);
        return result.Status switch
        {
            FileReadStatus.Missing => new(ValidationPlanReadState.Missing),
            FileReadStatus.UnsupportedSchema => new(ValidationPlanReadState.UnsupportedFutureVersion, ErrorMessage: "Validation-plan storage schema is newer than supported."),
            FileReadStatus.IoFailure or FileReadStatus.PermissionFailure => new(ValidationPlanReadState.Unavailable, ErrorMessage: "Validation-plan persistence is unavailable."),
            FileReadStatus.Empty or FileReadStatus.Corrupt => new(ValidationPlanReadState.Invalid, ErrorMessage: "Validation-plan JSON is invalid."),
            FileReadStatus.Valid => Map(projectId, reference, result.Value),
            _ => new(ValidationPlanReadState.Invalid, ErrorMessage: "Validation plan is invalid.")
        };
    }

    private ValidationPlanReadResult Map(Guid projectId, ValidationPlanReference reference, ValidationPlanRecord? record)
    {
        if (record is null || record.RecordType != "validation-plan") return new(ValidationPlanReadState.Invalid, ErrorMessage: "Validation-plan record type is invalid.");
        if (record.SchemaVersion > ValidationSchema.CurrentVersion) return new(ValidationPlanReadState.UnsupportedFutureVersion);
        if (record.SchemaVersion < ValidationSchema.CurrentVersion) return new(ValidationPlanReadState.MigrationRequired);
        if (record.ProjectId != projectId || record.PlanId != reference.PlanId || record.Revision != reference.Revision ||
            !string.Equals(record.ContentHash, reference.ContentHash, StringComparison.OrdinalIgnoreCase)) return new(ValidationPlanReadState.IntegrityFailure, ErrorMessage: "Validation-plan identity does not match its exact reference.");
        try
        {
            var value = record.ToApplication();
            return ValidationIntegrity.ComputePlanPayloadBytes(value) > ValidationLimits.MaxCanonicalPayloadBytes ||
                !string.Equals(value.ContentHash, record.ContentHash, StringComparison.OrdinalIgnoreCase)
                ? new(ValidationPlanReadState.IntegrityFailure, ErrorMessage: "Validation-plan content hash is invalid.")
                : new(ValidationPlanReadState.Valid, value);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or NullReferenceException)
        {
            _logger.LogWarning(exception, "Rejected invalid validation plan {PlanId}", reference.PlanId);
            return new(ValidationPlanReadState.IntegrityFailure, ErrorMessage: "Validation-plan content does not match its integrity evidence.");
        }
    }

    private static void Validate(ValidationPlan value)
    {
        if (value.SchemaVersion != ValidationSchema.CurrentVersion || !string.Equals(value.ContentHash, ValidationIntegrity.ComputePlanHash(value), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only a current, internally consistent validation plan can be written.", nameof(value));
    }

    private static void ValidateGuid(Guid value, string name)
    {
        if (value == Guid.Empty) throw new ArgumentException("Identifier cannot be empty.", name);
    }
}

public sealed class JsonValidationEvidenceRepository : IValidationEvidenceRepository
{
    private readonly ApplicationDataPaths _paths;
    private readonly JsonFileStore _files;
    private readonly ILogger<JsonValidationEvidenceRepository> _logger;

    public JsonValidationEvidenceRepository(ApplicationDataPaths paths, JsonFileStore files, ILogger<JsonValidationEvidenceRepository> logger)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ValidationEvidenceRepositoryWriteResult> CreateAsync(ValidationEvidence evidence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        Validate(evidence);
        var path = _paths.GetValidationEvidenceFile(evidence.ProjectId, evidence.PlanReference.PlanId, evidence.PlanReference.Revision, evidence.EvidenceId);
        try
        {
            await _paths.EnsureProjectDirectoriesAsync(evidence.ProjectId, cancellationToken).ConfigureAwait(false);
            Directory.CreateDirectory(_paths.GetValidationEvidenceDirectory(evidence.ProjectId, evidence.PlanReference.PlanId, evidence.PlanReference.Revision, evidence.EvidenceId));
            await _files.CreateNewAsync(path, ValidationEvidenceRecord.FromApplication(evidence), cancellationToken).ConfigureAwait(false);
            return new(ValidationEvidenceRepositoryWriteStatus.Created);
        }
        catch (OperationCanceledException) { throw; }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Permission denied while creating validation evidence {EvidenceId}", evidence.EvidenceId);
            return new(ValidationEvidenceRepositoryWriteStatus.Unavailable, "Validation-evidence persistence is unavailable.");
        }
        catch (IOException) when (File.Exists(path))
        {
            return new(ValidationEvidenceRepositoryWriteStatus.EvidenceConflict, "The immutable validation evidence already exists.");
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "I/O failure while creating validation evidence {EvidenceId}", evidence.EvidenceId);
            return new(ValidationEvidenceRepositoryWriteStatus.Unavailable, "Validation-evidence persistence is unavailable.");
        }
    }

    public async Task<ValidationEvidenceReadResult> GetAsync(Guid projectId, ValidationPlanReference planReference, ValidationEvidenceReference evidenceReference, CancellationToken cancellationToken = default)
    {
        ValidateGuid(projectId, nameof(projectId));
        ArgumentNullException.ThrowIfNull(planReference);
        ArgumentNullException.ThrowIfNull(evidenceReference);
        if (planReference.ProjectId != projectId) return new(ValidationEvidenceReadState.IntegrityFailure, ErrorMessage: "Validation-evidence project does not match its exact plan reference.");
        var path = _paths.GetValidationEvidenceFile(projectId, planReference.PlanId, planReference.Revision, evidenceReference.EvidenceId);
        var result = await _files.ReadPreservingAsync<ValidationEvidenceRecord>(path, cancellationToken).ConfigureAwait(false);
        return result.Status switch
        {
            FileReadStatus.Missing => new(ValidationEvidenceReadState.Missing),
            FileReadStatus.UnsupportedSchema => new(ValidationEvidenceReadState.UnsupportedFutureVersion),
            FileReadStatus.IoFailure or FileReadStatus.PermissionFailure => new(ValidationEvidenceReadState.Unavailable),
            FileReadStatus.Empty or FileReadStatus.Corrupt => new(ValidationEvidenceReadState.Invalid),
            FileReadStatus.Valid => Map(projectId, planReference, evidenceReference, result.Value),
            _ => new(ValidationEvidenceReadState.Invalid)
        };
    }

    public async Task<ValidationEvidenceSetReadResult> GetForPlanAsync(Guid projectId, ValidationPlanReference planReference, CancellationToken cancellationToken = default)
    {
        ValidateGuid(projectId, nameof(projectId));
        ArgumentNullException.ThrowIfNull(planReference);
        if (planReference.ProjectId != projectId) return new(ValidationEvidenceSetReadState.IntegrityFailure, ErrorMessage: "Validation-evidence project does not match its exact plan reference.");
        var directory = _paths.GetValidationEvidenceRevisionDirectory(projectId, planReference.PlanId, planReference.Revision);
        var values = new List<ValidationEvidence>();
        string[] children;
        try { children = Directory.GetDirectories(directory); }
        catch (DirectoryNotFoundException) { return new(ValidationEvidenceSetReadState.Valid, Array.Empty<ValidationEvidence>()); }
        catch (IOException) { return new(ValidationEvidenceSetReadState.Unavailable, ErrorMessage: "Validation-evidence enumeration is unavailable."); }
        catch (UnauthorizedAccessException) { return new(ValidationEvidenceSetReadState.Unavailable, ErrorMessage: "Validation-evidence enumeration permission was denied."); }
        if (children.Length > ValidationLimits.MaxEvidenceItems) return new(ValidationEvidenceSetReadState.CapacityExceeded, ErrorMessage: "Validation-evidence capacity was exceeded for the exact plan revision.");
        foreach (var child in children)
        {
            var file = Path.Combine(child, "evidence.json");
            var result = await _files.ReadPreservingAsync<ValidationEvidenceRecord>(file, cancellationToken).ConfigureAwait(false);
            if (result.Status != FileReadStatus.Valid || result.Value is null)
                return new(MapSetState(result.Status), ErrorMessage: "An exact-plan validation-evidence record is incomplete or unreadable.");
            if (result.Value.EvidenceId == Guid.Empty || result.Value.SchemaVersion <= 0 || !ValidationPlanReference.IsSha256(result.Value.ContentHash))
                return new(ValidationEvidenceSetReadState.IntegrityFailure, ErrorMessage: "An exact-plan validation-evidence record has an invalid identity.");
            var mapped = Map(projectId, planReference, new ValidationEvidenceReference(result.Value.EvidenceId, result.Value.SchemaVersion, result.Value.ContentHash), result.Value);
            if (!mapped.IsValid || mapped.Evidence is null) return new(ValidationEvidenceSetReadState.IntegrityFailure, ErrorMessage: mapped.ErrorMessage ?? "An exact-plan validation-evidence record failed integrity validation.");
            values.Add(mapped.Evidence);
        }
        return new(ValidationEvidenceSetReadState.Valid, values.OrderBy(value => value.EvidenceId).ToArray());
    }

    private ValidationEvidenceReadResult Map(Guid projectId, ValidationPlanReference planReference, ValidationEvidenceReference evidenceReference, ValidationEvidenceRecord? record)
    {
        if (record is null || record.RecordType != "validation-evidence") return new(ValidationEvidenceReadState.Invalid);
        if (record.SchemaVersion > ValidationSchema.CurrentVersion) return new(ValidationEvidenceReadState.UnsupportedFutureVersion);
        if (record.SchemaVersion < ValidationSchema.CurrentVersion) return new(ValidationEvidenceReadState.MigrationRequired);
        if (record.ProjectId != projectId || record.EvidenceId != evidenceReference.EvidenceId || record.PlanReference is null ||
            record.PlanReference.ProjectId != planReference.ProjectId || record.PlanReference.PlanId != planReference.PlanId || record.PlanReference.Revision != planReference.Revision ||
            !string.Equals(record.PlanReference.ContentHash, planReference.ContentHash, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(record.ContentHash, evidenceReference.ContentHash, StringComparison.OrdinalIgnoreCase)) return new(ValidationEvidenceReadState.IntegrityFailure, ErrorMessage: "Validation-evidence identity does not match its exact path or reference.");
        try
        {
            var value = record.ToApplication();
            return ValidationIntegrity.ComputeEvidencePayloadBytes(value) > ValidationLimits.MaxCanonicalPayloadBytes ||
                !string.Equals(value.ContentHash, record.ContentHash, StringComparison.OrdinalIgnoreCase)
                ? new(ValidationEvidenceReadState.IntegrityFailure, ErrorMessage: "Validation-evidence content hash is invalid.")
                : new(ValidationEvidenceReadState.Valid, value);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or NullReferenceException)
        {
            _logger.LogWarning(exception, "Rejected invalid validation evidence {EvidenceId}", evidenceReference.EvidenceId);
            return new(ValidationEvidenceReadState.IntegrityFailure, ErrorMessage: "Validation-evidence content does not match its integrity evidence.");
        }
    }

    private static void Validate(ValidationEvidence value)
    {
        if (value.SchemaVersion != ValidationSchema.CurrentVersion || !string.Equals(value.ContentHash, ValidationIntegrity.ComputeEvidenceHash(value), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only current, internally consistent validation evidence can be written.", nameof(value));
    }

    private static void ValidateGuid(Guid value, string name)
    {
        if (value == Guid.Empty) throw new ArgumentException("Identifier cannot be empty.", name);
    }

    private static ValidationEvidenceSetReadState MapSetState(FileReadStatus status) => status switch
    {
        FileReadStatus.UnsupportedSchema => ValidationEvidenceSetReadState.UnsupportedFutureVersion,
        FileReadStatus.IoFailure or FileReadStatus.PermissionFailure => ValidationEvidenceSetReadState.Unavailable,
        FileReadStatus.Empty or FileReadStatus.Corrupt => ValidationEvidenceSetReadState.Invalid,
        _ => ValidationEvidenceSetReadState.IntegrityFailure
    };
}

public sealed class JsonValidationGateDecisionRepository : IValidationGateDecisionRepository
{
    private readonly ApplicationDataPaths _paths;
    private readonly JsonFileStore _files;
    private readonly ILogger<JsonValidationGateDecisionRepository> _logger;

    public JsonValidationGateDecisionRepository(ApplicationDataPaths paths, JsonFileStore files, ILogger<JsonValidationGateDecisionRepository> logger)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ValidationDecisionRepositoryWriteResult> CreateAsync(ValidationGateDecision decision, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(decision);
        Validate(decision);
        var path = _paths.GetValidationDecisionFile(decision.ProjectId, decision.DecisionId);
        try
        {
            await _paths.EnsureProjectDirectoriesAsync(decision.ProjectId, cancellationToken).ConfigureAwait(false);
            Directory.CreateDirectory(_paths.GetValidationDecisionDirectory(decision.ProjectId, decision.DecisionId));
            await _files.CreateNewAsync(path, ValidationGateDecisionRecord.FromApplication(decision), cancellationToken).ConfigureAwait(false);
            return new(ValidationDecisionRepositoryWriteStatus.Created);
        }
        catch (OperationCanceledException) { throw; }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Permission denied while creating validation decision {DecisionId}", decision.DecisionId);
            return new(ValidationDecisionRepositoryWriteStatus.Unavailable, "Validation-decision persistence is unavailable.");
        }
        catch (IOException) when (File.Exists(path))
        {
            return new(ValidationDecisionRepositoryWriteStatus.DecisionConflict, "The immutable validation decision already exists.");
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "I/O failure while creating validation decision {DecisionId}", decision.DecisionId);
            return new(ValidationDecisionRepositoryWriteStatus.Unavailable, "Validation-decision persistence is unavailable.");
        }
    }

    public async Task<ValidationDecisionReadResult> GetAsync(Guid projectId, Guid decisionId, CancellationToken cancellationToken = default)
    {
        ValidateGuid(projectId, nameof(projectId));
        ValidateGuid(decisionId, nameof(decisionId));
        var path = _paths.GetValidationDecisionFile(projectId, decisionId);
        var result = await _files.ReadPreservingAsync<ValidationGateDecisionRecord>(path, cancellationToken).ConfigureAwait(false);
        return result.Status switch
        {
            FileReadStatus.Missing => new(ValidationDecisionReadState.Missing),
            FileReadStatus.UnsupportedSchema => new(ValidationDecisionReadState.UnsupportedFutureVersion),
            FileReadStatus.IoFailure or FileReadStatus.PermissionFailure => new(ValidationDecisionReadState.Unavailable),
            FileReadStatus.Empty or FileReadStatus.Corrupt => new(ValidationDecisionReadState.Invalid),
            FileReadStatus.Valid => Map(projectId, decisionId, result.Value),
            _ => new(ValidationDecisionReadState.Invalid)
        };
    }

    private ValidationDecisionReadResult Map(Guid projectId, Guid decisionId, ValidationGateDecisionRecord? record)
    {
        if (record is null || record.RecordType != "validation-gate-decision") return new(ValidationDecisionReadState.Invalid);
        if (record.SchemaVersion > ValidationSchema.CurrentVersion) return new(ValidationDecisionReadState.UnsupportedFutureVersion);
        if (record.SchemaVersion < ValidationSchema.CurrentVersion) return new(ValidationDecisionReadState.MigrationRequired);
        if (record.ProjectId != projectId || record.DecisionId != decisionId) return new(ValidationDecisionReadState.IntegrityFailure, ErrorMessage: "Validation-decision identity does not match its path.");
        try
        {
            var value = record.ToApplication();
            return ValidationIntegrity.ComputeDecisionPayloadBytes(value) > ValidationLimits.MaxCanonicalPayloadBytes ||
                !string.Equals(value.ContentHash, record.ContentHash, StringComparison.OrdinalIgnoreCase)
                ? new(ValidationDecisionReadState.IntegrityFailure, ErrorMessage: "Validation-decision content hash is invalid.")
                : new(ValidationDecisionReadState.Valid, value);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or NullReferenceException)
        {
            _logger.LogWarning(exception, "Rejected invalid validation decision {DecisionId}", decisionId);
            return new(ValidationDecisionReadState.IntegrityFailure, ErrorMessage: "Validation-decision content does not match its integrity evidence.");
        }
    }

    private static void Validate(ValidationGateDecision value)
    {
        if (value.SchemaVersion != ValidationSchema.CurrentVersion || !string.Equals(value.ContentHash, ValidationIntegrity.ComputeDecisionHash(value), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only a current, internally consistent validation decision can be written.", nameof(value));
    }

    private static void ValidateGuid(Guid value, string name)
    {
        if (value == Guid.Empty) throw new ArgumentException("Identifier cannot be empty.", name);
    }
}
