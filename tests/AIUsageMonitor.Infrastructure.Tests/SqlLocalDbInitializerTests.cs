using AIUsageMonitor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageMonitor.Infrastructure.Tests;

/// <summary>
/// Validates the two required <see cref="IDatabaseInitializer"/> outcomes: a clean database is
/// created/migrated successfully against real LocalDB, and an unreachable SQL Server never
/// throws out of <see cref="SqlLocalDbInitializer.InitializeAsync"/> — it always returns a
/// user-readable <see cref="DatabaseInitializationResult"/> instead (Session 03 "LOCALDB
/// MISSING" rule).
/// </summary>
public sealed class SqlLocalDbInitializerTests
{
    [Fact]
    public async Task InitializeAsync_ReturnsReady_OnCleanDatabase()
    {
        var databaseName = $"AIUsageMonitorTests_{Guid.NewGuid():N}";
        var connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<AIUsageMonitorDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        await using var dbContext = new AIUsageMonitorDbContext(optionsBuilder.Options);
        try
        {
            var initializer = new SqlLocalDbInitializer(dbContext, NullLogger<SqlLocalDbInitializer>.Instance);

            var result = await initializer.InitializeAsync();

            Assert.True(result.IsReady);
            Assert.Equal(DatabasePrerequisiteStatus.Ready, result.Status);
            Assert.Null(result.UserMessage);
        }
        finally
        {
            await dbContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task InitializeAsync_ReturnsLocalDbUnavailable_WhenServerUnreachable_NeverThrows()
    {
        // A syntactically valid but unreachable SQL Server host — simulates "LocalDB not
        // installed/reachable" without depending on the test machine's actual LocalDB state.
        const string unreachableConnectionString =
            "Server=tcp:127.0.0.1,1;Database=AIUsageMonitorUnreachable;Trusted_Connection=True;" +
            "TrustServerCertificate=True;Connect Timeout=1;";

        var optionsBuilder = new DbContextOptionsBuilder<AIUsageMonitorDbContext>();
        optionsBuilder.UseSqlServer(unreachableConnectionString);

        await using var dbContext = new AIUsageMonitorDbContext(optionsBuilder.Options);
        var initializer = new SqlLocalDbInitializer(dbContext, NullLogger<SqlLocalDbInitializer>.Instance);

        var result = await initializer.InitializeAsync();

        Assert.False(result.IsReady);
        Assert.NotNull(result.UserMessage);
        Assert.Contains(
            result.Status,
            new[] { DatabasePrerequisiteStatus.LocalDbUnavailable, DatabasePrerequisiteStatus.MigrationFailed });
    }
}
