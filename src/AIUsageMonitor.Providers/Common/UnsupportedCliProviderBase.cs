using AIUsageMonitor.Application.Providers;
using AIUsageMonitor.Application.Time;
using AIUsageMonitor.Domain.Providers;

namespace AIUsageMonitor.Providers.Common;

public abstract class UnsupportedCliProviderBase : ProviderAdapterBase
{
    private readonly IExecutableLocator _executableLocator;
    private readonly string _executableName;

    protected UnsupportedCliProviderBase(IClock clock, IExecutableLocator executableLocator, string executableName)
        : base(clock)
    {
        _executableLocator = executableLocator ?? throw new ArgumentNullException(nameof(executableLocator));
        _executableName = executableName;
    }

    public override Task<ProviderDetectionResult> DetectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var detected = _executableLocator.Find(_executableName) is not null;
        return Task.FromResult(new ProviderDetectionResult(
            Code,
            detected,
            detected ? $"Official {_executableName} CLI detected." : "No documented machine-readable capacity surface detected.",
            UtcNow));
    }

    public override async Task<ProviderConnectionStatus> GetConnectionStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var detection = await DetectAsync(cancellationToken).ConfigureAwait(false);
        return detection.IsDetected ? ProviderConnectionStatus.LocalDetected : ProviderConnectionStatus.Unsupported;
    }

    protected override Task<ProviderRefreshResult> RefreshCoreAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Unsupported());
}
