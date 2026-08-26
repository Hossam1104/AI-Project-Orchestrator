namespace AIUsageMonitor.Application.Handoffs;

public enum HandoffPackageRepositoryWriteStatus
{
    Created,
    PackageConflict,
    Unavailable
}

public sealed record HandoffPackageRepositoryWriteResult(
    HandoffPackageRepositoryWriteStatus Status,
    string? ErrorMessage = null)
{
    public bool Succeeded => Status == HandoffPackageRepositoryWriteStatus.Created;
}

public enum HandoffPackageReadState
{
    Missing,
    Valid,
    UnsupportedFutureVersion,
    MigrationRequired,
    Invalid,
    IntegrityFailure,
    Unavailable
}

public sealed record HandoffPackageReadResult(
    HandoffPackageReadState State,
    HandoffPackage? Package = null,
    string? ErrorMessage = null)
{
    public bool IsValid => State == HandoffPackageReadState.Valid && Package is not null;
}

/// <summary>
/// Create-once project-isolated authority for structured lifecycle handoffs. There is deliberately
/// no update, replace, delete, or latest-package operation.
/// </summary>
public interface IHandoffPackageRepository
{
    Task<HandoffPackageRepositoryWriteResult> CreateAsync(
        HandoffPackage package,
        CancellationToken cancellationToken = default);

    Task<HandoffPackageReadResult> GetAsync(
        Guid projectId,
        Guid packageId,
        CancellationToken cancellationToken = default);
}
