namespace AIUsageMonitor.Application.Agents;

/// <summary>
/// Stable, provider-independent identity metadata for a registered agent/model.
/// </summary>
public sealed class AgentIdentity
{
    public AgentIdentity(
        Guid id,
        string displayName,
        string? provider = null,
        string? modelIdentifier = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Agent identity id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Agent identity display name is required.", nameof(displayName));
        }

        Id = id;
        DisplayName = displayName.Trim();
        Provider = AgentContractValidation.NormalizeOptional(provider, nameof(provider), 200);
        ModelIdentifier = AgentContractValidation.NormalizeOptional(modelIdentifier, nameof(modelIdentifier), 300);
    }

    public Guid Id { get; }

    public string DisplayName { get; }

    public string? Provider { get; }

    public string? ModelIdentifier { get; }
}
