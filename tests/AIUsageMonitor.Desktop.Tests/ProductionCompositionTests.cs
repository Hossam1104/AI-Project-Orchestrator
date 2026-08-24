using System.IO;
using AIUsageMonitor.Desktop.ViewModels;
using AIUsageMonitor.Infrastructure;
using AIUsageMonitor.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace AIUsageMonitor.Desktop.Tests;

/// <summary>
/// Exercises the exact production composition root (AddInfrastructure + AddProviders +
/// AddDesktopWorkspaceServices) through the real Microsoft.Extensions.DependencyInjection
/// container, guarding against APO-36: AiCapacityViewModel's degraded-fallback constructor made
/// the implicit registration ambiguous and silently dropped App.OnStartup into the degraded shell.
/// </summary>
public sealed class ProductionCompositionTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(
        Path.GetTempPath(),
        "apo-composition-tests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void ProductionComposition_ResolvesAiCapacityViewModel()
    {
        using var provider = BuildProvider();

        var aiCapacity = provider.GetRequiredService<AiCapacityViewModel>();

        Assert.NotNull(aiCapacity);
    }

    [Fact]
    public void ProductionComposition_AiCapacityIsNotDegraded()
    {
        using var provider = BuildProvider();

        var aiCapacity = provider.GetRequiredService<AiCapacityViewModel>();

        Assert.False(aiCapacity.IsDegraded);
        Assert.Equal(5, aiCapacity.Cards.Count);
    }

    [Fact]
    public void ProductionComposition_ResolvesMainWindowViewModel()
    {
        using var provider = BuildProvider();

        var mainWindowViewModel = provider.GetRequiredService<MainWindowViewModel>();

        Assert.NotNull(mainWindowViewModel);
    }

    [Fact]
    public void MainWindowViewModel_UsesResolvedNormalAiCapacity()
    {
        using var provider = BuildProvider();

        var aiCapacity = provider.GetRequiredService<AiCapacityViewModel>();
        var mainWindowViewModel = provider.GetRequiredService<MainWindowViewModel>();

        Assert.Same(aiCapacity, mainWindowViewModel.AiCapacity);
        Assert.False(mainWindowViewModel.AiCapacity.IsDegraded);
    }

    [Fact]
    public void ProjectsViewModel_IsResolvedWithRegistryService()
    {
        using var provider = BuildProvider();

        var projects = provider.GetRequiredService<ProjectsViewModel>();

        Assert.True(projects.IsStorageAvailable);
        Assert.Same(projects, provider.GetRequiredService<MainWindowViewModel>().Projects);
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(_tempRoot);
        services.AddProviders();
        services.AddDesktopWorkspaceServices();
        return services.BuildServiceProvider();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}
