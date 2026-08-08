using AIUsageMonitor.Domain.Alerts;
using AIUsageMonitor.Domain.Common;
using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Subscriptions;
using AIUsageMonitor.Domain.Sync;
using AIUsageMonitor.Application.Time;
using AIUsageMonitor.Infrastructure.Persistence.Repositories;

namespace AIUsageMonitor.Infrastructure.Tests;

/// <summary>
/// Write/read coverage for every Session 03 repository other than usage snapshots (which have
/// their own dedicated round-trip/duplicate-prevention tests). Confirms clean-database writes,
/// FK relationships, and that <see cref="ProviderConnection.CredentialReference"/> round-trips
/// as an opaque string rather than a real secret field.
/// </summary>
public sealed class PersistenceSmokeTests : IAsyncLifetime
{
    private readonly LocalDbTestDatabase _database = new();

    public Task InitializeAsync() => _database.InitializeAsync();

    public Task DisposeAsync() => _database.DisposeAsync();

    private async Task<Guid> SeedProviderAsync(ProviderCode code)
    {
        var providerId = Guid.NewGuid();
        await using var context = _database.CreateContext();
        var repository = new EfProviderRepository(context);
        await repository.UpsertAsync(new Provider(
            providerId, code, code.ToString(), enabled: true, sortOrder: 0,
            createdAt: DateTimeOffset.UtcNow, updatedAt: DateTimeOffset.UtcNow));
        return providerId;
    }

    [Fact]
    public async Task ProviderConnection_RoundTrips_WithOpaqueCredentialReference()
    {
        var providerId = await SeedProviderAsync(ProviderCode.Copilot);
        var connectionId = Guid.NewGuid();

        await using (var context = _database.CreateContext())
        {
            var repository = new EfProviderConnectionRepository(context);
            await repository.UpsertAsync(new ProviderConnection(
                connectionId, providerId, ProviderConnectionType.OAuth, ProviderConnectionStatus.Connected,
                accountDisplayName: "user@example.com",
                lastSuccessfulSync: DateTimeOffset.UtcNow,
                lastAttempt: DateTimeOffset.UtcNow,
                lastErrorCode: null,
                lastErrorMessage: null,
                credentialReference: "GitHub:Copilot:Primary"));
        }

        await using var readContext = _database.CreateContext();
        var readRepository = new EfProviderConnectionRepository(readContext);
        var loaded = await readRepository.GetByProviderIdAsync(providerId);

        Assert.NotNull(loaded);
        Assert.Equal(connectionId, loaded!.Id);
        Assert.Equal("GitHub:Copilot:Primary", loaded.CredentialReference);
        Assert.Equal(ProviderConnectionStatus.Connected, loaded.Status);
    }

    [Fact]
    public async Task Subscription_RoundTrips_WithExplicitPricePrecision()
    {
        var providerId = await SeedProviderAsync(ProviderCode.Claude);

        await using (var context = _database.CreateContext())
        {
            var repository = new EfSubscriptionService(context);
            await repository.SaveManualSubscriptionAsync(new Subscription(
                Guid.NewGuid(), providerId, planName: "Pro",
                originalStartDate: DateTimeOffset.UtcNow.AddMonths(-3),
                billingPeriodStart: DateTimeOffset.UtcNow.AddDays(-10),
                billingPeriodEnd: DateTimeOffset.UtcNow.AddDays(20),
                renewalDate: DateTimeOffset.UtcNow.AddDays(20),
                cancelledDate: null,
                autoRenew: true,
                price: 20.00m,
                currency: "USD",
                cadence: BillingCadence.Monthly,
                source: DataSource.Manual,
                confidence: ConfidenceLevel.Manual,
                lastVerifiedAt: DateTimeOffset.UtcNow));
        }

        await using var readContext = _database.CreateContext();
        var readRepository = new EfSubscriptionService(readContext);
        var loaded = await readRepository.GetSubscriptionAsync(ProviderCode.Claude);

        Assert.NotNull(loaded);
        Assert.Equal(20.00m, loaded!.Price);
        Assert.Equal("USD", loaded.Currency);
    }

    [Fact]
    public async Task AlertRuleAndAlertEvent_RoundTrip()
    {
        var providerId = await SeedProviderAsync(ProviderCode.Kimi);
        var ruleId = Guid.NewGuid();

        await using (var context = _database.CreateContext())
        {
            var ruleRepository = new EfAlertRuleRepository(context);
            await ruleRepository.UpsertAsync(new AlertRule(
                ruleId, providerId, quotaDefinitionId: null,
                warningThreshold: 30, criticalThreshold: 15, enabled: true));
        }

        await using (var context = _database.CreateContext())
        {
            var eventRepository = new EfAlertEventRepository(context);
            await eventRepository.AddAsync(new AlertEvent(
                Guid.NewGuid(), ruleId, DateTimeOffset.UtcNow, resolvedAt: null,
                AlertType.ThresholdWarning, AlertSeverity.Warning, value: 28.5, message: "28.5% remaining"));
        }

        await using var readContext = _database.CreateContext();
        var rules = await new EfAlertRuleRepository(readContext).GetByProviderIdAsync(providerId);
        var events = await new EfAlertEventRepository(readContext).GetHistoryAsync(ruleId, maxCount: 10);

        Assert.Single(rules);
        Assert.Equal(30, rules[0].WarningThreshold);
        Assert.Single(events);
        Assert.Equal(28.5, events[0].Value);
    }

    [Fact]
    public async Task SyncEvent_RoundTrips()
    {
        var providerId = await SeedProviderAsync(ProviderCode.Antigravity);

        await using (var context = _database.CreateContext())
        {
            var repository = new EfSyncEventRepository(context);
            await repository.AddAsync(new SyncEvent(
                Guid.NewGuid(), providerId, DateTimeOffset.UtcNow.AddSeconds(-2), DateTimeOffset.UtcNow,
                success: true, dataChanged: true, errorCode: null, errorSummary: null));
        }

        await using var readContext = _database.CreateContext();
        var latest = await new EfSyncEventRepository(readContext).GetLatestAsync(providerId);

        Assert.NotNull(latest);
        Assert.True(latest!.Success);
    }

    [Fact]
    public async Task Settings_RoundTrips_ThroughTypedJson()
    {
        await using (var context = _database.CreateContext())
        {
            var service = new EfSettingsService(context, new SystemClock());
            await service.SetAsync("refresh-interval-seconds", 120);
        }

        await using var readContext = _database.CreateContext();
        var value = await new EfSettingsService(readContext, new SystemClock()).GetAsync<int>("refresh-interval-seconds");

        Assert.Equal(120, value);
    }
}
