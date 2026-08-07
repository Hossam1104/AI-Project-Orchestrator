namespace AIUsageMonitor.Provider.Tests;

public class FoundationSmokeTests
{
    [Fact]
    public void ProvidersAssembly_Loads()
    {
        var assembly = typeof(AIUsageMonitor.Providers.AssemblyMarker).Assembly;

        Assert.NotNull(assembly);
    }
}
