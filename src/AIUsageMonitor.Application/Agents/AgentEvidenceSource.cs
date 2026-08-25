namespace AIUsageMonitor.Application.Agents;

/// <summary>
/// Provider-independent category of evidence behind a connection result.
/// </summary>
public enum AgentEvidenceSource
{
    Unknown,
    ManualVerification,
    OfficialApi,
    OfficialCli,
    SupportedSdk,
    VerifiedLocalEvidence,
    InteractiveManualState
}
