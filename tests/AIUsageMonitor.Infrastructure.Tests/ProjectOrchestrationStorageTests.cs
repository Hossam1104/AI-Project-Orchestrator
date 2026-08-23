using System.Text.Json;
using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Projects;
using AIUsageMonitor.Application.Routing;
using AIUsageMonitor.Infrastructure.Persistence;
using AIUsageMonitor.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageMonitor.Infrastructure.Tests;

public sealed class ProjectOrchestrationStorageTests
{
    [Fact]
    public async Task ApplicationDataPaths_ExtendLegacyRootWithGuidScopedProjectLayout()
    {
        using var store = new TemporaryStore();
        var projectId = Guid.NewGuid();

        Assert.Contains("AIUsageMonitorTests", store.Paths.RootDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("projects.json", store.Paths.ProjectsFile, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("agents.json", store.Paths.AgentsFile, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("routing-policy.json", store.Paths.RoutingPolicyFile, StringComparison.OrdinalIgnoreCase);
        Assert.True(Directory.Exists(store.Paths.ProjectsDirectory));

        var projectPaths = store.Paths.GetProjectPaths(projectId);
        Assert.Equal(projectId, projectPaths.ProjectId);
        Assert.Contains(projectId.ToString("D"), projectPaths.RootDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(store.Paths.ProjectsDirectory, projectPaths.RootDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("routing-policy.json", projectPaths.RoutingPolicyFile, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("runs", projectPaths.RunsDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("evidence", projectPaths.EvidenceDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("reviews", projectPaths.ReviewsDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("activity", projectPaths.ActivityDirectory, StringComparison.OrdinalIgnoreCase);

        await store.Paths.EnsureProjectDirectoriesAsync(projectId);

        Assert.True(Directory.Exists(projectPaths.RootDirectory));
        Assert.True(Directory.Exists(projectPaths.RunsDirectory));
        Assert.True(Directory.Exists(projectPaths.EvidenceDirectory));
        Assert.True(Directory.Exists(projectPaths.ReviewsDirectory));
        Assert.True(Directory.Exists(projectPaths.ActivityDirectory));
        Assert.Throws<ArgumentException>(() => store.Paths.GetProjectDirectory(Guid.Empty));
    }

    [Fact]
    public async Task ProjectAndAgentRegistries_RoundTripMetadataWithExplicitSchema()
    {
        using var store = new TemporaryStore();
        var now = new DateTimeOffset(2026, 8, 23, 10, 0, 0, TimeSpan.Zero);
        var project = new Project(
            Guid.NewGuid(),
            "APO",
            "D:\\AI Tools\\Hossam\\AI-Project-Orchestrator",
            "main",
            ProjectStatus.Active,
            now.AddDays(-1),
            now,
            repositoryProvider: "GitHub",
            repositoryUrl: "https://github.com/Hossam1104/AI-Project-Orchestrator",
            repositoryId: "Hossam1104/AI-Project-Orchestrator",
            repositoryMetadata: new Dictionary<string, string?> { ["visibility"] = "private" },
            trackerType: "Jira",
            trackerId: "APO",
            trackerMetadata: new Dictionary<string, string?> { ["projectKey"] = "APO" },
            governanceReferences: ["AGENTS.md", "docs/BRD.md"],
            routingPolicyReference: "global",
            safetyPolicyReference: "default");
        var projectRepository = new JsonProjectRepository(
            store.Paths,
            store.Files,
            NullLogger<JsonProjectRepository>.Instance);

        await projectRepository.UpsertAsync(project);
        var loadedProject = await projectRepository.GetByIdAsync(project.Id);

        Assert.NotNull(loadedProject);
        Assert.Equal(project.Id, loadedProject!.Id);
        Assert.Equal(project.LocalPath, loadedProject.LocalPath);
        Assert.Equal(project.RepositoryUrl, loadedProject.RepositoryUrl);
        Assert.Equal("APO", loadedProject.TrackerId);
        Assert.Equal("main", loadedProject.DefaultBranch);
        Assert.Equal(["AGENTS.md", "docs/BRD.md"], loadedProject.GovernanceReferences);

        var agent = new AgentDefinition(
            Guid.NewGuid(),
            "GPT-5.6 Luna Max",
            "substantial implementation",
            AgentConnectionMode.Api,
            AgentAvailability.Available,
            enabled: true,
            now.AddDays(-2),
            now,
            provider: "OpenAI",
            capabilities: ["cross-cutting implementation", "C#"],
            limitations: ["requires explicit contract"],
            costAndQuotaMetadata: new Dictionary<string, string?> { ["billingUnit"] = "tokens" });
        var agentRepository = new JsonAgentRepository(
            store.Paths,
            store.Files,
            NullLogger<JsonAgentRepository>.Instance);

        await agentRepository.UpsertAsync(agent);
        var loadedAgent = await agentRepository.GetByIdAsync(agent.Id);

        Assert.NotNull(loadedAgent);
        Assert.Equal(agent.Name, loadedAgent!.Name);
        Assert.Equal(agent.ConnectionMode, loadedAgent.ConnectionMode);
        Assert.Equal(agent.Capabilities, loadedAgent.Capabilities);
        Assert.Equal("tokens", loadedAgent.CostAndQuotaMetadata["billingUnit"]);

        var projectJson = await File.ReadAllTextAsync(store.Paths.ProjectsFile);
        var agentJson = await File.ReadAllTextAsync(store.Paths.AgentsFile);
        Assert.Contains("schemaVersion", projectJson, StringComparison.Ordinal);
        Assert.Contains("schemaVersion", agentJson, StringComparison.Ordinal);
        Assert.DoesNotContain("password", projectJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credential", agentJson, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Directory.EnumerateFiles(store.Paths.RootDirectory, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task SecretBearingMetadataKeys_AreRejectedBeforeAnyPersistence()
    {
        using var store = new TemporaryStore();
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() => new Project(
            Guid.NewGuid(),
            "Unsafe",
            "C:\\workspace",
            "main",
            ProjectStatus.Active,
            now,
            now,
            repositoryMetadata: new Dictionary<string, string?> { ["api-token"] = "must-not-persist" }));

        Assert.Throws<ArgumentException>(() => new AgentDefinition(
            Guid.NewGuid(),
            "Unsafe",
            "manual",
            AgentConnectionMode.Manual,
            AgentAvailability.Unknown,
            enabled: true,
            now,
            now,
            costAndQuotaMetadata: new Dictionary<string, string?> { ["credentialReference"] = "opaque" }));

        Assert.Empty(Directory.EnumerateFiles(store.Paths.RootDirectory, "*.json", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task RoutingPolicyStore_PersistsGlobalAndProjectOverrideSeparately()
    {
        using var store = new TemporaryStore();
        var policies = new JsonRoutingPolicyStore(
            store.Paths,
            store.Files,
            NullLogger<JsonRoutingPolicyStore>.Instance);
        var global = new RoutingPolicy(
            qualityRiskFirst: true,
            requireIndependentReviewForHighRisk: true,
            requireHumanApprovalForHighRisk: true,
            maxConcurrentRuns: 1,
            maxRetries: 2,
            maxReviewRemediationCycles: 1,
            DateTimeOffset.UtcNow,
            new Dictionary<string, string?> { ["defaultRole"] = "substantial" });
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();
        var overridePolicy = new RoutingPolicy(
            qualityRiskFirst: true,
            requireIndependentReviewForHighRisk: null,
            requireHumanApprovalForHighRisk: false,
            maxConcurrentRuns: 2,
            maxRetries: null,
            maxReviewRemediationCycles: null,
            DateTimeOffset.UtcNow,
            new Dictionary<string, string?> { ["projectRole"] = "bounded" });

        await policies.SaveGlobalAsync(global);
        await policies.SaveProjectOverrideAsync(projectA, overridePolicy);

        var loadedGlobal = await policies.GetGlobalAsync();
        var loadedA = await policies.GetProjectOverrideAsync(projectA);
        var loadedB = await policies.GetProjectOverrideAsync(projectB);

        Assert.NotNull(loadedGlobal);
        Assert.True(loadedGlobal!.QualityRiskFirst);
        Assert.Equal(1, loadedGlobal.MaxConcurrentRuns);
        Assert.NotNull(loadedA);
        Assert.False(loadedA!.RequireHumanApprovalForHighRisk);
        Assert.Equal("bounded", loadedA.Rules["projectRole"]);
        Assert.Null(loadedB);
        Assert.True(File.Exists(store.Paths.RoutingPolicyFile));
        Assert.True(File.Exists(store.Paths.GetProjectRoutingPolicyFile(projectA)));
        Assert.False(File.Exists(store.Paths.GetProjectRoutingPolicyFile(projectB)));

        var globalJson = await File.ReadAllTextAsync(store.Paths.RoutingPolicyFile);
        var overrideJson = await File.ReadAllTextAsync(store.Paths.GetProjectRoutingPolicyFile(projectA));
        Assert.Contains("schemaVersion", globalJson, StringComparison.Ordinal);
        Assert.Contains("schemaVersion", overrideJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProjectOrchestrationStreams_IsolateProjectsAndPreserveChronologicalRangeReads()
    {
        using var store = new TemporaryStore();
        var orchestration = CreateOrchestrationStore(store);
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();
        var august = new DateTimeOffset(2026, 8, 31, 23, 0, 0, TimeSpan.Zero);
        var september = august.AddHours(2);
        var runA = new ExecutionRun(projectA, Guid.NewGuid(), ExecutionRunStatus.Completed, august);
        var runB = new ExecutionRun(projectB, Guid.NewGuid(), ExecutionRunStatus.Running, august.AddMinutes(1));
        var runA2 = new ExecutionRun(projectA, Guid.NewGuid(), ExecutionRunStatus.Review, september);
        var evidenceA = new EvidenceMetadata(
            projectA,
            Guid.NewGuid(),
            september,
            "test",
            "passed",
            runA2.RunId,
            validatorReference: "dotnet test",
            artifactReference: "artifacts/test-result.xml",
            contentHash: "sha256:abc",
            summary: "focused storage tests passed");
        var reviewA = new ReviewMetadata(
            projectA,
            Guid.NewGuid(),
            september.AddMinutes(1),
            "opus-5",
            "changes-required",
            "medium",
            blocking: true,
            runA2.RunId,
            findingCount: 1,
            evidenceReference: evidenceA.EvidenceId.ToString("D"),
            summary: "one bounded finding");
        var activityA = new ActivityAuditRecord(
            projectA,
            Guid.NewGuid(),
            september.AddMinutes(2),
            "apo",
            "review-recorded",
            "success",
            runA2.RunId,
            evidenceA.EvidenceId,
            "review metadata appended");

        await orchestration.AppendExecutionRunAsync(runA);
        await orchestration.AppendExecutionRunAsync(runB);
        await orchestration.AppendExecutionRunAsync(runA2);
        await orchestration.AppendEvidenceAsync(evidenceA);
        await orchestration.AppendReviewAsync(reviewA);
        await orchestration.AppendActivityAsync(activityA);

        var runsA = await orchestration.ReadExecutionRunsAsync(projectA, august.AddMinutes(-1), september.AddHours(1));
        var runsB = await orchestration.ReadExecutionRunsAsync(projectB, august.AddMinutes(-1), september.AddHours(1));
        var evidence = await orchestration.ReadEvidenceAsync(projectA, august, september.AddHours(1));
        var reviews = await orchestration.ReadReviewsAsync(projectA, august, september.AddHours(1));
        var activity = await orchestration.ReadActivityAsync(projectA, august, september.AddHours(1));

        Assert.Equal([runA.RunId, runA2.RunId], runsA.Select(value => value.RunId));
        Assert.Equal([runB.RunId], runsB.Select(value => value.RunId));
        Assert.Equal([evidenceA.EvidenceId], evidence.Select(value => value.EvidenceId));
        Assert.Equal([reviewA.ReviewId], reviews.Select(value => value.ReviewId));
        Assert.Equal([activityA.ActivityId], activity.Select(value => value.ActivityId));

        var runAugust = store.Paths.GetMonthlyPartition(store.Paths.GetProjectRunsDirectory(projectA), august);
        var runSeptember = store.Paths.GetMonthlyPartition(store.Paths.GetProjectRunsDirectory(projectA), september);
        Assert.True(File.Exists(runAugust));
        Assert.True(File.Exists(runSeptember));
        Assert.All(
            Directory.EnumerateFiles(store.Paths.GetProjectDirectory(projectA), "*.jsonl", SearchOption.AllDirectories),
            path => Assert.DoesNotContain(projectB.ToString("D"), File.ReadAllText(path), StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProjectOrchestrationJsonl_PreservesSchemaTailRecoveryAndIgnoresCrossProjectRecords()
    {
        using var store = new TemporaryStore();
        var orchestration = CreateOrchestrationStore(store);
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();
        var capturedAt = new DateTimeOffset(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);
        var first = new ExecutionRun(projectA, Guid.NewGuid(), ExecutionRunStatus.Running, capturedAt);
        var second = new ExecutionRun(projectA, Guid.NewGuid(), ExecutionRunStatus.Completed, capturedAt.AddMinutes(1));
        var foreign = new ExecutionRun(projectB, Guid.NewGuid(), ExecutionRunStatus.Running, capturedAt.AddMinutes(2));
        var path = store.Paths.GetMonthlyPartition(store.Paths.GetProjectRunsDirectory(projectA), capturedAt);
        Directory.CreateDirectory(store.Paths.GetProjectRunsDirectory(projectA));
        var firstLine = JsonSerializer.Serialize(
            ExecutionRunRecord.FromApplication(first),
            JsonFileStore.JsonlSerializerOptions);
        var foreignLine = JsonSerializer.Serialize(
            ExecutionRunRecord.FromApplication(foreign),
            JsonFileStore.JsonlSerializerOptions);
        await File.WriteAllTextAsync(
            path,
            $"{firstLine}\n{{\"schemaVersion\":999,\"recordType\":\"execution-run\"}}\n{foreignLine}\n{{\"schemaVersion\":1");

        await orchestration.AppendExecutionRunAsync(second);
        var loaded = await orchestration.ReadExecutionRunsAsync(projectA, capturedAt.AddMinutes(-1), capturedAt.AddMinutes(3));
        var persisted = await File.ReadAllTextAsync(path);

        Assert.Equal([first.RunId, second.RunId], loaded.Select(value => value.RunId));
        Assert.Contains("schemaVersion", persisted, StringComparison.Ordinal);
        Assert.Contains("{\"schemaVersion\":1\n", persisted, StringComparison.Ordinal);
        Assert.Contains(JsonSerializer.Serialize(ExecutionRunRecord.FromApplication(second), JsonFileStore.JsonlSerializerOptions), persisted, StringComparison.Ordinal);
        Assert.DoesNotContain(foreign.RunId.ToString("D"), string.Join(Environment.NewLine, loaded.Select(value => value.RunId)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProjectOrchestrationStreams_ConcurrentAppendsRemainComplete()
    {
        using var store = new TemporaryStore();
        var orchestration = CreateOrchestrationStore(store);
        var projectId = Guid.NewGuid();
        var capturedAt = new DateTimeOffset(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);
        var runs = Enumerable.Range(0, 24)
            .Select(index => new ExecutionRun(
                projectId,
                Guid.NewGuid(),
                ExecutionRunStatus.Completed,
                capturedAt.AddSeconds(index)))
            .ToArray();

        await Task.WhenAll(runs.Select(run => orchestration.AppendExecutionRunAsync(run)));

        var loaded = await orchestration.ReadExecutionRunsAsync(projectId, capturedAt.AddMinutes(-1), capturedAt.AddMinutes(1));
        Assert.Equal(runs.Length, loaded.Count);
        Assert.Equal(runs.Select(value => value.RunId).OrderBy(value => value), loaded.Select(value => value.RunId).OrderBy(value => value));

        var path = store.Paths.GetMonthlyPartition(store.Paths.GetProjectRunsDirectory(projectId), capturedAt);
        Assert.All(await File.ReadAllLinesAsync(path), line =>
        {
            Assert.Contains("schemaVersion", line, StringComparison.Ordinal);
            Assert.Contains("recordType", line, StringComparison.Ordinal);
        });
        Assert.Empty(Directory.EnumerateFiles(store.Paths.GetProjectDirectory(projectId), "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task UnsupportedRoutingPolicy_IsQuarantinedAndDoesNotBlockFutureWrite()
    {
        using var store = new TemporaryStore();
        var policies = new JsonRoutingPolicyStore(
            store.Paths,
            store.Files,
            NullLogger<JsonRoutingPolicyStore>.Instance);

        await File.WriteAllTextAsync(
            store.Paths.RoutingPolicyFile,
            "{ \"schemaVersion\": 999, \"payload\": {} }");

        Assert.Null(await policies.GetGlobalAsync());
        Assert.False(File.Exists(store.Paths.RoutingPolicyFile));
        Assert.NotEmpty(Directory.EnumerateFiles(
            store.Paths.RootDirectory,
            "routing-policy.json.unsupported-schema-*.bak"));

        await policies.SaveGlobalAsync(new RoutingPolicy(
            qualityRiskFirst: true,
            requireIndependentReviewForHighRisk: null,
            requireHumanApprovalForHighRisk: null,
            maxConcurrentRuns: null,
            maxRetries: null,
            maxReviewRemediationCycles: null,
            DateTimeOffset.UtcNow));

        Assert.NotNull(await policies.GetGlobalAsync());
    }

    private static JsonProjectOrchestrationStore CreateOrchestrationStore(TemporaryStore store) =>
        new(
            store.Paths,
            new JsonlEventStore<ExecutionRunRecord>(
                store.Paths,
                store.Files,
                NullLogger<JsonlEventStore<ExecutionRunRecord>>.Instance),
            new JsonlEventStore<EvidenceMetadataRecord>(
                store.Paths,
                store.Files,
                NullLogger<JsonlEventStore<EvidenceMetadataRecord>>.Instance),
            new JsonlEventStore<ReviewMetadataRecord>(
                store.Paths,
                store.Files,
                NullLogger<JsonlEventStore<ReviewMetadataRecord>>.Instance),
            new JsonlEventStore<ActivityAuditRecordFile>(
                store.Paths,
                store.Files,
                NullLogger<JsonlEventStore<ActivityAuditRecordFile>>.Instance),
            NullLogger<JsonProjectOrchestrationStore>.Instance);
}
