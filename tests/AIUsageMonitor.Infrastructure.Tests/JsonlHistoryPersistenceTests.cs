using AIUsageMonitor.Domain.Common;
using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Quotas;
using AIUsageMonitor.Domain.Usage;
using AIUsageMonitor.Infrastructure.Persistence;
using AIUsageMonitor.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageMonitor.Infrastructure.Tests;

public sealed class JsonlHistoryPersistenceTests
{
    [Fact]
    public async Task UsageSnapshots_AppendToMonthlyJsonl_PreserveOffsetsAndSuppressDuplicates()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var providerId = Guid.NewGuid();
        var quotaDefinitionId = Guid.NewGuid();
        var windowStart = new DateTimeOffset(2026, 8, 8, 10, 0, 0, TimeSpan.Zero);
        var resetAt = new DateTimeOffset(2026, 8, 8, 15, 0, 0, TimeSpan.Zero);
        var firstCapturedAt = new DateTimeOffset(2026, 8, 8, 10, 5, 0, TimeSpan.FromHours(5.5));

        var first = CreateSnapshot(
            providerId,
            quotaDefinitionId,
            usedPercentage: 42,
            firstCapturedAt,
            windowStart,
            resetAt);
        var negligible = CreateSnapshot(
            providerId,
            quotaDefinitionId,
            usedPercentage: 42.4,
            firstCapturedAt.AddMinutes(5),
            windowStart,
            resetAt);
        var material = CreateSnapshot(
            providerId,
            quotaDefinitionId,
            usedPercentage: 55,
            firstCapturedAt.AddMinutes(10),
            windowStart,
            resetAt);

        await repository.AddAsync(first);
        await repository.AddAsync(negligible);
        await repository.AddAsync(material);

        var from = firstCapturedAt.AddMinutes(-1);
        var to = material.CapturedAt.AddMinutes(1);
        var history = await repository.GetHistoryAsync(providerId, from, to);

        Assert.Equal(2, history.Count);
        Assert.Equal(42, history[0].Quota.UsedPercentage);
        Assert.Equal(55, history[1].Quota.UsedPercentage);
        Assert.Equal(firstCapturedAt.Offset, history[0].CapturedAt.Offset);
        Assert.Equal(material.Id, (await repository.GetLatestAsync(providerId, quotaDefinitionId))!.Id);

        var partition = store.Paths.GetMonthlyPartition(store.Paths.HistoryDirectory, firstCapturedAt);
        var lines = await File.ReadAllLinesAsync(partition);
        Assert.Equal(2, lines.Length);
        Assert.All(lines, line =>
        {
            Assert.Contains("schemaVersion", line, StringComparison.Ordinal);
            Assert.Contains("recordType", line, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task UsageSnapshots_QueryOnlyRelevantMonthlyPartitions_InChronologicalOrder()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var providerId = Guid.NewGuid();
        var quotaDefinitionId = Guid.NewGuid();
        var windowStart = new DateTimeOffset(2026, 8, 31, 10, 0, 0, TimeSpan.Zero);
        var resetAt = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var august = CreateSnapshot(
            providerId,
            quotaDefinitionId,
            20,
            new DateTimeOffset(2026, 8, 31, 23, 30, 0, TimeSpan.FromHours(5.5)),
            windowStart,
            resetAt);
        var september = CreateSnapshot(
            providerId,
            quotaDefinitionId,
            35,
            new DateTimeOffset(2026, 9, 1, 6, 30, 0, TimeSpan.FromHours(5.5)),
            windowStart,
            resetAt);

        await repository.AddAsync(august);
        await repository.AddAsync(september);

        var history = await repository.GetHistoryAsync(
            providerId,
            new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 2, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(2, history.Count);
        Assert.Equal(20, history[0].Quota.UsedPercentage);
        Assert.Equal(35, history[1].Quota.UsedPercentage);
        Assert.True(File.Exists(store.Paths.GetMonthlyPartition(store.Paths.HistoryDirectory, august.CapturedAt)));
        Assert.True(File.Exists(store.Paths.GetMonthlyPartition(store.Paths.HistoryDirectory, september.CapturedAt)));
    }

    [Fact]
    public async Task UsageSnapshots_LatestLookup_StopsAtNewestMatchingPartition()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var providerId = Guid.NewGuid();
        var quotaDefinitionId = Guid.NewGuid();
        var capturedAt = new DateTimeOffset(2026, 8, 8, 10, 5, 0, TimeSpan.Zero);
        var latest = CreateSnapshot(
            providerId,
            quotaDefinitionId,
            64,
            capturedAt,
            capturedAt.AddMinutes(-5),
            capturedAt.AddHours(5));

        var olderIrrelevantPartition = store.Paths.GetMonthlyPartition(
            store.Paths.HistoryDirectory,
            capturedAt.AddMonths(-1));
        await File.WriteAllTextAsync(olderIrrelevantPartition, "{ this older partition is corrupt\n");

        await repository.AddAsync(latest);

        var loaded = await repository.GetLatestAsync(providerId, quotaDefinitionId);

        Assert.NotNull(loaded);
        Assert.Equal(latest.Id, loaded!.Id);
        Assert.Equal(latest.Quota.CapturedAt, loaded.CapturedAt);
    }

    [Fact]
    public async Task CorruptJsonlRecord_IsolatedFromValidRecords()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var providerId = Guid.NewGuid();
        var quotaDefinitionId = Guid.NewGuid();
        var capturedAt = new DateTimeOffset(2026, 8, 8, 10, 5, 0, TimeSpan.Zero);
        var partition = store.Paths.GetMonthlyPartition(store.Paths.HistoryDirectory, capturedAt);
        Directory.CreateDirectory(store.Paths.HistoryDirectory);
        await File.WriteAllTextAsync(partition, "{ broken-json\n");

        var valid = CreateSnapshot(
            providerId,
            quotaDefinitionId,
            48,
            capturedAt.AddMinutes(1),
            new DateTimeOffset(2026, 8, 8, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 8, 15, 0, 0, TimeSpan.Zero));
        await repository.AddAsync(valid);

        var history = await repository.GetHistoryAsync(
            providerId,
            capturedAt.AddHours(-1),
            capturedAt.AddHours(1));

        Assert.Single(history);
        Assert.Equal(valid.Id, history[0].Id);
    }

    [Fact]
    public async Task JsonlAppend_IsolatesInterruptedTailBeforeNewRecord()
    {
        using var store = new TemporaryStore();
        var events = new JsonlEventStore<UsageSnapshotRecord>(
            store.Paths,
            store.Files,
            NullLogger<JsonlEventStore<UsageSnapshotRecord>>.Instance);
        var providerId = Guid.NewGuid();
        var quotaDefinitionId = Guid.NewGuid();
        var capturedAt = new DateTimeOffset(2026, 8, 8, 10, 5, 0, TimeSpan.Zero);
        var valid = CreateSnapshot(
            providerId,
            quotaDefinitionId,
            40,
            capturedAt,
            capturedAt.AddMinutes(-5),
            capturedAt.AddHours(5));
        var appended = CreateSnapshot(
            providerId,
            quotaDefinitionId,
            45,
            capturedAt.AddMinutes(5),
            capturedAt.AddMinutes(-5),
            capturedAt.AddHours(5));
        var partition = store.Paths.GetMonthlyPartition(store.Paths.HistoryDirectory, capturedAt);
        var validLine = System.Text.Json.JsonSerializer.Serialize(
            UsageSnapshotRecord.FromDomain(valid),
            JsonFileStore.JsonlSerializerOptions);
        const string partialTail = "{\"schemaVersion\":1,\"recordType\":\"usage-snapshot\"";
        await File.WriteAllTextAsync(partition, $"{validLine}\n{partialTail}");

        await events.AppendAsync(
            store.Paths.HistoryDirectory,
            appended.CapturedAt,
            UsageSnapshotRecord.FromDomain(appended));

        var records = new List<UsageSnapshotRecord>();
        await foreach (var record in events.ReadAllAsync(
                           store.Paths.HistoryDirectory,
                           static value => value.CapturedAt))
        {
            records.Add(record);
        }

        Assert.Equal(2, records.Count);
        Assert.Equal(valid.Id, records[0].Id);
        Assert.Equal(appended.Id, records[1].Id);

        var persisted = await File.ReadAllTextAsync(partition);
        Assert.Contains($"{partialTail}\n", persisted, StringComparison.Ordinal);
        Assert.Contains($"{partialTail}\n{System.Text.Json.JsonSerializer.Serialize(UsageSnapshotRecord.FromDomain(appended), JsonFileStore.JsonlSerializerOptions)}\n", persisted, StringComparison.Ordinal);
    }

    private static JsonUsageSnapshotRepository CreateRepository(TemporaryStore store)
    {
        var events = new JsonlEventStore<UsageSnapshotRecord>(
            store.Paths,
            store.Files,
            NullLogger<JsonlEventStore<UsageSnapshotRecord>>.Instance);
        return new JsonUsageSnapshotRepository(
            store.Paths,
            store.Files,
            events,
            NullLogger<JsonUsageSnapshotRepository>.Instance);
    }

    private static UsageSnapshot CreateSnapshot(
        Guid providerId,
        Guid quotaDefinitionId,
        double usedPercentage,
        DateTimeOffset capturedAt,
        DateTimeOffset windowStart,
        DateTimeOffset resetAt) =>
        new(
            Guid.NewGuid(),
            providerId,
            quotaDefinitionId,
            QuotaWindow.Create(
                externalKey: "rolling-window",
                type: QuotaType.Rolling5Hour,
                unit: QuotaUnit.Percentage,
                usedValue: usedPercentage,
                remainingValue: 100 - usedPercentage,
                limitValue: 100,
                usedPercentage: usedPercentage,
                remainingPercentage: 100 - usedPercentage,
                windowStart: windowStart,
                resetAt: resetAt,
                source: DataSource.OfficialApi,
                confidence: ConfidenceLevel.Official,
                capturedAt: capturedAt));
}
