using AIUsageMonitor.Domain.Alerts;

namespace AIUsageMonitor.Domain.Tests;

public class AlertRuleTests
{
    [Fact]
    public void Constructor_WithCriticalThresholdAboveWarningThreshold_Throws()
    {
        Assert.Throws<ArgumentException>(() => new AlertRule(
            Guid.NewGuid(), Guid.NewGuid(), null,
            warningThreshold: 30, criticalThreshold: 45, enabled: true));
    }

    [Fact]
    public void Constructor_WithDefaultBrdThresholds_Succeeds()
    {
        var rule = new AlertRule(Guid.NewGuid(), Guid.NewGuid(), null,
            warningThreshold: 30, criticalThreshold: 15, enabled: true);

        Assert.Equal(30, rule.WarningThreshold);
        Assert.Equal(15, rule.CriticalThreshold);
    }
}
