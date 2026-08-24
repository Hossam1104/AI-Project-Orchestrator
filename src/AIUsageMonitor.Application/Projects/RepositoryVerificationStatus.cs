namespace AIUsageMonitor.Application.Projects;

/// <summary>
/// Truthful outcomes for a read-only local repository inspection.
/// </summary>
public enum RepositoryVerificationStatus
{
    NotInspected,
    PathMissing,
    PathUnavailable,
    GitUnavailable,
    NotGitRepository,
    AvailableClean,
    AvailableDirty,
    Failed
}
