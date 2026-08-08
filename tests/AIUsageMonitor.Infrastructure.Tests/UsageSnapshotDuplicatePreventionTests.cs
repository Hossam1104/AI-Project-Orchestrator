using AIUsageMonitor.Domain.Common;
using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Quotas;
using AIUsageMonitor.Domain.Usage;
using AIUsageMonitor.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AIUsageMonitor.Infrastructure.Tests;

/// <summary>
/// BRD §16: refreshing with no material change must not grow the <c>UsageSnapshots</c> history
/// table, but a genuine change must always be persisted as a new row.
/// </summary>
public sealed class UsageSnapshotDuplicatePreventionTests : IAsyncLifetime
{
    private readonly LocalDbTestDatabase _database = new();

    public Task InitializeAsync() => _database.InitializeAsync();

    public Task DisposeAsync() => _database.DisposeAsync();

    private static QuotaWindow CreateQuota(double usedPercentage, DateTimeOffset capturedAt) =>
        QuotaWindow.Create(
            externalKey: "five-hour",
            type: QuotaType.Rolling5Hour,
            unit: QuotaUnit.Percentage,
            usedValue: usedPercentage,
            remainingValue: 100.0 - usedPercentage,
            limitValue: 100.0,
            usedPercentage: usedPercentage,
            remainingPercentage: 100.0 - usedPercentage,
            windowStart: new DateTimeOffset(2026, 8, 8, 10, 0, 0, TimeSpan.Zero),
            resetAt: new DateTimeOffset(2026, 8, 8, 15, 0, 0, TimeSpan.Zero),
            source: DataSource.OfficialApi,
            confidence: ConfidenceLevel.Official,
            capturedAt: capturedAt);

    private async Task<(Guid ProviderId, Guid QuotaDefinitionId)> SeedProviderAndQuotaAsync()
    {
        var providerId = Guid.NewGuid();
        var quotaDefinitionId = Guid.NewGuid();

        await using var seedContext = _database.CreateContext();
        seedContext.Providers.Add(new Provider(
            providerId, ProviderCode.Codex, "Codex", enabled: true, sortOrder: 0,
            createdAt: DateTimeOffset.UtcNow, updatedAt: DateTimeOffset.UtcNow));
        seedContext.QuotaDefinitions.Add(new QuotaDefinition(
            quotaDefinitionId, providerId, "five-hour", "Five Hour Window",
            QuotaType.Rolling5Hour, QuotaUnit.Percentage, sortOrder: 0));
        await seedContext.SaveChangesAsync();

        return (providerId, quotaDefinitionId);
    }

    [Fact]
    public async Task AddAsync_SkipsWrite_WhenNoMaterialChangeSincePrevious()
    {
        var (providerId, quotaDefinitionId) = await SeedProviderAndQuotaAsync();

        var first = new UsageSnapshot(
            Guid.NewGuid(), providerId, quotaDefinitionId,
            CreateQuota(42.0, new DateTimeOffset(2026, 8, 8, 10, 5, 0, TimeSpan.Zero)));

        // Within UsageSnapshotChangeDetector.PercentageChangeThreshold (1.0) — noise, not a
        // material change.
        var secondNegligibleChange = new UsageSnapshot(
            Guid.NewGuid(), providerId, quotaDefinitionId,
            CreateQuota(42.4, new DateTimeOffset(2026, 8, 8, 10, 10, 0, TimeSpan.Zero)));

        await using (var context = _database.CreateContext())
        {
            var repository = new EfUsageSnapshotRepository(context);
            await repository.AddAsync(first);
            await repository.AddAsync(secondNegligibleChange);
        }

        await using var readContext = _database.CreateContext();
        var count = await readContext.UsageSnapshots
            .AsNoTracking()
            .CountAsync(s => s.ProviderId == providerId && s.QuotaDefinitionId == quotaDefinitionId);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AddAsync_Writes_WhenMaterialChangeOccurs()
    {
        var (providerId, quotaDefinitionId) = await SeedProviderAndQuotaAsync();

        var first = new UsageSnapshot(
            Guid.NewGuid(), providerId, quotaDefinitionId,
            CreateQuota(42.0, new DateTimeOffset(2026, 8, 8, 10, 5, 0, TimeSpan.Zero)));

        // Well beyond PercentageChangeThreshold — a real change that must be persisted.
        var second = new UsageSnapshot(
            Guid.NewGuid(), providerId, quotaDefinitionId,
            CreateQuota(55.0, new DateTimeOffset(2026, 8, 8, 10, 10, 0, TimeSpan.Zero)));

        await using (var context = _database.CreateContext())
        {
            var repository = new EfUsageSnapshotRepository(context);
            await repository.AddAsync(first);
            await repository.AddAsync(second);
        }

        await using var readContext = _database.CreateContext();
        var count = await readContext.UsageSnapshots
            .AsNoTracking()
            .CountAsync(s => s.ProviderId == providerId && s.QuotaDefinitionId == quotaDefinitionId);

        Assert.Equal(2, count);
    }
}
