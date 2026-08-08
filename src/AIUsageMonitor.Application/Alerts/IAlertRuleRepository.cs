using AIUsageMonitor.Domain.Alerts;

namespace AIUsageMonitor.Application.Alerts;

/// <summary>
/// Persistence for configured alert rules (BRD §30 AlertRules, §27 default thresholds).
/// </summary>
public interface IAlertRuleRepository
{
    Task<IReadOnlyList<AlertRule>> GetByProviderIdAsync(Guid providerId, CancellationToken cancellationToken = default);

    Task UpsertAsync(AlertRule rule, CancellationToken cancellationToken = default);
}
