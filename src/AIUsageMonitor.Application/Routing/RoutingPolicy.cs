using AIUsageMonitor.Application.Common;

namespace AIUsageMonitor.Application.Routing;

/// <summary>
/// Persisted routing/safety policy values. A project override may leave values unset so a future
/// routing engine can resolve them against the global policy; APO-27 does not execute the policy.
/// </summary>
public sealed class RoutingPolicy
{
    public RoutingPolicy(
        bool? qualityRiskFirst,
        bool? requireIndependentReviewForHighRisk,
        bool? requireHumanApprovalForHighRisk,
        int? maxConcurrentRuns,
        int? maxRetries,
        int? maxReviewRemediationCycles,
        DateTimeOffset updatedAt,
        IReadOnlyDictionary<string, string?>? rules = null)
    {
        ValidateNonNegative(maxConcurrentRuns, nameof(maxConcurrentRuns));
        ValidateNonNegative(maxRetries, nameof(maxRetries));
        ValidateNonNegative(maxReviewRemediationCycles, nameof(maxReviewRemediationCycles));

        QualityRiskFirst = qualityRiskFirst;
        RequireIndependentReviewForHighRisk = requireIndependentReviewForHighRisk;
        RequireHumanApprovalForHighRisk = requireHumanApprovalForHighRisk;
        MaxConcurrentRuns = maxConcurrentRuns;
        MaxRetries = maxRetries;
        MaxReviewRemediationCycles = maxReviewRemediationCycles;
        Rules = MetadataValidation.Copy(rules);
        UpdatedAt = updatedAt;
    }

    public bool? QualityRiskFirst { get; }

    public bool? RequireIndependentReviewForHighRisk { get; }

    public bool? RequireHumanApprovalForHighRisk { get; }

    public int? MaxConcurrentRuns { get; }

    public int? MaxRetries { get; }

    public int? MaxReviewRemediationCycles { get; }

    public IReadOnlyDictionary<string, string?> Rules { get; }

    public DateTimeOffset UpdatedAt { get; }

    private static void ValidateNonNegative(int? value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Policy limits cannot be negative.");
        }
    }
}
