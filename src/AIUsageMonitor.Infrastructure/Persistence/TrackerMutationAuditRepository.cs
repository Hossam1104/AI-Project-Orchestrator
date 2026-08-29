using AIUsageMonitor.Application.Trackers;

namespace AIUsageMonitor.Infrastructure.Persistence;

public sealed class TrackerMutationAuditRecord
{
    public string RecordType { get; set; } = "tracker-mutation-receipt";
    public int SchemaVersion { get; set; } = TrackerMutationReceipt.CurrentSchemaVersion;
    public Guid ProjectId { get; set; }
    public string TrackerProvider { get; set; } = string.Empty;
    public string TrackerProjectId { get; set; } = string.Empty;
    public string? TrackerBaseUri { get; set; }
    public string TargetWorkItem { get; set; } = string.Empty;
    public string? RelatedWorkItem { get; set; }
    public string? LinkType { get; set; }
    public string LinkDirection { get; set; } = string.Empty;
    public string MutationKind { get; set; } = string.Empty;
    public Guid AuthorityId { get; set; }
    public string AuthorityHash { get; set; } = string.Empty;
    public string ActorIdentity { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTimeOffset AttemptedAt { get; set; }
    public string ExpectedStateIdentity { get; set; } = string.Empty;
    public string HttpOutcome { get; set; } = string.Empty;
    public string VerificationState { get; set; } = string.Empty;
    public string FinalOutcome { get; set; } = string.Empty;
    public bool MayHaveModifiedRemote { get; set; }
    public string? BodyHash { get; set; }
    public int? BodyLength { get; set; }
    public string? RemoteReference { get; set; }

    public static TrackerMutationAuditRecord FromApplication(TrackerMutationReceipt receipt) => new()
    {
        ProjectId = receipt.ProjectId,
        TrackerProvider = receipt.Tracker.Provider.ToString(),
        TrackerProjectId = receipt.Tracker.ProjectId,
        TrackerBaseUri = receipt.Tracker.BaseUri?.AbsoluteUri,
        TargetWorkItem = receipt.Target.WorkItem.CanonicalIdentity,
        RelatedWorkItem = receipt.Target.RelatedWorkItem?.CanonicalIdentity,
        LinkType = receipt.Target.LinkType,
        LinkDirection = receipt.Target.LinkDirection.ToString(),
        MutationKind = receipt.MutationKind.ToString(),
        AuthorityId = receipt.AuthorityId,
        AuthorityHash = receipt.AuthorityHash,
        ActorIdentity = receipt.ActorIdentity,
        CorrelationId = receipt.CorrelationId,
        AttemptedAt = receipt.AttemptedAt,
        ExpectedStateIdentity = receipt.ExpectedStateIdentity,
        HttpOutcome = receipt.HttpOutcome,
        VerificationState = receipt.VerificationState.ToString(),
        FinalOutcome = receipt.FinalOutcome.ToString(),
        MayHaveModifiedRemote = receipt.MayHaveModifiedRemote,
        BodyHash = receipt.BodyHash,
        BodyLength = receipt.BodyLength,
        RemoteReference = receipt.RemoteReference
    };
}

public sealed class JsonTrackerMutationAuditRepository : ITrackerMutationAuditRepository
{
    private const string ExpectedRecordType = "tracker-mutation-receipt";

    private readonly ApplicationDataPaths _paths;
    private readonly JsonlEventStore<TrackerMutationAuditRecord> _events;

    public JsonTrackerMutationAuditRepository(
        ApplicationDataPaths paths,
        JsonlEventStore<TrackerMutationAuditRecord> events)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public async Task AppendAsync(TrackerMutationReceipt receipt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        if (receipt.SchemaVersion != TrackerMutationReceipt.CurrentSchemaVersion)
        {
            throw new InvalidOperationException("Unsupported tracker mutation receipt schema.");
        }

        var record = TrackerMutationAuditRecord.FromApplication(receipt);
        if (record.RecordType != ExpectedRecordType || record.ProjectId != receipt.ProjectId)
        {
            throw new InvalidOperationException("Tracker mutation audit record failed its project boundary validation.");
        }

        await _paths.EnsureProjectDirectoriesAsync(receipt.ProjectId, cancellationToken).ConfigureAwait(false);
        await _events.AppendAsync(
                _paths.GetProjectTrackerAuditDirectory(receipt.ProjectId),
                receipt.AttemptedAt,
                record,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
