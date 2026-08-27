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

    [Theory]
    [MemberData(nameof(TamperCases))]
    public async Task TamperMatrixFailsClosedAndRepeatedReadsNeverMutateTheOriginalBytes(string tamperCase)
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var decision = CreateDecision();
        await repository.CreateAsync(decision);
        var path = store.Paths.GetRoutingDecisionFile(decision.ProjectId, decision.DecisionId);
        var record = RoutingDecisionRecord.FromApplication(decision);
        ApplyTamper(record, tamperCase);
        await WriteEnvelope(path, record);
        var bytesBeforeRead = await File.ReadAllBytesAsync(path);

        var firstRead = await repository.GetAsync(decision.ProjectId, decision.DecisionId);
        var secondRead = await repository.GetAsync(decision.ProjectId, decision.DecisionId);

        Assert.Equal(RoutingDecisionReadState.IntegrityFailure, firstRead.State);
        Assert.Equal(firstRead.State, secondRead.State);
        Assert.Equal(firstRead.ErrorMessage, secondRead.ErrorMessage);
        var bytesAfterRead = await File.ReadAllBytesAsync(path);
        Assert.True(bytesBeforeRead.SequenceEqual(bytesAfterRead));
        var directory = Path.GetDirectoryName(path)!;
        Assert.Equal(["decision.json"], Directory.GetFiles(directory)
            .Select(Path.GetFileName)
            .OrderBy(value => value, StringComparer.Ordinal));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.bak", SearchOption.AllDirectories));
    }

    public static IEnumerable<object[]> TamperCases =>
    [
        ["RequestedRole"],
        ["Risk"],
        ["RequiredCapability"],
        ["CandidateAgentId"],
        ["CandidateIsEligible"],
        ["CapacityState"],
        ["PolicyPreferredAgent"],
        ["SelectedAgentId"],
        ["OwnerOverrideDisposition"],
        ["OuterInputFingerprint"],
        ["InnerInputFingerprint"],
        ["ContentHash"]
    ];

    private static void ApplyTamper(RoutingDecisionRecord record, string tamperCase)
    {
        var alternateAgentId = Guid.Parse("30000000-0000-0000-0000-000000000002");
        switch (tamperCase)
        {
            case "RequestedRole":
                record.Recommendation!.RequestedRole = AgentRole.Reviewer;
                break;
            case "Risk":
                record.Input.Classification.Risk = RoutingTaskRisk.High;
                break;
            case "RequiredCapability":
                record.Input.Classification.RequiredCapabilities[0] = "review";
                break;
            case "CandidateAgentId":
                record.CandidateAssessments[0].Candidate.AgentId = alternateAgentId;
                break;
            case "CandidateIsEligible":
                record.CandidateAssessments[0].IsEligible = false;
                break;
            case "CapacityState":
                record.CandidateAssessments[0].CapacityState = RoutingCapacityState.Constrained;
                break;
            case "PolicyPreferredAgent":
                record.Input.Policy.PreferredAgentIds.Add(alternateAgentId);
                break;
            case "SelectedAgentId":
                record.SelectedAgentId = alternateAgentId;
                break;
            case "OwnerOverrideDisposition":
                record.OwnerOverrideDisposition = RoutingOverrideDisposition.Applied;
                break;
            case "OuterInputFingerprint":
                record.InputFingerprint = new string('b', 64);
                break;
            case "InnerInputFingerprint":
                record.Input.InputFingerprint = new string('b', 64);
                break;
            case "ContentHash":
                record.ContentHash = new string('b', 64);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(tamperCase), tamperCase, "Unknown tamper case.");
        }
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
