namespace AIUsageMonitor.Application.Routing;

public enum RoutingCapacityState
{
    Sufficient,
    Constrained,
    Insufficient,
    Unknown,
    Stale,
    NotMapped,
    NotApplicable
}

public enum RoutingCapacityEvidenceSource
{
    DurableUsageSnapshot,
    Manual,
    OfficialApi,
    OfficialCli,
    LocalMetadata,
    Other
}

/// <summary>
/// One exact agent-bound capacity observation. Raw remaining values are evidence only; the
/// routing engine ranks the normalized state and never compares raw percentages across providers.
/// </summary>
public sealed class RoutingCapacityEvidence
{
    public RoutingCapacityEvidence(
        Guid agentId,
        RoutingCapacityState capacityState,
        DateTimeOffset observedAt,
        DateTimeOffset? validUntil = null,
        string? evidenceReference = null,
        Guid? providerId = null,
        Guid? quotaDefinitionId = null,
        double? remainingFraction = null,
        RoutingCapacityEvidenceSource source = RoutingCapacityEvidenceSource.DurableUsageSnapshot)
    {
        if (agentId == Guid.Empty)
        {
            throw new ArgumentException("Capacity evidence agent id cannot be empty.", nameof(agentId));
        }

        if (!Enum.IsDefined(capacityState))
        {
            throw new ArgumentException("Capacity state is undefined.", nameof(capacityState));
        }

        if (!Enum.IsDefined(source))
        {
            throw new ArgumentException("Capacity evidence source is undefined.", nameof(source));
        }

        if (observedAt == default)
        {
            throw new ArgumentException("Capacity evidence observed time is required.", nameof(observedAt));
        }

        if (validUntil is not null && validUntil.Value < observedAt)
        {
            throw new ArgumentException("Capacity evidence validity cannot precede observation.", nameof(validUntil));
        }

        if (providerId == Guid.Empty)
        {
            throw new ArgumentException("Provider id cannot be empty when supplied.", nameof(providerId));
        }

        if (quotaDefinitionId == Guid.Empty)
        {
            throw new ArgumentException("Quota definition id cannot be empty when supplied.", nameof(quotaDefinitionId));
        }

        if (remainingFraction is < 0 or > 1 || (remainingFraction is not null && !double.IsFinite(remainingFraction.Value)))
        {
            throw new ArgumentOutOfRangeException(nameof(remainingFraction), remainingFraction, "Remaining fraction must be finite and between zero and one.");
        }

        AgentId = agentId;
        CapacityState = capacityState;
        ObservedAt = observedAt;
        ValidUntil = validUntil;
        EvidenceReference = NormalizeOptional(evidenceReference, nameof(evidenceReference), 400);
        ProviderId = providerId;
        QuotaDefinitionId = quotaDefinitionId;
        RemainingFraction = remainingFraction;
        Source = source;
    }

    public Guid AgentId { get; }
    public RoutingCapacityState CapacityState { get; }
    public DateTimeOffset ObservedAt { get; }
    public DateTimeOffset? ValidUntil { get; }
    public string? EvidenceReference { get; }
    public Guid? ProviderId { get; }
    public Guid? QuotaDefinitionId { get; }
    public double? RemainingFraction { get; }
    public RoutingCapacityEvidenceSource Source { get; }

    /// <summary>
    /// Returns the capacity state that was authoritative at the supplied evaluation time.
    /// Every state expires at the same inclusive validity boundary; an expired insufficiency
    /// must not remain authoritative indefinitely.
    /// </summary>
    public RoutingCapacityState GetStateAt(DateTimeOffset evaluationTime) =>
        ValidUntil is not null && ValidUntil.Value <= evaluationTime
            ? RoutingCapacityState.Stale
            : CapacityState;

    private static string? NormalizeOptional(string? value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException($"The value cannot exceed {maximumLength} characters.", parameterName);
        }

        return normalized;
    }
}
