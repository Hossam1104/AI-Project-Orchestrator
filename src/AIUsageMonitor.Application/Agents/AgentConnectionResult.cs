namespace AIUsageMonitor.Application.Agents;

/// <summary>
/// A provider-independent, bounded record of one evaluated access path. It records evidence
/// truth only; creating a result does not probe or invoke a provider.
/// </summary>
public sealed class AgentConnectionResult
{
    public AgentConnectionResult(
        AgentIdentity identity,
        DateTimeOffset testedAt,
        AgentConnectionMode evaluatedConnectionMode,
        AgentAvailability availability,
        AgentAuthenticationState authenticationState,
        AgentEntitlementState entitlementState,
        AgentEvidenceSource evidenceSource,
        string? limitationCode = null,
        string? message = null,
        IReadOnlyList<AgentConnectionMode>? supportedConnectionModes = null)
    {
        ArgumentNullException.ThrowIfNull(identity);

        if (testedAt == DateTimeOffset.MinValue)
        {
            throw new ArgumentException("Connection result timestamp is required.", nameof(testedAt));
        }

        ValidateEnum(evaluatedConnectionMode, nameof(evaluatedConnectionMode));
        ValidateEnum(availability, nameof(availability));
        ValidateEnum(authenticationState, nameof(authenticationState));
        ValidateEnum(entitlementState, nameof(entitlementState));
        ValidateEnum(evidenceSource, nameof(evidenceSource));

        var modes = AgentContractValidation.CopyDistinctEnums(
            supportedConnectionModes,
            nameof(supportedConnectionModes));
        AgentContractValidation.RejectUnverifiedOrUnsupportedSupportedModes(
            modes,
            nameof(supportedConnectionModes));

        if (availability == AgentAvailability.Available &&
            evaluatedConnectionMode == AgentConnectionMode.Unsupported)
        {
            throw new ArgumentException(
                "Unsupported invocation mode cannot produce an available connection result.",
                nameof(evaluatedConnectionMode));
        }

        if (availability == AgentAvailability.AuthenticationRequired &&
            authenticationState == AgentAuthenticationState.Authenticated)
        {
            throw new ArgumentException(
                "Authentication-required availability cannot be presented as authenticated.",
                nameof(authenticationState));
        }

        Identity = identity;
        TestedAt = testedAt;
        EvaluatedConnectionMode = evaluatedConnectionMode;
        Availability = availability;
        AuthenticationState = authenticationState;
        EntitlementState = entitlementState;
        EvidenceSource = evidenceSource;
        LimitationCode = AgentContractValidation.NormalizeOptional(limitationCode, nameof(limitationCode), 120);
        Message = AgentContractValidation.NormalizeOptional(message, nameof(message), 500);
        SupportedConnectionModes = modes;
    }

    public AgentIdentity Identity { get; }

    public DateTimeOffset TestedAt { get; }

    public AgentConnectionMode EvaluatedConnectionMode { get; }

    public AgentAvailability Availability { get; }

    public AgentAuthenticationState AuthenticationState { get; }

    public AgentEntitlementState EntitlementState { get; }

    public AgentEvidenceSource EvidenceSource { get; }

    public string? LimitationCode { get; }

    public string? Message { get; }

    public IReadOnlyList<AgentConnectionMode> SupportedConnectionModes { get; }

    private static void ValidateEnum<T>(T value, string parameterName)
        where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentException("The value is undefined.", parameterName);
        }
    }
}
