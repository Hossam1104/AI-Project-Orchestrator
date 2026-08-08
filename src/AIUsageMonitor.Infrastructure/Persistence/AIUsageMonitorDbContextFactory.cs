using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AIUsageMonitor.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef migrations</c>/<c>dotnet ef database update</c> construct the DbContext at
/// design time without the full Desktop DI/hosting pipeline. Not used at application runtime.
/// </summary>
public sealed class AIUsageMonitorDbContextFactory : IDesignTimeDbContextFactory<AIUsageMonitorDbContext>
{
    public AIUsageMonitorDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AIUsageMonitorDbContext>();
        optionsBuilder.UseSqlServer(PersistenceOptions.DefaultConnectionString);
        return new AIUsageMonitorDbContext(optionsBuilder.Options);
    }
}
