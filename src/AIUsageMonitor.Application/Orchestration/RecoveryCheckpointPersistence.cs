using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIUsageMonitor.Application.Orchestration;

/// <summary>Semantic version of the two-slot continuation-head authority.</summary>
public static class ContinuationHeadSchema
{
    public const int CurrentVersion = 1;
}

/// <summary>Project-scoped mutable pointer to immutable checkpoint references.</summary>
public sealed class ContinuationHead
{
    public ContinuationHead(
        Guid projectId,
        int schemaVersion,
        long generation,
        RecoveryCheckpointReference latestCheckpointReference,
        RecoveryCheckpointReference? lastSafeCheckpointReference,
        DateTimeOffset updatedAt,
        string? contentHash = null)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        if (schemaVersion != ContinuationHeadSchema.CurrentVersion)
        {
            throw new ArgumentException(
                $"Only continuation-head schema {ContinuationHeadSchema.CurrentVersion} is supported.",
                nameof(schemaVersion));
        }

        if (generation <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(generation));
        }

        LatestCheckpointReference = latestCheckpointReference ?? throw new ArgumentNullException(nameof(latestCheckpointReference));
        if (lastSafeCheckpointReference is not null &&
            lastSafeCheckpointReference.SchemaVersion <= 0)
        {
            throw new ArgumentException("The last-safe checkpoint reference is invalid.", nameof(lastSafeCheckpointReference));
        }

        if (updatedAt == default)
        {
            throw new ArgumentException("Head update time is required.", nameof(updatedAt));
        }

        ProjectId = projectId;
        SchemaVersion = schemaVersion;
        Generation = generation;
        LastSafeCheckpointReference = lastSafeCheckpointReference;
        UpdatedAt = updatedAt;
        ContentHash = string.Empty;

        var calculatedHash = ContinuationHeadIntegrity.ComputeContentHash(this);
        if (contentHash is not null &&
            (!RecoveryCheckpointReference.IsSha256(contentHash) ||
             !string.Equals(contentHash, calculatedHash, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("The supplied continuation-head hash does not match the payload.", nameof(contentHash));
        }

        ContentHash = calculatedHash;
    }

    public Guid ProjectId { get; }
    public int SchemaVersion { get; }
    public long Generation { get; }
    public RecoveryCheckpointReference LatestCheckpointReference { get; }
    public RecoveryCheckpointReference? LastSafeCheckpointReference { get; }
    public DateTimeOffset UpdatedAt { get; }
    /// <summary>SHA-256 content-integrity evidence, not a signature or authentication proof.</summary>
    public string ContentHash { get; private set; }
}

public static class ContinuationHeadIntegrity
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static string ComputeContentHash(ContinuationHead head)
    {
        ArgumentNullException.ThrowIfNull(head);
        var payload = new
        {
            head.ProjectId,
            head.SchemaVersion,
            head.Generation,
            latestCheckpointReference = CreateReference(head.LatestCheckpointReference),
            lastSafeCheckpointReference = head.LastSafeCheckpointReference is null
                ? null
                : CreateReference(head.LastSafeCheckpointReference),
            head.UpdatedAt
        };
        var json = JsonSerializer.Serialize(payload, Options);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    private static object CreateReference(RecoveryCheckpointReference reference) => new
    {
        reference.CheckpointId,
        reference.SchemaVersion,
        reference.ContentHash
    };

    private static JsonSerializerOptions CreateOptions() => new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}

public enum RecoveryCheckpointRepositoryWriteStatus
{
    Created,
    CheckpointConflict,
    Unavailable
}

public sealed record RecoveryCheckpointRepositoryWriteResult(
    RecoveryCheckpointRepositoryWriteStatus Status,
    string? ErrorMessage = null)
{
    public bool Succeeded => Status == RecoveryCheckpointRepositoryWriteStatus.Created;
}

public enum RecoveryCheckpointReadState
{
    Missing,
    Valid,
    UnsupportedFutureVersion,
    MigrationRequired,
    Invalid,
    IntegrityFailure,
    Unavailable
}

public sealed record RecoveryCheckpointReadResult(
    RecoveryCheckpointReadState State,
    RecoveryCheckpoint? Checkpoint = null,
    string? ErrorMessage = null)
{
    public bool IsValid => State == RecoveryCheckpointReadState.Valid && Checkpoint is not null;
}

public interface IRecoveryCheckpointRepository
{
    Task<RecoveryCheckpointRepositoryWriteResult> CreateAsync(
        RecoveryCheckpoint checkpoint,
        CancellationToken cancellationToken = default);

    Task<RecoveryCheckpointReadResult> GetAsync(
        Guid projectId,
        Guid checkpointId,
        CancellationToken cancellationToken = default);
}

public enum ContinuationHeadRepositoryWriteStatus
{
    Published,
    HeadConflict,
    Unavailable
}

public sealed record ContinuationHeadRepositoryWriteResult(
    ContinuationHeadRepositoryWriteStatus Status,
    string? ErrorMessage = null)
{
    public bool Succeeded => Status == ContinuationHeadRepositoryWriteStatus.Published;
}

public enum ContinuationHeadReadState
{
    Missing,
    Valid,
    UnsupportedFutureVersion,
    MigrationRequired,
    Invalid,
    IntegrityFailure,
    Unavailable
}

public sealed record ContinuationHeadReadResult(
    ContinuationHeadReadState State,
    ContinuationHead? Head = null,
    bool FallbackToPreviousGeneration = false,
    string? ErrorMessage = null)
{
    public bool IsValid => State == ContinuationHeadReadState.Valid && Head is not null;
}

/// <summary>
/// Two-slot, project-scoped continuation authority. Implementations must read both slots
/// observationally and publish only a newer generation into the inactive/older slot.
/// </summary>
public interface IContinuationHeadRepository
{
    Task<ContinuationHeadReadResult> GetAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<ContinuationHeadRepositoryWriteResult> PublishAsync(
        ContinuationHead head,
        CancellationToken cancellationToken = default);
}
