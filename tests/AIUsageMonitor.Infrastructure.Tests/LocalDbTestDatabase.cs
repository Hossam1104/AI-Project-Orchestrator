using AIUsageMonitor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIUsageMonitor.Infrastructure.Tests;

/// <summary>
/// Spins up a uniquely-named LocalDB database via the real <c>InitialCreate</c> migration (not
/// <c>EnsureCreated</c>) so tests also validate "migration apply" against a clean database, then
/// drops it on disposal. xUnit creates a fresh instance of each test class per test method, so
/// owning one of these as an <see cref="IAsyncLifetime"/>-delegated field on a test class gives
/// every test method its own isolated database — required here because
/// <see cref="AIUsageMonitor.Domain.Providers.ProviderCode"/> only has a handful of fixed values
/// and <c>Providers.Code</c> is uniquely indexed, so tests cannot safely share one database.
/// Requires a local <c>(localdb)\MSSQLLocalDB</c> instance; if it is unavailable,
/// <see cref="InitializeAsync"/> throws and the affected tests are reported as failed rather
/// than silently skipped.
/// </summary>
public sealed class LocalDbTestDatabase : IAsyncLifetime
{
    private readonly string _databaseName = $"AIUsageMonitorTests_{Guid.NewGuid():N}";

    public string ConnectionString => $@"Server=(localdb)\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

    public async Task InitializeAsync()
    {
        await using var dbContext = CreateContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var dbContext = CreateContext();
        await dbContext.Database.EnsureDeletedAsync();
    }

    public AIUsageMonitorDbContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AIUsageMonitorDbContext>();
        optionsBuilder.UseSqlServer(ConnectionString);
        return new AIUsageMonitorDbContext(optionsBuilder.Options);
    }
}
