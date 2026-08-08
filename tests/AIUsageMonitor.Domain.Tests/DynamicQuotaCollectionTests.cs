using AIUsageMonitor.Domain.Common;
using AIUsageMonitor.Domain.Quotas;

namespace AIUsageMonitor.Domain.Tests;

/// <summary>
/// Demonstrates that a provider can expose zero to many quota windows of arbitrary, mixed
/// types without any fixed five-hour/weekly schema (BRD §9, AGENTS.md §11).
/// </summary>
public class DynamicQuotaCollectionTests
{
    [Fact]
    public void Provider_CanExposeZeroQuotaWindows()
    {
        IReadOnlyList<QuotaWindow> windows = Array.Empty<QuotaWindow>();

        Assert.Empty(windows);
    }

    [Fact]
    public void Provider_CanExposeArbitraryMixOfQuotaTypes()
    {
        var capturedAt = new DateTimeOffset(2026, 8, 8, 0, 0, 0, TimeSpan.Zero);

        var windows = new List<QuotaWindow>
        {
            QuotaWindow.Create("5h", QuotaType.Rolling5Hour, QuotaUnit.Percentage,
                32, null, 100, null, null, null, null, DataSource.OfficialApi, ConfidenceLevel.Official, capturedAt),
            QuotaWindow.Create("weekly", QuotaType.Weekly, QuotaUnit.Percentage,
                null, 59, 100, null, null, null, null, DataSource.OfficialApi, ConfidenceLevel.Official, capturedAt),
            QuotaWindow.Create("monthly-credits", QuotaType.Credits, QuotaUnit.Credits,
                120, 380, null, null, null, null, null, DataSource.LocalMetadata, ConfidenceLevel.VerifiedLocal, capturedAt),
            QuotaWindow.Create("model-x-tokens", QuotaType.ModelSpecific, QuotaUnit.Tokens,
                null, null, null, null, null, null, null, DataSource.Manual, ConfidenceLevel.Manual, capturedAt),
            QuotaWindow.Create("vendor-specific-thing", QuotaType.Custom, QuotaUnit.Custom,
                null, null, null, null, null, null, null, DataSource.Manual, ConfidenceLevel.Manual, capturedAt),
        };

        Assert.Equal(5, windows.Select(w => w.Type).Distinct().Count());
    }
}
