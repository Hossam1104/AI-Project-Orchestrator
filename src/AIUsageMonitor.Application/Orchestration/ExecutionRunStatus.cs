namespace AIUsageMonitor.Application.Orchestration;

public enum ExecutionRunStatus
{
    Planned,
    Waiting,
    Running,
    Blocked,
    Failed,
    Review,
    Accepted,
    HumanApprovalRequired,
    Completed,
    Cancelled
}
