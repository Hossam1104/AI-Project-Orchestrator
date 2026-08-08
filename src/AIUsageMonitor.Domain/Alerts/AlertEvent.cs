namespace AIUsageMonitor.Domain.Alerts;

public sealed class AlertEvent
{
    public Guid Id { get; }
    public Guid AlertRuleId { get; }
    public DateTimeOffset TriggeredAt { get; }
    public DateTimeOffset? ResolvedAt { get; }
    public AlertType Type { get; }
    public AlertSeverity Severity { get; }
    public double? Value { get; }
    public string? Message { get; }

    public AlertEvent(
        Guid id,
        Guid alertRuleId,
        DateTimeOffset triggeredAt,
        DateTimeOffset? resolvedAt,
        AlertType type,
        AlertSeverity severity,
        double? value,
        string? message)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Alert event id cannot be empty.", nameof(id));
        }

        if (alertRuleId == Guid.Empty)
        {
            throw new ArgumentException("Alert rule id cannot be empty.", nameof(alertRuleId));
        }

        if (resolvedAt.HasValue && resolvedAt.Value < triggeredAt)
        {
            throw new ArgumentException("Resolved time cannot precede triggered time.", nameof(resolvedAt));
        }

        Id = id;
        AlertRuleId = alertRuleId;
        TriggeredAt = triggeredAt;
        ResolvedAt = resolvedAt;
        Type = type;
        Severity = severity;
        Value = value;
        Message = message;
    }
}
