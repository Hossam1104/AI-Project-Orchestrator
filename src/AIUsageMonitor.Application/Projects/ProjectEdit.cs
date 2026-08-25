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

    /// <summary>
    /// Bounded local evidence accepted during onboarding. This is metadata only; it must not
    /// contain repository contents, diffs, command output, or credentials.
    /// </summary>
    public IReadOnlyDictionary<string, string?>? RepositoryMetadata { get; init; }

    public string? DefaultBranch { get; init; }

    public string? TrackerType { get; init; }

    public string? TrackerId { get; init; }

    /// <summary>Bounded tracker reference state; no live tracker payload is stored.</summary>
    public IReadOnlyDictionary<string, string?>? TrackerMetadata { get; init; }

    public IReadOnlyList<string>? GovernanceReferences { get; init; }

    public string? RoutingPolicyReference { get; init; }

    public string? SafetyPolicyReference { get; init; }
}
