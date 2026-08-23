using AIUsageMonitor.Application.Common;

namespace AIUsageMonitor.Application.Projects;

/// <summary>
/// Provider-independent project registry metadata. It identifies a local workspace and its
/// external references without carrying repository contents, tracker payloads, credentials, or
/// prompts.
/// </summary>
public sealed class Project
{
    public Project(
        Guid id,
        string name,
        string localPath,
        string defaultBranch,
        ProjectStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string? repositoryProvider = null,
        string? repositoryUrl = null,
        string? repositoryId = null,
        IReadOnlyDictionary<string, string?>? repositoryMetadata = null,
        string? trackerType = null,
        string? trackerId = null,
        IReadOnlyDictionary<string, string?>? trackerMetadata = null,
        IReadOnlyList<string>? governanceReferences = null,
        string? routingPolicyReference = null,
        string? safetyPolicyReference = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(localPath))
        {
            throw new ArgumentException("Project local path is required.", nameof(localPath));
        }

        if (string.IsNullOrWhiteSpace(defaultBranch))
        {
            throw new ArgumentException("Project default branch is required.", nameof(defaultBranch));
        }

        if (updatedAt < createdAt)
        {
            throw new ArgumentException("Project UpdatedAt cannot precede CreatedAt.", nameof(updatedAt));
        }

        Id = id;
        Name = name.Trim();
        LocalPath = localPath;
        DefaultBranch = defaultBranch.Trim();
        Status = status;
        RepositoryProvider = NormalizeOptional(repositoryProvider);
        RepositoryUrl = NormalizeOptional(repositoryUrl);
        RepositoryId = NormalizeOptional(repositoryId);
        RepositoryMetadata = MetadataValidation.Copy(repositoryMetadata);
        TrackerType = NormalizeOptional(trackerType);
        TrackerId = NormalizeOptional(trackerId);
        TrackerMetadata = MetadataValidation.Copy(trackerMetadata);
        GovernanceReferences = CopyReferences(governanceReferences);
        RoutingPolicyReference = NormalizeOptional(routingPolicyReference);
        SafetyPolicyReference = NormalizeOptional(safetyPolicyReference);
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string LocalPath { get; }

    public string DefaultBranch { get; }

    public ProjectStatus Status { get; }

    public string? RepositoryProvider { get; }

    public string? RepositoryUrl { get; }

    public string? RepositoryId { get; }

    public IReadOnlyDictionary<string, string?> RepositoryMetadata { get; }

    public string? TrackerType { get; }

    public string? TrackerId { get; }

    public IReadOnlyDictionary<string, string?> TrackerMetadata { get; }

    public IReadOnlyList<string> GovernanceReferences { get; }

    public string? RoutingPolicyReference { get; }

    public string? SafetyPolicyReference { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IReadOnlyList<string> CopyReferences(IReadOnlyList<string>? references)
    {
        if (references is null || references.Count == 0)
        {
            return Array.Empty<string>();
        }

        var result = new List<string>(references.Count);
        foreach (var reference in references)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                throw new ArgumentException("Governance references cannot contain blank values.", nameof(references));
            }

            result.Add(reference.Trim());
        }

        return result.AsReadOnly();
    }
}
