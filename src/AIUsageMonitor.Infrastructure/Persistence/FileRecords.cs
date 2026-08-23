using AIUsageMonitor.Domain.Alerts;
using AIUsageMonitor.Domain.Common;
using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Quotas;
using AIUsageMonitor.Domain.Subscriptions;
using AIUsageMonitor.Domain.Sync;
using AIUsageMonitor.Domain.Usage;

namespace AIUsageMonitor.Infrastructure.Persistence;

internal sealed class ProviderRecord
{
    public Guid Id { get; set; }
    public ProviderCode Code { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static ProviderRecord FromDomain(Provider value) => new()
    {
        Id = value.Id,
        Code = value.Code,
        DisplayName = value.DisplayName,
        Enabled = value.Enabled,
        SortOrder = value.SortOrder,
        CreatedAt = value.CreatedAt,
        UpdatedAt = value.UpdatedAt
    };

    public Provider ToDomain() => new(Id, Code, DisplayName, Enabled, SortOrder, CreatedAt, UpdatedAt);
}

internal sealed class ProviderConnectionRecord
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public ProviderConnectionType ConnectionType { get; set; }
    public ProviderConnectionStatus Status { get; set; }
    public string? AccountDisplayName { get; set; }
    public DateTimeOffset? LastSuccessfulSync { get; set; }
    public DateTimeOffset? LastAttempt { get; set; }
    public string? LastErrorCode { get; set; }
    public string? LastErrorMessage { get; set; }
    public string? CredentialReference { get; set; }
    public Dictionary<string, string?>? Configuration { get; set; }

    public static ProviderConnectionRecord FromDomain(ProviderConnection value) => new()
    {
        Id = value.Id,
        ProviderId = value.ProviderId,
        ConnectionType = value.ConnectionType,
        Status = value.Status,
        AccountDisplayName = value.AccountDisplayName,
        LastSuccessfulSync = value.LastSuccessfulSync,
        LastAttempt = value.LastAttempt,
        LastErrorCode = value.LastErrorCode,
        LastErrorMessage = value.LastErrorMessage,
        CredentialReference = value.CredentialReference,
        Configuration = value.Configuration.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.OrdinalIgnoreCase)
    };

    public ProviderConnection ToDomain() => new(
        Id,
        ProviderId,
        ConnectionType,
        Status,
        AccountDisplayName,
        LastSuccessfulSync,
        LastAttempt,
        LastErrorCode,
        LastErrorMessage,
        CredentialReference,
        Configuration);
}

internal sealed class QuotaDefinitionRecord
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public string ExternalKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public QuotaType Type { get; set; }
    public QuotaUnit Unit { get; set; }
    public int SortOrder { get; set; }

    public static QuotaDefinitionRecord FromDomain(QuotaDefinition value) => new()
    {
        Id = value.Id,
        ProviderId = value.ProviderId,
        ExternalKey = value.ExternalKey,
        Name = value.Name,
        Type = value.Type,
        Unit = value.Unit,
        SortOrder = value.SortOrder
    };

    public QuotaDefinition ToDomain() => new(Id, ProviderId, ExternalKey, Name, Type, Unit, SortOrder);
}

internal sealed class SubscriptionRecord
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public string? PlanName { get; set; }
    public DateTimeOffset? OriginalStartDate { get; set; }
    public DateTimeOffset? BillingPeriodStart { get; set; }
    public DateTimeOffset? BillingPeriodEnd { get; set; }
    public DateTimeOffset? RenewalDate { get; set; }
    public DateTimeOffset? CancelledDate { get; set; }
    public bool? AutoRenew { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
    public BillingCadence? Cadence { get; set; }
    public DataSource Source { get; set; }
    public ConfidenceLevel Confidence { get; set; }
    public DateTimeOffset? LastVerifiedAt { get; set; }

    public static SubscriptionRecord FromDomain(Subscription value) => new()
    {
        Id = value.Id,
        ProviderId = value.ProviderId,
        PlanName = value.PlanName,
        OriginalStartDate = value.OriginalStartDate,
        BillingPeriodStart = value.BillingPeriodStart,
        BillingPeriodEnd = value.BillingPeriodEnd,
        RenewalDate = value.RenewalDate,
        CancelledDate = value.CancelledDate,
        AutoRenew = value.AutoRenew,
        Price = value.Price,
        Currency = value.Currency,
        Cadence = value.Cadence,
        Source = value.Source,
        Confidence = value.Confidence,
        LastVerifiedAt = value.LastVerifiedAt
    };

    public Subscription ToDomain() => new(
        Id,
        ProviderId,
        PlanName,
        OriginalStartDate,
        BillingPeriodStart,
        BillingPeriodEnd,
        RenewalDate,
        CancelledDate,
        AutoRenew,
        Price,
        Currency,
        Cadence,
        Source,
        Confidence,
        LastVerifiedAt);
}

internal sealed class AlertRuleRecord
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public Guid? QuotaDefinitionId { get; set; }
    public double WarningThreshold { get; set; }
    public double CriticalThreshold { get; set; }
    public bool Enabled { get; set; }

    public static AlertRuleRecord FromDomain(AlertRule value) => new()
    {
        Id = value.Id,
        ProviderId = value.ProviderId,
        QuotaDefinitionId = value.QuotaDefinitionId,
        WarningThreshold = value.WarningThreshold,
        CriticalThreshold = value.CriticalThreshold,
        Enabled = value.Enabled
    };

    public AlertRule ToDomain() => new(Id, ProviderId, QuotaDefinitionId, WarningThreshold, CriticalThreshold, Enabled);
}

public sealed class UsageSnapshotRecord
{
    public int SchemaVersion { get; set; } = JsonFileStore.CurrentSchemaVersion;
    public string RecordType { get; set; } = "usage-snapshot";
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public Guid QuotaDefinitionId { get; set; }
    public string ExternalKey { get; set; } = string.Empty;
    public QuotaType Type { get; set; }
    public QuotaUnit Unit { get; set; }
    public double? UsedValue { get; set; }
    public double? RemainingValue { get; set; }
    public double? LimitValue { get; set; }
    public double? UsedPercentage { get; set; }
    public double? RemainingPercentage { get; set; }
    public DateTimeOffset? WindowStart { get; set; }
    public DateTimeOffset? ResetAt { get; set; }
    public DataSource Source { get; set; }
    public ConfidenceLevel Confidence { get; set; }
    public DateTimeOffset CapturedAt { get; set; }

    public static UsageSnapshotRecord FromDomain(UsageSnapshot value) => new()
    {
        Id = value.Id,
        ProviderId = value.ProviderId,
        QuotaDefinitionId = value.QuotaDefinitionId,
        ExternalKey = value.Quota.ExternalKey,
        Type = value.Quota.Type,
        Unit = value.Quota.Unit,
        UsedValue = value.Quota.UsedValue,
        RemainingValue = value.Quota.RemainingValue,
        LimitValue = value.Quota.LimitValue,
        UsedPercentage = value.Quota.UsedPercentage,
        RemainingPercentage = value.Quota.RemainingPercentage,
        WindowStart = value.Quota.WindowStart,
        ResetAt = value.Quota.ResetAt,
        Source = value.Quota.Source,
        Confidence = value.Quota.Confidence,
        CapturedAt = value.Quota.CapturedAt
    };

    public UsageSnapshot ToDomain() => new(
        Id,
        ProviderId,
        QuotaDefinitionId,
        QuotaWindow.Create(
            ExternalKey,
            Type,
            Unit,
            UsedValue,
            RemainingValue,
            LimitValue,
            UsedPercentage,
            RemainingPercentage,
            WindowStart,
            ResetAt,
            Source,
            Confidence,
            CapturedAt));
}

public sealed class AlertEventRecord
{
    public int SchemaVersion { get; set; } = JsonFileStore.CurrentSchemaVersion;
    public string RecordType { get; set; } = "alert-event";
    public Guid Id { get; set; }
    public Guid AlertRuleId { get; set; }
    public DateTimeOffset TriggeredAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public AlertType Type { get; set; }
    public AlertSeverity Severity { get; set; }
    public double? Value { get; set; }
    public string? Message { get; set; }

    public static AlertEventRecord FromDomain(AlertEvent value) => new()
    {
        Id = value.Id,
        AlertRuleId = value.AlertRuleId,
        TriggeredAt = value.TriggeredAt,
        ResolvedAt = value.ResolvedAt,
        Type = value.Type,
        Severity = value.Severity,
        Value = value.Value,
        Message = value.Message
    };

    public AlertEvent ToDomain() => new(Id, AlertRuleId, TriggeredAt, ResolvedAt, Type, Severity, Value, Message);
}

public sealed class SyncEventRecord
{
    public int SchemaVersion { get; set; } = JsonFileStore.CurrentSchemaVersion;
    public string RecordType { get; set; } = "sync-event";
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public bool Success { get; set; }
    public bool DataChanged { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorSummary { get; set; }

    public static SyncEventRecord FromDomain(SyncEvent value) => new()
    {
        Id = value.Id,
        ProviderId = value.ProviderId,
        StartedAt = value.StartedAt,
        CompletedAt = value.CompletedAt,
        Success = value.Success,
        DataChanged = value.DataChanged,
        ErrorCode = value.ErrorCode,
        ErrorSummary = value.ErrorSummary
    };

    public SyncEvent ToDomain() => new(Id, ProviderId, StartedAt, CompletedAt, Success, DataChanged, ErrorCode, ErrorSummary);
}

internal sealed class SettingsDocument
{
    public Dictionary<string, SettingsValueRecord> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class SettingsValueRecord
{
    public System.Text.Json.JsonElement Value { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
