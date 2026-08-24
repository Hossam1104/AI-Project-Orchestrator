namespace AIUsageMonitor.Application.Projects;

/// <summary>
/// A sanitized local remote configuration entry. It does not imply that the remote is reachable.
/// </summary>
public sealed record RepositoryRemote(string Name, string SanitizedUrl);
