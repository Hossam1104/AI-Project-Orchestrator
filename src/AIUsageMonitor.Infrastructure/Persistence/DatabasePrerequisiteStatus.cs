namespace AIUsageMonitor.Infrastructure.Persistence;

/// <summary>
/// Outcome of preparing the local database at startup (BRD §29 "LocalDB installation is a
/// deployment prerequisite and must be checked"). Never a reason to crash the application —
/// see <see cref="IDatabaseInitializer"/>.
/// </summary>
public enum DatabasePrerequisiteStatus
{
    Ready,
    LocalDbUnavailable,
    MigrationFailed
}
