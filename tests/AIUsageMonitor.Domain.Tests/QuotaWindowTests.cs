using AIUsageMonitor.Domain.Common;
using AIUsageMonitor.Domain.Quotas;

namespace AIUsageMonitor.Domain.Tests;

public class QuotaWindowTests
{
    private static readonly DateTimeOffset CapturedAt = new(2026, 8, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithUsedValueAndLimit_ComputesBothPercentages()
    {
        var window = QuotaWindow.Create(
            "session", QuotaType.Rolling5Hour, QuotaUnit.Percentage,
            usedValue: 30, remainingValue: null, limitValue: 100,
            usedPercentage: null, remainingPercentage: null,
            windowStart: null, resetAt: null,
            DataSource.OfficialApi, ConfidenceLevel.Official, CapturedAt);

        Assert.Equal(30, window.UsedPercentage);
        Assert.Equal(70, window.RemainingPercentage);
    }

    [Fact]
    public void Create_WithRemainingValueAndLimit_ComputesBothPercentages()
    {
        var window = QuotaWindow.Create(
            "weekly", QuotaType.Weekly, QuotaUnit.Percentage,
            usedValue: null, remainingValue: 41, limitValue: 100,
            usedPercentage: null, remainingPercentage: null,
            windowStart: null, resetAt: null,
            DataSource.OfficialApi, ConfidenceLevel.Official, CapturedAt);

        Assert.Equal(59, window.UsedPercentage);
        Assert.Equal(41, window.RemainingPercentage);
    }

    [Fact]
    public void Create_WithUsedAndRemainingOnly_DerivesPercentagesFromImpliedTotal()
    {
        var window = QuotaWindow.Create(
            "credits", QuotaType.Credits, QuotaUnit.Credits,
            usedValue: 25, remainingValue: 75, limitValue: null,
            usedPercentage: null, remainingPercentage: null,
            windowStart: null, resetAt: null,
            DataSource.LocalMetadata, ConfidenceLevel.VerifiedLocal, CapturedAt);

        Assert.Equal(25, window.UsedPercentage);
        Assert.Equal(75, window.RemainingPercentage);
    }

    [Fact]
    public void Create_WithConsistentExplicitPercentages_Succeeds()
    {
        var window = QuotaWindow.Create(
            "weekly", QuotaType.Weekly, QuotaUnit.Percentage,
            null, null, null, usedPercentage: 59, remainingPercentage: 41,
            null, null, DataSource.OfficialApi, ConfidenceLevel.Official, CapturedAt);

        Assert.Equal(59, window.UsedPercentage);
        Assert.Equal(41, window.RemainingPercentage);
    }

    [Fact]
    public void Create_WithInconsistentExplicitPercentages_Throws()
    {
        Assert.Throws<ArgumentException>(() => QuotaWindow.Create(
            "weekly", QuotaType.Weekly, QuotaUnit.Percentage,
            null, null, null, usedPercentage: 80, remainingPercentage: 80,
            null, null, DataSource.OfficialApi, ConfidenceLevel.Official, CapturedAt));
    }

    [Fact]
    public void Create_NeverInterpretsUsedPercentageAsRemainingPercentage()
    {
        // AGENTS.md §8: used_percent = 80 must never be treated as 80% available.
        var window = QuotaWindow.Create(
            "weekly", QuotaType.Weekly, QuotaUnit.Percentage,
            null, null, null, usedPercentage: 80, remainingPercentage: null,
            null, null, DataSource.OfficialApi, ConfidenceLevel.Official, CapturedAt);

        Assert.Equal(80, window.UsedPercentage);
        Assert.Equal(20, window.RemainingPercentage);
    }

    [Fact]
    public void Create_WithNoUsageData_LeavesPercentagesNullRatherThanInventingThem()
    {
        var window = QuotaWindow.Create(
            "unsupported", QuotaType.Custom, QuotaUnit.Custom,
            null, null, null, null, null,
            null, null, DataSource.Manual, ConfidenceLevel.Manual, CapturedAt);

        Assert.Null(window.UsedPercentage);
        Assert.Null(window.RemainingPercentage);
    }

    [Fact]
    public void Create_WithNegativeUsedValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => QuotaWindow.Create(
            "session", QuotaType.Session, QuotaUnit.Count,
            usedValue: -1, remainingValue: null, limitValue: 100,
            null, null, null, null,
            DataSource.OfficialApi, ConfidenceLevel.Official, CapturedAt));
    }

    [Fact]
    public void Create_WithResetBeforeWindowStart_Throws()
    {
        var start = CapturedAt;
        var reset = CapturedAt.AddHours(-1);

        Assert.Throws<ArgumentException>(() => QuotaWindow.Create(
            "session", QuotaType.Session, QuotaUnit.Count,
            null, null, null, null, null,
            windowStart: start, resetAt: reset,
            DataSource.OfficialApi, ConfidenceLevel.Official, CapturedAt));
    }

    [Fact]
    public void Create_PreservesNonUtcSourceOffsetOnResetAt()
    {
        // AGENTS.md §9: preserve the provider's source offset rather than normalizing to UTC.
        var reset = new DateTimeOffset(2026, 8, 9, 2, 0, 0, TimeSpan.FromHours(-7));

        var window = QuotaWindow.Create(
            "weekly", QuotaType.Weekly, QuotaUnit.Percentage,
            null, null, null, null, null,
            windowStart: null, resetAt: reset,
            DataSource.OfficialApi, ConfidenceLevel.Official, CapturedAt);

        Assert.Equal(TimeSpan.FromHours(-7), window.ResetAt!.Value.Offset);
        Assert.Equal(reset, window.ResetAt);
    }

    [Fact]
    public void Create_RequiresNonEmptyExternalKey()
    {
        Assert.Throws<ArgumentException>(() => QuotaWindow.Create(
            "", QuotaType.Custom, QuotaUnit.Custom,
            null, null, null, null, null, null, null,
            DataSource.Manual, ConfidenceLevel.Manual, CapturedAt));
    }
}
