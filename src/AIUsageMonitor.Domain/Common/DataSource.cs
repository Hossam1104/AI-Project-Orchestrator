namespace AIUsageMonitor.Domain.Common;

/// <summary>
/// How a piece of provider data was obtained, per the BRD's provider data source priority.
/// </summary>
public enum DataSource
{
    OfficialApi,
    OfficialOAuth,
    OfficialCli,
    LocalMetadata,
    ReadOnlyEndpoint,
    Manual
}
