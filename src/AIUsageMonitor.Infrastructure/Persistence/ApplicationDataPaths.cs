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
    }

    public string RootDirectory { get; }

    public string HistoryDirectory { get; }

    public string AlertsDirectory { get; }

    public string SyncDirectory { get; }

    public string LogsDirectory { get; }

    public string SettingsFile => Path.Combine(RootDirectory, "settings.json");

    public string ProvidersFile => Path.Combine(RootDirectory, "providers.json");

    public string ConnectionsFile => Path.Combine(RootDirectory, "connections.json");

    public string SubscriptionsFile => Path.Combine(RootDirectory, "subscriptions.json");

    public string QuotaDefinitionsFile => Path.Combine(RootDirectory, "quota-definitions.json");

    public string AlertRulesFile => Path.Combine(RootDirectory, "alert-rules.json");

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
    }

    public Task EnsureDirectoriesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureDirectories();
        return Task.CompletedTask;
    }

    public string GetMonthlyPartition(string directory, DateTimeOffset timestamp) =>
        Path.Combine(directory, $"{timestamp.UtcDateTime:yyyy-MM}.jsonl");
}
