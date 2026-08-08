namespace AIUsageMonitor.Domain.Alerts;

public enum AlertType
{
    ThresholdWarning,
    ThresholdCritical,
    QuotaExhausted,
    QuotaRestored,
    ProviderStale,
    AuthenticationExpired,
    SubscriptionRenewalApproaching,
    SubscriptionExpired
}
