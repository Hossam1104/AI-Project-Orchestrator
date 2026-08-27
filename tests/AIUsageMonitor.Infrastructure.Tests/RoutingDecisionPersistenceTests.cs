using System.Text.Json;
using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Routing;
using AIUsageMonitor.Infrastructure.Persistence;
using AIUsageMonitor.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageMonitor.Infrastructure.Tests;

public sealed class RoutingDecisionPersistenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAndReadRoundTripUsesExactGuidPathAndPreservesIntegrity()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var decision = CreateDecision();

        var write = await repository.CreateAsync(decision);
        var read = await repository.GetAsync(decision.ProjectId, decision.DecisionId);

        Assert.Equal(RoutingDecisionRepositoryWriteStatus.Created, write.Status);
        Assert.Equal(RoutingDecisionReadState.Valid, read.State);
        Assert.Equal(decision.ContentHash, read.Decision!.ContentHash);
        Assert.Equal(decision.InputFingerprint, read.Decision.InputFingerprint);
        Assert.Equal(store.Paths.GetRoutingDecisionFile(decision.ProjectId, decision.DecisionId),
            Path.Combine(store.Paths.GetRoutingDecisionDirectory(decision.ProjectId, decision.DecisionId), "decision.json"));
        Assert.True(File.Exists(store.Paths.GetRoutingDecisionFile(decision.ProjectId, decision.DecisionId)));
        Assert.True(RoutingDecisionIntegrity.ComputeCanonicalPayloadBytes(read.Decision).Length <= RoutingDecisionLimits.MaxCanonicalPayloadBytes);
    }

    [Fact]
    public async Task SameDecisionIdIsCreateOnceAndOriginalBytesRemainUnchanged()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var decision = CreateDecision();
        await repository.CreateAsync(decision);
        var path = store.Paths.GetRoutingDecisionFile(decision.ProjectId, decision.DecisionId);
        var original = await File.ReadAllTextAsync(path);

        var conflict = await repository.CreateAsync(decision);

        Assert.Equal(RoutingDecisionRepositoryWriteStatus.DecisionConflict, conflict.Status);
        Assert.Equal(original, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task TamperedContentIsIntegrityFailureWithoutQuarantineOrRepair()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var decision = CreateDecision();
        await repository.CreateAsync(decision);
        var path = store.Paths.GetRoutingDecisionFile(decision.ProjectId, decision.DecisionId);
        var original = await File.ReadAllTextAsync(path);
        var record = RoutingDecisionRecord.FromApplication(decision);
        record.Limitations.Add("tampered");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(
            new { schemaVersion = JsonFileStore.CurrentSchemaVersion, payload = record },
            JsonFileStore.SerializerOptions));

        var read = await repository.GetAsync(decision.ProjectId, decision.DecisionId);

        Assert.Equal(RoutingDecisionReadState.IntegrityFailure, read.State);
        Assert.NotEqual(original, await File.ReadAllTextAsync(path));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.bak", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task FutureAndLowerDecisionSchemasAreReportedWithoutMutation()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var decision = CreateDecision();
        var record = RoutingDecisionRecord.FromApplication(decision);
        var path = store.Paths.GetRoutingDecisionFile(decision.ProjectId, decision.DecisionId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        record.SchemaVersion = RoutingDecisionSchema.CurrentVersion + 1;
        await WriteEnvelope(path, record);
        var futureBytes = await File.ReadAllTextAsync(path);
        var future = await repository.GetAsync(decision.ProjectId, decision.DecisionId);
        Assert.Equal(RoutingDecisionReadState.UnsupportedFutureVersion, future.State);
        Assert.Equal(futureBytes, await File.ReadAllTextAsync(path));

        record.SchemaVersion = RoutingDecisionSchema.CurrentVersion - 1;
        await WriteEnvelope(path, record);
        var migration = await repository.GetAsync(decision.ProjectId, decision.DecisionId);
        Assert.Equal(RoutingDecisionReadState.MigrationRequired, migration.State);
    }

    [Fact]
    public async Task DecisionStorageIsProjectIsolatedAndDoesNotScanOtherDirectories()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var decision = CreateDecision();
        await repository.CreateAsync(decision);

        var otherProject = Guid.Parse("90000000-0000-0000-0000-000000000002");
        var read = await repository.GetAsync(otherProject, decision.DecisionId);

        Assert.Equal(RoutingDecisionReadState.Missing, read.State);
        Assert.False(File.Exists(store.Paths.GetRoutingDecisionFile(otherProject, decision.DecisionId)));
    }

    private static JsonRoutingDecisionRepository CreateRepository(TemporaryStore store) =>
        new(store.Paths, store.Files, NullLogger<JsonRoutingDecisionRepository>.Instance);

    private static async Task WriteEnvelope(string path, RoutingDecisionRecord record)
    {
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(
            new { schemaVersion = JsonFileStore.CurrentSchemaVersion, payload = record },
            JsonFileStore.SerializerOptions));
    }

    private static RoutingDecision CreateDecision()
    {
        var projectId = Guid.Parse("90000000-0000-0000-0000-000000000001");
        var agentId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var classification = new RoutingTaskClassification(
            RoutingScopeScale.Bounded,
            RoutingTaskRisk.Low,
            RoutingBlastRadius.Local,
            RoutingValidationCost.Low,
            AgentRole.Executor,
            ["code"]);
        var policy = new RoutingPolicySnapshot("policy:persistence", AgentRole.Executor);
        var agent = new RoutingAgentSnapshot(
            projectId,
            agentId,
            new AgentIdentity(agentId, "Persistence Agent", "provider", "model"),
            Now,
            true,
            [AgentRole.Executor],
            ["code"],
            [],
            AgentConnectionMode.Api,
            [AgentConnectionMode.Api],
            AgentAvailability.Available,
            AgentAuthenticationState.Authenticated,
            AgentEntitlementState.VerifiedAvailable);
        var input = new RoutingInputSnapshot(
            projectId,
            new PlanningExecutionContractReference(Guid.Parse("10000000-0000-0000-0000-000000000001"), 1, 1, new('a', 64)),
            new RoutingContextReference(Guid.Parse("20000000-0000-0000-0000-000000000001"), 1, Now),
            classification,
            policy,
            [agent],
            [new RoutingCapacityEvidence(agentId, RoutingCapacityState.Sufficient, Now, Now.AddHours(1), "fixture:capacity", source: RoutingCapacityEvidenceSource.Manual)],
            null,
            Now);
        var evaluation = new RoutingDecisionEngine().Evaluate(input);
        return new(projectId, Guid.Parse("40000000-0000-0000-0000-000000000001"), RoutingDecisionSchema.CurrentVersion, Now, evaluation);
    }
}
