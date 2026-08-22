using AIUsageMonitor.Application.Time;
using AIUsageMonitor.Providers.Common;

namespace AIUsageMonitor.Providers.Antigravity;

/// <summary>
/// Antigravity documents quota through the interactive /usage (/quota) TUI. Detection is safe,
/// but quota acquisition remains unsupported/manual without a documented structured interface.
/// </summary>
public sealed class AntigravityProvider : UnsupportedCliProviderBase
{
    public AntigravityProvider(IClock clock, IExecutableLocator executableLocator)
        : base(clock, executableLocator, "agy")
    {
    }

    public override AIUsageMonitor.Domain.Providers.ProviderCode Code =>
        AIUsageMonitor.Domain.Providers.ProviderCode.Antigravity;
}
