using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIUsageMonitor.Infrastructure.Persistence;

/// <summary>
/// Applies pending EF Core migrations against SQL Server LocalDB on startup, creating the
/// database on first run. Every failure path is caught and translated into a
/// <see cref="DatabaseInitializationResult"/> instead of propagating — a missing/unreachable
/// LocalDB instance is an expected, user-visible prerequisite state (BRD §29), not a crash.
/// </summary>
public sealed class SqlLocalDbInitializer : IDatabaseInitializer
{
    // LocalDB-specific SQL error numbers observed for "instance not installed/reachable" and
    // "cannot open database" conditions. Anything else is treated as a generic migration failure.
    private static readonly HashSet<int> LocalDbUnavailableErrorNumbers = [2, 53, 1231, -2146893019, 18456];

    private readonly AIUsageMonitorDbContext _dbContext;
    private readonly ILogger<SqlLocalDbInitializer> _logger;

    public SqlLocalDbInitializer(AIUsageMonitorDbContext dbContext, ILogger<SqlLocalDbInitializer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DatabaseInitializationResult> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            return DatabaseInitializationResult.Ready();
        }
        catch (SqlException ex) when (IsLocalDbUnavailable(ex))
        {
            _logger.LogWarning(ex, "SQL Server LocalDB is not available; continuing without persistence this session.");
            return DatabaseInitializationResult.Failure(
                DatabasePrerequisiteStatus.LocalDbUnavailable,
                "Microsoft SQL Server LocalDB is not available on this PC. Usage history and saved settings are " +
                "disabled until LocalDB is installed and reachable. Install \"SQL Server Express LocalDB\" and restart AI Usage Monitor.",
                ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to prepare the local database.");
            return DatabaseInitializationResult.Failure(
                DatabasePrerequisiteStatus.MigrationFailed,
                "AI Usage Monitor could not prepare its local database. The application will continue without saved history until this is resolved.",
                ex.Message);
        }
    }

    private static bool IsLocalDbUnavailable(SqlException ex) =>
        ex.Errors.Cast<SqlError>().Any(e => LocalDbUnavailableErrorNumbers.Contains(e.Number));
}
