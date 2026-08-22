namespace AIUsageMonitor.Providers.Kimi;

/// <summary>
/// Kimi Code local-server configuration. The server address must be loopback-only because the
/// bearer token retrieved through the opaque secure-store reference must never be sent remotely.
/// The token is never part of this options object or persisted application state.
/// </summary>
public sealed class KimiOptions
{
    public string? CredentialReference { get; init; }
    public Uri ServerAddress { get; init; } = new("http://127.0.0.1:58627/");
}
