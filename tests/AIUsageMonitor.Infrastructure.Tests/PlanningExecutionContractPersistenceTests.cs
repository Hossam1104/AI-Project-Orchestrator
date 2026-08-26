using System.Text.Json;
using System.Text.Json.Nodes;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Infrastructure.Persistence;
using AIUsageMonitor.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageMonitor.Infrastructure.Tests;

public sealed class PlanningExecutionContractPersistenceTests
{
    [Fact]
    public async Task ContractRoundTripsIntoProjectAndContractGuidScopedImmutablePath()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var contract = ContractFixture.Create();

        var write = await repository.CreateAsync(contract);
        var read = await repository.GetAsync(contract.ProjectId, contract.ContractId, contract.Revision);
        var latest = await repository.GetLatestAsync(contract.ProjectId, contract.ContractId);
        var revisions = await repository.ListRevisionsAsync(contract.ProjectId, contract.ContractId);

        Assert.Equal(PlanningContractRepositoryWriteStatus.Created, write.Status);
        Assert.Equal(PlanningContractReadState.Valid, read.State);
        Assert.Equal(contract.ContentHash, read.Contract!.ContentHash);
        Assert.Equal(contract.Reference.ToString(), read.Contract.Reference.ToString());
        Assert.Equal(PlanningContractReadState.Valid, latest.State);
        Assert.Equal(contract.Revision, latest.Contract!.Revision);
        Assert.Equal(PlanningContractReadState.Valid, revisions.State);
        Assert.Single(revisions.Revisions);
        Assert.True(File.Exists(store.Paths.GetPlanningExecutionContractRevisionFile(
            contract.ProjectId,
            contract.ContractId,
            contract.Revision)));
        Assert.Contains(
            $"{contract.ProjectId:D}{Path.DirectorySeparatorChar}contracts{Path.DirectorySeparatorChar}{contract.ContractId:D}",
            store.Paths.GetPlanningExecutionContractRevisionFile(contract.ProjectId, contract.ContractId, 1),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExistingRevisionCannotBeOverwrittenAndBytesRemainUnchanged()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var contract = ContractFixture.Create();
        var path = store.Paths.GetPlanningExecutionContractRevisionFile(
            contract.ProjectId,
            contract.ContractId,
            contract.Revision);

        Assert.Equal(
            PlanningContractRepositoryWriteStatus.Created,
            (await repository.CreateAsync(contract)).Status);
        var originalBytes = await File.ReadAllBytesAsync(path);

        var conflict = await repository.CreateAsync(contract);
        var afterBytes = await File.ReadAllBytesAsync(path);

        Assert.Equal(PlanningContractRepositoryWriteStatus.RevisionConflict, conflict.Status);
        Assert.Equal(originalBytes, afterBytes);
    }

    [Fact]
    public async Task ConcurrentSameRevisionCreationHasOneWinnerAndOneConflict()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var contract = ContractFixture.Create();

        var results = await Task.WhenAll(
            repository.CreateAsync(contract),
            repository.CreateAsync(contract));

        Assert.Equal(1, results.Count(result => result.Status == PlanningContractRepositoryWriteStatus.Created));
        Assert.Equal(1, results.Count(result => result.Status == PlanningContractRepositoryWriteStatus.RevisionConflict));
    }

    [Fact]
    public async Task MultipleRevisionsListChronologicallyWithoutOverwritingHistory()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var first = ContractFixture.Create();
        var second = ContractFixture.Create(
            projectId: first.ProjectId,
            contractId: first.ContractId,
            revision: 2,
            previousRevision: 1,
            previousContentHash: first.ContentHash);

        await repository.CreateAsync(first);
        await repository.CreateAsync(second);
        var revisions = await repository.ListRevisionsAsync(first.ProjectId, first.ContractId);

        Assert.Equal(PlanningContractReadState.Valid, revisions.State);
        Assert.Equal([1, 2], revisions.Revisions.Select(value => value.Revision));
        Assert.Equal(first.ContentHash, (await repository.GetAsync(first.ProjectId, first.ContractId, 1)).Contract!.ContentHash);
    }

    [Fact]
    public async Task MissingAndInvalidIdentifiersAreExplicit()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var projectId = Guid.NewGuid();
        var contractId = Guid.NewGuid();

        var missing = await repository.GetAsync(projectId, contractId, 1);
        var latest = await repository.GetLatestAsync(projectId, contractId);

        Assert.Equal(PlanningContractReadState.Missing, missing.State);
        Assert.Equal(PlanningContractReadState.Missing, latest.State);
        await Assert.ThrowsAsync<ArgumentException>(() => repository.GetAsync(Guid.Empty, contractId, 1));
        await Assert.ThrowsAsync<ArgumentException>(() => repository.GetAsync(projectId, Guid.Empty, 1));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.GetAsync(projectId, contractId, 0));
    }

    [Fact]
    public async Task MismatchedEmbeddedIdentityIsRejected()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var requestedProject = Guid.NewGuid();
        var requestedContract = Guid.NewGuid();
        var embedded = ContractFixture.Create(
            projectId: Guid.NewGuid(),
            contractId: Guid.NewGuid());
        var record = PlanningExecutionContractRecord.FromApplication(embedded);
        var path = store.Paths.GetPlanningExecutionContractRevisionFile(requestedProject, requestedContract, 1);
        await store.Paths.EnsureProjectDirectoriesAsync(requestedProject);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await store.Files.WriteAsync(path, record);

        var result = await repository.GetAsync(requestedProject, requestedContract, 1);

        Assert.Equal(PlanningContractReadState.Invalid, result.State);
        Assert.Null(result.Contract);
    }

    [Fact]
    public async Task TamperedPayloadAndStoredHashFailClosedAsIntegrityFailure()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var contract = ContractFixture.Create();
        await repository.CreateAsync(contract);
        var path = store.Paths.GetPlanningExecutionContractRevisionFile(
            contract.ProjectId,
            contract.ContractId,
            contract.Revision);

        var tamperedPayload = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
        tamperedPayload["payload"]!["ownerReference"] = "changed-owner";
        await File.WriteAllTextAsync(path, tamperedPayload.ToJsonString(JsonFileStore.SerializerOptions));
        var payloadResult = await repository.GetAsync(contract.ProjectId, contract.ContractId, 1);
        Assert.Equal(PlanningContractReadState.IntegrityFailure, payloadResult.State);

        var hashRecord = PlanningExecutionContractRecord.FromApplication(contract);
        hashRecord.ContentHash = new string('a', 64);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(
            new
            {
                schemaVersion = JsonFileStore.CurrentSchemaVersion,
                payload = hashRecord
            },
            JsonFileStore.SerializerOptions));
        var hashResult = await repository.GetAsync(contract.ProjectId, contract.ContractId, 1);
        Assert.Equal(PlanningContractReadState.IntegrityFailure, hashResult.State);
    }

    [Fact]
    public async Task FutureAndOlderContractSchemasDoNotSilentlyDeserialize()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var future = ContractFixture.Create();
        var futureRecord = PlanningExecutionContractRecord.FromApplication(future);
        futureRecord.SchemaVersion = PlanningExecutionContractSchema.CurrentVersion + 1;
        await WriteRecordAsync(store, future.ProjectId, future.ContractId, futureRecord);

        var futureResult = await repository.GetAsync(future.ProjectId, future.ContractId, 1);
        Assert.Equal(PlanningContractReadState.UnsupportedFutureVersion, futureResult.State);

        var older = ContractFixture.Create(contractId: Guid.NewGuid());
        var olderRecord = PlanningExecutionContractRecord.FromApplication(older);
        olderRecord.SchemaVersion = PlanningExecutionContractSchema.CurrentVersion - 1;
        await WriteRecordAsync(store, older.ProjectId, older.ContractId, olderRecord);

        var olderResult = await repository.GetAsync(older.ProjectId, older.ContractId, 1);
        Assert.Equal(PlanningContractReadState.MigrationRequired, olderResult.State);
    }

    [Fact]
    public async Task CorruptJsonIsInvalidAndNeverReturnedAsValid()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var projectId = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var path = store.Paths.GetPlanningExecutionContractRevisionFile(projectId, contractId, 1);
        await store.Paths.EnsureProjectDirectoriesAsync(projectId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, "{ invalid json");

        var result = await repository.GetAsync(projectId, contractId, 1);

        Assert.Equal(PlanningContractReadState.Invalid, result.State);
        Assert.Null(result.Contract);
    }

    [Fact]
    public async Task DifferentProjectsCannotReadEachOthersContracts()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var projectA = ContractFixture.Create(projectId: Guid.NewGuid());
        var projectB = ContractFixture.Create(
            projectId: Guid.NewGuid(),
            contractId: projectA.ContractId);
        await repository.CreateAsync(projectA);

        var result = await repository.GetAsync(projectB.ProjectId, projectB.ContractId, 1);

        Assert.Equal(PlanningContractReadState.Missing, result.State);
        Assert.NotEqual(
            store.Paths.GetPlanningExecutionContractRevisionFile(projectA.ProjectId, projectA.ContractId, 1),
            store.Paths.GetPlanningExecutionContractRevisionFile(projectB.ProjectId, projectB.ContractId, 1));
    }

    private static async Task WriteRecordAsync(
        TemporaryStore store,
        Guid projectId,
        Guid contractId,
        PlanningExecutionContractRecord record)
    {
        var path = store.Paths.GetPlanningExecutionContractRevisionFile(projectId, contractId, record.Revision);
        await store.Paths.EnsureProjectDirectoriesAsync(projectId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await store.Files.WriteAsync(path, record);
    }

    private static JsonPlanningExecutionContractRepository CreateRepository(TemporaryStore store) =>
        new(store.Paths, store.Files, NullLogger<JsonPlanningExecutionContractRepository>.Instance);

    private static class ContractFixture
    {
        public static PlanningExecutionContract Create(
            Guid? projectId = null,
            Guid? contractId = null,
            int revision = 1,
            int? previousRevision = null,
            string? previousContentHash = null)
        {
            return new PlanningExecutionContract(
                projectId ?? Guid.NewGuid(),
                contractId ?? Guid.NewGuid(),
                PlanningExecutionContractSchema.CurrentVersion,
                revision,
                new DateTimeOffset(2026, 8, 26, 0, 0, 0, TimeSpan.Zero),
                "owner-ref",
                Guid.NewGuid(),
                new PlanningContextBinding(Guid.NewGuid(), 1),
                new PlanningWorkItem(PlanningWorkItemSource.Jira, "APO-40", "Define contracts"),
                new PlanningRepositoryTarget(PlanningRepositoryMode.None),
                [new("include", "Included")],
                [new("constraint", "Constraint")],
                [new("forbid", "Forbidden")],
                [new("deliverable", "Deliverable", true)],
                [new("build", PlanningValidationKind.Build, "Build", true)],
                [new("criterion", "Criterion", true)],
                [new(PlanningBudgetKind.Attempts, 1)],
                [
                    new("target", PlanningStopConditionKind.ImmutableTargetMoved, "Target moved"),
                    new("scope", PlanningStopConditionKind.ScopeViolation, "Scope violation"),
                    new("budget", PlanningStopConditionKind.BudgetExceeded, "Budget exceeded")
                ],
                ["governance/ref"],
                "routing/ref",
                "safety/ref",
                previousRevision,
                previousContentHash);
        }
    }
}
