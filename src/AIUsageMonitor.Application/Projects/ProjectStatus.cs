namespace AIUsageMonitor.Application.Projects;

/// <summary>
/// Lifecycle state for a registered project. Orchestration behavior is intentionally out of
/// scope for APO-27; this enum is persisted so later work can apply policy without changing the
/// storage schema.
/// </summary>
public enum ProjectStatus
{
    Active,
    Paused,
    Blocked,
    Archived
}
