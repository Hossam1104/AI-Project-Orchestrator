namespace AIUsageMonitor.Application.Routing;

/// <summary>Auditable owner request that can change soft ranking only.</summary>
public sealed class RoutingOwnerOverrideRequest
{
    public RoutingOwnerOverrideRequest(
        Guid requestedAgentId,
        string actorReference,
        string reason,
        DateTimeOffset requestedAt)
    {
        if (requestedAgentId == Guid.Empty)
        {
            throw new ArgumentException("Requested agent id cannot be empty.", nameof(requestedAgentId));
        }

        ActorReference = Required(actorReference, nameof(actorReference), 300);
        Reason = Required(reason, nameof(reason), 1_000);
        if (requestedAt == default)
        {
            throw new ArgumentException("Owner override request time is required.", nameof(requestedAt));
        }

        RequestedAgentId = requestedAgentId;
        RequestedAt = requestedAt;
    }

    public Guid RequestedAgentId { get; }
    public string ActorReference { get; }
    public string Reason { get; }
    public DateTimeOffset RequestedAt { get; }

    public RoutingOwnerOverrideRequest WithReason(string reason) =>
        new(RequestedAgentId, ActorReference, reason, RequestedAt);

    private static string Required(string value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A bounded owner override value is required.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException($"The value cannot exceed {maximumLength} characters.", parameterName);
        }

        return normalized;
    }
}
