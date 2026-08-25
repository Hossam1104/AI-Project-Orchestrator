namespace AIUsageMonitor.Application.Agents;

/// <summary>
/// Authentication truth for an agent/model access path. This never contains a credential.
/// </summary>
public enum AgentAuthenticationState
{
    Unknown,
    NotRequired,
    Authenticated,
    AuthenticationRequired
}
