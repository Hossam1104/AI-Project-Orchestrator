namespace AIUsageMonitor.Application.Projects;

/// <summary>
/// Visible project-registration fields accepted by the first Projects workspace editor.
/// Persistence-only metadata is deliberately not part of this type so the update use case
/// can preserve it from the loaded project record.
/// </summary>
public sealed class ProjectEdit
{
    public string Name { get; init; } = string.Empty;

    public string LocalPath { get; init; } = string.Empty;

    public ProjectStatus Status { get; init; } = ProjectStatus.Active;

    public string? RepositoryProvider { get; init; }

    public string? RepositoryUrl { get; init; }

    public string? RepositoryId { get; init; }

    public string? DefaultBranch { get; init; }

    public string? TrackerType { get; init; }

    public string? TrackerId { get; init; }

    public IReadOnlyList<string>? GovernanceReferences { get; init; }

    public string? RoutingPolicyReference { get; init; }

    public string? SafetyPolicyReference { get; init; }
}
