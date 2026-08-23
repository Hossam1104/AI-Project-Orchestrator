namespace AIUsageMonitor.Application.Orchestration;

/// <summary>
/// Truthfulness state for a requested orchestration-history read.
/// </summary>
public enum HistoryReadStatus
{
    Success = 0,
    Partial = 1,
    Unavailable = 2
}
