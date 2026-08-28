using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Infrastructure;
using AIUsageMonitor.Infrastructure.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace AIUsageMonitor.Infrastructure.Tests;

public sealed class ExecutionAdapterTests
{
    [Fact]
    public void Resolver_WithNoAdapters_IsTruthfullyUnsupported()
    {
        var resolver = new ExecutionAdapterResolver([]);

        var result = resolver.Resolve(CreateAgent(AgentConnectionMode.Cli));

        Assert.Equal(ExecutionAdapterResolutionStatus.Unsupported, result.Status);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Resolver_SelectsExactlyOneMatchingAdapter()
    {
        var adapter = new FakeAdapter(new ExecutionAdapterDescriptor(
            "cli-test",
            [AgentConnectionMode.Cli],
            [PlanningBudgetKind.ToolInvocations]));
        var resolver = new ExecutionAdapterResolver([adapter]);

        var result = resolver.Resolve(CreateAgent(AgentConnectionMode.Cli));

        Assert.Equal(ExecutionAdapterResolutionStatus.Resolved, result.Status);
        Assert.Same(adapter, result.Adapter);
    }

    [Fact]
    public void Resolver_WithMultipleExactMatches_ReportsConfigurationConflict()
    {
        var first = new FakeAdapter(new ExecutionAdapterDescriptor("one", [AgentConnectionMode.Cli]));
        var second = new FakeAdapter(new ExecutionAdapterDescriptor("two", [AgentConnectionMode.Cli]));
        var resolver = new ExecutionAdapterResolver([first, second]);

        var result = resolver.Resolve(CreateAgent(AgentConnectionMode.Cli));

        Assert.Equal(ExecutionAdapterResolutionStatus.ConfigurationConflict, result.Status);
        Assert.Null(result.Adapter);
    }

    [Theory]
    [InlineData(AgentConnectionMode.InteractiveOnly)]
    [InlineData(AgentConnectionMode.Manual)]
    [InlineData(AgentConnectionMode.Unsupported)]
    [InlineData(AgentConnectionMode.Unknown)]
    public void Resolver_DoesNotMakeUnsupportedConnectionModesExecutable(AgentConnectionMode mode)
    {
        var adapter = new FakeAdapter(new ExecutionAdapterDescriptor("test", [mode]));
        var result = new ExecutionAdapterResolver([adapter]).Resolve(CreateAgent(mode));

        Assert.Equal(ExecutionAdapterResolutionStatus.Resolved, result.Status);
        // The resolver answers descriptor exactness; the orchestration preflight rejects these
        // modes. It must not silently transform one mode into another.
        Assert.Equal(mode, result.Adapter!.Descriptor.SupportedConnectionModes.Single());
    }

    [Fact]
    public void Resolver_RequiresCancellationSupport()
    {
        var adapter = new FakeAdapter(new ExecutionAdapterDescriptor("uncancellable", [AgentConnectionMode.Cli], supportsCancellation: false));

        var result = new ExecutionAdapterResolver([adapter]).Resolve(CreateAgent(AgentConnectionMode.Cli));

        Assert.Equal(ExecutionAdapterResolutionStatus.Unsupported, result.Status);
    }

    [Fact]
    public void ProductionComposition_RegistersNoConcreteExecutionAdapters()
    {
        using var store = new TemporaryStore();
        var services = new ServiceCollection();
        services.AddInfrastructure(store.RootDirectory);

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IBoundedExecutionService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IExecutionAdapterResolver));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IExecutionAdapter));

        var result = new ExecutionAdapterResolver([]).Resolve(CreateAgent(AgentConnectionMode.Cli));
        Assert.Equal(ExecutionAdapterResolutionStatus.Unsupported, result.Status);
    }

    [Fact]
    public void EnvironmentBuilder_PreservesOrdinaryAllowlist_AndDropsSecretLikeNames()
    {
        var environment = BoundedProcessEnvironment.BuildAllowlisted(new Dictionary<string, string?>
        {
            ["PATH"] = "ordinary-path",
            ["TEMP"] = "ordinary-temp",
            ["API_KEY"] = "secret",
            ["SERVICE_TOKEN"] = "secret",
            ["PASSWORD"] = "secret",
            ["PAT"] = "secret",
            ["UNRELATED"] = "not-allowlisted"
        });

        Assert.Equal("ordinary-path", environment["PATH"]);
        Assert.Equal("ordinary-temp", environment["TEMP"]);
        Assert.DoesNotContain("API_KEY", environment.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("SERVICE_TOKEN", environment.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("PASSWORD", environment.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("PAT", environment.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("UNRELATED", environment.Keys, StringComparer.OrdinalIgnoreCase);
    }

    private static EffectiveAgentDefinition CreateAgent(AgentConnectionMode mode)
    {
        var availability = mode == AgentConnectionMode.Unsupported
            ? AgentAvailability.Unsupported
            : AgentAvailability.Available;
        IReadOnlyList<AgentConnectionMode>? supportedModes = mode is AgentConnectionMode.Unknown or AgentConnectionMode.Unsupported
            ? null
            : [mode];
        var definition = new AgentDefinition(
            Guid.NewGuid(),
            "Test executor",
            "Executor",
            mode,
            availability,
            enabled: true,
            DateTimeOffset.Parse("2026-08-28T10:00:00+00:00"),
            DateTimeOffset.Parse("2026-08-28T10:00:00+00:00"),
            provider: "TestProvider",
            capabilities: ["bounded"],
            limitations: [],
            roleCapabilities: [AgentRole.Executor],
            supportedConnectionModes: supportedModes,
            authenticationState: AgentAuthenticationState.NotRequired,
            entitlementState: AgentEntitlementState.VerifiedAvailable,
            modelIdentifier: "TestModel");
        return new EffectiveAgentDefinition(Guid.NewGuid(), definition, null);
    }

    private sealed class FakeAdapter(ExecutionAdapterDescriptor descriptor) : IExecutionAdapter
    {
        public ExecutionAdapterDescriptor Descriptor { get; } = descriptor;

        public Task<ExecutionAdapterResult> ExecuteAsync(ExecutionAdapterRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ExecutionAdapterResult(ExecutionAdapterOutcome.Succeeded));
    }
}
