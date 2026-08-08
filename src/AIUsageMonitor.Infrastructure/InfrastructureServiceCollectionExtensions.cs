using AIUsageMonitor.Application.Alerts;
using AIUsageMonitor.Application.Providers;
using AIUsageMonitor.Application.Quotas;
using AIUsageMonitor.Application.Settings;
using AIUsageMonitor.Application.Subscriptions;
using AIUsageMonitor.Application.Sync;
using AIUsageMonitor.Application.Time;
using AIUsageMonitor.Application.Usage;
using AIUsageMonitor.Infrastructure.Persistence;
using AIUsageMonitor.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AIUsageMonitor.Infrastructure;

/// <summary>
/// Composition-root wiring for the persistence layer (BRD §29-31). Registers a pooled
/// <see cref="AIUsageMonitorDbContext"/> factory plus the EF Core-backed implementations of
/// every Session 03 repository/service contract.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string? connectionString = null)
    {
        var options = new PersistenceOptions();
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            options.ConnectionString = connectionString;
        }

        services.AddSingleton(options);

        services.AddDbContext<AIUsageMonitorDbContext>((serviceProvider, builder) =>
        {
            var persistenceOptions = serviceProvider.GetRequiredService<PersistenceOptions>();
            builder.UseSqlServer(persistenceOptions.ConnectionString);
        });

        services.AddScoped<IDatabaseInitializer, SqlLocalDbInitializer>();

        services.AddScoped<IUsageSnapshotRepository, EfUsageSnapshotRepository>();
        services.AddScoped<IProviderRepository, EfProviderRepository>();
        services.AddScoped<IProviderConnectionRepository, EfProviderConnectionRepository>();
        services.AddScoped<ISubscriptionService, EfSubscriptionService>();
        services.AddScoped<IQuotaDefinitionRepository, EfQuotaDefinitionRepository>();
        services.AddScoped<IAlertRuleRepository, EfAlertRuleRepository>();
        services.AddScoped<IAlertEventRepository, EfAlertEventRepository>();
        services.AddScoped<ISyncEventRepository, EfSyncEventRepository>();
        services.AddScoped<ISettingsService, EfSettingsService>();

        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
