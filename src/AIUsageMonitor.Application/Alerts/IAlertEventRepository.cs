using AIUsageMonitor.Domain.Alerts;

namespace AIUsageMonitor.Application.Alerts;

/// <summary>
/// Persistence for triggered alert history (BRD §30 AlertEvents). Used by
/// <see cref="IAlertEvaluator"/> callers to avoid re-raising an event every refresh cycle for
/// a state that hasn't changed (BRD §27).
/// </summary>
public interface IAlertEventRepository
{
    Task AddAsync(AlertEvent alertEvent, CancellationToken cancellationToken = default);

    Task<AlertEvent?> GetLatestUnresolvedAsync(Guid alertRuleId, AlertType type, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlertEvent>> GetHistoryAsync(Guid alertRuleId, int maxCount, CancellationToken cancellationToken = default);
}
