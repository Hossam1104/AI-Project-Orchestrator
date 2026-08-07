namespace AIUsageMonitor.Domain.Quotas;

/// <summary>
/// Supported quota window types (BRD §9). New provider quota shapes are expected to fit
/// one of these, with <see cref="Custom"/> as the escape hatch — never add a fixed
/// provider-specific column/property instead.
/// </summary>
public enum QuotaType
{
    Rolling5Hour,
    Session,
    Daily,
    Weekly,
    Rolling7Day,
    Monthly,
    BillingCycle,
    Credits,
    AiCredits,
    ModelSpecific,
    RequestAllowance,
    Tokens,
    ExtraUsage,
    Custom
}
