using System.Text.Json;
using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Infrastructure.Persistence;
using AIUsageMonitor.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageMonitor.Infrastructure.Tests;

public sealed class AgentRegistryPersistenceTests
{
    [Fact]
    public async Task LegacyAgentRecord_LoadsWithSafeDefaults_AndRoundTripsMeaning()
    {
        using var store = new TemporaryStore();
        var agentId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 8, 20, 10, 0, 0, TimeSpan.Zero);
        var legacy = new
        {
            schemaVersion = JsonFileStore.CurrentSchemaVersion,
            payload = new
            {
                items = new[]
                {
                    new
                    {
                        id = agentId,
                        name = "Legacy reviewer",
                        role = "review",
                        provider = "Manual",
                        connectionMode = (int)AgentConnectionMode.Manual,
                        availability = (int)AgentAvailability.Available,
                        enabled = true,
                        capabilities = new[] { "review" },
                        limitations = new[] { "human initiated" },
                        costAndQuotaMetadata = new Dictionary<string, string?>(),
                        createdAt,
                        updatedAt = createdAt.AddMinutes(1)
                    }
                }
            }
        };
        await File.WriteAllTextAsync(
            store.Paths.AgentsFile,
            JsonSerializer.Serialize(legacy, JsonFileStore.SerializerOptions));
        var repository = new JsonAgentRepository(
            store.Paths,
            store.Files,
            NullLogger<JsonAgentRepository>.Instance);

        var loaded = await repository.GetByIdAsync(agentId);

        Assert.NotNull(loaded);
        Assert.Equal(agentId, loaded!.Id);
        Assert.Equal("review", loaded.Role);
        Assert.Equal(AgentConnectionMode.Manual, loaded.ConnectionMode);
        Assert.Equal([AgentConnectionMode.Manual], loaded.SupportedConnectionModes);
        Assert.Equal(AgentAvailability.Available, loaded.Availability);
        Assert.Equal(AgentAuthenticationState.Unknown, loaded.AuthenticationState);
        Assert.Equal(AgentEntitlementState.Unknown, loaded.EntitlementState);
        Assert.Null(loaded.ModelIdentifier);
        Assert.Empty(loaded.RoleCapabilities);

        await repository.UpsertAsync(loaded);
        var reloaded = await repository.GetByIdAsync(agentId);

        Assert.NotNull(reloaded);
        Assert.Equal(loaded.Role, reloaded!.Role);
        Assert.Equal(loaded.ConnectionMode, reloaded.ConnectionMode);
        Assert.Equal(loaded.Availability, reloaded.Availability);
        Assert.Equal(loaded.SupportedConnectionModes, reloaded.SupportedConnectionModes);
        Assert.Equal(AgentAuthenticationState.Unknown, reloaded.AuthenticationState);
        Assert.Equal(AgentEntitlementState.Unknown, reloaded.EntitlementState);
    }

    [Fact]
    public async Task AgentRepository_RoundTripsConnectionTruthWithoutSecrets()
    {
        using var store = new TemporaryStore();
        var now = new DateTimeOffset(2026, 8, 25, 10, 0, 0, TimeSpan.Zero);
        var agent = new AgentDefinition(
            Guid.NewGuid(),
            "Manual Agent",
            "executor",
            AgentConnectionMode.InteractiveOnly,
            AgentAvailability.AuthenticationRequired,
            enabled: true,
            now.AddMinutes(-1),
            now,
            provider: "Sanitized Provider",
            capabilities: ["bounded work"],
            limitations: ["manual verification"],
            roleCapabilities: [AgentRole.Executor],
            supportedConnectionModes: [AgentConnectionMode.InteractiveOnly, AgentConnectionMode.Manual],
            authenticationState: AgentAuthenticationState.AuthenticationRequired,
            entitlementState: AgentEntitlementState.Unknown,
            modelIdentifier: "sanitized-model",
            rolePolicyMetadata: [new AgentRolePolicyMetadata(AgentRole.Executor, "bounded execution", "Primary")]);

        // The result identity must belong to the agent, so attach it in the immutable rebuild.
        agent = new AgentDefinition(
            agent.Id,
            agent.Name,
            agent.Role,
            agent.ConnectionMode,
            agent.Availability,
            agent.Enabled,
            agent.CreatedAt,
            agent.UpdatedAt,
            agent.Provider,
            agent.Capabilities,
            agent.Limitations,
            agent.CostAndQuotaMetadata,
            agent.RoleCapabilities,
            agent.SupportedConnectionModes,
            agent.AuthenticationState,
            agent.EntitlementState,
            agent.ModelIdentifier,
            agent.RolePolicyMetadata,
            new AgentConnectionResult(
                new AgentIdentity(agent.Id, agent.Name, agent.Provider, agent.ModelIdentifier),
                now,
                AgentConnectionMode.InteractiveOnly,
                AgentAvailability.AuthenticationRequired,
                AgentAuthenticationState.AuthenticationRequired,
                AgentEntitlementState.Unknown,
                AgentEvidenceSource.ManualVerification,
                limitationCode: "AUTH_REQUIRED",
                message: "Sanitized verification state.",
                supportedConnectionModes: [AgentConnectionMode.InteractiveOnly]));
        var repository = new JsonAgentRepository(
            store.Paths,
            store.Files,
            NullLogger<JsonAgentRepository>.Instance);

        await repository.UpsertAsync(agent);
        var loaded = await repository.GetByIdAsync(agent.Id);
        var json = await File.ReadAllTextAsync(store.Paths.AgentsFile);

        Assert.NotNull(loaded);
        Assert.Equal(agent.ModelIdentifier, loaded!.ModelIdentifier);
        Assert.Equal(agent.RoleCapabilities, loaded.RoleCapabilities);
        Assert.Equal(agent.SupportedConnectionModes, loaded.SupportedConnectionModes);
        Assert.Equal(agent.AuthenticationState, loaded.AuthenticationState);
        Assert.Equal(agent.EntitlementState, loaded.EntitlementState);
        Assert.NotNull(loaded.LastConnectionResult);
        Assert.Equal(AgentEvidenceSource.ManualVerification, loaded.LastConnectionResult!.EvidenceSource);
        Assert.Equal(AgentEntitlementState.Unknown, loaded.LastConnectionResult.EntitlementState);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bearer", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("session", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProjectAgentOverrides_AreGuidScopedAndDoNotLeakBetweenProjects()
    {
        using var store = new TemporaryStore();
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var repository = new JsonAgentProjectOverrideRepository(
            store.Paths,
            store.Files,
            NullLogger<JsonAgentProjectOverrideRepository>.Instance);
        var projectOverride = new AgentProjectOverride(
            projectA,
            agentId,
            enabledOverride: false,
            permittedRoles: [AgentRole.Reviewer],
            permittedConnectionModes: [AgentConnectionMode.Manual],
            policyReference: "project-a-policy",
            metadata: new Dictionary<string, string?> { ["owner"] = "sanitized" });

        await repository.UpsertAsync(projectOverride);

        var loadedA = await repository.GetAsync(projectA, agentId);
        var loadedB = await repository.GetAllAsync(projectB);

        Assert.NotNull(loadedA);
        Assert.Equal(projectA, loadedA!.ProjectId);
        Assert.False(loadedA.EnabledOverride);
        Assert.Equal([AgentRole.Reviewer], loadedA.PermittedRoles);
        Assert.Empty(loadedB);
        Assert.NotEqual(
            store.Paths.GetProjectAgentOverridesFile(projectA),
            store.Paths.GetProjectAgentOverridesFile(projectB));
        Assert.True(File.Exists(store.Paths.GetProjectAgentOverridesFile(projectA)));
        Assert.False(File.Exists(store.Paths.GetProjectAgentOverridesFile(projectB)));
        var json = await File.ReadAllTextAsync(store.Paths.GetProjectAgentOverridesFile(projectA));
        Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectAgentOverride_RejectsEmptyProjectOrAgent()
    {
        var agentId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new AgentProjectOverride(Guid.Empty, agentId));
        Assert.Throws<ArgumentException>(() => new AgentProjectOverride(projectId, Guid.Empty));
    }
}
