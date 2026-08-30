using AIUsageMonitor.Application.Security;
using AIUsageMonitor.Application.Time;
using AIUsageMonitor.Application.RemoteEvidence;
using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace AIUsageMonitor.Provider.Tests;

public sealed class ProviderRegistrationTests
{
    [Fact]
    public void AddProviders_ResolvesEveryV1ProviderExactlyOnce()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IClock>(new TestClock());
        services.AddSingleton<ISecureCredentialStore>(new TestCredentialStore());
        services.AddProviders();

        using var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<AIUsageMonitor.Application.Providers.IProviderRegistry>();

        Assert.Equal(Enum.GetValues<ProviderCode>().Length, registry.GetAll().Count);
        Assert.Equal(
            Enum.GetValues<ProviderCode>().OrderBy(code => code),
            registry.GetAll().Select(provider => provider.Code));

        foreach (var code in Enum.GetValues<ProviderCode>())
        {
            Assert.NotNull(registry.Find(code));
        }
    }

    [Fact]
    public void AddProviders_ResolvesBothRemoteEvidenceAdaptersAndService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IClock>(new TestClock());
        services.AddSingleton<ISecureCredentialStore>(new TestCredentialStore());
        services.AddProviders();

        using var serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetRequiredService<IRemoteRepositoryEvidenceService>());
        Assert.Equal(
            [RemoteRepositoryProvider.GitHub, RemoteRepositoryProvider.AzureRepos],
            serviceProvider.GetServices<IRemoteRepositoryEvidenceProvider>().Select(provider => provider.Provider));
    }
}
