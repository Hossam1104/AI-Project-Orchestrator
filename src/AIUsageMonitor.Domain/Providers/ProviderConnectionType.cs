namespace AIUsageMonitor.Domain.Providers;

/// <summary>
/// How the application is connected to a provider, independent of current <see cref="ProviderConnectionStatus"/>.
/// </summary>
public enum ProviderConnectionType
{
    OfficialApi,
    OAuth,
    OfficialCli,
    LocalMetadata,
    Manual,
    Unknown
}
