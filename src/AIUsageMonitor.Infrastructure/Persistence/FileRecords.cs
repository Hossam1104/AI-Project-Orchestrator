using AIUsageMonitor.Domain.Alerts;
using AIUsageMonitor.Domain.Common;
using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Quotas;
using AIUsageMonitor.Domain.Subscriptions;
using AIUsageMonitor.Domain.Sync;
using AIUsageMonitor.Domain.Usage;
using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Projects;
using AIUsageMonitor.Application.Routing;

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

internal sealed class ProjectRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public string DefaultBranch { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; }
    public string? RepositoryProvider { get; set; }
    public string? RepositoryUrl { get; set; }
    public string? RepositoryId { get; set; }
    public Dictionary<string, string?> RepositoryMetadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? TrackerType { get; set; }
    public string? TrackerId { get; set; }
    public Dictionary<string, string?> TrackerMetadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> GovernanceReferences { get; set; } = [];
    public string? RoutingPolicyReference { get; set; }
    public string? SafetyPolicyReference { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static ProjectRecord FromApplication(Project value) => new()
    {
        Id = value.Id,
        Name = value.Name,
        LocalPath = value.LocalPath,
        DefaultBranch = value.DefaultBranch,
        Status = value.Status,
        RepositoryProvider = value.RepositoryProvider,
        RepositoryUrl = value.RepositoryUrl,
        RepositoryId = value.RepositoryId,
        RepositoryMetadata = new Dictionary<string, string?>(value.RepositoryMetadata, StringComparer.OrdinalIgnoreCase),
        TrackerType = value.TrackerType,
        TrackerId = value.TrackerId,
        TrackerMetadata = new Dictionary<string, string?>(value.TrackerMetadata, StringComparer.OrdinalIgnoreCase),
        GovernanceReferences = value.GovernanceReferences.ToList(),
        RoutingPolicyReference = value.RoutingPolicyReference,
        SafetyPolicyReference = value.SafetyPolicyReference,
        CreatedAt = value.CreatedAt,
        UpdatedAt = value.UpdatedAt
    };

    public Project ToApplication() => new(
        Id,
        Name,
        LocalPath,
        DefaultBranch,
        Status,
        CreatedAt,
        UpdatedAt,
        RepositoryProvider,
        RepositoryUrl,
        RepositoryId,
        RepositoryMetadata,
        TrackerType,
        TrackerId,
        TrackerMetadata,
        GovernanceReferences,
        RoutingPolicyReference,
        SafetyPolicyReference);
}

internal sealed class AgentRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public AgentConnectionMode ConnectionMode { get; set; }
    public AgentAvailability Availability { get; set; }
    public bool Enabled { get; set; }
    public List<string> Capabilities { get; set; } = [];
    public List<string> Limitations { get; set; } = [];
    public Dictionary<string, string?> CostAndQuotaMetadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static AgentRecord FromApplication(AgentDefinition value) => new()
    {
        Id = value.Id,
        Name = value.Name,
        Role = value.Role,
        Provider = value.Provider,
        ConnectionMode = value.ConnectionMode,
        Availability = value.Availability,
        Enabled = value.Enabled,
        Capabilities = value.Capabilities.ToList(),
        Limitations = value.Limitations.ToList(),
        CostAndQuotaMetadata = new Dictionary<string, string?>(value.CostAndQuotaMetadata, StringComparer.OrdinalIgnoreCase),
        CreatedAt = value.CreatedAt,
        UpdatedAt = value.UpdatedAt
    };

    public AgentDefinition ToApplication() => new(
        Id,
        Name,
        Role,
        ConnectionMode,
        Availability,
        Enabled,
        CreatedAt,
        UpdatedAt,
        Provider,
        Capabilities,
        Limitations,
        CostAndQuotaMetadata);
}

internal sealed class RoutingPolicyRecord
{
    public bool? QualityRiskFirst { get; set; }
    public bool? RequireIndependentReviewForHighRisk { get; set; }
    public bool? RequireHumanApprovalForHighRisk { get; set; }
    public int? MaxConcurrentRuns { get; set; }
    public int? MaxRetries { get; set; }
    public int? MaxReviewRemediationCycles { get; set; }
    public Dictionary<string, string?> Rules { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public DateTimeOffset UpdatedAt { get; set; }

    public static RoutingPolicyRecord FromApplication(RoutingPolicy value) => new()
    {
        QualityRiskFirst = value.QualityRiskFirst,
        RequireIndependentReviewForHighRisk = value.RequireIndependentReviewForHighRisk,
        RequireHumanApprovalForHighRisk = value.RequireHumanApprovalForHighRisk,
        MaxConcurrentRuns = value.MaxConcurrentRuns,
        MaxRetries = value.MaxRetries,
        MaxReviewRemediationCycles = value.MaxReviewRemediationCycles,
        Rules = new Dictionary<string, string?>(value.Rules, StringComparer.OrdinalIgnoreCase),
        UpdatedAt = value.UpdatedAt
    };

    public RoutingPolicy ToApplication() => new(
        QualityRiskFirst,
        RequireIndependentReviewForHighRisk,
        RequireHumanApprovalForHighRisk,
        MaxConcurrentRuns,
        MaxRetries,
        MaxReviewRemediationCycles,
        UpdatedAt,
        Rules);
}

public sealed class ExecutionRunRecord
{
    public int SchemaVersion { get; set; } = JsonFileStore.CurrentSchemaVersion;
    public string RecordType { get; set; } = "execution-run";
    public Guid ProjectId { get; set; }
    public Guid RunId { get; set; }
    public ExecutionRunStatus Status { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? WorkItemReference { get; set; }
    public string? TaskTitle { get; set; }
    public Guid? AgentId { get; set; }
    public string? ModelReference { get; set; }
    public string? Outcome { get; set; }
    public string? StopReason { get; set; }
    public string? ContractReference { get; set; }

    public static ExecutionRunRecord FromApplication(ExecutionRun value) => new()
    {
        ProjectId = value.ProjectId,
        RunId = value.RunId,
        Status = value.Status,
        StartedAt = value.StartedAt,
        CompletedAt = value.CompletedAt,
        WorkItemReference = value.WorkItemReference,
        TaskTitle = value.TaskTitle,
        AgentId = value.AgentId,
        ModelReference = value.ModelReference,
        Outcome = value.Outcome,
        StopReason = value.StopReason,
        ContractReference = value.ContractReference
    };

    public ExecutionRun ToApplication() => new(
        ProjectId,
        RunId,
        Status,
        StartedAt,
        CompletedAt,
        WorkItemReference,
        TaskTitle,
        AgentId,
        ModelReference,
        Outcome,
        StopReason,
        ContractReference);
}

public sealed class EvidenceMetadataRecord
{
    public int SchemaVersion { get; set; } = JsonFileStore.CurrentSchemaVersion;
    public string RecordType { get; set; } = "evidence-metadata";
    public Guid ProjectId { get; set; }
    public Guid EvidenceId { get; set; }
    public Guid? RunId { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string? ValidatorReference { get; set; }
    public string? ArtifactReference { get; set; }
    public string? ContentHash { get; set; }
    public string? Summary { get; set; }

    public static EvidenceMetadataRecord FromApplication(EvidenceMetadata value) => new()
    {
        ProjectId = value.ProjectId,
        EvidenceId = value.EvidenceId,
        RunId = value.RunId,
        CapturedAt = value.CapturedAt,
        Kind = value.Kind,
        Outcome = value.Outcome,
        ValidatorReference = value.ValidatorReference,
        ArtifactReference = value.ArtifactReference,
        ContentHash = value.ContentHash,
        Summary = value.Summary
    };

    public EvidenceMetadata ToApplication() => new(
        ProjectId,
        EvidenceId,
        CapturedAt,
        Kind,
        Outcome,
        RunId,
        ValidatorReference,
        ArtifactReference,
        ContentHash,
        Summary);
}

public sealed class ReviewMetadataRecord
{
    public int SchemaVersion { get; set; } = JsonFileStore.CurrentSchemaVersion;
    public string RecordType { get; set; } = "review-metadata";
    public Guid ProjectId { get; set; }
    public Guid ReviewId { get; set; }
    public Guid? RunId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string ReviewerReference { get; set; } = string.Empty;
    public string Verdict { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public bool Blocking { get; set; }
    public int FindingCount { get; set; }
    public string? EvidenceReference { get; set; }
    public string? Summary { get; set; }

    public static ReviewMetadataRecord FromApplication(ReviewMetadata value) => new()
    {
        ProjectId = value.ProjectId,
        ReviewId = value.ReviewId,
        RunId = value.RunId,
        OccurredAt = value.OccurredAt,
        ReviewerReference = value.ReviewerReference,
        Verdict = value.Verdict,
        Severity = value.Severity,
        Blocking = value.Blocking,
        FindingCount = value.FindingCount,
        EvidenceReference = value.EvidenceReference,
        Summary = value.Summary
    };

    public ReviewMetadata ToApplication() => new(
        ProjectId,
        ReviewId,
        OccurredAt,
        ReviewerReference,
        Verdict,
        Severity,
        Blocking,
        RunId,
        FindingCount,
        EvidenceReference,
        Summary);
}

public sealed class ActivityAuditRecordFile
{
    public int SchemaVersion { get; set; } = JsonFileStore.CurrentSchemaVersion;
    public string RecordType { get; set; } = "activity-audit";
    public Guid ProjectId { get; set; }
    public Guid ActivityId { get; set; }
    public Guid? RunId { get; set; }
    public Guid? EvidenceId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string ActorReference { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string? Summary { get; set; }

    public static ActivityAuditRecordFile FromApplication(ActivityAuditRecord value) => new()
    {
        ProjectId = value.ProjectId,
        ActivityId = value.ActivityId,
        RunId = value.RunId,
        EvidenceId = value.EvidenceId,
        OccurredAt = value.OccurredAt,
        ActorReference = value.ActorReference,
        Action = value.Action,
        Outcome = value.Outcome,
        Summary = value.Summary
    };

    public ActivityAuditRecord ToApplication() => new(
        ProjectId,
        ActivityId,
        OccurredAt,
        ActorReference,
        Action,
        Outcome,
        RunId,
        EvidenceId,
        Summary);
}
