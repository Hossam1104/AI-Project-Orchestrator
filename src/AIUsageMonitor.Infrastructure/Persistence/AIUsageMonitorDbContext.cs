using AIUsageMonitor.Domain.Alerts;
using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Quotas;
using AIUsageMonitor.Domain.Subscriptions;
using AIUsageMonitor.Domain.Sync;
using AIUsageMonitor.Domain.Usage;
using Microsoft.EntityFrameworkCore;

namespace AIUsageMonitor.Infrastructure.Persistence;

/// <summary>
/// EF Core 10 + SQL Server LocalDB persistence context (BRD §29-31). Maps exactly the entities
/// listed in the Session 03 scope — no provider secrets, no fixed quota columns.
/// </summary>
public sealed class AIUsageMonitorDbContext : DbContext
{
    public AIUsageMonitorDbContext(DbContextOptions<AIUsageMonitorDbContext> options)
        : base(options)
    {
    }

    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<ProviderConnection> ProviderConnections => Set<ProviderConnection>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<QuotaDefinition> QuotaDefinitions => Set<QuotaDefinition>();
    public DbSet<UsageSnapshot> UsageSnapshots => Set<UsageSnapshot>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<AlertEvent> AlertEvents => Set<AlertEvent>();
    public DbSet<SyncEvent> SyncEvents => Set<SyncEvent>();
    public DbSet<SettingsEntry> Settings => Set<SettingsEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AIUsageMonitorDbContext).Assembly);
    }
}
