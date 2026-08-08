namespace AIUsageMonitor.Domain.Providers;

/// <summary>
/// The V1 provider set from the BRD. Adding a provider is an explicit planner-approved
/// scope change, not something Domain consumers should infer from strings.
/// </summary>
public enum ProviderCode
{
    Codex,
    Claude,
    Kimi,
    Copilot,
    Antigravity
}
