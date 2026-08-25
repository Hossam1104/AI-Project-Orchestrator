namespace AIUsageMonitor.Application.Agents;

/// <summary>
/// Explicit entitlement truth. Subscription or quota records must not be used to infer this
/// value for a model, API, CLI, or SDK access path.
/// </summary>
public enum AgentEntitlementState
{
    Unknown,
    VerifiedAvailable,
    VerifiedUnavailable,
    NotApplicable
}
