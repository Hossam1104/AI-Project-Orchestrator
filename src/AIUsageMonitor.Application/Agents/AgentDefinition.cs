using AIUsageMonitor.Application.Common;

namespace AIUsageMonitor.Application.Agents;

/// <summary>
/// Small-state registry entry for an AI agent/model. Connection metadata is descriptive only;
/// credentials and authenticated provider payloads never belong here.
/// </summary>
public sealed class AgentDefinition
{
    public AgentDefinition(
        Guid id,
        string name,
        string role,
        AgentConnectionMode connectionMode,
        AgentAvailability availability,
        bool enabled,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string? provider = null,
        IReadOnlyList<string>? capabilities = null,
        IReadOnlyList<string>? limitations = null,
        IReadOnlyDictionary<string, string?>? costAndQuotaMetadata = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Agent name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Agent role is required.", nameof(role));
        }

        if (updatedAt < createdAt)
        {
            throw new ArgumentException("Agent UpdatedAt cannot precede CreatedAt.", nameof(updatedAt));
        }

        if (!Enum.IsDefined(typeof(AgentConnectionMode), connectionMode))
        {
            throw new ArgumentException("Agent connection mode is undefined.", nameof(connectionMode));
        }

        if (!Enum.IsDefined(typeof(AgentAvailability), availability))
        {
            throw new ArgumentException("Agent availability is undefined.", nameof(availability));
        }

        Id = id;
        Name = name.Trim();
        Role = role.Trim();
        Provider = NormalizeOptional(provider);
        ConnectionMode = connectionMode;
        Availability = availability;
        Enabled = enabled;
        Capabilities = CopyList(capabilities);
        Limitations = CopyList(limitations);
        CostAndQuotaMetadata = MetadataValidation.Copy(costAndQuotaMetadata);
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string Role { get; }

    public string? Provider { get; }

    public AgentConnectionMode ConnectionMode { get; }

    public AgentAvailability Availability { get; }

    public bool Enabled { get; }

    public IReadOnlyList<string> Capabilities { get; }

    public IReadOnlyList<string> Limitations { get; }

    public IReadOnlyDictionary<string, string?> CostAndQuotaMetadata { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IReadOnlyList<string> CopyList(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<string>();
        }

        var result = new List<string>(values.Count);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Agent capability and limitation values cannot be blank.", nameof(values));
            }

            result.Add(value.Trim());
        }

        return result.AsReadOnly();
    }
}
