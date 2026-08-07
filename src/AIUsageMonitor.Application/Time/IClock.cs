namespace AIUsageMonitor.Application.Time;

/// <summary>
/// Abstracts the current time so refresh orchestration, alert evaluation, and analytics stay
/// deterministic and testable rather than calling <see cref="DateTimeOffset.UtcNow"/> directly.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
