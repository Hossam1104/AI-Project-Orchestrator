using System.Security.Cryptography;
using System.Text;
using AIUsageMonitor.Application.Projects;

namespace AIUsageMonitor.Application.Trackers;

public enum TrackerProviderKind
{
    Jira,
    AzureDevOps
}

public enum TrackerEvidenceState
{
    Available,
    NotConfigured,
    AuthenticationRequired,
    PermissionDenied,
    NotFound,
    Unsupported,
    Unavailable,
    RateLimited,
    Partial,
    Stale,
    InvalidResponse,
    Cancelled
}

public enum TrackerMutationKind
{
    AddComment,
    TransitionStatus,
    AddDependencyLink
}

public enum TrackerMutationOutcome
{
    Succeeded,
    InvalidAuthority,
    Conflict,
    Unsupported,
    AuthenticationRequired,
    PermissionDenied,
    NotFound,
    RateLimited,
    Unavailable,
    InvalidResponse,
    Cancelled,
    ReconciliationRequired
}

public enum TrackerLinkDirection
{
    Inward,
    Outward
}

public enum TrackerAdapterResolutionStatus
{
    Resolved,
    NotConfigured,
    Unsupported,
    ConfigurationConflict
}

public enum TrackerSynchronizationDirection
{
    TrackerAuthoritative
}

public static class TrackerMetadataKeys
{
    public const string BaseUri = "baseUri";
    public const string ProjectKey = "projectKey";
    public const string AuthReference = "authRef";
    public const string EvidenceFreshnessSeconds = "freshnessSeconds";
}

public static class TrackerLimits
{
    public const int MaxWorkItems = 100;
    public const int MaxPages = 5;
    public const int MaxComments = 50;
    public const int MaxLinks = 50;
    public const int MaxStringLength = 4_000;
    public const int MaxDiagnosticLength = 1_000;
    public const int MaxResponseBytes = 512 * 1024;
    public const int MaxPlanOperations = 32;
    public const int MaxAuthorityPayloadBytes = 64 * 1024;
}

internal static class TrackerInputValidation
{
    public static string? Optional(string? value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? null : Required(value, parameterName);

    public static string? Comment(string? value, string parameterName)
    {
        if (value is null)
        {
            return null;
        }

        if (value.Length == 0 || value.Length > TrackerLimits.MaxStringLength ||
            value.Any(static character => char.IsControl(character) && character is not ('\r' or '\n' or '\t')))
        {
            throw new ArgumentException("Tracker comment body is empty, oversized, or contains unsupported control characters.", parameterName);
        }

        return value;
    }

    public static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Tracker value is required.", parameterName);
        }

        var normalized = value.Trim();
        return normalized.Length <= TrackerLimits.MaxStringLength
            ? normalized
            : throw new ArgumentException("Tracker value exceeds its supported bound.", parameterName);
    }
}

public sealed class TrackerProjectIdentity
{
    public TrackerProjectIdentity(
        TrackerProviderKind provider,
        string projectId,
        Uri? baseUri = null)
    {
        if (!Enum.IsDefined(provider))
        {
            throw new ArgumentException("Tracker provider is undefined.", nameof(provider));
        }

        ProjectId = Required(projectId, nameof(projectId));
        if (baseUri is not null)
        {
            if (!baseUri.IsAbsoluteUri || baseUri.Scheme != Uri.UriSchemeHttps ||
                !string.IsNullOrEmpty(baseUri.UserInfo) || baseUri.Query.Length != 0 || baseUri.Fragment.Length != 0)
            {
                throw new ArgumentException("Tracker base URI must be an absolute HTTPS URI without user information or query data.", nameof(baseUri));
            }

            BaseUri = new Uri(baseUri.AbsoluteUri.TrimEnd('/') + "/", UriKind.Absolute);
        }

        Provider = provider;
    }

    public TrackerProviderKind Provider { get; }

    public string ProjectId { get; }

    public Uri? BaseUri { get; }

    public string CanonicalIdentity =>
        $"{Provider}:{ProjectId}:{BaseUri?.AbsoluteUri.TrimEnd('/') ?? "(no-base-uri)"}";

    public override string ToString() => CanonicalIdentity;

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Tracker identity is required.", parameterName);
        }

        var normalized = value.Trim();
        return normalized.Length <= TrackerLimits.MaxStringLength
            ? normalized
            : throw new ArgumentException("Tracker identity exceeds its supported bound.", parameterName);
    }
}

public sealed class TrackerConfiguration
{
    private TrackerConfiguration(
        Guid projectId,
        TrackerProjectIdentity identity,
        string? authReference,
        TimeSpan evidenceMaxAge)
    {
        ProjectId = projectId;
        Identity = identity;
        AuthReference = authReference;
        EvidenceMaxAge = evidenceMaxAge;
    }

    public Guid ProjectId { get; }

    public TrackerProjectIdentity Identity { get; }

    public string? AuthReference { get; }

    public TimeSpan EvidenceMaxAge { get; }

    public static bool TryCreate(
        Project project,
        out TrackerConfiguration? configuration,
        out TrackerEvidenceState state,
        out string? errorMessage)
    {
        ArgumentNullException.ThrowIfNull(project);
        configuration = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(project.TrackerType) || string.IsNullOrWhiteSpace(project.TrackerId))
        {
            state = TrackerEvidenceState.NotConfigured;
            errorMessage = "The project has no complete tracker identity.";
            return false;
        }

        var trackerType = project.TrackerType.Trim();
        var provider = trackerType.Equals(nameof(TrackerProviderKind.Jira), StringComparison.OrdinalIgnoreCase)
            ? TrackerProviderKind.Jira
            : trackerType.Equals(nameof(TrackerProviderKind.AzureDevOps), StringComparison.OrdinalIgnoreCase)
                ? TrackerProviderKind.AzureDevOps
                : (TrackerProviderKind?)null;
        if (provider is null)
        {
            state = TrackerEvidenceState.Unsupported;
            errorMessage = "The configured tracker type is not supported.";
            return false;
        }

        var metadata = project.TrackerMetadata;
        var metadataProjectId = Get(metadata, TrackerMetadataKeys.ProjectKey);
        if (metadataProjectId is not null &&
            !string.Equals(metadataProjectId, project.TrackerId, StringComparison.OrdinalIgnoreCase))
        {
            state = TrackerEvidenceState.Partial;
            errorMessage = "TrackerId and tracker project metadata identify different remote projects.";
            return false;
        }

        Uri? baseUri = null;
        var baseUriText = Get(metadata, TrackerMetadataKeys.BaseUri);
        if (baseUriText is not null &&
            (!Uri.TryCreate(baseUriText, UriKind.Absolute, out baseUri) ||
             baseUri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(baseUri.UserInfo) ||
             baseUri.Query.Length != 0 || baseUri.Fragment.Length != 0))
        {
            state = TrackerEvidenceState.NotConfigured;
            errorMessage = "Tracker base URI is malformed or unsafe.";
            return false;
        }

        if (provider == TrackerProviderKind.Jira && baseUri is null)
        {
            state = TrackerEvidenceState.NotConfigured;
            errorMessage = "Jira requires an HTTPS base URI in tracker metadata.";
            return false;
        }

        var authReference = Get(metadata, TrackerMetadataKeys.AuthReference);
        if (authReference is not null && authReference.Length > TrackerLimits.MaxStringLength)
        {
            state = TrackerEvidenceState.NotConfigured;
            errorMessage = "The tracker auth reference exceeds its supported bound.";
            return false;
        }

        var freshnessSeconds = 900;
        var freshnessText = Get(metadata, TrackerMetadataKeys.EvidenceFreshnessSeconds);
        if (freshnessText is not null &&
            (!int.TryParse(freshnessText, out freshnessSeconds) || freshnessSeconds is < 1 or > 86_400))
        {
            state = TrackerEvidenceState.NotConfigured;
            errorMessage = "Tracker evidence freshness must be between 1 and 86400 seconds.";
            return false;
        }

        configuration = new TrackerConfiguration(
            project.Id,
            new TrackerProjectIdentity(provider.Value, project.TrackerId, baseUri),
            authReference,
            TimeSpan.FromSeconds(freshnessSeconds));
        state = TrackerEvidenceState.Available;
        return true;
    }

    private static string? Get(IReadOnlyDictionary<string, string?> metadata, string key) =>
        metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;
}

public sealed class TrackerWorkItemIdentity
{
    public TrackerWorkItemIdentity(
        TrackerProviderKind provider,
        string projectId,
        string keyOrId,
        string? remoteId = null,
        Uri? referenceUri = null)
    {
        if (!Enum.IsDefined(provider))
        {
            throw new ArgumentException("Tracker provider is undefined.", nameof(provider));
        }

        Provider = provider;
        ProjectId = Required(projectId, nameof(projectId));
        KeyOrId = Required(keyOrId, nameof(keyOrId));
        RemoteId = NormalizeOptional(remoteId);
        if (referenceUri is not null &&
            (!referenceUri.IsAbsoluteUri || referenceUri.Scheme != Uri.UriSchemeHttps ||
             !string.IsNullOrEmpty(referenceUri.UserInfo) || referenceUri.Query.Length != 0 || referenceUri.Fragment.Length != 0))
        {
            throw new ArgumentException("Tracker reference URI is unsafe.", nameof(referenceUri));
        }

        ReferenceUri = referenceUri;
    }

    public TrackerProviderKind Provider { get; }
    public string ProjectId { get; }
    public string KeyOrId { get; }
    public string? RemoteId { get; }
    public Uri? ReferenceUri { get; }

    public string CanonicalIdentity =>
        $"{Provider}:{ProjectId}:{KeyOrId}:{RemoteId ?? "(no-id)"}";

    public override string ToString() => CanonicalIdentity;

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Tracker work-item identity is required.", parameterName);
        }

        var normalized = value.Trim();
        return normalized.Length <= TrackerLimits.MaxStringLength
            ? normalized
            : throw new ArgumentException("Tracker work-item identity exceeds its supported bound.", parameterName);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : Required(value, nameof(value));
}

public sealed class TrackerStatusSnapshot
{
    public TrackerStatusSnapshot(string id, string name, string? category = null)
    {
        Id = Required(id, nameof(id));
        Name = Required(name, nameof(name));
        Category = string.IsNullOrWhiteSpace(category) ? null : Required(category, nameof(category));
    }

    public string Id { get; }
    public string Name { get; }
    public string? Category { get; }

    private static string Required(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Tracker status value is required.", parameterName)
            : value.Trim().Length <= TrackerLimits.MaxStringLength
                ? value.Trim()
                : throw new ArgumentException("Tracker status value exceeds its supported bound.", parameterName);
}

public sealed class TrackerHierarchyReference
{
    public TrackerHierarchyReference(TrackerWorkItemIdentity parent)
    {
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }

    public TrackerWorkItemIdentity Parent { get; }
}

public sealed class TrackerDependencyLink
{
    public TrackerDependencyLink(
        TrackerWorkItemIdentity source,
        TrackerWorkItemIdentity target,
        string relationship,
        TrackerLinkDirection direction,
        string? remoteLinkId = null,
        bool isDependency = false,
        string? remoteTypeId = null,
        string? remoteTypeName = null)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Relationship = Required(relationship, nameof(relationship));
        if (!Enum.IsDefined(direction))
        {
            throw new ArgumentException("Tracker link direction is undefined.", nameof(direction));
        }

        Direction = direction;
        RemoteLinkId = string.IsNullOrWhiteSpace(remoteLinkId) ? null : Required(remoteLinkId, nameof(remoteLinkId));
        IsDependency = isDependency;
        RemoteTypeId = string.IsNullOrWhiteSpace(remoteTypeId) ? null : Required(remoteTypeId, nameof(remoteTypeId));
        RemoteTypeName = string.IsNullOrWhiteSpace(remoteTypeName) ? null : Required(remoteTypeName, nameof(remoteTypeName));
    }

    public TrackerWorkItemIdentity Source { get; }
    public TrackerWorkItemIdentity Target { get; }
    public string Relationship { get; }
    public TrackerLinkDirection Direction { get; }
    public string? RemoteLinkId { get; }
    public bool IsDependency { get; }
    public string? RemoteTypeId { get; }
    public string? RemoteTypeName { get; }

    public string CanonicalIdentity =>
        $"{Source.CanonicalIdentity}|{Direction}|{Relationship}|{Target.CanonicalIdentity}|{RemoteTypeId ?? "(no-type-id)"}|{RemoteTypeName ?? "(no-type-name)"}|{RemoteLinkId ?? "(no-id)"}";

    private static string Required(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Tracker link relationship is required.", parameterName)
            : value.Trim().Length <= TrackerLimits.MaxStringLength
                ? value.Trim()
                : throw new ArgumentException("Tracker link relationship exceeds its supported bound.", parameterName);
}

public sealed class TrackerCommentMetadata
{
    public TrackerCommentMetadata(
        string commentId,
        string? author,
        DateTimeOffset? createdAt,
        DateTimeOffset? updatedAt,
        string body,
        Uri? referenceUri = null)
    {
        CommentId = Required(commentId, nameof(commentId));
        Author = string.IsNullOrWhiteSpace(author) ? null : Required(author, nameof(author));
        Body = RequiredBody(body);
        BodyHash = ComputeBodyHash(Body);
        BodyLength = Body.Length;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        if (referenceUri is not null &&
            (!referenceUri.IsAbsoluteUri || referenceUri.Scheme != Uri.UriSchemeHttps ||
             !string.IsNullOrEmpty(referenceUri.UserInfo) || referenceUri.Query.Length != 0 || referenceUri.Fragment.Length != 0))
        {
            throw new ArgumentException("Tracker comment reference URI is unsafe.", nameof(referenceUri));
        }

        ReferenceUri = referenceUri;
    }

    public string CommentId { get; }
    public string? Author { get; }
    public DateTimeOffset? CreatedAt { get; }
    public DateTimeOffset? UpdatedAt { get; }
    public string Body { get; }
    public string BodyHash { get; }
    public int BodyLength { get; }
    public Uri? ReferenceUri { get; }

    public static string ComputeBodyHash(string body) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Tracker comment value is required.", parameterName);
        }

        var normalized = value.Trim();
        return normalized.Length <= TrackerLimits.MaxStringLength
            ? normalized
            : throw new ArgumentException("Tracker comment value exceeds its supported bound.", parameterName);
    }

    private static string RequiredBody(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length == 0 || value.Length > TrackerLimits.MaxStringLength ||
            value.Any(static character => char.IsControl(character) && character is not ('\r' or '\n' or '\t')))
        {
            throw new ArgumentException("Tracker comment body is empty, oversized, or contains unsupported control characters.", nameof(value));
        }

        return value;
    }
}

public sealed class TrackerWorkItemSnapshot
{
    public TrackerWorkItemSnapshot(
        TrackerWorkItemIdentity identity,
        TrackerProjectIdentity project,
        string issueType,
        string summary,
        TrackerStatusSnapshot status,
        DateTimeOffset? updatedAt,
        TrackerHierarchyReference? parent = null,
        IReadOnlyList<TrackerDependencyLink>? links = null,
        IReadOnlyList<TrackerCommentMetadata>? comments = null,
        Uri? referenceUri = null)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        Project = project ?? throw new ArgumentNullException(nameof(project));
        if (identity.Provider != project.Provider || !string.Equals(identity.ProjectId, project.ProjectId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Work-item and project identities do not match.", nameof(identity));
        }

        IssueType = Required(issueType, nameof(issueType));
        Summary = Required(summary, nameof(summary));
        Status = status ?? throw new ArgumentNullException(nameof(status));
        UpdatedAt = updatedAt;
        Parent = parent;
        Links = Copy(links, TrackerLimits.MaxLinks, nameof(links));
        Comments = Copy(comments, TrackerLimits.MaxComments, nameof(comments));
        if (referenceUri is not null &&
            (!referenceUri.IsAbsoluteUri || referenceUri.Scheme != Uri.UriSchemeHttps ||
             !string.IsNullOrEmpty(referenceUri.UserInfo) || referenceUri.Query.Length != 0 || referenceUri.Fragment.Length != 0))
        {
            throw new ArgumentException("Tracker work-item reference URI is unsafe.", nameof(referenceUri));
        }

        ReferenceUri = referenceUri;
        StateFingerprint = ComputeStateFingerprint();
    }

    public TrackerWorkItemIdentity Identity { get; }
    public TrackerProjectIdentity Project { get; }
    public string IssueType { get; }
    public string Summary { get; }
    public TrackerStatusSnapshot Status { get; }
    public DateTimeOffset? UpdatedAt { get; }
    public TrackerHierarchyReference? Parent { get; }
    public IReadOnlyList<TrackerDependencyLink> Links { get; }
    public IReadOnlyList<TrackerCommentMetadata> Comments { get; }
    public Uri? ReferenceUri { get; }
    public string StateFingerprint { get; }

    private string ComputeStateFingerprint()
    {
        var builder = new StringBuilder()
            .Append(Identity.CanonicalIdentity).Append('|')
            .Append(Project.CanonicalIdentity).Append('|')
            .Append(IssueType).Append('|')
            .Append(Summary).Append('|')
            .Append(Status.Id).Append('|')
            .Append(Status.Name).Append('|')
            .Append(Status.Category).Append('|')
            .Append(UpdatedAt?.ToUniversalTime().ToString("O") ?? "(no-updated)").Append('|')
            .Append(Parent?.Parent.CanonicalIdentity ?? "(no-parent)");
        foreach (var link in Links.OrderBy(static value => value.CanonicalIdentity, StringComparer.Ordinal))
        {
            builder.Append('|').Append(link.CanonicalIdentity);
        }

        foreach (var comment in Comments.OrderBy(static value => value.CommentId, StringComparer.Ordinal))
        {
            builder.Append('|').Append(comment.CommentId).Append('|').Append(comment.BodyHash).Append('|').Append(comment.UpdatedAt?.ToUniversalTime().ToString("O"));
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()))).ToLowerInvariant();
    }

    private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T>? values, int maximum, string parameterName)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<T>();
        }

        if (values.Count > maximum || values.Any(static value => value is null))
        {
            throw new ArgumentException("Tracker collection exceeds its supported bound or contains null.", parameterName);
        }

        return values.ToArray();
    }

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Tracker work-item value is required.", parameterName);
        }

        var normalized = value.Trim();
        return normalized.Length <= TrackerLimits.MaxStringLength
            ? normalized
            : throw new ArgumentException("Tracker work-item value exceeds its supported bound.", parameterName);
    }
}

public sealed class TrackerReadResult<T>
{
    public TrackerReadResult(
        Guid projectId,
        TrackerEvidenceState state,
        TrackerProjectIdentity project,
        TrackerWorkItemIdentity? target,
        DateTimeOffset capturedAt,
        T? value = default,
        T? lastKnownValue = default,
        IReadOnlyList<string>? limitations = null,
        string? errorMessage = null)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id is required.", nameof(projectId));
        }

        if (!Enum.IsDefined(state))
        {
            throw new ArgumentException("Tracker evidence state is undefined.", nameof(state));
        }

        Project = project ?? throw new ArgumentNullException(nameof(project));
        if (capturedAt == default)
        {
            throw new ArgumentException("Tracker capture time is required.", nameof(capturedAt));
        }

        ProjectId = projectId;
        State = state;
        Target = target;
        CapturedAt = capturedAt;
        Value = value;
        LastKnownValue = lastKnownValue;
        Limitations = NormalizeList(limitations, nameof(limitations));
        ErrorMessage = NormalizeOptional(errorMessage, nameof(errorMessage), TrackerLimits.MaxDiagnosticLength);
    }

    public TrackerEvidenceState State { get; }
    public Guid ProjectId { get; }
    public TrackerProjectIdentity Project { get; }
    public TrackerWorkItemIdentity? Target { get; }
    public DateTimeOffset CapturedAt { get; }
    public T? Value { get; }
    public T? LastKnownValue { get; }
    public IReadOnlyList<string> Limitations { get; }
    public string? ErrorMessage { get; }
    public bool IsFresh => State == TrackerEvidenceState.Available;

    private static IReadOnlyList<string> NormalizeList(IReadOnlyList<string>? values, string parameterName)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<string>();
        }

        if (values.Count > 32)
        {
            throw new ArgumentException("Tracker limitations exceed their supported bound.", parameterName);
        }

        return values.Select(value => NormalizeOptional(value, parameterName, TrackerLimits.MaxDiagnosticLength)!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string? NormalizeOptional(string? value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maximumLength
            ? normalized
            : throw new ArgumentException("Tracker diagnostic exceeds its supported bound.", parameterName);
    }
}

public sealed class TrackerWorkItemQuery
{
    public TrackerWorkItemQuery(
        Guid projectId,
        IReadOnlyList<string>? keys = null,
        IReadOnlyList<string>? statuses = null,
        string? parentKeyOrId = null,
        DateTimeOffset? updatedSince = null,
        int maxResults = TrackerLimits.MaxWorkItems,
        int maxPages = TrackerLimits.MaxPages)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id is required.", nameof(projectId));
        }

        if (maxResults is < 1 or > TrackerLimits.MaxWorkItems)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResults));
        }

        if (maxPages is < 1 or > TrackerLimits.MaxPages)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPages));
        }

        ProjectId = projectId;
        Keys = Copy(keys, nameof(keys));
        Statuses = Copy(statuses, nameof(statuses));
        ParentKeyOrId = string.IsNullOrWhiteSpace(parentKeyOrId) ? null : Normalize(parentKeyOrId, nameof(parentKeyOrId));
        UpdatedSince = updatedSince;
        MaxResults = maxResults;
        MaxPages = maxPages;
    }

    public Guid ProjectId { get; }
    public IReadOnlyList<string> Keys { get; }
    public IReadOnlyList<string> Statuses { get; }
    public string? ParentKeyOrId { get; }
    public DateTimeOffset? UpdatedSince { get; }
    public int MaxResults { get; }
    public int MaxPages { get; }

    private static IReadOnlyList<string> Copy(IReadOnlyList<string>? values, string parameterName)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<string>();
        }

        if (values.Count > TrackerLimits.MaxWorkItems || values.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Tracker query values exceed their supported bound.", parameterName);
        }

        return values.Select(value => Normalize(value, parameterName)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static string Normalize(string value, string parameterName)
    {
        var normalized = value.Trim();
        return normalized.Length <= TrackerLimits.MaxStringLength
            ? normalized
            : throw new ArgumentException("Tracker query value exceeds its supported bound.", parameterName);
    }
}

public sealed class TrackerMutationTarget
{
    public TrackerMutationTarget(
        TrackerWorkItemIdentity workItem,
        TrackerWorkItemIdentity? relatedWorkItem = null,
        string? linkType = null,
        TrackerLinkDirection linkDirection = TrackerLinkDirection.Outward,
        string? remoteTypeId = null,
        string? relationship = null)
    {
        WorkItem = workItem ?? throw new ArgumentNullException(nameof(workItem));
        RelatedWorkItem = relatedWorkItem;
        LinkType = string.IsNullOrWhiteSpace(linkType) ? null : Normalize(linkType, nameof(linkType));
        RemoteTypeId = string.IsNullOrWhiteSpace(remoteTypeId) ? null : Normalize(remoteTypeId, nameof(remoteTypeId));
        Relationship = string.IsNullOrWhiteSpace(relationship) ? null : Normalize(relationship, nameof(relationship));
        if (RelatedWorkItem is null && (LinkType is not null || RemoteTypeId is not null || Relationship is not null))
        {
            throw new ArgumentException("A link type requires a related work item.", nameof(relatedWorkItem));
        }

        if (RelatedWorkItem is not null && LinkType is null && RemoteTypeId is null)
        {
            throw new ArgumentException("A related work item requires an exact remote link type identity.", nameof(linkType));
        }

        if (!Enum.IsDefined(linkDirection))
        {
            throw new ArgumentException("Tracker link direction is undefined.", nameof(linkDirection));
        }

        LinkDirection = linkDirection;
    }

    /// <summary>The current synchronization work item whose state fingerprint authorizes the mutation.</summary>
    public TrackerWorkItemIdentity WorkItem { get; }
    /// <summary>The peer endpoint of the relationship relative to <see cref="WorkItem"/>.</summary>
    public TrackerWorkItemIdentity? RelatedWorkItem { get; }
    /// <summary>Compatibility alias for the exact remote type name.</summary>
    public string? LinkType { get; }
    public string? RemoteTypeName => LinkType;
    public string? RemoteTypeId { get; }
    public string? Relationship { get; }
    public TrackerLinkDirection LinkDirection { get; }
    public string CanonicalIdentity =>
        $"{WorkItem.CanonicalIdentity}|{LinkDirection}|{Relationship ?? "(no-relationship)"}|{RemoteTypeId ?? "(no-type-id)"}|{RemoteTypeName ?? "(no-type-name)"}|{RelatedWorkItem?.CanonicalIdentity ?? "(no-related)"}";

    private static string Normalize(string value, string parameterName)
    {
        var normalized = value.Trim();
        return normalized.Length <= TrackerLimits.MaxStringLength
            ? normalized
            : throw new ArgumentException("Tracker target value exceeds its supported bound.", parameterName);
    }
}

public sealed class TrackerMutationAuthority
{
    public TrackerMutationAuthority(
        Guid authorityId,
        Guid projectId,
        TrackerProjectIdentity tracker,
        TrackerMutationTarget target,
        TrackerMutationKind mutationKind,
        string expectedStateIdentity,
        string actorIdentity,
        string correlationId,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        string? contentIdentity = null,
        string? contentHash = null)
    {
        if (authorityId == Guid.Empty || projectId == Guid.Empty)
        {
            throw new ArgumentException("Authority and project identifiers are required.");
        }

        if (!Enum.IsDefined(mutationKind))
        {
            throw new ArgumentException("Mutation kind is undefined.", nameof(mutationKind));
        }

        if (issuedAt == default || expiresAt <= issuedAt)
        {
            throw new ArgumentException("Authority issue and expiry times are invalid.");
        }

        AuthorityId = authorityId;
        ProjectId = projectId;
        Tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        MutationKind = mutationKind;
        ExpectedStateIdentity = Required(expectedStateIdentity, nameof(expectedStateIdentity));
        ActorIdentity = Required(actorIdentity, nameof(actorIdentity));
        CorrelationId = Required(correlationId, nameof(correlationId));
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
        ContentIdentity = string.IsNullOrWhiteSpace(contentIdentity) ? null : Required(contentIdentity, nameof(contentIdentity));
        ContentHash = ComputeHash();
        if (contentHash is not null && !string.Equals(ContentHash, contentHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Authority content integrity does not match its immutable payload.", nameof(contentHash));
        }
    }

    public Guid AuthorityId { get; }
    public Guid ProjectId { get; }
    public TrackerProjectIdentity Tracker { get; }
    public TrackerMutationTarget Target { get; }
    public TrackerMutationKind MutationKind { get; }
    public string ExpectedStateIdentity { get; }
    public string ActorIdentity { get; }
    public string CorrelationId { get; }
    public DateTimeOffset IssuedAt { get; }
    public DateTimeOffset ExpiresAt { get; }
    public string? ContentIdentity { get; }
    public string ContentHash { get; }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt || now < IssuedAt;

    public bool Matches(TrackerMutationRequest request, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(request);
        return !IsExpired(now) &&
            request.ProjectId == ProjectId &&
            request.Kind == MutationKind &&
            request.Target.CanonicalIdentity == Target.CanonicalIdentity &&
            request.Tracker.CanonicalIdentity == Tracker.CanonicalIdentity &&
            request.ContentIdentity == ContentIdentity;
    }

    private string ComputeHash()
    {
        var canonical = string.Join(
            "|",
            AuthorityId,
            ProjectId,
            Tracker.CanonicalIdentity,
            Target.CanonicalIdentity,
            MutationKind,
            ExpectedStateIdentity,
            ActorIdentity,
            CorrelationId,
            IssuedAt.ToUniversalTime().ToString("O"),
            ExpiresAt.ToUniversalTime().ToString("O"),
            ContentIdentity ?? "(no-content)");
        var bytes = Encoding.UTF8.GetBytes(canonical);
        if (bytes.Length > TrackerLimits.MaxAuthorityPayloadBytes)
        {
            throw new ArgumentException("Authority canonical payload exceeds its supported bound.");
        }

        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Authority text is required.", parameterName);
        }

        var normalized = value.Trim();
        return normalized.Length <= TrackerLimits.MaxStringLength
            ? normalized
            : throw new ArgumentException("Authority text exceeds its supported bound.", parameterName);
    }
}

public sealed class TrackerMutationRequest
{
    public TrackerMutationRequest(
        Guid projectId,
        TrackerProjectIdentity tracker,
        TrackerMutationKind kind,
        TrackerMutationTarget target,
        TrackerMutationAuthority? authority = null,
        string? commentBody = null,
        string? statusId = null)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id is required.", nameof(projectId));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentException("Mutation kind is undefined.", nameof(kind));
        }

        ProjectId = projectId;
        Tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        Kind = kind;
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Authority = authority;
        CommentBody = TrackerInputValidation.Comment(commentBody, nameof(commentBody));
        StatusId = TrackerInputValidation.Optional(statusId, nameof(statusId));
    }

    public Guid ProjectId { get; }
    public TrackerProjectIdentity Tracker { get; }
    public TrackerMutationKind Kind { get; }
    public TrackerMutationTarget Target { get; }
    public TrackerMutationAuthority? Authority { get; }
    public string? CommentBody { get; }
    public string? StatusId { get; }

    public string? ContentIdentity =>
        Kind == TrackerMutationKind.AddComment && CommentBody is not null
            ? TrackerCommentMetadata.ComputeBodyHash(CommentBody)
            : StatusId;
}

public sealed class TrackerMutationReceipt
{
    public const int CurrentSchemaVersion = 1;

    public TrackerMutationReceipt(
        Guid projectId,
        TrackerProjectIdentity tracker,
        TrackerMutationTarget target,
        TrackerMutationKind mutationKind,
        Guid authorityId,
        string authorityHash,
        string actorIdentity,
        string correlationId,
        DateTimeOffset attemptedAt,
        string expectedStateIdentity,
        string httpOutcome,
        TrackerEvidenceState verificationState,
        TrackerMutationOutcome finalOutcome,
        bool mayHaveModifiedRemote,
        string? bodyHash = null,
        int? bodyLength = null,
        string? remoteReference = null,
        int schemaVersion = CurrentSchemaVersion)
    {
        if (projectId == Guid.Empty || authorityId == Guid.Empty || attemptedAt == default)
        {
            throw new ArgumentException("Mutation receipt identifiers and time are required.");
        }

        if (schemaVersion != CurrentSchemaVersion)
        {
            throw new ArgumentException("Unsupported tracker mutation receipt schema.", nameof(schemaVersion));
        }

        if (!Enum.IsDefined(mutationKind) || !Enum.IsDefined(verificationState) || !Enum.IsDefined(finalOutcome))
        {
            throw new ArgumentException("Tracker mutation receipt contains an undefined state.");
        }

        if (bodyLength is < 0 or > TrackerLimits.MaxStringLength)
        {
            throw new ArgumentOutOfRangeException(nameof(bodyLength));
        }

        ProjectId = projectId;
        Tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        MutationKind = mutationKind;
        AuthorityId = authorityId;
        AuthorityHash = Required(authorityHash, nameof(authorityHash));
        ActorIdentity = Required(actorIdentity, nameof(actorIdentity));
        CorrelationId = Required(correlationId, nameof(correlationId));
        AttemptedAt = attemptedAt;
        ExpectedStateIdentity = Required(expectedStateIdentity, nameof(expectedStateIdentity));
        HttpOutcome = Required(httpOutcome, nameof(httpOutcome));
        VerificationState = verificationState;
        FinalOutcome = finalOutcome;
        MayHaveModifiedRemote = mayHaveModifiedRemote;
        BodyHash = string.IsNullOrWhiteSpace(bodyHash) ? null : NormalizeHash(bodyHash, nameof(bodyHash));
        BodyLength = bodyLength;
        RemoteReference = string.IsNullOrWhiteSpace(remoteReference) ? null : Required(remoteReference, nameof(remoteReference));
        SchemaVersion = schemaVersion;
    }

    public int SchemaVersion { get; }
    public Guid ProjectId { get; }
    public TrackerProjectIdentity Tracker { get; }
    public TrackerMutationTarget Target { get; }
    public TrackerMutationKind MutationKind { get; }
    public Guid AuthorityId { get; }
    public string AuthorityHash { get; }
    public string ActorIdentity { get; }
    public string CorrelationId { get; }
    public DateTimeOffset AttemptedAt { get; }
    public string ExpectedStateIdentity { get; }
    public string HttpOutcome { get; }
    public TrackerEvidenceState VerificationState { get; }
    public TrackerMutationOutcome FinalOutcome { get; }
    public bool MayHaveModifiedRemote { get; }
    public string? BodyHash { get; }
    public int? BodyLength { get; }
    public string? RemoteReference { get; }

    private static string Required(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Mutation receipt text is required.", parameterName)
            : value.Trim().Length <= TrackerLimits.MaxDiagnosticLength
                ? value.Trim()
                : throw new ArgumentException("Mutation receipt text exceeds its supported bound.", parameterName);

    private static string NormalizeHash(string value, string parameterName)
    {
        var normalized = value.Trim();
        return normalized.Length == 64 && normalized.All(static character => Uri.IsHexDigit(character))
            ? normalized.ToLowerInvariant()
            : throw new ArgumentException("Mutation receipt hash is malformed.", parameterName);
    }
}

public sealed class TrackerMutationResult
{
    public TrackerMutationResult(
        TrackerMutationOutcome outcome,
        string? errorMessage = null,
        TrackerMutationReceipt? receipt = null,
        bool mayHaveModifiedRemote = false,
        TrackerEvidenceState verificationState = TrackerEvidenceState.NotConfigured)
    {
        if (!Enum.IsDefined(outcome) || !Enum.IsDefined(verificationState))
        {
            throw new ArgumentException("Tracker mutation result contains an undefined state.");
        }

        Outcome = outcome;
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
            ? null
            : errorMessage.Trim().Length <= TrackerLimits.MaxDiagnosticLength
                ? errorMessage.Trim()
                : throw new ArgumentException("Tracker mutation diagnostic exceeds its supported bound.", nameof(errorMessage));
        Receipt = receipt;
        MayHaveModifiedRemote = mayHaveModifiedRemote;
        VerificationState = verificationState;
    }

    public TrackerMutationOutcome Outcome { get; }
    public string? ErrorMessage { get; }
    public TrackerMutationReceipt? Receipt { get; }
    public bool MayHaveModifiedRemote { get; }
    public TrackerEvidenceState VerificationState { get; }
}

public sealed class TrackerSynchronizationDesiredState
{
    public TrackerSynchronizationDesiredState(
        string? statusId = null,
        string? commentBody = null,
        IReadOnlyList<TrackerDependencyLink>? linksToAdd = null,
        IReadOnlyList<string>? unsupportedChanges = null)
    {
        StatusId = TrackerInputValidation.Optional(statusId, nameof(statusId));
        CommentBody = TrackerInputValidation.Comment(commentBody, nameof(commentBody));
        LinksToAdd = linksToAdd?.ToArray() ?? Array.Empty<TrackerDependencyLink>();
        UnsupportedChanges = unsupportedChanges?.Where(static value => !string.IsNullOrWhiteSpace(value)).Select(value => TrackerInputValidation.Required(value, nameof(unsupportedChanges))).Distinct(StringComparer.Ordinal).ToArray() ?? Array.Empty<string>();
        if (LinksToAdd.Count > TrackerLimits.MaxPlanOperations || UnsupportedChanges.Count > TrackerLimits.MaxPlanOperations)
        {
            throw new ArgumentException("Desired tracker changes exceed their supported bound.");
        }
    }

    public string? StatusId { get; }
    public string? CommentBody { get; }
    public IReadOnlyList<TrackerDependencyLink> LinksToAdd { get; }
    public IReadOnlyList<string> UnsupportedChanges { get; }
}

public sealed class TrackerSynchronizationRequest
{
    public TrackerSynchronizationRequest(
        Guid projectId,
        TrackerReadResult<TrackerWorkItemSnapshot> currentEvidence,
        TrackerSynchronizationDesiredState desired,
        TrackerSynchronizationDirection direction = TrackerSynchronizationDirection.TrackerAuthoritative)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id is required.", nameof(projectId));
        }

        ProjectId = projectId;
        CurrentEvidence = currentEvidence ?? throw new ArgumentNullException(nameof(currentEvidence));
        Desired = desired ?? throw new ArgumentNullException(nameof(desired));
        if (!Enum.IsDefined(direction))
        {
            throw new ArgumentException("Synchronization direction is undefined.", nameof(direction));
        }

        Direction = direction;
    }

    public Guid ProjectId { get; }
    public TrackerReadResult<TrackerWorkItemSnapshot> CurrentEvidence { get; }
    public TrackerSynchronizationDesiredState Desired { get; }
    public TrackerSynchronizationDirection Direction { get; }
}

public sealed class TrackerSynchronizationOperation
{
    public TrackerSynchronizationOperation(
        TrackerMutationKind kind,
        TrackerMutationTarget target,
        string expectedStateIdentity,
        string? commentBody = null,
        string? statusId = null)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentException("Mutation kind is undefined.", nameof(kind));
        }

        Kind = kind;
        Target = target ?? throw new ArgumentNullException(nameof(target));
        ExpectedStateIdentity = TrackerInputValidation.Required(expectedStateIdentity, nameof(expectedStateIdentity));
        CommentBody = TrackerInputValidation.Comment(commentBody, nameof(commentBody));
        StatusId = TrackerInputValidation.Optional(statusId, nameof(statusId));
    }

    public TrackerMutationKind Kind { get; }
    public TrackerMutationTarget Target { get; }
    public string ExpectedStateIdentity { get; }
    public string? CommentBody { get; }
    public string? StatusId { get; }
}

public sealed class TrackerSynchronizationPlan
{
    public TrackerSynchronizationPlan(
        Guid projectId,
        TrackerEvidenceState evidenceState,
        string? evidenceFingerprint,
        IReadOnlyList<TrackerSynchronizationOperation>? operations = null,
        IReadOnlyList<string>? conflicts = null,
        IReadOnlyList<string>? unsupportedChanges = null,
        IReadOnlyList<string>? blockers = null)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id is required.", nameof(projectId));
        }

        ProjectId = projectId;
        EvidenceState = evidenceState;
        EvidenceFingerprint = evidenceFingerprint;
        Operations = operations?.ToArray() ?? Array.Empty<TrackerSynchronizationOperation>();
        Conflicts = conflicts?.ToArray() ?? Array.Empty<string>();
        UnsupportedChanges = unsupportedChanges?.ToArray() ?? Array.Empty<string>();
        Blockers = blockers?.ToArray() ?? Array.Empty<string>();
        if (Operations.Count > TrackerLimits.MaxPlanOperations)
        {
            throw new ArgumentException("Synchronization plan exceeds its supported bound.", nameof(operations));
        }
    }

    public Guid ProjectId { get; }
    public TrackerEvidenceState EvidenceState { get; }
    public string? EvidenceFingerprint { get; }
    public IReadOnlyList<TrackerSynchronizationOperation> Operations { get; }
    public IReadOnlyList<string> Conflicts { get; }
    public IReadOnlyList<string> UnsupportedChanges { get; }
    public IReadOnlyList<string> Blockers { get; }
    public bool IsExecutable => EvidenceState == TrackerEvidenceState.Available &&
        Operations.Count > 0 && Conflicts.Count == 0 && UnsupportedChanges.Count == 0 && Blockers.Count == 0;
}

public sealed record TrackerAdapterResolution(
    TrackerAdapterResolutionStatus Status,
    IWorkItemTrackerAdapter? Adapter = null,
    TrackerConfiguration? Configuration = null,
    string? ErrorMessage = null)
{
    public bool Succeeded => Status == TrackerAdapterResolutionStatus.Resolved && Adapter is not null && Configuration is not null;
}

public interface IWorkItemTrackerAdapter
{
    TrackerProviderKind Provider { get; }

    Task<TrackerReadResult<IReadOnlyList<TrackerWorkItemSnapshot>>> DiscoverAsync(
        TrackerConfiguration configuration,
        TrackerWorkItemQuery query,
        CancellationToken cancellationToken = default);

    Task<TrackerReadResult<TrackerWorkItemSnapshot>> ReadAsync(
        TrackerConfiguration configuration,
        TrackerWorkItemIdentity target,
        TrackerWorkItemSnapshot? lastKnownValue = null,
        CancellationToken cancellationToken = default);

    Task<TrackerMutationResult> MutateAsync(
        TrackerConfiguration configuration,
        TrackerMutationRequest request,
        CancellationToken cancellationToken = default);
}

public interface IWorkItemTrackerAdapterResolver
{
    TrackerAdapterResolution Resolve(Project project);
}

public interface ITrackerMutationAuditRepository
{
    Task AppendAsync(TrackerMutationReceipt receipt, CancellationToken cancellationToken = default);
}

public interface ITrackerSynchronizationService
{
    TrackerSynchronizationPlan CreatePlan(TrackerSynchronizationRequest request);

    Task<TrackerMutationResult> ExecuteAsync(
        TrackerSynchronizationPlan plan,
        TrackerSynchronizationOperation operation,
        TrackerMutationAuthority authority,
        CancellationToken cancellationToken = default);
}
