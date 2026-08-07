using AIUsageMonitor.Domain.Providers;

namespace AIUsageMonitor.Domain.Tests;

public class ProviderConnectionTests
{
    [Fact]
    public void Constructor_WithSuccessfulSyncAfterLastAttempt_Throws()
    {
        var attempt = new DateTimeOffset(2026, 8, 8, 10, 0, 0, TimeSpan.Zero);
        var success = attempt.AddMinutes(5);

        Assert.Throws<ArgumentException>(() => new ProviderConnection(
            Guid.NewGuid(), Guid.NewGuid(), ProviderConnectionType.OfficialApi, ProviderConnectionStatus.Connected,
            null, lastSuccessfulSync: success, lastAttempt: attempt, null, null));
    }
}
