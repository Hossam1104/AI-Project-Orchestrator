using AIUsageMonitor.Domain.Common;
using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Quotas;
using AIUsageMonitor.Domain.Usage;
using Microsoft.EntityFrameworkCore;

namespace AIUsageMonitor.Infrastructure.Tests;

/// <summary>
/// The mandatory Session 03 materialization addendum round-trip test: a <see cref="UsageSnapshot"/>
/// with its owned <see cref="QuotaWindow"/> must survive save-then-reload through a FRESH
/// <see cref="AIUsageMonitorDbContext"/> against real LocalDB, not just a change-tracker echo.
/// </summary>
public sealed class UsageSnapshotRoundTripTests : IAsyncLifetime
{
    private readonly LocalDbTestDatabase _database = new();

    public Task InitializeAsync() => _database.InitializeAsync();

    public Task DisposeAsync() => _database.DisposeAsync();

    [Fact]
    public async Task UsageSnapshot_SurvivesRoundTrip_ThroughFreshDbContext()
    {
        var providerId = Guid.NewGuid();
        var quotaDefinitionId = Guid.NewGuid();
        var snapshotId = Guid.NewGuid();

        var quota = QuotaWindow.Create(
            externalKey: "five-hour",
            type: QuotaType.Rolling5Hour,
            unit: QuotaUnit.Percentage,
            usedValue: 42.0,
            remainingValue: 58.0,
            limitValue: 100.0,
            usedPercentage: 42.0,
            remainingPercentage: 58.0,
            windowStart: new DateTimeOffset(2026, 8, 8, 10, 0, 0, TimeSpan.Zero),
            resetAt: new DateTimeOffset(2026, 8, 8, 15, 0, 0, TimeSpan.Zero),
            source: DataSource.OfficialApi,
            confidence: ConfidenceLevel.Official,
            capturedAt: new DateTimeOffset(2026, 8, 8, 10, 5, 0, TimeSpan.Zero));

        var snapshot = new UsageSnapshot(snapshotId, providerId, quotaDefinitionId, quota);

        await using (var seedContext = _database.CreateContext())
        {
            seedContext.Providers.Add(new Provider(
                providerId, ProviderCode.Claude, "Claude", enabled: true, sortOrder: 0,
                createdAt: DateTimeOffset.UtcNow, updatedAt: DateTimeOffset.UtcNow));

            seedContext.QuotaDefinitions.Add(new QuotaDefinition(
                quotaDefinitionId, providerId, "five-hour", "Five Hour Window",
                QuotaType.Rolling5Hour, QuotaUnit.Percentage, sortOrder: 0));

            await seedContext.SaveChangesAsync();
        }

        await using (var writeContext = _database.CreateContext())
        {
            writeContext.UsageSnapshots.Add(snapshot);
            await writeContext.SaveChangesAsync();
        }

        // Fresh DbContext — a true round trip, not a change-tracker assertion on the same instance.
        await using var readContext = _database.CreateContext();
        var loaded = await readContext.UsageSnapshots
            .AsNoTracking()
            .SingleAsync(s => s.Id == snapshotId);

        Assert.Equal(snapshotId, loaded.Id);
        Assert.Equal(providerId, loaded.ProviderId);
        Assert.Equal(quotaDefinitionId, loaded.QuotaDefinitionId);
        Assert.Equal(quota.ExternalKey, loaded.Quota.ExternalKey);
        Assert.Equal(quota.Type, loaded.Quota.Type);
        Assert.Equal(quota.Unit, loaded.Quota.Unit);
        Assert.Equal(quota.UsedValue, loaded.Quota.UsedValue);
        Assert.Equal(quota.RemainingValue, loaded.Quota.RemainingValue);
        Assert.Equal(quota.LimitValue, loaded.Quota.LimitValue);
        Assert.Equal(quota.UsedPercentage, loaded.Quota.UsedPercentage);
        Assert.Equal(quota.RemainingPercentage, loaded.Quota.RemainingPercentage);
        Assert.Equal(quota.WindowStart, loaded.Quota.WindowStart);
        Assert.Equal(quota.ResetAt, loaded.Quota.ResetAt);
        Assert.Equal(quota.Source, loaded.Quota.Source);
        Assert.Equal(quota.Confidence, loaded.Quota.Confidence);
        Assert.Equal(quota.CapturedAt, loaded.Quota.CapturedAt);
        Assert.Equal(quota.CapturedAt, loaded.CapturedAt);
    }
}
