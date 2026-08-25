using AIUsageMonitor.Application.Agents;

namespace AIUsageMonitor.Connection.Tests;

public sealed class AgentRegistryTruthTests
{
    [Fact]
    public void AgentDefinition_PreservesLegacyFieldsAndNormalizesNewCapabilities()
    {
        var now = new DateTimeOffset(2026, 8, 25, 10, 0, 0, TimeSpan.Zero);
        var agent = new AgentDefinition(
            Guid.NewGuid(),
            "Luna",
            "implementation",
            AgentConnectionMode.Api,
            AgentAvailability.AuthenticationRequired,
            enabled: true,
            now,
            now,
            provider: "OpenAI",
            roleCapabilities: [AgentRole.Executor, AgentRole.Executor, AgentRole.Reviewer],
            supportedConnectionModes: [AgentConnectionMode.Api, AgentConnectionMode.Cli, AgentConnectionMode.Api],
            authenticationState: AgentAuthenticationState.AuthenticationRequired,
            entitlementState: AgentEntitlementState.Unknown,
            modelIdentifier: "model-luna",
            rolePolicyMetadata: [new AgentRolePolicyMetadata(AgentRole.Executor, "primary implementation executor", "Primary")]);

        Assert.Equal("Luna", agent.Name);
        Assert.Equal("OpenAI", agent.Provider);
        Assert.Equal("model-luna", agent.ModelIdentifier);
        Assert.Equal([AgentRole.Executor, AgentRole.Reviewer], agent.RoleCapabilities);
        Assert.Equal([AgentConnectionMode.Api, AgentConnectionMode.Cli], agent.SupportedConnectionModes);
        Assert.Equal(AgentAuthenticationState.AuthenticationRequired, agent.AuthenticationState);
        Assert.Equal(AgentEntitlementState.Unknown, agent.EntitlementState);
        Assert.Equal("implementation", agent.Role);
    }

    [Fact]
    public void ConnectionResult_SeparatesAuthenticationFromUnverifiedEntitlement()
    {
        var result = new AgentConnectionResult(
            new AgentIdentity(Guid.NewGuid(), "Manual Claude", "Anthropic", "claude-manual"),
            new DateTimeOffset(2026, 8, 25, 10, 0, 0, TimeSpan.Zero),
            AgentConnectionMode.InteractiveOnly,
            AgentAvailability.AuthenticationRequired,
            AgentAuthenticationState.AuthenticationRequired,
            AgentEntitlementState.Unknown,
            AgentEvidenceSource.InteractiveManualState,
            limitationCode: "AUTH_REQUIRED",
            message: "Interactive verification is required.",
            supportedConnectionModes: [AgentConnectionMode.InteractiveOnly]);

        Assert.Equal(AgentAvailability.AuthenticationRequired, result.Availability);
        Assert.Equal(AgentAuthenticationState.AuthenticationRequired, result.AuthenticationState);
        Assert.Equal(AgentEntitlementState.Unknown, result.EntitlementState);
        Assert.DoesNotContain("token", result.Message!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConnectionResult_RejectsUnsupportedModeAsAvailable()
    {
        Assert.Throws<ArgumentException>(() => new AgentConnectionResult(
            new AgentIdentity(Guid.NewGuid(), "Unknown"),
            DateTimeOffset.UtcNow,
            AgentConnectionMode.Unsupported,
            AgentAvailability.Available,
            AgentAuthenticationState.Unknown,
            AgentEntitlementState.Unknown,
            AgentEvidenceSource.ManualVerification));
    }

    [Fact]
    public void DefaultCatalog_ContainsSixUnverifiedOwnerApprovedEntries()
    {
        var defaults = new DefaultAgentCatalog().GetDefaults();

        Assert.Equal(6, defaults.Count);
        Assert.Equal(
            ["GPT-5.6 Sol", "GPT-5.6 Luna xHigh", "Claude Sonnet 5", "Claude Opus 5", "GPT-5.6 Terra HIGH", "Gemini 3.7"],
            defaults.Select(value => value.Name));

        var sol = defaults.Single(value => value.Name == "GPT-5.6 Sol");
        Assert.Equal(
            [AgentRole.Planner, AgentRole.Architect, AgentRole.AcceptanceAuthority],
            sol.RoleCapabilities);
        Assert.All(defaults, value =>
        {
            Assert.Equal(AgentAvailability.Unknown, value.Availability);
            Assert.Equal(AgentAuthenticationState.Unknown, value.AuthenticationState);
            Assert.Equal(AgentEntitlementState.Unknown, value.EntitlementState);
            Assert.Equal([AgentConnectionMode.Unsupported], value.SupportedConnectionModes);
        });
    }

    [Fact]
    public async Task EffectiveRegistry_AppliesOnlyProjectConfigurationAndReturnsExplicitMissing()
    {
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var agent = new AgentDefinition(
            agentId,
            "Agent",
            "executor",
            AgentConnectionMode.Manual,
            AgentAvailability.Available,
            enabled: true,
            now,
            now,
            roleCapabilities: [AgentRole.Executor, AgentRole.Reviewer],
            supportedConnectionModes: [AgentConnectionMode.Manual]);
        var agents = new InMemoryAgentRepository(agent);
        var overrides = new InMemoryAgentOverrideRepository();
        await overrides.UpsertAsync(new AgentProjectOverride(
            projectA,
            agentId,
            enabledOverride: false,
            permittedRoles: [AgentRole.Reviewer],
            permittedConnectionModes: [AgentConnectionMode.Manual],
            policyReference: "project-a-policy"));
        var service = new AgentRegistryService(agents, overrides);

        var projectAView = await service.ResolveAsync(projectA, agentId);
        var projectBView = await service.ResolveAsync(projectB, agentId);
        var missing = await service.ResolveAsync(projectA, Guid.NewGuid());

        Assert.True(projectAView.Found);
        Assert.NotNull(projectAView.Agent);
        Assert.False(projectAView.Agent!.Enabled);
        Assert.Equal([AgentRole.Reviewer], projectAView.Agent.RoleCapabilities);
        Assert.Equal("project-a-policy", projectAView.Agent.ProjectOverride!.PolicyReference);
        Assert.True(projectBView.Found);
        Assert.True(projectBView.Agent!.Enabled);
        Assert.Equal([AgentRole.Executor, AgentRole.Reviewer], projectBView.Agent.RoleCapabilities);
        Assert.Null(projectBView.Agent.ProjectOverride);
        Assert.False(missing.Found);
        Assert.Equal(AgentRegistryResolutionStatus.NotFound, missing.Status);
        Assert.True(agent.Enabled);
        Assert.Equal([AgentRole.Executor, AgentRole.Reviewer], agent.RoleCapabilities);
    }

    private sealed class InMemoryAgentRepository : IAgentRepository
    {
        private readonly Dictionary<Guid, AgentDefinition> _agents;

        public InMemoryAgentRepository(AgentDefinition agent) => _agents = new() { [agent.Id] = agent };

        public Task<IReadOnlyList<AgentDefinition>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AgentDefinition>>(_agents.Values.ToArray());

        public Task<AgentDefinition?> GetByIdAsync(Guid agentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_agents.GetValueOrDefault(agentId));

        public Task UpsertAsync(AgentDefinition agent, CancellationToken cancellationToken = default)
        {
            _agents[agent.Id] = agent;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryAgentOverrideRepository : IAgentProjectOverrideRepository
    {
        private readonly Dictionary<(Guid ProjectId, Guid AgentId), AgentProjectOverride> _overrides = [];

        public Task<IReadOnlyList<AgentProjectOverride>> GetAllAsync(
            Guid projectId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AgentProjectOverride>>(
                _overrides
                    .Where(pair => pair.Key.ProjectId == projectId)
                    .Select(pair => pair.Value)
                    .ToArray());

        public Task<AgentProjectOverride?> GetAsync(
            Guid projectId,
            Guid agentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_overrides.GetValueOrDefault((projectId, agentId)));

        public Task UpsertAsync(
            AgentProjectOverride projectOverride,
            CancellationToken cancellationToken = default)
        {
            _overrides[(projectOverride.ProjectId, projectOverride.AgentId)] = projectOverride;
            return Task.CompletedTask;
        }
    }
}
