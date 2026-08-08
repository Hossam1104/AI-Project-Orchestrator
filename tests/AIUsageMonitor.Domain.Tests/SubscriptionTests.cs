using AIUsageMonitor.Domain.Common;
using AIUsageMonitor.Domain.Subscriptions;

namespace AIUsageMonitor.Domain.Tests;

public class SubscriptionTests
{
    [Fact]
    public void Constructor_WithBillingPeriodEndBeforeStart_Throws()
    {
        var start = new DateTimeOffset(2026, 8, 3, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddDays(-1);

        Assert.Throws<ArgumentException>(() => new Subscription(
            Guid.NewGuid(), Guid.NewGuid(), "Pro", null, start, end,
            null, null, null, null, null, null, DataSource.Manual, ConfidenceLevel.Manual, null));
    }

    [Fact]
    public void Constructor_WithPriceButNoCurrency_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Subscription(
            Guid.NewGuid(), Guid.NewGuid(), "Pro", null, null, null,
            null, null, null, 20m, null, null, DataSource.Manual, ConfidenceLevel.Manual, null));
    }

    [Fact]
    public void Constructor_RetainsConfidence()
    {
        var subscription = new Subscription(
            Guid.NewGuid(), Guid.NewGuid(), "Pro", null, null, null,
            null, null, null, null, null, null, DataSource.OfficialApi, ConfidenceLevel.Official, null);

        Assert.Equal(ConfidenceLevel.Official, subscription.Confidence);
    }
}
