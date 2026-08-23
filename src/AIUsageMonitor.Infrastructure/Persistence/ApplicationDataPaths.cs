namespace AIUsageMonitor.Infrastructure.Persistence;

/// <summary>
/// Resolves the per-user storage layout. Runtime data never lives beside the executable or
/// under Program Files, so a self-contained install can be updated without touching user data.
/// </summary>
public sealed class ApplicationDataPaths
{
    public const string ApplicationDirectoryName = "AIUsageMonitor";

    public ApplicationDataPaths(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);

        RootDirectory = Path.GetFullPath(rootDirectory);
        HistoryDirectory = Path.Combine(RootDirectory, "history");
        AlertsDirectory = Path.Combine(RootDirectory, "alerts");
        SyncDirectory = Path.Combine(RootDirectory, "sync");
        LogsDirectory = Path.Combine(RootDirectory, "logs");
        ProjectsDirectory = Path.Combine(RootDirectory, "projects");
    }

    public string RootDirectory { get; }

    public string HistoryDirectory { get; }

    public string AlertsDirectory { get; }

    public string SyncDirectory { get; }

    public string LogsDirectory { get; }

    /// <summary>
    /// Root directory for GUID-scoped project state. The legacy root and all existing stores are
    /// intentionally preserved; this is an additive APO-27 layout extension.
    /// </summary>
    public string ProjectsDirectory { get; }

    public string SettingsFile => Path.Combine(RootDirectory, "settings.json");

    public string ProvidersFile => Path.Combine(RootDirectory, "providers.json");

    public string ConnectionsFile => Path.Combine(RootDirectory, "connections.json");

    public string SubscriptionsFile => Path.Combine(RootDirectory, "subscriptions.json");

    public string QuotaDefinitionsFile => Path.Combine(RootDirectory, "quota-definitions.json");

    public string AlertRulesFile => Path.Combine(RootDirectory, "alert-rules.json");

    public string ProjectsFile => Path.Combine(RootDirectory, "projects.json");

    public string AgentsFile => Path.Combine(RootDirectory, "agents.json");

    public string RoutingPolicyFile => Path.Combine(RootDirectory, "routing-policy.json");

    public static ApplicationDataPaths CreateDefault()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
        {
            throw new InvalidOperationException(
                "The Windows LocalApplicationData location is unavailable; persistent storage cannot be resolved safely.");
        }

        var root = Path.Combine(localApplicationData, ApplicationDirectoryName);
        return new ApplicationDataPaths(root);
    }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(HistoryDirectory);
        Directory.CreateDirectory(AlertsDirectory);
        Directory.CreateDirectory(SyncDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(ProjectsDirectory);
    }

    public Task EnsureDirectoriesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureDirectories();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Resolves the complete project-scoped layout without creating it. The only project path
    /// component accepted is a canonical GUID, so a caller cannot inject a relative path.
    /// </summary>
    public ProjectDataPaths GetProjectPaths(Guid projectId)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        var projectDirectory = Path.Combine(ProjectsDirectory, projectId.ToString("D"));
        return new ProjectDataPaths(projectId, projectDirectory);
    }

    public string GetProjectDirectory(Guid projectId) => GetProjectPaths(projectId).RootDirectory;

    public string GetProjectRoutingPolicyFile(Guid projectId) => GetProjectPaths(projectId).RoutingPolicyFile;

    public string GetProjectRunsDirectory(Guid projectId) => GetProjectPaths(projectId).RunsDirectory;

    public string GetProjectEvidenceDirectory(Guid projectId) => GetProjectPaths(projectId).EvidenceDirectory;

    public string GetProjectReviewsDirectory(Guid projectId) => GetProjectPaths(projectId).ReviewsDirectory;

    public string GetProjectActivityDirectory(Guid projectId) => GetProjectPaths(projectId).ActivityDirectory;

    public void EnsureProjectDirectories(Guid projectId)
    {
        var projectPaths = GetProjectPaths(projectId);
        EnsureDirectories();
        Directory.CreateDirectory(projectPaths.RootDirectory);
        Directory.CreateDirectory(projectPaths.RunsDirectory);
        Directory.CreateDirectory(projectPaths.EvidenceDirectory);
        Directory.CreateDirectory(projectPaths.ReviewsDirectory);
        Directory.CreateDirectory(projectPaths.ActivityDirectory);
    }

    public Task EnsureProjectDirectoriesAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureProjectDirectories(projectId);
        return Task.CompletedTask;
    }

    public string GetMonthlyPartition(string directory, DateTimeOffset timestamp) =>
        Path.Combine(directory, $"{timestamp.UtcDateTime:yyyy-MM}.jsonl");
}

/// <summary>
/// Paths beneath one registered project's GUID directory. These paths are derived, never loaded
/// from project metadata, so project records cannot redirect one project's stream to another.
/// </summary>
public sealed class ProjectDataPaths
{
    internal ProjectDataPaths(Guid projectId, string rootDirectory)
    {
        ProjectId = projectId;
        RootDirectory = rootDirectory;
        RunsDirectory = Path.Combine(rootDirectory, "runs");
        EvidenceDirectory = Path.Combine(rootDirectory, "evidence");
        ReviewsDirectory = Path.Combine(rootDirectory, "reviews");
        ActivityDirectory = Path.Combine(rootDirectory, "activity");
        RoutingPolicyFile = Path.Combine(rootDirectory, "routing-policy.json");
    }

    public Guid ProjectId { get; }

    public string RootDirectory { get; }

    public string RunsDirectory { get; }

    public string EvidenceDirectory { get; }

    public string ReviewsDirectory { get; }

    public string ActivityDirectory { get; }

    public string RoutingPolicyFile { get; }
}
