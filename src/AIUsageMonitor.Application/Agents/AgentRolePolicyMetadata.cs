namespace AIUsageMonitor.Application.Agents;

/// <summary>
/// Non-executable owner-approved metadata describing how a role is normally used. Preference
/// labels such as Primary, Exceptional, Periodic, or RiskTriggered are intentionally descriptive
/// and are not routing instructions.
/// </summary>
public sealed class AgentRolePolicyMetadata
{
    public AgentRolePolicyMetadata(
        AgentRole role,
        string usageDescription,
        string? preferenceLabel = null)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentException("Agent role is undefined.", nameof(role));
        }

        if (string.IsNullOrWhiteSpace(usageDescription))
        {
            throw new ArgumentException("Role usage description is required.", nameof(usageDescription));
        }

        Role = role;
        UsageDescription = usageDescription.Trim();
        PreferenceLabel = AgentContractValidation.NormalizeOptional(preferenceLabel, nameof(preferenceLabel), 120);
        if (UsageDescription.Length > 500)
        {
            throw new ArgumentException("Role usage description cannot exceed 500 characters.", nameof(usageDescription));
        }
    }

    public AgentRole Role { get; }

    public string UsageDescription { get; }

    public string? PreferenceLabel { get; }
}
