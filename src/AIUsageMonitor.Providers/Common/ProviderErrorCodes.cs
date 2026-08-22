namespace AIUsageMonitor.Providers.Common;

internal static class ProviderErrorCodes
{
    public const string AuthenticationRequired = "authentication_required";
    public const string PermissionDenied = "permission_denied";
    public const string UnsupportedScope = "unsupported_scope";
    public const string RateLimited = "rate_limited";
    public const string Timeout = "timeout";
    public const string Network = "network_error";
    public const string MalformedResponse = "malformed_response";
    public const string ProviderServerError = "provider_server_error";
    public const string Cancelled = "cancelled";
    public const string ProviderError = "provider_error";
}
