using AIUsageMonitor.Application.Providers;
using AIUsageMonitor.Domain.Common;
using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Quotas;

namespace AIUsageMonitor.Provider.Tests;

public class ProviderRefreshResultTests
{
    private static readonly DateTimeOffset CompletedAt = new(2026, 8, 8, 12, 0, 0, TimeSpan.Zero);

    private static QuotaWindow SampleQuotaWindow() =>
        QuotaWindow.Create(
            "weekly", QuotaType.Weekly, QuotaUnit.Percentage,
            usedValue: 40, remainingValue: null, limitValue: 100,
            usedPercentage: null, remainingPercentage: null,
            windowStart: null, resetAt: null,
            DataSource.OfficialApi, ConfidenceLevel.Official, CompletedAt);

    [Fact]
    public void Failed_WithLastKnownQuotaWindows_RetainsThemAndBecomesStale()
    {
        var lastKnown = new[] { SampleQuotaWindow() };

        var result = ProviderRefreshResult.Failed(
            ProviderCode.Claude, "timeout", "Request timed out.", CompletedAt,
            lastKnownQuotaWindows: lastKnown);

        Assert.Equal(ProviderRefreshOutcome.Stale, result.Outcome);
        Assert.Same(lastKnown, result.QuotaWindows);
        Assert.NotEmpty(result.QuotaWindows);
    }

    [Fact]
    public void Failed_WithoutLastKnownData_IsProviderErrorWithNoFabricatedData()
    {
        var result = ProviderRefreshResult.Failed(
            ProviderCode.Codex, "network_unreachable", "Could not reach the network.", CompletedAt);

        Assert.Equal(ProviderRefreshOutcome.ProviderError, result.Outcome);
        Assert.Empty(result.QuotaWindows);
        Assert.Null(result.Account);
        Assert.Null(result.Subscription);
    }

    [Fact]
    public void Failed_AlwaysRetainsErrorCodeAndMessage()
    {
        var withData = ProviderRefreshResult.Failed(
            ProviderCode.Kimi, "timeout", "Request timed out.", CompletedAt,
            lastKnownQuotaWindows: new[] { SampleQuotaWindow() });

        var withoutData = ProviderRefreshResult.Failed(
            ProviderCode.Kimi, "timeout", "Request timed out.", CompletedAt);

        Assert.Equal("timeout", withData.ErrorCode);
        Assert.Equal("Request timed out.", withData.ErrorMessage);
        Assert.Equal("timeout", withoutData.ErrorCode);
        Assert.Equal("Request timed out.", withoutData.ErrorMessage);
    }

    [Fact]
    public void Failed_WithData_IsDistinguishableFromPlainStale()
    {
        // Plain Stale() (periodic staleness, no active error) has no error code; a Failed()
        // result with retained data does — that's the distinguishing signal between the two
        // "stale" paths, per the Session 02R remediation.
        var plainStale = ProviderRefreshResult.Stale(
            ProviderCode.Copilot, null, null, new[] { SampleQuotaWindow() }, CompletedAt);

        var failedButRecovered = ProviderRefreshResult.Failed(
            ProviderCode.Copilot, "timeout", "Request timed out.", CompletedAt,
            lastKnownQuotaWindows: new[] { SampleQuotaWindow() });

        Assert.Equal(ProviderRefreshOutcome.Stale, plainStale.Outcome);
        Assert.Null(plainStale.ErrorCode);

        Assert.Equal(ProviderRefreshOutcome.Stale, failedButRecovered.Outcome);
        Assert.NotNull(failedButRecovered.ErrorCode);
    }

    [Fact]
    public void AuthenticationRequired_HasNoData()
    {
        var result = ProviderRefreshResult.AuthenticationRequired(ProviderCode.Antigravity, CompletedAt);

        Assert.Equal(ProviderRefreshOutcome.AuthenticationRequired, result.Outcome);
        Assert.Empty(result.QuotaWindows);
    }

    [Fact]
    public void Unsupported_HasNoData()
    {
        var result = ProviderRefreshResult.Unsupported(ProviderCode.Antigravity, CompletedAt);

        Assert.Equal(ProviderRefreshOutcome.Unsupported, result.Outcome);
        Assert.Empty(result.QuotaWindows);
    }
}
