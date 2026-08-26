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

    public string GetProjectOrchestrationDirectory(Guid projectId) =>
        GetProjectPaths(projectId).OrchestrationDirectory;

    public string GetProjectRoutingPolicyFile(Guid projectId) => GetProjectPaths(projectId).RoutingPolicyFile;

    public string GetProjectAgentOverridesFile(Guid projectId) =>
        GetProjectPaths(projectId).AgentOverridesFile;

    public string GetProjectContextReferenceFile(Guid projectId) =>
        GetProjectPaths(projectId).ContextReferenceFile;

    public string GetProjectWorkGraphsDirectory(Guid projectId) =>
        GetProjectPaths(projectId).WorkGraphsDirectory;

    public string GetProjectHandoffsDirectory(Guid projectId) =>
        GetProjectPaths(projectId).HandoffsDirectory;

    public string GetHandoffPackageDirectory(Guid projectId, Guid packageId)
    {
        if (packageId == Guid.Empty)
        {
            throw new ArgumentException("Package id cannot be empty.", nameof(packageId));
        }

        return Path.Combine(GetProjectHandoffsDirectory(projectId), packageId.ToString("D"));
    }

    public string GetHandoffPackageFile(Guid projectId, Guid packageId) =>
        Path.Combine(GetHandoffPackageDirectory(projectId, packageId), "package.json");

    public string GetWorkGraphDirectory(Guid projectId, Guid graphId)
    {
        if (graphId == Guid.Empty)
        {
            throw new ArgumentException("Graph id cannot be empty.", nameof(graphId));
        }

        return Path.Combine(GetProjectWorkGraphsDirectory(projectId), graphId.ToString("D"));
    }

    public string GetWorkGraphFile(Guid projectId, Guid graphId) =>
        Path.Combine(GetWorkGraphDirectory(projectId, graphId), "graph.json");

    public string GetWorkGraphCompletionEvidenceDirectory(Guid projectId, Guid graphId) =>
        Path.Combine(GetWorkGraphDirectory(projectId, graphId), "completion-evidence");

    public string GetWorkGraphCompletionEvidenceFile(Guid projectId, Guid graphId, Guid nodeId)
    {
        if (nodeId == Guid.Empty)
        {
            throw new ArgumentException("Node id cannot be empty.", nameof(nodeId));
        }

        return Path.Combine(
            GetWorkGraphCompletionEvidenceDirectory(projectId, graphId),
            $"node-{nodeId:D}.json");
    }

    public string GetProjectContractsDirectory(Guid projectId) =>
        GetProjectPaths(projectId).ContractsDirectory;

    public string GetPlanningExecutionContractDirectory(Guid projectId, Guid contractId)
    {
        if (contractId == Guid.Empty)
        {
            throw new ArgumentException("Contract id cannot be empty.", nameof(contractId));
        }

        return Path.Combine(GetProjectContractsDirectory(projectId), contractId.ToString("D"));
    }

    public string GetPlanningExecutionContractRevisionFile(Guid projectId, Guid contractId, int revision)
    {
        if (revision <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(revision));
        }

        return Path.Combine(
            GetPlanningExecutionContractDirectory(projectId, contractId),
            $"revision-{revision:D6}.json");
    }

    public string GetProjectRunsDirectory(Guid projectId) => GetProjectPaths(projectId).RunsDirectory;

    public string GetProjectEvidenceDirectory(Guid projectId) => GetProjectPaths(projectId).EvidenceDirectory;

    public string GetProjectReviewsDirectory(Guid projectId) => GetProjectPaths(projectId).ReviewsDirectory;

    public string GetProjectActivityDirectory(Guid projectId) => GetProjectPaths(projectId).ActivityDirectory;

    public void EnsureProjectDirectories(Guid projectId)
    {
        var projectPaths = GetProjectPaths(projectId);
        EnsureDirectories();
        Directory.CreateDirectory(projectPaths.RootDirectory);
        Directory.CreateDirectory(projectPaths.OrchestrationDirectory);
        Directory.CreateDirectory(projectPaths.ContractsDirectory);
        Directory.CreateDirectory(projectPaths.WorkGraphsDirectory);
        Directory.CreateDirectory(projectPaths.HandoffsDirectory);
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
        OrchestrationDirectory = Path.Combine(rootDirectory, "orchestration");
        RunsDirectory = Path.Combine(OrchestrationDirectory, "runs");
        EvidenceDirectory = Path.Combine(OrchestrationDirectory, "evidence");
        ReviewsDirectory = Path.Combine(OrchestrationDirectory, "reviews");
        ActivityDirectory = Path.Combine(OrchestrationDirectory, "activity");
        ContractsDirectory = Path.Combine(rootDirectory, "contracts");
        WorkGraphsDirectory = Path.Combine(rootDirectory, "work-graphs");
        HandoffsDirectory = Path.Combine(rootDirectory, "handoffs");
        RoutingPolicyFile = Path.Combine(rootDirectory, "routing-policy.json");
        AgentOverridesFile = Path.Combine(rootDirectory, "agent-overrides.json");
        ContextReferenceFile = Path.Combine(rootDirectory, "context-reference.json");
    }

    public Guid ProjectId { get; }

    public string RootDirectory { get; }

    public string OrchestrationDirectory { get; }

    public string RunsDirectory { get; }

    public string EvidenceDirectory { get; }

    public string ReviewsDirectory { get; }

    public string ActivityDirectory { get; }

    public string ContractsDirectory { get; }

    /// <summary>Immutable dependency-aware graph snapshots for this project.</summary>
    public string WorkGraphsDirectory { get; }

    /// <summary>Immutable planner/executor/reviewer lifecycle packages for this project.</summary>
    public string HandoffsDirectory { get; }

    public string RoutingPolicyFile { get; }

    /// <summary>
    /// Project-specific agent configuration only. Global agent truth remains in the root
    /// <c>agents.json</c> document.
    /// </summary>
    public string AgentOverridesFile { get; }

    /// <summary>Single current APO-39 onboarding context document for this project boundary.</summary>
    public string ContextReferenceFile { get; }
}
