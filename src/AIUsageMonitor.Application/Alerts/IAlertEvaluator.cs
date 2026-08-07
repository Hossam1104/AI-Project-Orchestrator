using AIUsageMonitor.Domain.Alerts;
using AIUsageMonitor.Domain.Quotas;

namespace AIUsageMonitor.Application.Alerts;

/// <summary>
/// Evaluates one alert rule against the current (and, for restore/reset detection, previous)
/// quota reading, producing zero or more alert events. Must not re-raise an event every
/// refresh cycle for a state that hasn't changed (BRD §27).
/// </summary>
public interface IAlertEvaluator
{
    IReadOnlyList<AlertEvent> Evaluate(
        AlertRule rule, QuotaWindow currentQuota, QuotaWindow? previousQuota, DateTimeOffset evaluatedAt);
}
