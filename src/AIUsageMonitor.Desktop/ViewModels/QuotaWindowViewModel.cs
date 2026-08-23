using System.Globalization;
using AIUsageMonitor.Domain.Common;
using AIUsageMonitor.Domain.Quotas;

namespace AIUsageMonitor.Desktop.ViewModels;

public sealed class QuotaWindowViewModel
{
    private readonly QuotaWindow _window;

    public QuotaWindowViewModel(QuotaWindow window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }

    public string DisplayName => _window.Type switch
    {
        QuotaType.Rolling5Hour => "Rolling five-hour",
        QuotaType.Rolling7Day => "Rolling seven-day",
        QuotaType.BillingCycle => "Billing cycle",
        QuotaType.RequestAllowance => "Request allowance",
        QuotaType.AiCredits => "AI credits",
        QuotaType.ModelSpecific => "Model-specific",
        _ => ToTitleCase(_window.Type.ToString())
    };

    public string RemainingText => _window.RemainingPercentage is { } remainingPercentage
        ? $"{remainingPercentage:0.#}% remaining"
        : _window.RemainingValue is { } remainingValue && _window.LimitValue is { } limitValue
            ? $"{FormatNumber(remainingValue)} remaining of {FormatNumber(limitValue)}"
            : "Remaining: unavailable";

    public string UsageText => _window.UsedValue is not { } used
        ? "Usage: not reported"
        : _window.LimitValue is { } limit
            ? $"Used {FormatNumber(used)} of {FormatNumber(limit)} {UnitLabel}"
            : $"Used {FormatNumber(used)} {UnitLabel}";

    public string SourceText => $"Source: {ToTitleCase(_window.Source.ToString())} · Confidence: {ToTitleCase(_window.Confidence.ToString())}";

    public string ResetText => _window.ResetAt is { } resetAt
        ? $"Resets {resetAt.ToLocalTime():MMM d, h:mm tt}"
        : "Reset: not reported";

    public bool HasProgress => _window.RemainingPercentage.HasValue;

    public double ProgressValue => _window.RemainingPercentage ?? 0;

    public double? RemainingPercentage => _window.RemainingPercentage;

    public DateTimeOffset? ResetAt => _window.ResetAt;

    public double? UsedValue => _window.UsedValue;

    private string UnitLabel => _window.Unit switch
    {
        QuotaUnit.Tokens => "tokens",
        QuotaUnit.Credits => "credits",
        QuotaUnit.Requests => "requests",
        QuotaUnit.Messages => "messages",
        QuotaUnit.Count => "count",
        _ => string.Empty
    };

    private static string FormatNumber(double value) => value.ToString("0.##", CultureInfo.CurrentCulture);

    private static string ToTitleCase(string value) =>
        string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Length == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..]));
}
