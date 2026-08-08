namespace AIUsageMonitor.Application.Providers;

/// <summary>
/// The normalized outcome of a provider refresh attempt. UI/orchestration code must branch
/// on this instead of inspecting provider-specific exceptions or payloads.
/// </summary>
public enum ProviderRefreshOutcome
{
    Success,
    Partial,
    AuthenticationRequired,
    Unsupported,
    Stale,
    ProviderError
}
