namespace AIUsageMonitor.Domain.Providers;

public enum ProviderConnectionStatus
{
    NotConfigured,
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
