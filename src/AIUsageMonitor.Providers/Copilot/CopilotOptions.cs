namespace AIUsageMonitor.Providers.Copilot;

/// <summary>
/// Explicit GitHub billing scope. The credential reference is an opaque key into the secure
/// credential store, never the token itself.
/// </summary>
public sealed class CopilotOptions
{
    public string? CredentialReference { get; init; }
    public string? Username { get; init; }
    public string? Organization { get; init; }
    public string? Enterprise { get; init; }
    public CopilotBillingScope Scope { get; init; } = CopilotBillingScope.PersonalUser;
}
