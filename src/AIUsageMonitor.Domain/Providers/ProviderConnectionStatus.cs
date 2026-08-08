namespace AIUsageMonitor.Domain.Providers;

public enum ProviderConnectionStatus
{
    Connected,
    LocalDetected,
    AuthenticationRequired,
    Partial,
    Unsupported,
    Disabled,
    RateLimited,
    Stale,
    Error,
    Updating
}
