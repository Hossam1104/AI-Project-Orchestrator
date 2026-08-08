using System.Security;

namespace AIUsageMonitor.Infrastructure.Persistence;

/// <summary>
/// Captures whether per-user storage could be initialized without forcing callers to redirect
/// permanent data to an unapproved fallback location.
/// </summary>
public sealed class StorageInitializationResult
{
    private StorageInitializationResult(ApplicationDataPaths? paths, Exception? failure)
    {
        Paths = paths;
        Failure = failure;
    }

    public ApplicationDataPaths? Paths { get; }

    public Exception? Failure { get; }

    public bool IsAvailable => Paths is not null && Failure is null;

    internal static StorageInitializationResult Available(ApplicationDataPaths paths) =>
        new(paths, null);

    internal static StorageInitializationResult Unavailable(Exception failure) =>
        new(null, failure);
}

/// <summary>
/// Guarded startup boundary for LocalAppData storage. The injectable overload keeps the
/// failure-to-degraded transition directly testable without changing the production path.
/// </summary>
public static class StorageStartup
{
    public static StorageInitializationResult TryInitialize() =>
        TryInitialize(
            ApplicationDataPaths.CreateDefault,
            static paths => paths.EnsureDirectories());

    public static StorageInitializationResult TryInitialize(
        Func<ApplicationDataPaths> createPaths,
        Action<ApplicationDataPaths> ensureDirectories)
    {
        ArgumentNullException.ThrowIfNull(createPaths);
        ArgumentNullException.ThrowIfNull(ensureDirectories);

        try
        {
            var paths = createPaths();
            ensureDirectories(paths);
            return StorageInitializationResult.Available(paths);
        }
        catch (Exception exception) when (IsRecoverableStorageFailure(exception))
        {
            return StorageInitializationResult.Unavailable(exception);
        }
    }

    private static bool IsRecoverableStorageFailure(Exception exception) =>
        exception is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or InvalidOperationException
            or NotSupportedException
            or SecurityException;
}
