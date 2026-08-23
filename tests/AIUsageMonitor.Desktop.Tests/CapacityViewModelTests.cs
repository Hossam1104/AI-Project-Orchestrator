using AIUsageMonitor.Application.Providers;
using AIUsageMonitor.Desktop.ViewModels;
using AIUsageMonitor.Domain.Common;
using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Quotas;
using AIUsageMonitor.Domain.Subscriptions;
using AIUsageMonitor.Providers.Common;
using AIUsageMonitor.Providers.Copilot;

namespace AIUsageMonitor.Desktop.Tests;

public sealed class CapacityViewModelTests
{
    [Fact]
    public void DegradedWorkspace_ContainsExactlyFiveProvidersInStableOrder()
    {
        var viewModel = new AiCapacityViewModel();

        Assert.Equal(5, viewModel.Cards.Count);
        Assert.Equal(
            Enum.GetValues<ProviderCode>().OrderBy(code => code),
            viewModel.Cards.Select(card => card.Code));
        Assert.Equal(
            ["Codex", "Claude / Anthropic", "Kimi", "GitHub Copilot", "Antigravity"],
            viewModel.Cards.Select(card => card.DisplayName));
    }

    [Fact]
    public void ShellStartsOnAiCapacity_WhileOverviewRemainsAvailable()
    {
        var viewModel = new MainWindowViewModel(new AiCapacityViewModel());

        Assert.Same(viewModel.AiCapacity, viewModel.ActiveWorkspace);
        Assert.False(viewModel.IsOverviewSelected);
        Assert.True(viewModel.IsAiCapacitySelected);
    }

    [Fact]
    public void RemainingPercentage_IsPresentedAsRemainingWithoutInversion()
    {
        var window = QuotaWindow.Create(
            "test-window",
            QuotaType.Rolling5Hour,
            QuotaUnit.Count,
            usedValue: 20,
            remainingValue: 80,
            limitValue: 100,
            usedPercentage: null,
            remainingPercentage: null,
            windowStart: null,
            resetAt: null,
            DataSource.OfficialApi,
            ConfidenceLevel.Official,
            DateTimeOffset.UtcNow);

        var viewModel = new QuotaWindowViewModel(window);

        Assert.Equal("80% remaining", viewModel.RemainingText);
        Assert.True(viewModel.HasProgress);
        Assert.Equal(80, viewModel.ProgressValue);
    }

    [Fact]
    public void UsageOnlyQuota_DoesNotInventRemainingOrProgress()
    {
        var window = QuotaWindow.Create(
            "credits",
            QuotaType.AiCredits,
            QuotaUnit.Credits,
            usedValue: 123,
            remainingValue: null,
            limitValue: null,
            usedPercentage: null,
            remainingPercentage: null,
            windowStart: null,
            resetAt: null,
            DataSource.OfficialApi,
            ConfidenceLevel.Official,
            DateTimeOffset.UtcNow);

        var viewModel = new QuotaWindowViewModel(window);

        Assert.Equal("Remaining: unavailable", viewModel.RemainingText);
        Assert.Equal("Used 123 credits", viewModel.UsageText);
        Assert.False(viewModel.HasProgress);
    }

    [Fact]
    public void UnknownValues_AreRenderedAsUnavailable_NotZero()
    {
        var window = QuotaWindow.Create(
            "unknown",
            QuotaType.Custom,
            QuotaUnit.Custom,
            usedValue: null,
            remainingValue: null,
            limitValue: null,
            usedPercentage: null,
            remainingPercentage: null,
            windowStart: null,
            resetAt: null,
            DataSource.Manual,
            ConfidenceLevel.Manual,
            DateTimeOffset.UtcNow);

        var viewModel = new QuotaWindowViewModel(window);

        Assert.Contains("unavailable", viewModel.RemainingText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("0%", viewModel.RemainingText, StringComparison.Ordinal);
        Assert.DoesNotContain("0 /", viewModel.UsageText, StringComparison.Ordinal);
    }

    [Fact]
    public void ResetTime_IsLocalizedForDisplay_WithoutChangingSourceTimestamp()
    {
        var source = new DateTimeOffset(2026, 8, 29, 15, 0, 0, TimeSpan.FromHours(2));
        var window = QuotaWindow.Create(
            "reset",
            QuotaType.Daily,
            QuotaUnit.Count,
            usedValue: 1,
            remainingValue: 9,
            limitValue: 10,
            usedPercentage: null,
            remainingPercentage: null,
            windowStart: null,
            resetAt: source,
            DataSource.OfficialApi,
            ConfidenceLevel.Official,
            DateTimeOffset.UtcNow);

        var viewModel = new QuotaWindowViewModel(window);

        Assert.Equal(source, viewModel.ResetAt);
        Assert.Contains("Resets", viewModel.ResetText, StringComparison.Ordinal);
    }

    [Fact]
    public void StaleResult_RetainsValuesAndShowsStaleStatus()
    {
        var provider = new FakeProvider(ProviderCode.Copilot);
        var card = new ProviderCapacityCardViewModel(ProviderCode.Copilot, "GitHub Copilot", provider);
        var window = CreateWindow(80);
        var first = ProviderRefreshResult.Partial(
            ProviderCode.Copilot,
            null,
            null,
            [window],
            "Usage only.",
            DateTimeOffset.UtcNow);
        var stale = ProviderRefreshResult.Failed(
            ProviderCode.Copilot,
            "network",
            "Showing stale data.",
            DateTimeOffset.UtcNow,
            lastKnownQuotaWindows: [window]);

        card.ApplyResult(first);
        card.ApplyResult(stale);

        Assert.Equal("Stale", card.StatusText);
        Assert.Single(card.QuotaWindows);
        Assert.Equal("80% remaining", card.QuotaWindows[0].RemainingText);
    }

    [Fact]
    public void AuthenticationAndUnsupportedStates_RemainDistinct()
    {
        var authCard = new ProviderCapacityCardViewModel(ProviderCode.Copilot, "GitHub Copilot", new FakeProvider(ProviderCode.Copilot));
        authCard.ApplyResult(ProviderRefreshResult.AuthenticationRequired(ProviderCode.Copilot, DateTimeOffset.UtcNow));
        var unsupportedCard = new ProviderCapacityCardViewModel(ProviderCode.Codex, "Codex", new FakeProvider(ProviderCode.Codex));
        unsupportedCard.ApplyResult(ProviderRefreshResult.Unsupported(ProviderCode.Codex, DateTimeOffset.UtcNow));

        Assert.Equal("Authentication Required", authCard.StatusText);
        Assert.Equal("Unsupported / Manual", unsupportedCard.StatusText);
    }

    [Fact]
    public async Task RefreshAll_IsolatesProviderFailure_AndDoesNotOverlap()
    {
        var providers = Enum.GetValues<ProviderCode>()
            .Select(code => new FakeProvider(code) { ThrowOnRefresh = code == ProviderCode.Kimi })
            .ToArray();
        var registry = new FakeRegistry(providers);
        var viewModel = new AiCapacityViewModel(registry, new FakeConnectionService());

        var first = viewModel.RefreshAllAsync();
        var second = viewModel.RefreshAllAsync();
        await Task.WhenAll(first, second);

        Assert.Equal("Error", Assert.Single(viewModel.Cards, card => card.Code == ProviderCode.Kimi).StatusText);
        Assert.All(viewModel.Cards, card => Assert.False(card.IsRefreshing));
        Assert.All(providers, provider => Assert.Equal(1, provider.RefreshCount));
        Assert.All(providers, provider => Assert.Equal(1, provider.MaxConcurrentRefreshes));
    }

    [Fact]
    public async Task PerProviderRefresh_InvokesOneRefreshAsync()
    {
        var provider = new FakeProvider(ProviderCode.Copilot)
        {
            Result = ProviderRefreshResult.Partial(
                ProviderCode.Copilot,
                null,
                null,
                [CreateWindow(70)],
                "Usage-only.",
                DateTimeOffset.UtcNow)
        };
        var card = new ProviderCapacityCardViewModel(ProviderCode.Copilot, "GitHub Copilot", provider);

        await card.RefreshAsync();

        Assert.Equal(1, provider.RefreshCount);
        Assert.Single(card.QuotaWindows);
    }

    [Fact]
    public void SavedSettingsCanBeAppliedToRuntimeSnapshotWithoutRestart()
    {
        var settings = new ProviderRuntimeSettingsAccessor();

        settings.Apply(
            ProviderCode.Copilot,
            "opaque-copilot-reference",
            new Dictionary<string, string?>
            {
                [ProviderConnectionConfigurationKeys.CopilotScope] = CopilotBillingScope.Organization.ToString(),
                [ProviderConnectionConfigurationKeys.CopilotOrganization] = "example-org"
            });

        Assert.Equal("opaque-copilot-reference", settings.Current.Copilot.CredentialReference);
        Assert.Equal(CopilotBillingScope.Organization, settings.Current.Copilot.Scope);
        Assert.Equal("example-org", settings.Current.Copilot.Organization);
    }

    [Fact]
    public async Task OpeningSavedEditorStateDoesNotReadBackCredential()
    {
        var service = new FakeConnectionService
        {
            Connection = new ProviderConnection(
                Guid.NewGuid(),
                Guid.NewGuid(),
                ProviderConnectionType.OfficialApi,
                ProviderConnectionStatus.Connected,
                null,
                null,
                null,
                null,
                null,
                "opaque-reference")
        };
        var editor = new ProviderConnectionEditorViewModel(ProviderCode.Copilot, service.Connection, service);

        Assert.True(editor.CredentialSaved);
        Assert.Equal(0, service.RetrieveCount);
        await Task.CompletedTask;
    }

    private static QuotaWindow CreateWindow(double remaining) => QuotaWindow.Create(
        "window",
        QuotaType.Rolling5Hour,
        QuotaUnit.Count,
        usedValue: 100 - remaining,
        remainingValue: remaining,
        limitValue: 100,
        usedPercentage: null,
        remainingPercentage: null,
        windowStart: null,
        resetAt: null,
        DataSource.OfficialApi,
        ConfidenceLevel.Official,
        DateTimeOffset.UtcNow);

    private sealed class FakeProvider : IAiUsageProvider
    {
        public FakeProvider(ProviderCode code) => Code = code;

        public ProviderCode Code { get; }

        public int RefreshCount { get; private set; }

        public int MaxConcurrentRefreshes { get; private set; }

        private int _activeRefreshes;

        public bool ThrowOnRefresh { get; init; }

        public ProviderRefreshResult? Result { get; init; }

        public Task<ProviderDetectionResult> DetectAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProviderDetectionResult(Code, false, "test", DateTimeOffset.UtcNow));

        public Task<ProviderConnectionStatus> GetConnectionStatusAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ProviderConnectionStatus.NotConfigured);

        public Task<ProviderAccount?> GetAccountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<ProviderAccount?>(null);

        public Task<Subscription?> GetSubscriptionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<Subscription?>(null);

        public Task<IReadOnlyList<QuotaWindow>> GetQuotasAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<QuotaWindow>>([]);

        public async Task<ProviderRefreshResult> RefreshAsync(CancellationToken cancellationToken = default)
        {
            RefreshCount++;
            var active = Interlocked.Increment(ref _activeRefreshes);
            MaxConcurrentRefreshes = Math.Max(MaxConcurrentRefreshes, active);
            try
            {
                await Task.Delay(20, cancellationToken);
                if (ThrowOnRefresh)
                {
                    throw new InvalidOperationException("synthetic provider failure");
                }

                return Result ?? ProviderRefreshResult.Unsupported(Code, DateTimeOffset.UtcNow);
            }
            finally
            {
                Interlocked.Decrement(ref _activeRefreshes);
            }
        }
    }

    private sealed class FakeRegistry : IProviderRegistry
    {
        private readonly List<IAiUsageProvider> _providers;

        public FakeRegistry(IEnumerable<IAiUsageProvider> providers) => _providers = providers.ToList();

        public IReadOnlyList<IAiUsageProvider> GetAll() => _providers;

        public IAiUsageProvider? Find(ProviderCode code) => _providers.FirstOrDefault(provider => provider.Code == code);

    }

    private sealed class FakeConnectionService : IProviderConnectionService
    {
        public ProviderConnection? Connection { get; init; }

        public int RetrieveCount { get; private set; }

        public Task<ProviderConnection?> GetAsync(ProviderCode code, CancellationToken cancellationToken = default) =>
            Task.FromResult(Connection);

        public Task<IReadOnlyList<ProviderConnection>> LoadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProviderConnection>>(Connection is null ? [] : [Connection]);

        public Task<ProviderConnection> SaveAsync(ProviderConnectionEdit edit, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
