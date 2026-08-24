using AIUsageMonitor.Application.Alerts;
using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Providers;
using AIUsageMonitor.Application.Projects;
using AIUsageMonitor.Application.Quotas;
using AIUsageMonitor.Application.Routing;
using AIUsageMonitor.Application.Security;
using AIUsageMonitor.Application.Settings;
using AIUsageMonitor.Application.Subscriptions;
using AIUsageMonitor.Application.Sync;
using AIUsageMonitor.Application.Time;
using AIUsageMonitor.Application.Usage;
using AIUsageMonitor.Infrastructure.Persistence;
using AIUsageMonitor.Infrastructure.Persistence.Repositories;
using AIUsageMonitor.Infrastructure.Security;
using AIUsageMonitor.Infrastructure.Git;
using Microsoft.Extensions.DependencyInjection;

namespace AIUsageMonitor.Infrastructure;

/// <summary>
/// Composition-root wiring for the local file persistence layer. Small state documents use
/// versioned JSON and append-oriented data uses monthly JSONL partitions under LocalAppData.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string? rootDirectory = null)
    {
        var paths = new ApplicationDataPaths(rootDirectory ?? ApplicationDataPaths.CreateDefault().RootDirectory);
        services.AddSingleton(paths);
        services.AddSingleton<JsonFileStore>();

        services.AddSingleton<JsonlEventStore<UsageSnapshotRecord>>();
        services.AddSingleton<JsonlEventStore<AlertEventRecord>>();
        services.AddSingleton<JsonlEventStore<SyncEventRecord>>();
        services.AddSingleton<JsonlEventStore<ExecutionRunRecord>>();
        services.AddSingleton<JsonlEventStore<EvidenceMetadataRecord>>();
        services.AddSingleton<JsonlEventStore<ReviewMetadataRecord>>();
        services.AddSingleton<JsonlEventStore<ActivityAuditRecordFile>>();

        services.AddSingleton<IUsageSnapshotRepository, JsonUsageSnapshotRepository>();
        services.AddSingleton<IProviderRepository, JsonProviderRepository>();
        services.AddSingleton<IProviderConnectionRepository, JsonProviderConnectionRepository>();
        services.AddSingleton<IProviderConnectionService, ProviderConnectionService>();
        services.AddSingleton<ISubscriptionService, JsonSubscriptionService>();
        services.AddSingleton<IQuotaDefinitionRepository, JsonQuotaDefinitionRepository>();
        services.AddSingleton<IAlertRuleRepository, JsonAlertRuleRepository>();
        services.AddSingleton<IAlertEventRepository, JsonAlertEventRepository>();
        services.AddSingleton<ISyncEventRepository, JsonSyncEventRepository>();
        services.AddSingleton<ISettingsService, JsonSettingsService>();
        services.AddSingleton<IProjectRepository, JsonProjectRepository>();
        services.AddSingleton<ILocalRepositoryInspector>(
            _ => new GitLocalRepositoryInspector(new SystemGitCommandRunner()));
        services.AddSingleton<IProjectRepositoryStateService, ProjectRepositoryStateService>();
        services.AddSingleton<IAgentRepository, JsonAgentRepository>();
        services.AddSingleton<IRoutingPolicyRepository, JsonRoutingPolicyStore>();
        services.AddSingleton<IProjectOrchestrationStore, JsonProjectOrchestrationStore>();

        // Stateless native-call wrapper; singleton avoids re-allocating it per resolution while
        // matching the lifetime of every other adapter registered here.
        services.AddSingleton<ISecureCredentialStore, WindowsCredentialManagerStore>();

        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
