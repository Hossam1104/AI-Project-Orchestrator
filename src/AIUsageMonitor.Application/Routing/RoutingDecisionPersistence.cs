using System.Security.Cryptography;

namespace AIUsageMonitor.Application.Routing;

public static class RoutingDecisionSchema
{
    public const int CurrentVersion = 1;
}

public static class RoutingDecisionLimits
{
    public const int MaxCanonicalPayloadBytes = 128 * 1024;
}

public sealed class RoutingDecisionReference
{
    public RoutingDecisionReference(Guid decisionId, int schemaVersion, string contentHash)
    {
        if (decisionId == Guid.Empty)
        {
            throw new ArgumentException("Decision id cannot be empty.", nameof(decisionId));
        }

        if (schemaVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        }

        if (!IsSha256(contentHash))
        {
            throw new ArgumentException("Decision content hash must be a SHA-256 hex value.", nameof(contentHash));
        }

        DecisionId = decisionId;
        SchemaVersion = schemaVersion;
        ContentHash = contentHash.ToLowerInvariant();
    }

    public Guid DecisionId { get; }
    public int SchemaVersion { get; }
    public string ContentHash { get; }

    public static bool IsSha256(string? value) =>
        value is { Length: 64 } && value.All(Uri.IsHexDigit);
}

public enum RoutingDecisionRepositoryWriteStatus
{
    Created,
    DecisionConflict,
    Unavailable
}

public sealed record RoutingDecisionRepositoryWriteResult(
    RoutingDecisionRepositoryWriteStatus Status,
    string? ErrorMessage = null)
{
    public bool Succeeded => Status == RoutingDecisionRepositoryWriteStatus.Created;
}

public enum RoutingDecisionReadState
{
    Missing,
    Valid,
    UnsupportedFutureVersion,
    MigrationRequired,
    Invalid,
    IntegrityFailure,
    Unavailable
}

public sealed record RoutingDecisionReadResult(
    RoutingDecisionReadState State,
    RoutingDecision? Decision = null,
    string? ErrorMessage = null)
{
    public bool IsValid => State == RoutingDecisionReadState.Valid && Decision is not null;
}

public interface IRoutingDecisionRepository
{
    Task<RoutingDecisionRepositoryWriteResult> CreateAsync(
        RoutingDecision decision,
        CancellationToken cancellationToken = default);

    Task<RoutingDecisionReadResult> GetAsync(
        Guid projectId,
        Guid decisionId,
        CancellationToken cancellationToken = default);
}
