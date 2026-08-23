using AIUsageMonitor.Application.Providers;
using AIUsageMonitor.Application.Security;
using AIUsageMonitor.Application.Time;
using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Infrastructure.Persistence;
using AIUsageMonitor.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageMonitor.Connection.Tests;

public sealed class ProviderConnectionServiceTests
{
    [Fact]
    public async Task Save_PersistsOnlyOpaqueReference_AndAppliesRuntimeSettings()
    {
        using var store = new TemporaryStore();
        var repository = new JsonProviderConnectionRepository(
            store.Paths,
            store.Files,
            NullLogger<JsonProviderConnectionRepository>.Instance);
        var credentials = new FakeCredentialStore();
        var runtime = new FakeRuntimeSettingsUpdater();
        var service = CreateService(repository, credentials, runtime);

        var saved = await service.SaveAsync(new ProviderConnectionEdit(
            ProviderCode.Copilot,
            ProviderConnectionType.OfficialApi,
            new Dictionary<string, string?>
            {
                [ProviderConnectionConfigurationKeys.CopilotScope] = "PersonalUser",
                [ProviderConnectionConfigurationKeys.CopilotUsername] = "octocat"
            },
            secret: "synthetic-secret"));

        Assert.NotNull(saved.CredentialReference);
        Assert.StartsWith("apo-copilot-", saved.CredentialReference, StringComparison.Ordinal);
        Assert.Equal("synthetic-secret", await credentials.RetrieveAsync(saved.CredentialReference!));
        Assert.Equal(saved.CredentialReference, runtime.LastCredentialReference);
        Assert.Equal("octocat", runtime.LastConfiguration[ProviderConnectionConfigurationKeys.CopilotUsername]);

        var json = await File.ReadAllTextAsync(store.Paths.ConnectionsFile);
        Assert.DoesNotContain("synthetic-secret", json, StringComparison.Ordinal);
        Assert.Contains(saved.CredentialReference, json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FailedPersistence_RemovesStagedCredential_AndPreservesPreviousReference()
    {
        var repository = new InMemoryConnectionRepository { FailWrites = true };
        var previous = CreateConnection("old-reference");
        repository.Current = previous;
        var credentials = new FakeCredentialStore();
        await credentials.StoreAsync("old-reference", "old-secret");
        var service = CreateService(repository, credentials, new FakeRuntimeSettingsUpdater());

        await Assert.ThrowsAsync<IOException>(() => service.SaveAsync(new ProviderConnectionEdit(
            ProviderCode.Copilot,
            ProviderConnectionType.OfficialApi,
            new Dictionary<string, string?>(),
            secret: "new-secret")));

        Assert.Equal(previous.CredentialReference, repository.Current!.CredentialReference);
        Assert.Equal("old-secret", await credentials.RetrieveAsync("old-reference"));
        Assert.Single(credentials.RemovedReferences);
        Assert.StartsWith("apo-copilot-", credentials.RemovedReferences[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task RemoveCredential_CommitsNullReferenceBeforeSecureCleanup()
    {
        var repository = new InMemoryConnectionRepository { Current = CreateConnection("old-reference") };
        var credentials = new FakeCredentialStore();
        await credentials.StoreAsync("old-reference", "old-secret");
        var service = CreateService(repository, credentials, new FakeRuntimeSettingsUpdater());

        var saved = await service.SaveAsync(new ProviderConnectionEdit(
            ProviderCode.Copilot,
            ProviderConnectionType.OfficialApi,
            new Dictionary<string, string?>(),
            removeCredential: true));

        Assert.Null(saved.CredentialReference);
        Assert.Null(repository.Current!.CredentialReference);
        Assert.Equal(["old-reference"], credentials.RemovedReferences);
        Assert.Null(await credentials.RetrieveAsync("old-reference"));
    }

    private static ProviderConnectionService CreateService(
        IProviderConnectionRepository repository,
        FakeCredentialStore credentials,
        IProviderRuntimeSettingsUpdater runtime) =>
        new(repository, new FixedIdentityCatalog(), credentials, runtime, new SystemClock());

    private static ProviderConnection CreateConnection(string credentialReference) => new(
        Guid.NewGuid(),
        FixedIdentityCatalog.CopilotId,
        ProviderConnectionType.OfficialApi,
        ProviderConnectionStatus.Connected,
        "test-account",
        null,
        null,
        null,
        null,
        credentialReference);

    private sealed class TemporaryStore : IDisposable
    {
        public TemporaryStore()
        {
            RootDirectory = Path.Combine(Path.GetTempPath(), "AIUsageMonitorTests", Guid.NewGuid().ToString("N"));
            Paths = new ApplicationDataPaths(RootDirectory);
            Files = new JsonFileStore(NullLogger<JsonFileStore>.Instance);
            Paths.EnsureDirectories();
        }

        public string RootDirectory { get; }

        public ApplicationDataPaths Paths { get; }

        public JsonFileStore Files { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(RootDirectory))
                {
                    Directory.Delete(RootDirectory, recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private sealed class FixedIdentityCatalog : IProviderIdentityCatalog
    {
        public static Guid CopilotId { get; } = Guid.Parse("e6cb6e5a-e4ec-4a53-8d83-cb1f97f51011");

        public Guid GetProviderId(ProviderCode code) => CopilotId;
    }

    private sealed class FakeRuntimeSettingsUpdater : IProviderRuntimeSettingsUpdater
    {
        public string? LastCredentialReference { get; private set; }

        public IReadOnlyDictionary<string, string?> LastConfiguration { get; private set; } =
            new Dictionary<string, string?>();

        public void Apply(
            ProviderCode code,
            string? credentialReference,
            IReadOnlyDictionary<string, string?> configuration)
        {
            LastCredentialReference = credentialReference;
            LastConfiguration = configuration;
        }
    }

    private sealed class FakeCredentialStore : ISecureCredentialStore
    {
        private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

        public List<string> RemovedReferences { get; } = [];

        public Task StoreAsync(string credentialReference, string secret, CancellationToken cancellationToken = default)
        {
            _values[credentialReference] = secret;
            return Task.CompletedTask;
        }

        public Task<string?> RetrieveAsync(string credentialReference, CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.TryGetValue(credentialReference, out var secret) ? secret : null);

        public Task RemoveAsync(string credentialReference, CancellationToken cancellationToken = default)
        {
            _values.Remove(credentialReference);
            RemovedReferences.Add(credentialReference);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryConnectionRepository : IProviderConnectionRepository
    {
        public ProviderConnection? Current { get; set; }

        public bool FailWrites { get; init; }

        public Task<ProviderConnection?> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Current);

        public Task UpsertAsync(ProviderConnection connection, CancellationToken cancellationToken = default)
        {
            if (FailWrites)
            {
                throw new IOException("synthetic persistence failure");
            }

            Current = connection;
            return Task.CompletedTask;
        }
    }
}
