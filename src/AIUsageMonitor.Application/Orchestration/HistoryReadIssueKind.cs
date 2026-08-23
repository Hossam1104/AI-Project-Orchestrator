namespace AIUsageMonitor.Application.Orchestration;

/// <summary>
/// Non-secret categories for a degraded history read.
/// </summary>
public enum HistoryReadIssueKind
{
    CorruptRecord = 0,
    UnsupportedSchema = 1,
    PermissionFailure = 2,
    IoFailure = 3
}
