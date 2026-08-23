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
        Assert.Equal(projectPaths.OrchestrationDirectory, store.Paths.GetProjectOrchestrationDirectory(projectId));
        Assert.EndsWith(
            Path.Combine(projectId.ToString("D"), "orchestration"),
            projectPaths.OrchestrationDirectory,
            StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("routing-policy.json", projectPaths.RoutingPolicyFile, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("runs", projectPaths.RunsDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("evidence", projectPaths.EvidenceDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("reviews", projectPaths.ReviewsDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("activity", projectPaths.ActivityDirectory, StringComparison.OrdinalIgnoreCase);

        await store.Paths.EnsureProjectDirectoriesAsync(projectId);

        Assert.True(Directory.Exists(projectPaths.RootDirectory));
        Assert.True(Directory.Exists(projectPaths.OrchestrationDirectory));
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
    public async Task ProjectRegistry_UpsertSameIdReplacesOneLogicalRecord()
    {
        using var store = new TemporaryStore();
        var repository = new JsonProjectRepository(
            store.Paths,
            store.Files,
            NullLogger<JsonProjectRepository>.Instance);
        var projectId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 8, 20, 10, 0, 0, TimeSpan.Zero);
        var first = new Project(
            projectId,
            "Initial",
            "C:\\workspace",
            null,
            ProjectStatus.Active,
            createdAt,
            createdAt);
        var latest = new Project(
            projectId,
            "Latest",
            "C:\\workspace-2",
            null,
            ProjectStatus.Paused,
            createdAt,
            createdAt.AddHours(1));

        await repository.UpsertAsync(first);
        await repository.UpsertAsync(latest);

        var all = await repository.GetAllAsync();
        var loaded = await repository.GetByIdAsync(projectId);

        Assert.Single(all);
        Assert.NotNull(loaded);
        Assert.Equal("Latest", loaded!.Name);
        Assert.Equal("C:\\workspace-2", loaded.LocalPath);
        Assert.Equal(ProjectStatus.Paused, loaded.Status);
    }

    [Fact]
    public async Task ProjectRegistry_ArchivedAndNoRepositoryProjectRoundTripWithNullableDefaultBranch()
    {
        using var store = new TemporaryStore();
        var repository = new JsonProjectRepository(
            store.Paths,
            store.Files,
            NullLogger<JsonProjectRepository>.Instance);
        var now = DateTimeOffset.UtcNow;
        var project = new Project(
            Guid.NewGuid(),
            "Archived notes project",
            "C:\\notes",
            "   ",
            ProjectStatus.Archived,
            now,
            now);

        await repository.UpsertAsync(project);
        var loaded = await repository.GetByIdAsync(project.Id);

        Assert.NotNull(loaded);
        Assert.Equal(ProjectStatus.Archived, loaded!.Status);
        Assert.Null(loaded.DefaultBranch);
    }

    [Fact]
    public void RepositoryBackedProject_RequiresDefaultBranch()
    {
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() => new Project(
            Guid.NewGuid(),
            "Repository project",
            "C:\\workspace",
            null,
            ProjectStatus.Active,
            now,
            now,
            repositoryProvider: "GitHub"));

        var project = new Project(
            Guid.NewGuid(),
            "Repository project",
            "C:\\workspace",
            "main",
            ProjectStatus.Active,
            now,
            now,
            repositoryProvider: "GitHub");

        Assert.Equal("main", project.DefaultBranch);
    }

    [Fact]
    public async Task ProjectRegistry_ConcurrentDistinctUpsertsDoNotDropRecords()
    {
        using var store = new TemporaryStore();
        var repository = new JsonProjectRepository(
            store.Paths,
            store.Files,
            NullLogger<JsonProjectRepository>.Instance);
        var now = DateTimeOffset.UtcNow;
        var projects = Enumerable.Range(0, 12)
            .Select(index => new Project(
                Guid.NewGuid(),
                $"Project {index}",
                $"C:\\workspace-{index}",
                null,
                ProjectStatus.Active,
                now,
                now))
            .ToArray();

        await Task.WhenAll(projects.Select(project => repository.UpsertAsync(project)));

        var loaded = await repository.GetAllAsync();
        Assert.Equal(projects.Length, loaded.Count);
        Assert.Equal(
            projects.Select(project => project.Id).OrderBy(id => id),
            loaded.Select(project => project.Id).OrderBy(id => id));
    }

    [Fact]
    public async Task ProjectRegistry_InvalidPersistedStatusFailsClosedWhileValidSiblingRemainsReadable()
    {
        using var store = new TemporaryStore();
        var repository = new JsonProjectRepository(
            store.Paths,
            store.Files,
            NullLogger<JsonProjectRepository>.Instance);
        var now = DateTimeOffset.UtcNow;
        var valid = new Project(
            Guid.NewGuid(),
            "Valid",
            "C:\\valid",
            null,
            ProjectStatus.Active,
            now,
            now);
        var invalidId = Guid.NewGuid();
        var payload = new
        {
            schemaVersion = JsonFileStore.CurrentSchemaVersion,
            payload = new
            {
                items = new object[]
                {
                    ProjectRecordForJson(valid),
                    new
                    {
                        id = invalidId,
                        name = "Invalid status",
                        localPath = "C:\\invalid",
                        defaultBranch = (string?)null,
                        status = 999,
                        createdAt = now,
                        updatedAt = now
                    }
                }
            }
        };
        await File.WriteAllTextAsync(
            store.Paths.ProjectsFile,
            JsonSerializer.Serialize(payload, JsonFileStore.SerializerOptions));

        var loaded = await repository.GetAllAsync();

        Assert.Single(loaded);
        Assert.Equal(valid.Id, loaded[0].Id);
        Assert.Null(await repository.GetByIdAsync(invalidId));
    }

    [Fact]
    public async Task ProjectRegistry_CorruptAndUnsupportedDocumentsQuarantineAndFutureWriteWorks()
    {
        using var store = new TemporaryStore();
        var repository = new JsonProjectRepository(
            store.Paths,
            store.Files,
            NullLogger<JsonProjectRepository>.Instance);
        var now = DateTimeOffset.UtcNow;
        var project = new Project(
            Guid.NewGuid(),
            "Recoverable",
            "C:\\recoverable",
            null,
            ProjectStatus.Active,
            now,
            now);

        await File.WriteAllTextAsync(store.Paths.ProjectsFile, "{ not-json");
        Assert.Empty(await repository.GetAllAsync());
        Assert.False(File.Exists(store.Paths.ProjectsFile));
        Assert.NotEmpty(Directory.EnumerateFiles(store.Paths.RootDirectory, "projects.json.corrupt-*.bak"));

        await repository.UpsertAsync(project);
        Assert.NotNull(await repository.GetByIdAsync(project.Id));

        await File.WriteAllTextAsync(
            store.Paths.ProjectsFile,
            "{ \"schemaVersion\": 999, \"payload\": { \"items\": [] } }");
        Assert.Empty(await repository.GetAllAsync());
        Assert.False(File.Exists(store.Paths.ProjectsFile));
        Assert.NotEmpty(Directory.EnumerateFiles(store.Paths.RootDirectory, "projects.json.unsupported-schema-*.bak"));

        await repository.UpsertAsync(project);
        Assert.NotNull(await repository.GetByIdAsync(project.Id));
    }

    [Fact]
    public async Task AgentRegistry_InvalidEnumValuesFailClosedWhileValidSiblingRemainsReadable()
    {
        using var store = new TemporaryStore();
        var repository = new JsonAgentRepository(
            store.Paths,
            store.Files,
            NullLogger<JsonAgentRepository>.Instance);
        var now = DateTimeOffset.UtcNow;
        var valid = new AgentDefinition(
            Guid.NewGuid(),
            "Valid agent",
            "review",
            AgentConnectionMode.Api,
            AgentAvailability.Available,
            enabled: true,
            now,
            now);
        var invalidModeId = Guid.NewGuid();
        var invalidAvailabilityId = Guid.NewGuid();
        var payload = new
        {
            schemaVersion = JsonFileStore.CurrentSchemaVersion,
            payload = new
            {
                items = new object[]
                {
                    AgentRecordForJson(valid),
                    new
                    {
                        id = invalidModeId,
                        name = "Invalid mode",
                        role = "unknown",
                        connectionMode = 999,
                        availability = (int)AgentAvailability.Available,
                        enabled = true,
                        createdAt = now,
                        updatedAt = now
                    },
                    new
                    {
                        id = invalidAvailabilityId,
                        name = "Invalid availability",
                        role = "unknown",
                        connectionMode = (int)AgentConnectionMode.Manual,
                        availability = 999,
                        enabled = true,
                        createdAt = now,
                        updatedAt = now
                    }
                }
            }
        };
        await File.WriteAllTextAsync(
            store.Paths.AgentsFile,
            JsonSerializer.Serialize(payload, JsonFileStore.SerializerOptions));

        var loaded = await repository.GetAllAsync();

        Assert.Single(loaded);
        Assert.Equal(valid.Id, loaded[0].Id);
        Assert.Null(await repository.GetByIdAsync(invalidModeId));
        Assert.Null(await repository.GetByIdAsync(invalidAvailabilityId));
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
            summary: "focused storage tests passed",
            relatedRequirementReferences: ["FR-PROJ-003", "FR-REV-003"]);
        var secondEvidenceId = Guid.NewGuid();
        var reviewA = new ReviewMetadata(
            projectA,
            Guid.NewGuid(),
            september.AddMinutes(1),
            "opus-5",
            "changes-required",
            "medium",
            runId: runA2.RunId,
            evidenceReference: evidenceA.EvidenceId.ToString("D"),
            summary: "two bounded findings",
            findings:
            [
                new ReviewFindingMetadata(
                    "OPUS-01",
                    "high",
                    "FR-REV-003",
                    "Remediated",
                    blocking: true,
                    evidenceIds: [evidenceA.EvidenceId],
                    evidenceReferences: ["EV-PRIMARY"],
                    summary: "finding one"),
                new ReviewFindingMetadata(
                    "OPUS-02",
                    "medium",
                    "acceptance:R-02",
                    "Deferred",
                    blocking: false,
                    evidenceIds: [secondEvidenceId],
                    evidenceReferences: ["EV-SECONDARY"],
                    summary: "finding two")
            ]);
        var activityA = new ActivityAuditRecord(
            projectA,
            Guid.NewGuid(),
            september.AddMinutes(2),
            "apo",
            "review-recorded",
            "success",
            runA2.RunId,
            evidenceA.EvidenceId,
            "review metadata appended",
            taskReference: "APO-27",
            evidenceIds: [evidenceA.EvidenceId, secondEvidenceId]);

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

        Assert.Equal(HistoryReadStatus.Success, runsA.Status);
        Assert.Equal(HistoryReadStatus.Success, runsB.Status);
        Assert.Equal(HistoryReadStatus.Success, evidence.Status);
        Assert.Equal(HistoryReadStatus.Success, reviews.Status);
        Assert.Equal(HistoryReadStatus.Success, activity.Status);
        Assert.Equal([runA.RunId, runA2.RunId], runsA.Records.Select(value => value.RunId));
        Assert.Equal([runB.RunId], runsB.Records.Select(value => value.RunId));
        Assert.Equal([evidenceA.EvidenceId], evidence.Records.Select(value => value.EvidenceId));
        Assert.Equal([reviewA.ReviewId], reviews.Records.Select(value => value.ReviewId));
        Assert.Equal([activityA.ActivityId], activity.Records.Select(value => value.ActivityId));
        Assert.Equal(["FR-PROJ-003", "FR-REV-003"], evidence.Records[0].RelatedRequirementReferences);
        Assert.Equal(["OPUS-01", "OPUS-02"], reviews.Records[0].Findings.Select(finding => finding.FindingId));
        Assert.Equal(["FR-REV-003", "acceptance:R-02"], reviews.Records[0].Findings.Select(finding => finding.AffectedReference));
        Assert.Equal(["Remediated", "Deferred"], reviews.Records[0].Findings.Select(finding => finding.Disposition));
        Assert.Equal([true, false], reviews.Records[0].Findings.Select(finding => finding.Blocking));
        Assert.Equal([evidenceA.EvidenceId], reviews.Records[0].Findings[0].EvidenceIds);
        Assert.Equal(["EV-SECONDARY"], reviews.Records[0].Findings[1].EvidenceReferences);
        Assert.Equal("APO-27", activity.Records[0].TaskReference);
        Assert.Equal([evidenceA.EvidenceId, secondEvidenceId], activity.Records[0].EvidenceIds);

        var runAugust = store.Paths.GetMonthlyPartition(store.Paths.GetProjectRunsDirectory(projectA), august);
        var runSeptember = store.Paths.GetMonthlyPartition(store.Paths.GetProjectRunsDirectory(projectA), september);
        Assert.True(File.Exists(runAugust));
        Assert.True(File.Exists(runSeptember));
        Assert.All(
            Directory.EnumerateFiles(store.Paths.GetProjectDirectory(projectA), "*.jsonl", SearchOption.AllDirectories),
            path => Assert.DoesNotContain(projectB.ToString("D"), File.ReadAllText(path), StringComparison.Ordinal));
    }

    [Fact]
    public void ReviewMetadata_DerivesFindingCountAndBlockingFromDetailedFindings()
    {
        var projectId = Guid.NewGuid();
        var occurredAt = new DateTimeOffset(2026, 8, 23, 11, 0, 0, TimeSpan.Zero);
        var review = new ReviewMetadata(
            projectId,
            Guid.NewGuid(),
            occurredAt,
            "opus-5",
            "changes-required",
            "high",
            findings:
            [
                new ReviewFindingMetadata("OPUS-01", "high", "FR-REV-003", "Open", blocking: true),
                new ReviewFindingMetadata("OPUS-02", "medium", "FR-REV-004", "Deferred", blocking: false)
            ]);
        var emptyReview = new ReviewMetadata(
            projectId,
            Guid.NewGuid(),
            occurredAt,
            "opus-5",
            "accepted",
            "none",
            findings: []);

        Assert.Equal(2, review.FindingCount);
        Assert.True(review.Blocking);
        Assert.Equal(0, emptyReview.FindingCount);
        Assert.False(emptyReview.Blocking);
    }

    [Fact]
    public void ReviewMetadata_RejectsBlankNullAndCaseInsensitiveDuplicateFindings()
    {
        var projectId = Guid.NewGuid();
        var occurredAt = new DateTimeOffset(2026, 8, 23, 11, 0, 0, TimeSpan.Zero);
        var finding = new ReviewFindingMetadata("OPUS-01", "high", "FR-REV-003", "Open", blocking: true);

        Assert.Throws<ArgumentException>(() => new ReviewFindingMetadata(
            " ",
            "high",
            "FR-REV-003",
            "Open",
            blocking: true));
        Assert.ThrowsAny<ArgumentException>(() => new ReviewMetadata(
            projectId,
            Guid.NewGuid(),
            occurredAt,
            "opus-5",
            "changes-required",
            "high",
            findings: [finding, new ReviewFindingMetadata("opus-01", "medium", "FR-REV-004", "Open", blocking: false)]));
        Assert.ThrowsAny<ArgumentException>(() => new ReviewMetadata(
            projectId,
            Guid.NewGuid(),
            occurredAt,
            "opus-5",
            "changes-required",
            "high",
            findings: new ReviewFindingMetadata[] { null! }));
    }

    [Fact]
    public void ReviewMetadataRecord_DerivedAggregateFieldsRoundTripWithoutContradiction()
    {
        var review = new ReviewMetadata(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 8, 23, 11, 0, 0, TimeSpan.Zero),
            "opus-5",
            "changes-required",
            "high",
            findings:
            [
                new ReviewFindingMetadata("OPUS-01", "high", "FR-REV-003", "Open", blocking: true),
                new ReviewFindingMetadata("OPUS-02", "medium", "FR-REV-004", "Deferred", blocking: false)
            ]);
        var record = ReviewMetadataRecord.FromApplication(review);
        var json = JsonSerializer.Serialize(record, JsonFileStore.JsonlSerializerOptions);
        using var document = JsonDocument.Parse(json);

        Assert.Equal(review.Findings.Count, record.FindingCount);
        Assert.Equal(review.Blocking, record.Blocking);
        Assert.Equal(2, document.RootElement.GetProperty("findingCount").GetInt32());
        Assert.True(document.RootElement.GetProperty("blocking").GetBoolean());

        var roundTrip = record.ToApplication();
        Assert.Equal(2, roundTrip.FindingCount);
        Assert.True(roundTrip.Blocking);
        Assert.Equal(review.Findings.Select(finding => finding.FindingId), roundTrip.Findings.Select(finding => finding.FindingId));
    }

    [Fact]
    public void ReviewMetadataRecord_RejectsContradictoryPersistedAggregateFields()
    {
        var review = new ReviewMetadata(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTimeOffset(2026, 8, 23, 11, 0, 0, TimeSpan.Zero),
            "opus-5",
            "accepted",
            "none",
            findings:
            [new ReviewFindingMetadata("OPUS-01", "none", "FR-REV-003", "Accepted", blocking: false)]);

        var countMismatch = ReviewMetadataRecord.FromApplication(review);
        countMismatch.FindingCount = 2;
        Assert.Throws<ArgumentException>(() => countMismatch.ToApplication());

        var blockingMismatch = ReviewMetadataRecord.FromApplication(review);
        blockingMismatch.Blocking = true;
        Assert.Throws<ArgumentException>(() => blockingMismatch.ToApplication());
    }

    [Fact]
    public async Task HistoryRead_NoProjectHistoryDirectoryIsSuccessWithEmptyRecords()
    {
        using var store = new TemporaryStore();
        var orchestration = CreateOrchestrationStore(store);
        var from = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

        var result = await orchestration.ReadReviewsAsync(Guid.NewGuid(), from, from.AddDays(1));

        Assert.Equal(HistoryReadStatus.Success, result.Status);
        Assert.Empty(result.Records);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task HistoryRead_ExistingEmptyPartitionIsSuccessWithEmptyRecords()
    {
        using var store = new TemporaryStore();
        var projectId = Guid.NewGuid();
        var capturedAt = new DateTimeOffset(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);
        var directory = store.Paths.GetProjectRunsDirectory(projectId);
        var path = store.Paths.GetMonthlyPartition(directory, capturedAt);
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(path, string.Empty);

        var result = await CreateOrchestrationStore(store).ReadExecutionRunsAsync(
            projectId,
            capturedAt.AddMinutes(-1),
            capturedAt.AddMinutes(1));

        Assert.Equal(HistoryReadStatus.Success, result.Status);
        Assert.Empty(result.Records);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task HistoryRead_MalformedAndUnsupportedRecordsPreserveValidSiblingAndReportPartial()
    {
        using var store = new TemporaryStore();
        var projectId = Guid.NewGuid();
        var capturedAt = new DateTimeOffset(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);
        var valid = new ExecutionRun(projectId, Guid.NewGuid(), ExecutionRunStatus.Completed, capturedAt);
        var directory = store.Paths.GetProjectRunsDirectory(projectId);
        var path = store.Paths.GetMonthlyPartition(directory, capturedAt);
        Directory.CreateDirectory(directory);
        var validLine = JsonSerializer.Serialize(
            ExecutionRunRecord.FromApplication(valid),
            JsonFileStore.JsonlSerializerOptions);
        await File.WriteAllTextAsync(
            path,
            "{ not-json\n" +
            "{\"schemaVersion\":999,\"recordType\":\"execution-run\"}\n" +
            validLine + Environment.NewLine);

        var result = await CreateOrchestrationStore(store).ReadExecutionRunsAsync(
            projectId,
            capturedAt.AddMinutes(-1),
            capturedAt.AddMinutes(1));

        Assert.Equal(HistoryReadStatus.Partial, result.Status);
        Assert.Single(result.Records);
        Assert.Equal(valid.RunId, result.Records[0].RunId);
        Assert.Contains(result.Issues, issue => issue.Kind == HistoryReadIssueKind.CorruptRecord);
        Assert.Contains(result.Issues, issue => issue.Kind == HistoryReadIssueKind.UnsupportedSchema);
        Assert.All(result.Issues, issue =>
        {
            Assert.Equal("2026-08.jsonl", issue.Partition);
            Assert.DoesNotContain("not-json", issue.NonSecretMessage, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("{", issue.NonSecretMessage, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task HistoryRead_OneValidMonthAndOneIoFailurePreservesRecordsAsPartial()
    {
        using var store = new TemporaryStore();
        var projectId = Guid.NewGuid();
        var august = new DateTimeOffset(2026, 8, 31, 23, 0, 0, TimeSpan.Zero);
        var september = new DateTimeOffset(2026, 9, 1, 1, 0, 0, TimeSpan.Zero);
        var valid = new ExecutionRun(projectId, Guid.NewGuid(), ExecutionRunStatus.Completed, august);
        var writer = CreateOrchestrationStore(store);
        await writer.AppendExecutionRunAsync(valid);

        var failedPath = store.Paths.GetMonthlyPartition(
            store.Paths.GetProjectRunsDirectory(projectId),
            september);
        var reader = new FaultingJsonlPartitionReader(failedPath, static () => new IOException("synthetic I/O failure"));
        var result = await CreateOrchestrationStore(store, reader).ReadExecutionRunsAsync(
            projectId,
            august.AddMinutes(-1),
            september.AddMinutes(1));

        Assert.Equal(HistoryReadStatus.Partial, result.Status);
        Assert.Single(result.Records);
        Assert.Equal(valid.RecordId, result.Records[0].RecordId);
        Assert.Contains(result.Issues, issue => issue.Kind == HistoryReadIssueKind.IoFailure);
        Assert.DoesNotContain(result.Issues, issue => issue.NonSecretMessage.Contains("synthetic", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task HistoryRead_PermissionFailureWithNoReadablePartitionIsUnavailable()
    {
        using var store = new TemporaryStore();
        var projectId = Guid.NewGuid();
        var capturedAt = new DateTimeOffset(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);
        var directory = store.Paths.GetProjectRunsDirectory(projectId);
        var path = store.Paths.GetMonthlyPartition(directory, capturedAt);
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(path, "placeholder");
        var reader = new FaultingJsonlPartitionReader(path, static () => new UnauthorizedAccessException("synthetic permission failure"));

        var result = await CreateOrchestrationStore(store, reader).ReadExecutionRunsAsync(
            projectId,
            capturedAt.AddMinutes(-1),
            capturedAt.AddMinutes(1));

        Assert.Equal(HistoryReadStatus.Unavailable, result.Status);
        Assert.Empty(result.Records);
        Assert.Contains(result.Issues, issue => issue.Kind == HistoryReadIssueKind.PermissionFailure);
        Assert.DoesNotContain(result.Issues, issue => issue.NonSecretMessage.Contains("synthetic", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task HistoryRead_PartialFailureStillEnforcesProjectIsolation()
    {
        using var store = new TemporaryStore();
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();
        var august = new DateTimeOffset(2026, 8, 31, 23, 0, 0, TimeSpan.Zero);
        var september = new DateTimeOffset(2026, 9, 1, 1, 0, 0, TimeSpan.Zero);
        var recordA = new ExecutionRun(projectA, Guid.NewGuid(), ExecutionRunStatus.Completed, august);
        var foreign = new ExecutionRun(projectB, Guid.NewGuid(), ExecutionRunStatus.Completed, august.AddMinutes(1));
        var directory = store.Paths.GetProjectRunsDirectory(projectA);
        var augustPath = store.Paths.GetMonthlyPartition(directory, august);
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(
            augustPath,
            JsonSerializer.Serialize(ExecutionRunRecord.FromApplication(recordA), JsonFileStore.JsonlSerializerOptions) + "\n" +
            JsonSerializer.Serialize(ExecutionRunRecord.FromApplication(foreign), JsonFileStore.JsonlSerializerOptions) + "\n");
        var failedPath = store.Paths.GetMonthlyPartition(directory, september);
        var reader = new FaultingJsonlPartitionReader(failedPath, static () => new IOException("synthetic I/O failure"));

        var result = await CreateOrchestrationStore(store, reader).ReadExecutionRunsAsync(
            projectA,
            august.AddMinutes(-1),
            september.AddMinutes(1));

        Assert.Equal(HistoryReadStatus.Partial, result.Status);
        Assert.Single(result.Records);
        Assert.Equal(recordA.RecordId, result.Records[0].RecordId);
        Assert.DoesNotContain(result.Records, value => value.ProjectId == projectB);
        Assert.Contains(result.Issues, issue => issue.Kind == HistoryReadIssueKind.IoFailure);
    }

    [Fact]
    public async Task ExecutionRun_SameLifecycleUsesIndependentRecordIdsAndRecordedAtPartitions()
    {
        using var store = new TemporaryStore();
        var orchestration = CreateOrchestrationStore(store);
        var projectId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var startedAt = new DateTimeOffset(2026, 8, 31, 23, 0, 0, TimeSpan.Zero);
        var startedRecordId = Guid.NewGuid();
        var reviewRecordId = Guid.NewGuid();
        var started = new ExecutionRun(
            projectId,
            runId,
            ExecutionRunStatus.Running,
            startedAt,
            recordId: startedRecordId,
            recordedAt: new DateTimeOffset(2026, 8, 31, 23, 1, 0, TimeSpan.Zero));
        var reviewed = new ExecutionRun(
            projectId,
            runId,
            ExecutionRunStatus.Review,
            startedAt,
            recordId: reviewRecordId,
            recordedAt: new DateTimeOffset(2026, 9, 1, 0, 5, 0, TimeSpan.Zero));

        await orchestration.AppendExecutionRunAsync(started);
        await orchestration.AppendExecutionRunAsync(reviewed);

        var augustPath = store.Paths.GetMonthlyPartition(
            store.Paths.GetProjectRunsDirectory(projectId),
            started.RecordedAt);
        var septemberPath = store.Paths.GetMonthlyPartition(
            store.Paths.GetProjectRunsDirectory(projectId),
            reviewed.RecordedAt);
        var septemberRead = await orchestration.ReadExecutionRunsAsync(
            projectId,
            reviewed.RecordedAt.AddMinutes(-1),
            reviewed.RecordedAt.AddMinutes(1));
        var all = await orchestration.ReadExecutionRunsAsync(
            projectId,
            started.RecordedAt.AddMinutes(-1),
            reviewed.RecordedAt.AddMinutes(1));

        Assert.True(File.Exists(augustPath));
        Assert.True(File.Exists(septemberPath));
        Assert.Equal(HistoryReadStatus.Success, septemberRead.Status);
        Assert.Single(septemberRead.Records);
        Assert.Equal(reviewRecordId, septemberRead.Records[0].RecordId);
        Assert.Equal([startedRecordId, reviewRecordId], all.Records.Select(value => value.RecordId));
        Assert.Equal(runId, all.Records[0].RunId);
        Assert.Equal(runId, all.Records[1].RunId);
        Assert.NotEqual(all.Records[0].RecordId, all.Records[1].RecordId);
        Assert.Equal(ExecutionRunStatus.Review, all.Records[1].Status);
    }

    [Fact]
    public void ExecutionRun_RejectsEmptyRecordIdAndRecordedTimeBeforeStart()
    {
        var projectId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var startedAt = new DateTimeOffset(2026, 8, 31, 23, 0, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() => new ExecutionRun(
            projectId,
            runId,
            ExecutionRunStatus.Running,
            startedAt,
            recordId: Guid.Empty));
        Assert.Throws<ArgumentException>(() => new ExecutionRun(
            projectId,
            runId,
            ExecutionRunStatus.Running,
            startedAt,
            recordedAt: startedAt.AddSeconds(-1)));
    }

    [Fact]
    public async Task ExecutionRun_InvalidPersistedStatusIsSkippedWithoutHidingValidCheckpoint()
    {
        using var store = new TemporaryStore();
        var orchestration = CreateOrchestrationStore(store);
        var projectId = Guid.NewGuid();
        var recordedAt = new DateTimeOffset(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);
        var valid = new ExecutionRun(
            projectId,
            Guid.NewGuid(),
            ExecutionRunStatus.Completed,
            recordedAt,
            recordId: Guid.NewGuid(),
            recordedAt: recordedAt);
        var path = store.Paths.GetMonthlyPartition(
            store.Paths.GetProjectRunsDirectory(projectId),
            recordedAt);
        Directory.CreateDirectory(store.Paths.GetProjectRunsDirectory(projectId));
        var invalid = new
        {
            schemaVersion = JsonFileStore.CurrentSchemaVersion,
            recordType = "execution-run",
            projectId,
            runId = Guid.NewGuid(),
            recordId = Guid.NewGuid(),
            recordedAt,
            status = 999,
            startedAt = recordedAt
        };
        await File.WriteAllTextAsync(
            path,
            JsonSerializer.Serialize(invalid, JsonFileStore.JsonlSerializerOptions) + Environment.NewLine +
            JsonSerializer.Serialize(ExecutionRunRecord.FromApplication(valid), JsonFileStore.JsonlSerializerOptions) + Environment.NewLine);

        var loaded = await orchestration.ReadExecutionRunsAsync(
            projectId,
            recordedAt.AddMinutes(-1),
            recordedAt.AddMinutes(1));

        Assert.Equal(HistoryReadStatus.Partial, loaded.Status);
        Assert.Single(loaded.Records);
        Assert.Equal(valid.RecordId, loaded.Records[0].RecordId);
        Assert.Equal(valid.Status, loaded.Records[0].Status);
        Assert.Contains(loaded.Issues, issue => issue.Kind == HistoryReadIssueKind.CorruptRecord);
    }

    [Fact]
    public async Task Reviews_RemainProjectIsolatedWithFindingLevelTraceability()
    {
        using var store = new TemporaryStore();
        var orchestration = CreateOrchestrationStore(store);
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();
        var occurredAt = new DateTimeOffset(2026, 8, 23, 13, 0, 0, TimeSpan.Zero);
        var evidenceA = Guid.NewGuid();
        var reviewA = new ReviewMetadata(
            projectA,
            Guid.NewGuid(),
            occurredAt,
            "opus-5",
            "changes-required",
            "high",
            findings:
            [
                new ReviewFindingMetadata(
                    "OPUS-01",
                    "high",
                    "FR-REV-003",
                    "Open",
                    blocking: true,
                    evidenceIds: [evidenceA]),
                new ReviewFindingMetadata(
                    "SOL-02",
                    "medium",
                    "acceptance:R-07",
                    "Accepted",
                    blocking: false,
                    evidenceReferences: ["validation:focused-tests"])
            ]);
        var reviewB = new ReviewMetadata(
            projectB,
            Guid.NewGuid(),
            occurredAt.AddMinutes(1),
            "opus-5",
            "accepted",
            "none",
            findings: []);

        await orchestration.AppendReviewAsync(reviewA);
        await orchestration.AppendReviewAsync(reviewB);

        var loadedA = await orchestration.ReadReviewsAsync(projectA, occurredAt.AddMinutes(-1), occurredAt.AddMinutes(2));
        var loadedB = await orchestration.ReadReviewsAsync(projectB, occurredAt.AddMinutes(-1), occurredAt.AddMinutes(2));

        Assert.Equal(HistoryReadStatus.Success, loadedA.Status);
        Assert.Equal(HistoryReadStatus.Success, loadedB.Status);
        Assert.Single(loadedA.Records);
        Assert.Single(loadedB.Records);
        Assert.Equal(reviewA.ReviewId, loadedA.Records[0].ReviewId);
        Assert.Equal(reviewB.ReviewId, loadedB.Records[0].ReviewId);
        Assert.Equal(["OPUS-01", "SOL-02"], loadedA.Records[0].Findings.Select(finding => finding.FindingId));
        Assert.Equal(["FR-REV-003", "acceptance:R-07"], loadedA.Records[0].Findings.Select(finding => finding.AffectedReference));
        Assert.Equal(["Open", "Accepted"], loadedA.Records[0].Findings.Select(finding => finding.Disposition));
        Assert.Equal([true, false], loadedA.Records[0].Findings.Select(finding => finding.Blocking));
        Assert.Equal([evidenceA], loadedA.Records[0].Findings[0].EvidenceIds);
        Assert.Equal(["validation:focused-tests"], loadedA.Records[0].Findings[1].EvidenceReferences);
    }

    [Fact]
    public void EvidenceMetadata_RelatedRequirementReferencesAreBoundedAndNonSecret()
    {
        var evidence = new EvidenceMetadata(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "test",
            "passed",
            relatedRequirementReferences: ["FR-PROJ-003", "FR-REV-003", "FR-PROJ-003"]);

        var record = EvidenceMetadataRecord.FromApplication(evidence);
        var json = JsonSerializer.Serialize(record, JsonFileStore.JsonlSerializerOptions);

        Assert.Equal(["FR-PROJ-003", "FR-REV-003"], evidence.RelatedRequirementReferences);
        Assert.Equal(["FR-PROJ-003", "FR-REV-003"], record.RelatedRequirementReferences);
        Assert.DoesNotContain("raw-output-fixture", json, StringComparison.Ordinal);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Activity_RoundTripsTaskAndMultipleEvidenceIdsAndPreservesChronologicalRange()
    {
        using var store = new TemporaryStore();
        var orchestration = CreateOrchestrationStore(store);
        var projectId = Guid.NewGuid();
        var earlyAt = new DateTimeOffset(2026, 8, 23, 14, 0, 0, TimeSpan.Zero);
        var lateAt = earlyAt.AddMinutes(2);
        var firstEvidence = Guid.NewGuid();
        var secondEvidence = Guid.NewGuid();
        var early = new ActivityAuditRecord(
            projectId,
            Guid.NewGuid(),
            earlyAt,
            "apo",
            "validation-started",
            "success",
            taskReference: "APO-27",
            evidenceIds: [firstEvidence, secondEvidence]);
        var late = new ActivityAuditRecord(
            projectId,
            Guid.NewGuid(),
            lateAt,
            "apo",
            "validation-finished",
            "success",
            taskReference: "APO-27",
            evidenceIds: [secondEvidence]);

        await orchestration.AppendActivityAsync(late);
        await orchestration.AppendActivityAsync(early);

        var loaded = await orchestration.ReadActivityAsync(
            projectId,
            earlyAt.AddMinutes(-1),
            lateAt.AddMinutes(1));

        Assert.Equal(HistoryReadStatus.Success, loaded.Status);
        Assert.Equal([early.ActivityId, late.ActivityId], loaded.Records.Select(value => value.ActivityId));
        Assert.Equal("APO-27", loaded.Records[0].TaskReference);
        Assert.Equal([firstEvidence, secondEvidence], loaded.Records[0].EvidenceIds);
        Assert.Equal([secondEvidence], loaded.Records[1].EvidenceIds);
        Assert.NotSame(early.EvidenceIds, loaded.Records[0].EvidenceIds);
    }

    [Fact]
    public async Task Activity_MalformedAndUnsupportedRecordsDoNotHideValidRecords()
    {
        using var store = new TemporaryStore();
        var orchestration = CreateOrchestrationStore(store);
        var projectId = Guid.NewGuid();
        var occurredAt = new DateTimeOffset(2026, 8, 23, 15, 0, 0, TimeSpan.Zero);
        var valid = new ActivityAuditRecord(
            projectId,
            Guid.NewGuid(),
            occurredAt,
            "apo",
            "recorded",
            "success",
            taskReference: "APO-27");
        var path = store.Paths.GetMonthlyPartition(
            store.Paths.GetProjectActivityDirectory(projectId),
            occurredAt);
        Directory.CreateDirectory(store.Paths.GetProjectActivityDirectory(projectId));
        var validLine = JsonSerializer.Serialize(
            ActivityAuditRecordFile.FromApplication(valid),
            JsonFileStore.JsonlSerializerOptions);
        await File.WriteAllTextAsync(
            path,
            "{ definitely-not-json\n" +
            "{\"schemaVersion\":999,\"recordType\":\"activity-audit\"}\n" +
            validLine + Environment.NewLine);

        var loaded = await orchestration.ReadActivityAsync(
            projectId,
            occurredAt.AddMinutes(-1),
            occurredAt.AddMinutes(1));

        Assert.Equal(HistoryReadStatus.Partial, loaded.Status);
        Assert.Single(loaded.Records);
        Assert.Equal(valid.ActivityId, loaded.Records[0].ActivityId);
        Assert.Equal(2, loaded.Issues.Count);
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

        Assert.Equal(HistoryReadStatus.Partial, loaded.Status);
        Assert.Equal([first.RunId, second.RunId], loaded.Records.Select(value => value.RunId));
        Assert.Contains("schemaVersion", persisted, StringComparison.Ordinal);
        Assert.Contains("{\"schemaVersion\":1\n", persisted, StringComparison.Ordinal);
        Assert.Contains(JsonSerializer.Serialize(ExecutionRunRecord.FromApplication(second), JsonFileStore.JsonlSerializerOptions), persisted, StringComparison.Ordinal);
        Assert.DoesNotContain(foreign.RunId.ToString("D"), string.Join(Environment.NewLine, loaded.Records.Select(value => value.RunId)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProjectOrchestrationStreams_ConcurrentAppendsRemainComplete()
    {
        using var store = new TemporaryStore();
        var orchestration = CreateOrchestrationStore(store);
        var projectId = Guid.NewGuid();
        var capturedAt = new DateTimeOffset(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);
        var runId = Guid.NewGuid();
        var runs = Enumerable.Range(0, 24)
            .Select(index => new ExecutionRun(
                projectId,
                runId,
                ExecutionRunStatus.Completed,
                capturedAt,
                recordId: Guid.NewGuid(),
                recordedAt: capturedAt.AddSeconds(index)))
            .ToArray();

        await Task.WhenAll(runs.Select(run => orchestration.AppendExecutionRunAsync(run)));

        var loaded = await orchestration.ReadExecutionRunsAsync(projectId, capturedAt.AddMinutes(-1), capturedAt.AddMinutes(1));
        Assert.Equal(HistoryReadStatus.Success, loaded.Status);
        Assert.Equal(runs.Length, loaded.Records.Count);
        Assert.Equal(
            runs.Select(value => value.RecordId).OrderBy(value => value),
            loaded.Records.Select(value => value.RecordId).OrderBy(value => value));
        Assert.Single(loaded.Records.Select(value => value.RunId).Distinct());
        Assert.Equal(runs.Length, loaded.Records.Select(value => value.RecordId).Distinct().Count());

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

    private static object ProjectRecordForJson(Project project) => new
    {
        id = project.Id,
        name = project.Name,
        localPath = project.LocalPath,
        defaultBranch = project.DefaultBranch,
        status = (int)project.Status,
        createdAt = project.CreatedAt,
        updatedAt = project.UpdatedAt
    };

    private static object AgentRecordForJson(AgentDefinition agent) => new
    {
        id = agent.Id,
        name = agent.Name,
        role = agent.Role,
        provider = agent.Provider,
        connectionMode = (int)agent.ConnectionMode,
        availability = (int)agent.Availability,
        enabled = agent.Enabled,
        capabilities = agent.Capabilities,
        limitations = agent.Limitations,
        costAndQuotaMetadata = agent.CostAndQuotaMetadata,
        createdAt = agent.CreatedAt,
        updatedAt = agent.UpdatedAt
    };

    private static JsonProjectOrchestrationStore CreateOrchestrationStore(
        TemporaryStore store,
        IJsonlPartitionReader? partitionReader = null) =>
        new(
            store.Paths,
            CreateJsonlStore<ExecutionRunRecord>(store, partitionReader),
            CreateJsonlStore<EvidenceMetadataRecord>(store, partitionReader),
            CreateJsonlStore<ReviewMetadataRecord>(store, partitionReader),
            CreateJsonlStore<ActivityAuditRecordFile>(store, partitionReader),
            NullLogger<JsonProjectOrchestrationStore>.Instance);

    private static JsonlEventStore<TRecord> CreateJsonlStore<TRecord>(
        TemporaryStore store,
        IJsonlPartitionReader? partitionReader)
        where TRecord : class =>
        partitionReader is null
            ? new JsonlEventStore<TRecord>(
                store.Paths,
                store.Files,
                NullLogger<JsonlEventStore<TRecord>>.Instance)
            : new JsonlEventStore<TRecord>(
                store.Paths,
                store.Files,
                NullLogger<JsonlEventStore<TRecord>>.Instance,
                partitionReader);

    private sealed class FaultingJsonlPartitionReader : IJsonlPartitionReader
    {
        private readonly string _failingPath;
        private readonly Func<Exception> _failureFactory;

        public FaultingJsonlPartitionReader(string failingPath, Func<Exception> failureFactory)
        {
            _failingPath = failingPath;
            _failureFactory = failureFactory;
        }

        public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.Equals(path, _failingPath, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromException<Stream>(_failureFactory());
            }

            Stream stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 16 * 1024,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);
            return Task.FromResult(stream);
        }
    }
}
