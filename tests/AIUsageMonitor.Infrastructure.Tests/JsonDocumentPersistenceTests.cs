using System.Text.Json;
using AIUsageMonitor.Application.Time;
using AIUsageMonitor.Domain.Common;
using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Subscriptions;
using AIUsageMonitor.Infrastructure.Persistence;
using AIUsageMonitor.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageMonitor.Infrastructure.Tests;

public sealed class JsonDocumentPersistenceTests
{
    [Fact]
    public async Task ApplicationDataPaths_InitializesPerUserLayout()
    {
        using var store = new TemporaryStore();

        Assert.True(Directory.Exists(store.Paths.RootDirectory));
        Assert.True(Directory.Exists(store.Paths.HistoryDirectory));
        Assert.True(Directory.Exists(store.Paths.AlertsDirectory));
        Assert.True(Directory.Exists(store.Paths.SyncDirectory));
        Assert.True(Directory.Exists(store.Paths.LogsDirectory));

        await store.Paths.EnsureDirectoriesAsync();
    }

    [Fact]
    public async Task ProviderAndConnectionJson_RoundTrip_WithOpaqueCredentialReference()
    {
        using var store = new TemporaryStore();
        var provider = new Provider(
            Guid.NewGuid(),
            ProviderCode.Copilot,
            "GitHub Copilot",
            enabled: true,
            sortOrder: 3,
            createdAt: new DateTimeOffset(2026, 8, 8, 10, 0, 0, TimeSpan.Zero),
            updatedAt: new DateTimeOffset(2026, 8, 8, 10, 0, 0, TimeSpan.Zero));
        var providerRepository = new JsonProviderRepository(
            store.Paths,
            store.Files,
            NullLogger<JsonProviderRepository>.Instance);

        await providerRepository.UpsertAsync(provider);
        var loadedProvider = await providerRepository.GetByCodeAsync(ProviderCode.Copilot);

        Assert.NotNull(loadedProvider);
        Assert.Equal(provider.Id, loadedProvider!.Id);
        Assert.Equal(provider.UpdatedAt, loadedProvider.UpdatedAt);

        var connectionRepository = new JsonProviderConnectionRepository(
            store.Paths,
            store.Files,
            NullLogger<JsonProviderConnectionRepository>.Instance);
        var connection = new ProviderConnection(
            Guid.NewGuid(),
            provider.Id,
            ProviderConnectionType.OAuth,
            ProviderConnectionStatus.Connected,
            "account-label",
            new DateTimeOffset(2026, 8, 8, 10, 2, 0, TimeSpan.FromHours(5.5)),
            new DateTimeOffset(2026, 8, 8, 10, 3, 0, TimeSpan.FromHours(5.5)),
            null,
            null,
            "GitHub:Copilot:Primary");

        await connectionRepository.UpsertAsync(connection);
        var loadedConnection = await connectionRepository.GetByProviderIdAsync(provider.Id);

        Assert.NotNull(loadedConnection);
        Assert.Equal(connection.Id, loadedConnection!.Id);
        Assert.Equal("GitHub:Copilot:Primary", loadedConnection.CredentialReference);

        var json = await File.ReadAllTextAsync(store.Paths.ConnectionsFile);
        Assert.DoesNotContain("raw-secret", json, StringComparison.Ordinal);
        Assert.Contains("schemaVersion", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubscriptionJson_RoundTrips_ThroughProviderCodeLookup()
    {
        using var store = new TemporaryStore();
        var provider = new Provider(
            Guid.NewGuid(),
            ProviderCode.Claude,
            "Claude",
            enabled: true,
            sortOrder: 1,
            createdAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow);
        var providerRepository = new JsonProviderRepository(
            store.Paths,
            store.Files,
            NullLogger<JsonProviderRepository>.Instance);
        await providerRepository.UpsertAsync(provider);

        var service = new JsonSubscriptionService(
            store.Paths,
            providerRepository,
            store.Files,
            NullLogger<JsonSubscriptionService>.Instance);
        var subscription = new Subscription(
            Guid.NewGuid(),
            provider.Id,
            "Pro",
            new DateTimeOffset(2026, 5, 8, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 8, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 8, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 8, 0, 0, 0, TimeSpan.Zero),
            null,
            true,
            20.00m,
            "USD",
            BillingCadence.Monthly,
            DataSource.Manual,
            ConfidenceLevel.Manual,
            new DateTimeOffset(2026, 8, 8, 10, 0, 0, TimeSpan.FromHours(5.5)));

        await service.SaveManualSubscriptionAsync(subscription);
        var loaded = await service.GetSubscriptionAsync(ProviderCode.Claude);

        Assert.NotNull(loaded);
        Assert.Equal(subscription.Id, loaded!.Id);
        Assert.Equal(subscription.Price, loaded.Price);
        Assert.Equal(subscription.LastVerifiedAt, loaded.LastVerifiedAt);
        Assert.Equal(ConfidenceLevel.Manual, loaded.Confidence);
    }

    [Fact]
    public async Task SettingsJson_RoundTrips_AndReplacesAtomically()
    {
        using var store = new TemporaryStore();
        var service = new JsonSettingsService(
            store.Paths,
            store.Files,
            new SystemClock(),
            NullLogger<JsonSettingsService>.Instance);

        await service.SetAsync("refresh-interval-seconds", 120);
        Assert.Equal(120, await service.GetAsync<int>("refresh-interval-seconds"));

        await service.SetAsync("refresh-interval-seconds", 60);
        Assert.Equal(60, await service.GetAsync<int>("refresh-interval-seconds"));

        var json = await File.ReadAllTextAsync(store.Paths.SettingsFile);
        Assert.Contains("schemaVersion", json, StringComparison.Ordinal);
        Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Directory.EnumerateFiles(store.Paths.RootDirectory, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task MissingAndEmptyJsonFiles_ReturnSafeDefaults_AndAreDistinguished()
    {
        using var store = new TemporaryStore();

        var missing = await store.Files.ReadAsync<JsonElement>(store.Paths.SettingsFile);
        Assert.Equal(FileReadStatus.Missing, missing.Status);

        await File.WriteAllTextAsync(store.Paths.SettingsFile, "  ");
        var empty = await store.Files.ReadAsync<JsonElement>(store.Paths.SettingsFile);
        Assert.Equal(FileReadStatus.Empty, empty.Status);

        var service = new JsonSettingsService(
            store.Paths,
            store.Files,
            new SystemClock(),
            NullLogger<JsonSettingsService>.Instance);
        Assert.Null(await service.GetAsync<int?>("missing"));
    }

    [Fact]
    public async Task CorruptAndUnsupportedJsonFiles_AreQuarantinedWithoutThrowing()
    {
        using var store = new TemporaryStore();

        await File.WriteAllTextAsync(store.Paths.SettingsFile, "{ definitely-not-json");
        var corrupt = await store.Files.ReadAsync<JsonElement>(store.Paths.SettingsFile);
        Assert.Equal(FileReadStatus.Corrupt, corrupt.Status);
        Assert.False(File.Exists(store.Paths.SettingsFile));
        Assert.NotEmpty(Directory.EnumerateFiles(store.Paths.RootDirectory, "settings.json.corrupt-*.bak"));

        await File.WriteAllTextAsync(
            store.Paths.SettingsFile,
            "{ \"schemaVersion\": 999, \"payload\": {} }");
        var unsupported = await store.Files.ReadAsync<JsonElement>(store.Paths.SettingsFile);
        Assert.Equal(FileReadStatus.UnsupportedSchema, unsupported.Status);
        Assert.False(File.Exists(store.Paths.SettingsFile));
        Assert.NotEmpty(Directory.EnumerateFiles(store.Paths.RootDirectory, "settings.json.unsupported-schema-*.bak"));
    }

    [Fact]
    public async Task ConcurrentJsonWriters_LeaveOneCompleteDocument()
    {
        using var store = new TemporaryStore();
        var writes = Enumerable.Range(0, 20)
            .Select(index => store.Files.WriteAsync(store.Paths.SettingsFile, new { Writer = index }))
            .ToArray();

        await Task.WhenAll(writes);

        var content = await File.ReadAllTextAsync(store.Paths.SettingsFile);
        using var document = JsonDocument.Parse(content);
        Assert.Equal(JsonValueKind.Object, document.RootElement.GetProperty("payload").ValueKind);
        Assert.Empty(Directory.EnumerateFiles(store.Paths.RootDirectory, "*.tmp", SearchOption.AllDirectories));
    }
}
