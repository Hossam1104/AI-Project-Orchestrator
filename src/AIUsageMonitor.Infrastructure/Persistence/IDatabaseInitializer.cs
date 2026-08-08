namespace AIUsageMonitor.Infrastructure.Persistence;

/// <summary>
/// Prepares the local database (applies pending migrations, creating the database on first
/// run) before the rest of the application relies on persistence. Must never throw — any
/// failure, including a missing LocalDB installation, is reported through
/// <see cref="DatabaseInitializationResult"/> so the caller can display it and continue running
/// with in-memory-only behavior for the session.
/// </summary>
public interface IDatabaseInitializer
{
    Task<DatabaseInitializationResult> InitializeAsync(CancellationToken cancellationToken = default);
}
