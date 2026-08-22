using AIUsageMonitor.Application.Providers;
using AIUsageMonitor.Providers.Antigravity;
using AIUsageMonitor.Providers.Claude;
using AIUsageMonitor.Providers.Codex;
using AIUsageMonitor.Providers.Common;
using AIUsageMonitor.Providers.Copilot;
using AIUsageMonitor.Providers.Kimi;
using Microsoft.Extensions.DependencyInjection;

namespace AIUsageMonitor.Providers;

public static class ProvidersServiceCollectionExtensions
{
    public static IServiceCollection AddProviders(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IExecutableLocator, SystemExecutableLocator>();
        services.AddSingleton<CopilotOptions>();
        services.AddSingleton<AnthropicOptions>();
        services.AddSingleton<KimiOptions>();

        services.AddHttpClient(CopilotProvider.HttpClientName, client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/", UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AI-Project-Orchestrator/1.0");
            client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2026-03-10");
        });

        services.AddHttpClient(ClaudeProvider.HttpClientName, client =>
        {
            client.BaseAddress = new Uri("https://api.anthropic.com/", UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AI-Project-Orchestrator/1.0");
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false
        });

        services.AddHttpClient(KimiProvider.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AI-Project-Orchestrator/1.0");
        });

        services.AddSingleton<CopilotProvider>();
        services.AddSingleton<ClaudeProvider>();
        services.AddSingleton<KimiProvider>();
        services.AddSingleton<CodexProvider>();
        services.AddSingleton<AntigravityProvider>();

        services.AddSingleton<IAiUsageProvider>(provider => provider.GetRequiredService<CodexProvider>());
        services.AddSingleton<IAiUsageProvider>(provider => provider.GetRequiredService<ClaudeProvider>());
        services.AddSingleton<IAiUsageProvider>(provider => provider.GetRequiredService<KimiProvider>());
        services.AddSingleton<IAiUsageProvider>(provider => provider.GetRequiredService<CopilotProvider>());
        services.AddSingleton<IAiUsageProvider>(provider => provider.GetRequiredService<AntigravityProvider>());

        services.AddSingleton<IProviderRegistry, ProviderRegistry>();
        services.AddSingleton<IProviderDiscoveryService, ProviderDiscoveryService>();
        return services;
    }
}
