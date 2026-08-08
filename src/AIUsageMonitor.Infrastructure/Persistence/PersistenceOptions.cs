namespace AIUsageMonitor.Infrastructure.Persistence;

/// <summary>
/// Local database connection configuration (BRD §29). LocalDB requires no username/password —
/// the connection string never carries a secret, so it is safe to keep alongside other local
/// configuration rather than in secure storage.
/// </summary>
public sealed class PersistenceOptions
{
    public const string DatabaseName = "AIUsageMonitor";

    /// <summary>
    /// Default LocalDB connection string: named-pipe/shared-memory local instance, integrated
    /// (trusted) Windows authentication, no credentials.
    /// </summary>
    public const string DefaultConnectionString =
        $@"Server=(localdb)\MSSQLLocalDB;Database={DatabaseName};Trusted_Connection=True;TrustServerCertificate=True;";

    public string ConnectionString { get; set; } = DefaultConnectionString;
}
