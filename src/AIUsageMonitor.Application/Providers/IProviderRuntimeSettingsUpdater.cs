using AIUsageMonitor.Domain.Providers;

namespace AIUsageMonitor.Application.Providers;

/// <summary>
/// Provider-neutral seam used by connection persistence to publish a complete settings edit to
/// the live provider graph. The Providers project maps the values to typed immutable snapshots.
/// </summary>
public interface IProviderRuntimeSettingsUpdater
{
    void Apply(
        ProviderCode code,
        string? credentialReference,
        IReadOnlyDictionary<string, string?> configuration);
}
