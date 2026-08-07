namespace AIUsageMonitor.Domain.Tests;

public class FoundationSmokeTests
{
    [Fact]
    public void DomainAssembly_Loads()
    {
        var assembly = typeof(AIUsageMonitor.Domain.AssemblyMarker).Assembly;

        Assert.NotNull(assembly);
    }
}
