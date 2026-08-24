namespace AIUsageMonitor.Application.Projects;

/// <summary>
/// Conservative local-only comparison between registered repository metadata and configured
/// local remotes. It never represents network reachability or synchronization.
/// </summary>
public enum RepositoryRemoteComparison
{
    NotConfigured,
    NoLocalRemote,
    Match,
    Different,
    ComparisonUnavailable
}
