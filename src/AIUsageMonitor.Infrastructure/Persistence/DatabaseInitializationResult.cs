namespace AIUsageMonitor.Infrastructure.Persistence;

/// <summary>
/// User-readable result of preparing the local database at startup. Never throws out of
/// <see cref="IDatabaseInitializer.InitializeAsync"/> — a missing/unreachable LocalDB instance
/// is a normal, displayable state, not an application crash (BRD §29, AGENTS.md Session 03
/// "LOCALDB MISSING" rule).
/// </summary>
public sealed class DatabaseInitializationResult
{
    public DatabasePrerequisiteStatus Status { get; }
    public bool IsReady => Status == DatabasePrerequisiteStatus.Ready;

    /// <summary>Safe to show directly in the UI. Never includes a connection string or stack trace.</summary>
    public string? UserMessage { get; }

    /// <summary>Technical detail for an expandable "diagnostics" section — never a secret.</summary>
    public string? DiagnosticDetail { get; }

    private DatabaseInitializationResult(DatabasePrerequisiteStatus status, string? userMessage, string? diagnosticDetail)
    {
        Status = status;
        UserMessage = userMessage;
        DiagnosticDetail = diagnosticDetail;
    }

    public static DatabaseInitializationResult Ready() =>
        new(DatabasePrerequisiteStatus.Ready, userMessage: null, diagnosticDetail: null);

    public static DatabaseInitializationResult Failure(
        DatabasePrerequisiteStatus status, string userMessage, string? diagnosticDetail) =>
        new(status, userMessage, diagnosticDetail);
}
