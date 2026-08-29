using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AIUsageMonitor.Application.Security;
using AIUsageMonitor.Application.Time;
using AIUsageMonitor.Application.Trackers;

namespace AIUsageMonitor.Providers.Jira;

/// <summary>
/// Bounded Jira Cloud REST v3 adapter. It uses only configured HTTPS project metadata and the
/// opaque secure-store reference; no browser, CLI, arbitrary JQL, or arbitrary field patching is
/// involved.
/// </summary>
public sealed class JiraWorkItemTrackerAdapter : IWorkItemTrackerAdapter
{
    public const string HttpClientName = "AIUsageMonitor.Jira";

    private readonly IClock _clock;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISecureCredentialStore _credentials;
    private readonly ITrackerMutationAuditRepository _audit;

    public JiraWorkItemTrackerAdapter(
        IClock clock,
        IHttpClientFactory httpClientFactory,
        ISecureCredentialStore credentials,
        ITrackerMutationAuditRepository audit)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public TrackerProviderKind Provider => TrackerProviderKind.Jira;

    public async Task<TrackerReadResult<IReadOnlyList<TrackerWorkItemSnapshot>>> DiscoverAsync(
        TrackerConfiguration configuration,
        TrackerWorkItemQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(query);
        if (configuration.ProjectId != query.ProjectId || configuration.Identity.Provider != Provider || configuration.Identity.BaseUri is null)
        {
            return ListResult(TrackerEvidenceState.NotConfigured, configuration.Identity, null, "Tracker configuration does not match the project query.");
        }

        var credentialResult = await GetAuthorizationAsync(configuration, cancellationToken).ConfigureAwait(false);
        if (!credentialResult.Success)
        {
            return ListResult(credentialResult.State, configuration.Identity, null, credentialResult.ErrorMessage);
        }

        var capturedAt = _clock.UtcNow;
        var items = new List<TrackerWorkItemSnapshot>();
        var limitations = new List<string>();
        string? nextPageToken = null;
        var page = 0;

        while (page < query.MaxPages && items.Count < query.MaxResults)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ListResult(TrackerEvidenceState.Cancelled, configuration.Identity, null, "Tracker discovery was cancelled by the caller.", capturedAt, limitations, items);
            }
            page++;
            var payload = new
            {
                jql = BuildJql(configuration.Identity.ProjectId, query),
                maxResults = Math.Min(query.MaxResults - items.Count, TrackerLimits.MaxWorkItems),
                fields = new[] { "summary", "project", "issuetype", "status", "parent", "issuelinks", "comment" },
                nextPageToken
            };

            var responseResult = await SendAsync(
                HttpMethod.Post,
                BuildUri(configuration, "rest/api/3/search/jql"),
                credentialResult.Authorization!,
                payload,
                cancellationToken).ConfigureAwait(false);
            if (!responseResult.Success)
            {
                return ListFailure(configuration.Identity, responseResult, items, capturedAt, limitations);
            }

            try
            {
                using var document = JsonDocument.Parse(responseResult.Body!);
                var root = document.RootElement;
                if (!root.TryGetProperty("issues", out var issueArray) || issueArray.ValueKind != JsonValueKind.Array)
                {
                    return ListResult(TrackerEvidenceState.InvalidResponse, configuration.Identity, null, "Jira search response did not contain an issue array.", capturedAt, limitations);
                }

                var seenOnPage = 0;
                foreach (var issue in issueArray.EnumerateArray())
                {
                    if (seenOnPage++ >= TrackerLimits.MaxWorkItems)
                    {
                        limitations.Add("Jira returned more issues than the adapter bound.");
                        break;
                    }

                    try
                    {
                        if (TryParseIssue(issue, configuration.Identity, out var snapshot, out var issueLimitations))
                        {
                            items.Add(snapshot!);
                            limitations.AddRange(issueLimitations);
                        }
                        else
                        {
                            limitations.AddRange(issueLimitations);
                        }
                    }
                    catch (ArgumentException)
                    {
                        limitations.Add("A Jira issue exceeded a supported remote-field bound and was skipped.");
                    }

                    if (items.Count >= query.MaxResults)
                    {
                        break;
                    }
                }

                var isLast = root.TryGetProperty("isLast", out var isLastElement) && isLastElement.ValueKind == JsonValueKind.True;
                nextPageToken = root.TryGetProperty("nextPageToken", out var nextTokenElement) && nextTokenElement.ValueKind == JsonValueKind.String
                    ? Limit(nextTokenElement.GetString(), 2_000)
                    : null;
                if (isLast || string.IsNullOrWhiteSpace(nextPageToken))
                {
                    break;
                }

                if (page == query.MaxPages)
                {
                    limitations.Add("Jira search pagination stopped at the configured page bound.");
                }
            }
            catch (JsonException)
            {
                return ListResult(TrackerEvidenceState.InvalidResponse, configuration.Identity, null, "Jira search response JSON was malformed.", capturedAt, limitations);
            }
            catch (ArgumentException)
            {
                return ListResult(TrackerEvidenceState.InvalidResponse, configuration.Identity, null, "Jira search response exceeded a supported field bound.", capturedAt, limitations, items);
            }
        }

        if (items.Count >= query.MaxResults && !string.IsNullOrWhiteSpace(nextPageToken))
        {
            limitations.Add("Jira search results were capped by the requested maximum.");
        }

        return ListResult(
            limitations.Count == 0 ? TrackerEvidenceState.Available : TrackerEvidenceState.Partial,
            configuration.Identity,
            null,
            limitations.Count == 0 ? null : "Jira search completed with bounded limitations.",
            capturedAt,
            limitations,
            items);
    }

    public async Task<TrackerReadResult<TrackerWorkItemSnapshot>> ReadAsync(
        TrackerConfiguration configuration,
        TrackerWorkItemIdentity target,
        TrackerWorkItemSnapshot? lastKnownValue = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(target);
        if (configuration.Identity.Provider != Provider || configuration.Identity.BaseUri is null)
        {
            return SingleResult(TrackerEvidenceState.NotConfigured, configuration.Identity, target, null, "Jira configuration is incomplete.", lastKnownValue: lastKnownValue);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return SingleResult(TrackerEvidenceState.Cancelled, configuration.Identity, target, null, "Tracker read was cancelled by the caller.", lastKnownValue: lastKnownValue);
        }

        if (target.Provider != Provider || !string.Equals(target.ProjectId, configuration.Identity.ProjectId, StringComparison.OrdinalIgnoreCase))
        {
            return SingleResult(TrackerEvidenceState.InvalidResponse, configuration.Identity, target, null, "Tracker target does not belong to the configured Jira project.", lastKnownValue: lastKnownValue);
        }

        var credentialResult = await GetAuthorizationAsync(configuration, cancellationToken).ConfigureAwait(false);
        if (!credentialResult.Success)
        {
            return SingleResult(credentialResult.State, configuration.Identity, target, null, credentialResult.ErrorMessage, lastKnownValue: lastKnownValue);
        }

        var responseResult = await SendAsync<object?>(
            HttpMethod.Get,
            BuildUri(configuration, $"rest/api/3/issue/{Uri.EscapeDataString(target.KeyOrId)}?fields=summary,project,issuetype,status,parent,issuelinks,comment&fieldsByKeys=false"),
            credentialResult.Authorization!,
            payload: null,
            cancellationToken).ConfigureAwait(false);
        if (!responseResult.Success)
        {
            return SingleFailure(configuration.Identity, target, lastKnownValue, responseResult);
        }

        try
        {
            using var document = JsonDocument.Parse(responseResult.Body!);
            if (!TryParseIssue(document.RootElement, configuration.Identity, out var snapshot, out var limitations))
            {
                return SingleResult(TrackerEvidenceState.InvalidResponse, configuration.Identity, target, null, "Jira issue response did not contain the required core fields.", limitations, lastKnownValue);
            }

            if (!IdentityMatches(target, snapshot!.Identity))
            {
                return SingleResult(TrackerEvidenceState.InvalidResponse, configuration.Identity, target, null, "Jira returned a different work-item identity than requested.", lastKnownValue: lastKnownValue);
            }

            return SingleResult(
                limitations.Count == 0 ? TrackerEvidenceState.Available : TrackerEvidenceState.Partial,
                configuration.Identity,
                snapshot.Identity,
                snapshot,
                limitations.Count == 0 ? null : "Jira issue was read with bounded limitations.",
                limitations);
        }
        catch (JsonException)
        {
            return SingleResult(TrackerEvidenceState.InvalidResponse, configuration.Identity, target, null, "Jira issue response JSON was malformed.", lastKnownValue: lastKnownValue);
        }
        catch (ArgumentException)
        {
            return SingleResult(TrackerEvidenceState.InvalidResponse, configuration.Identity, target, null, "Jira issue response exceeded a supported field bound.", lastKnownValue: lastKnownValue);
        }
    }

    public async Task<TrackerMutationResult> MutateAsync(
        TrackerConfiguration configuration,
        TrackerMutationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(request);
        var attemptedAt = _clock.UtcNow;
        if (request.Authority is null)
        {
            return new(TrackerMutationOutcome.InvalidAuthority, "An explicit tracker mutation authority is required.");
        }

        if (configuration.ProjectId != request.ProjectId ||
            configuration.Identity.CanonicalIdentity != request.Tracker.CanonicalIdentity)
        {
            return new(TrackerMutationOutcome.InvalidAuthority, "Tracker mutation configuration does not exactly match the requested project and tracker.");
        }

        if (configuration.Identity.Provider != Provider || configuration.Identity.BaseUri is null ||
            !request.Authority.Matches(request, attemptedAt))
        {
            return new(TrackerMutationOutcome.InvalidAuthority, "Tracker mutation authority does not exactly match the requested operation.");
        }

        if (request.Kind == TrackerMutationKind.AddComment &&
            (string.IsNullOrEmpty(request.CommentBody) || request.CommentBody.Length > TrackerLimits.MaxStringLength))
        {
            return new(TrackerMutationOutcome.InvalidAuthority, "Comment mutation content is empty or oversized.");
        }

        if (request.Kind == TrackerMutationKind.TransitionStatus && string.IsNullOrWhiteSpace(request.StatusId))
        {
            return new(TrackerMutationOutcome.InvalidAuthority, "A target status id is required for a transition.");
        }

        if (request.Kind == TrackerMutationKind.AddDependencyLink &&
            (request.Target.RelatedWorkItem is null || string.IsNullOrWhiteSpace(request.Target.LinkType)))
        {
            return new(TrackerMutationOutcome.InvalidAuthority, "An explicit related work item and link type are required.");
        }

        if (request.Target.WorkItem.Provider != Provider ||
            !string.Equals(request.Target.WorkItem.ProjectId, configuration.Identity.ProjectId, StringComparison.OrdinalIgnoreCase) ||
            (request.Target.RelatedWorkItem is not null &&
             (request.Target.RelatedWorkItem.Provider != Provider ||
              !string.Equals(request.Target.RelatedWorkItem.ProjectId, configuration.Identity.ProjectId, StringComparison.OrdinalIgnoreCase))))
        {
            return new(TrackerMutationOutcome.InvalidAuthority, "Tracker mutation target does not belong to the configured Jira project.");
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return new(TrackerMutationOutcome.Cancelled, "Tracker mutation was cancelled before the remote mutation request.", verificationState: TrackerEvidenceState.Cancelled);
        }
        var fresh = await ReadAsync(configuration, request.Target.WorkItem, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (fresh.State != TrackerEvidenceState.Available || fresh.Value is null)
        {
            return new(
                fresh.State == TrackerEvidenceState.Stale ? TrackerMutationOutcome.Conflict : MapReadOutcome(fresh.State),
                fresh.ErrorMessage ?? "Fresh exact target evidence was not available.",
                mayHaveModifiedRemote: false,
                verificationState: fresh.State);
        }

        if (!string.Equals(request.Authority.ExpectedStateIdentity, fresh.Value.StateFingerprint, StringComparison.OrdinalIgnoreCase))
        {
            return new(TrackerMutationOutcome.Conflict, "The Jira target changed after the authority was issued.", verificationState: TrackerEvidenceState.Available);
        }

        if (request.Kind == TrackerMutationKind.TransitionStatus)
        {
            var transitions = await GetTransitionsAsync(configuration, request.Target.WorkItem, cancellationToken).ConfigureAwait(false);
            if (!transitions.Success)
            {
                return await FinishAttemptAsync(
                    request,
                    attemptedAt,
                    transitions.Outcome,
                    transitions.ErrorMessage,
                    transitions.HttpOutcome,
                    mayHaveModifiedRemote: false,
                    verificationState: transitions.State,
                    remoteReference: null,
                    cancellationToken).ConfigureAwait(false);
            }

            var matches = transitions.Values!.Where(value => string.Equals(value.TargetStatusId, request.StatusId, StringComparison.Ordinal)).ToArray();
            if (matches.Length != 1 || !matches[0].Available)
            {
                return new(TrackerMutationOutcome.Conflict, matches.Length == 0 ? "No exact permitted Jira transition reaches the requested status." : "The requested Jira transition is ambiguous or unavailable.", verificationState: TrackerEvidenceState.Available);
            }

            return await SendMutationAsync(
                request,
                attemptedAt,
                configuration,
                new { transition = new { id = matches[0].TransitionId } },
                HttpMethod.Post,
                BuildUri(configuration, $"rest/api/3/issue/{Uri.EscapeDataString(request.Target.WorkItem.KeyOrId)}/transitions"),
                verify: snapshot => string.Equals(snapshot.Status.Id, request.StatusId, StringComparison.Ordinal),
                remoteReference: matches[0].TransitionId,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        if (request.Kind == TrackerMutationKind.AddComment)
        {
            var bodyHash = TrackerCommentMetadata.ComputeBodyHash(request.CommentBody!);
            var payload = new
            {
                body = new
                {
                    type = "doc",
                    version = 1,
                    content = new[]
                    {
                        new
                        {
                            type = "paragraph",
                            content = new[] { new { type = "text", text = request.CommentBody } }
                        }
                    }
                }
            };
            return await SendMutationAsync(
                request,
                attemptedAt,
                configuration,
                payload,
                HttpMethod.Post,
                BuildUri(configuration, $"rest/api/3/issue/{Uri.EscapeDataString(request.Target.WorkItem.KeyOrId)}/comment"),
                verify: snapshot => snapshot.Comments.Any(comment => string.Equals(comment.BodyHash, bodyHash, StringComparison.OrdinalIgnoreCase)),
                remoteReference: null,
                bodyHash,
                cancellationToken).ConfigureAwait(false);
        }

        var linkPayload = request.Target.LinkDirection == TrackerLinkDirection.Outward
            ? new
            {
                outwardIssue = new { key = request.Target.WorkItem.KeyOrId },
                inwardIssue = new { key = request.Target.RelatedWorkItem!.KeyOrId },
                type = new { name = request.Target.LinkType }
            }
            : new
            {
                outwardIssue = new { key = request.Target.RelatedWorkItem!.KeyOrId },
                inwardIssue = new { key = request.Target.WorkItem.KeyOrId },
                type = new { name = request.Target.LinkType }
            };
        return await SendMutationAsync(
            request,
            attemptedAt,
            configuration,
            linkPayload,
            HttpMethod.Post,
            BuildUri(configuration, "rest/api/3/issueLink"),
            verify: snapshot => snapshot.Links.Any(link => LinkMatches(link, request.Target)),
            remoteReference: request.Target.CanonicalIdentity,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private async Task<TrackerMutationResult> SendMutationAsync<TPayload>(
        TrackerMutationRequest request,
        DateTimeOffset attemptedAt,
        TrackerConfiguration configuration,
        TPayload payload,
        HttpMethod method,
        Uri uri,
        Func<TrackerWorkItemSnapshot, bool> verify,
        string? remoteReference,
        string? bodyHash = null,
        CancellationToken cancellationToken = default)
    {
        var credentialResult = await GetAuthorizationAsync(configuration, cancellationToken).ConfigureAwait(false);
        if (!credentialResult.Success)
        {
            return new(MapCredentialOutcome(credentialResult.State), credentialResult.ErrorMessage, mayHaveModifiedRemote: false, verificationState: credentialResult.State);
        }

        var responseResult = await SendAsync(method, uri, credentialResult.Authorization!, payload, cancellationToken).ConfigureAwait(false);
        if (!responseResult.Success)
        {
            return await FinishAttemptAsync(
                request,
                attemptedAt,
                responseResult.Outcome,
                responseResult.ErrorMessage,
                responseResult.HttpOutcome,
                responseResult.MayHaveModifiedRemote,
                responseResult.State,
                remoteReference,
                cancellationToken,
                bodyHash).ConfigureAwait(false);
        }

        var remoteId = remoteReference;
        if (request.Kind == TrackerMutationKind.AddComment && !string.IsNullOrWhiteSpace(responseResult.Body))
        {
            try
            {
                using var document = JsonDocument.Parse(responseResult.Body);
                remoteId = document.RootElement.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String
                    ? Limit(id.GetString(), TrackerLimits.MaxStringLength)
                    : remoteReference;
            }
            catch (JsonException)
            {
                // Verification below remains authoritative; the response id is only a convenience.
            }
        }

        var verification = await ReadAsync(configuration, request.Target.WorkItem, cancellationToken: cancellationToken).ConfigureAwait(false);
        var verified = verification.State == TrackerEvidenceState.Available && verification.Value is not null && verify(verification.Value);
        var outcome = verified ? TrackerMutationOutcome.Succeeded : TrackerMutationOutcome.ReconciliationRequired;
        var message = verified ? null : "Jira accepted the mutation, but independent post-mutation verification did not complete.";
        return await FinishAttemptAsync(
            request,
            attemptedAt,
            outcome,
            message,
            responseResult.HttpOutcome,
            mayHaveModifiedRemote: true,
            verification.State,
            remoteId,
            cancellationToken,
            bodyHash).ConfigureAwait(false);
    }

    private async Task<TrackerMutationResult> FinishAttemptAsync(
        TrackerMutationRequest request,
        DateTimeOffset attemptedAt,
        TrackerMutationOutcome outcome,
        string? errorMessage,
        string httpOutcome,
        bool mayHaveModifiedRemote,
        TrackerEvidenceState verificationState,
        string? remoteReference,
        CancellationToken cancellationToken,
        string? bodyHash = null)
    {
        var authority = request.Authority!;
        TrackerMutationReceipt receipt;
        try
        {
            receipt = new TrackerMutationReceipt(
                request.ProjectId,
                request.Tracker,
                request.Target,
                request.Kind,
                authority.AuthorityId,
                authority.ContentHash,
                authority.ActorIdentity,
                authority.CorrelationId,
                attemptedAt,
                authority.ExpectedStateIdentity,
                httpOutcome,
                verificationState,
                outcome,
                mayHaveModifiedRemote,
                bodyHash,
                request.Kind == TrackerMutationKind.AddComment ? request.CommentBody?.Length : null,
                remoteReference);
            await _audit.AppendAsync(receipt, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new(TrackerMutationOutcome.ReconciliationRequired, "Mutation audit persistence was cancelled after the remote attempt.", mayHaveModifiedRemote: true, verificationState: TrackerEvidenceState.Cancelled);
        }
        catch (Exception) when (outcome == TrackerMutationOutcome.Succeeded || mayHaveModifiedRemote)
        {
            return new(TrackerMutationOutcome.ReconciliationRequired, "Remote mutation evidence could not be persisted; reconciliation is required.", mayHaveModifiedRemote: true, verificationState: verificationState);
        }
        catch (Exception)
        {
            return new(outcome, "Tracker mutation was not fully audited locally.", mayHaveModifiedRemote: false, verificationState: verificationState);
        }

        return new(outcome, errorMessage, receipt, mayHaveModifiedRemote, verificationState);
    }

    private async Task<TransitionResult> GetTransitionsAsync(
        TrackerConfiguration configuration,
        TrackerWorkItemIdentity target,
        CancellationToken cancellationToken)
    {
        var credentialResult = await GetAuthorizationAsync(configuration, cancellationToken).ConfigureAwait(false);
        if (!credentialResult.Success)
        {
            return new(false, MapCredentialOutcome(credentialResult.State), credentialResult.State, credentialResult.ErrorMessage, credentialResult.State.ToString());
        }

        var responseResult = await SendAsync<object?>(
            HttpMethod.Get,
            BuildUri(configuration, $"rest/api/3/issue/{Uri.EscapeDataString(target.KeyOrId)}/transitions"),
            credentialResult.Authorization!,
            null,
            cancellationToken).ConfigureAwait(false);
        if (!responseResult.Success)
        {
            return new(false, responseResult.Outcome, responseResult.State, responseResult.ErrorMessage, responseResult.HttpOutcome);
        }

        try
        {
            using var document = JsonDocument.Parse(responseResult.Body!);
            if (!document.RootElement.TryGetProperty("transitions", out var values) || values.ValueKind != JsonValueKind.Array)
            {
                return new(false, TrackerMutationOutcome.InvalidResponse, TrackerEvidenceState.InvalidResponse, "Jira transition response did not contain a transition array.", responseResult.HttpOutcome);
            }

            var transitions = new List<TransitionValue>();
            foreach (var value in values.EnumerateArray().Take(TrackerLimits.MaxLinks))
            {
                if (!value.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String ||
                    !value.TryGetProperty("to", out var to) || to.ValueKind != JsonValueKind.Object ||
                    !to.TryGetProperty("id", out var statusId) || statusId.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var available = !value.TryGetProperty("isAvailable", out var isAvailable) || isAvailable.ValueKind != JsonValueKind.False;
                transitions.Add(new(id.GetString()!, statusId.GetString()!, available));
            }

            return new(true, TrackerMutationOutcome.Succeeded, TrackerEvidenceState.Available, null, responseResult.HttpOutcome, transitions);
        }
        catch (JsonException)
        {
            return new(false, TrackerMutationOutcome.InvalidResponse, TrackerEvidenceState.InvalidResponse, "Jira transition response JSON was malformed.", responseResult.HttpOutcome);
        }
    }

    private async Task<AuthorizationResult> GetAuthorizationAsync(
        TrackerConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(configuration.AuthReference))
        {
            return new(false, TrackerEvidenceState.AuthenticationRequired, null, "A Jira auth reference is not configured.");
        }

        string? secret;
        try
        {
            secret = await _credentials.RetrieveAsync(configuration.AuthReference, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new(false, TrackerEvidenceState.Cancelled, null, "Tracker credential lookup was cancelled by the caller.");
        }
        if (string.IsNullOrWhiteSpace(secret))
        {
            return new(false, TrackerEvidenceState.AuthenticationRequired, null, "The configured Jira auth reference has no secret material.");
        }

        var value = secret.Trim();
        if (value.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            var token = value[6..].Trim();
            return token.Length == 0
                ? new(false, TrackerEvidenceState.AuthenticationRequired, null, "The configured Jira Basic credential is empty.")
                : new(true, TrackerEvidenceState.Available, new AuthenticationHeaderValue("Basic", token), null);
        }

        if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = value[7..].Trim();
            return token.Length == 0
                ? new(false, TrackerEvidenceState.AuthenticationRequired, null, "The configured Jira bearer credential is empty.")
                : new(true, TrackerEvidenceState.Available, new AuthenticationHeaderValue("Bearer", token), null);
        }

        // Raw secure-store values are treated as bearer tokens to preserve the existing provider
        // credential boundary while keeping the HTTP authorization header out of persistence.
        return new(true, TrackerEvidenceState.Available, new AuthenticationHeaderValue("Bearer", value), null);
    }

    private async Task<ResponseResult> SendAsync<TPayload>(
        HttpMethod method,
        Uri uri,
        AuthenticationHeaderValue authorization,
        TPayload? payload,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = authorization;
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (payload is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        }

        try
        {
            using var response = await _httpClientFactory.CreateClient(HttpClientName)
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            var body = await ReadBoundedBodyAsync(response, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return new(true, TrackerMutationOutcome.Succeeded, TrackerEvidenceState.Available, null, $"{(int)response.StatusCode} {response.StatusCode}", body, false);
            }

            var mapped = MapStatus(response.StatusCode);
            return new(false, mapped.Outcome, mapped.State, mapped.Message, $"{(int)response.StatusCode} {response.StatusCode}", body, (int)response.StatusCode >= 500);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new(false, TrackerMutationOutcome.Cancelled, TrackerEvidenceState.Cancelled, "Tracker request was cancelled by the caller.", "cancelled", null, method != HttpMethod.Get);
        }
        catch (OperationCanceledException)
        {
            return new(false, method == HttpMethod.Get ? TrackerMutationOutcome.Unavailable : TrackerMutationOutcome.ReconciliationRequired, TrackerEvidenceState.Unavailable, "Tracker request timed out or was interrupted.", "timeout-or-transport-cancelled", null, method != HttpMethod.Get);
        }
        catch (HttpRequestException)
        {
            return new(false, method == HttpMethod.Get ? TrackerMutationOutcome.Unavailable : TrackerMutationOutcome.ReconciliationRequired, TrackerEvidenceState.Unavailable, "Tracker request was unavailable.", "transport-failure", null, method != HttpMethod.Get);
        }
        catch (ResponseTooLargeException)
        {
            return new(false, TrackerMutationOutcome.InvalidResponse, TrackerEvidenceState.InvalidResponse, "Tracker response exceeded its bounded size.", "oversized-response", null, false);
        }
    }

    private static async Task<string> ReadBoundedBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength > TrackerLimits.MaxResponseBytes)
        {
            throw new ResponseTooLargeException();
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var memory = new MemoryStream();
        var buffer = new byte[8 * 1024];
        while (true)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            if (memory.Length + read > TrackerLimits.MaxResponseBytes)
            {
                throw new ResponseTooLargeException();
            }

            memory.Write(buffer, 0, read);
        }

        return Encoding.UTF8.GetString(memory.ToArray());
    }

    private static string BuildJql(string projectId, TrackerWorkItemQuery query)
    {
        var clauses = new List<string> { $"project = {QuoteJql(projectId)}" };
        if (query.Keys.Count > 0)
        {
            clauses.Add($"key in ({string.Join(", ", query.Keys.Select(QuoteJql))})");
        }

        if (query.Statuses.Count > 0)
        {
            clauses.Add($"status in ({string.Join(", ", query.Statuses.Select(QuoteJql))})");
        }

        if (query.ParentKeyOrId is not null)
        {
            clauses.Add($"parent = {QuoteJql(query.ParentKeyOrId)}");
        }

        if (query.UpdatedSince is { } updatedSince)
        {
            clauses.Add($"updated >= {QuoteJql(updatedSince.ToUniversalTime().ToString("yyyy-MM-dd HH:mm"))}");
        }

        return string.Join(" AND ", clauses) + " ORDER BY updated ASC, key ASC";
    }

    private static string QuoteJql(string value) =>
        $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"";

    private static Uri BuildUri(TrackerConfiguration configuration, string relative) =>
        new(configuration.Identity.BaseUri!, relative);

    private static bool TryParseIssue(
        JsonElement issue,
        TrackerProjectIdentity configuredProject,
        out TrackerWorkItemSnapshot? snapshot,
        out List<string> limitations)
    {
        snapshot = null;
        limitations = [];
        if (issue.ValueKind != JsonValueKind.Object || !issue.TryGetProperty("fields", out var fields) || fields.ValueKind != JsonValueKind.Object)
        {
            limitations.Add("A Jira issue item was malformed and skipped.");
            return false;
        }

        var key = StringProperty(issue, "key");
        var id = StringProperty(issue, "id");
        var project = fields.TryGetProperty("project", out var projectElement) && projectElement.ValueKind == JsonValueKind.Object
            ? projectElement
            : default;
        var remoteProjectId = StringProperty(project, "key") ?? StringProperty(project, "id");
        if (key is null || id is null || remoteProjectId is null ||
            !string.Equals(remoteProjectId, configuredProject.ProjectId, StringComparison.OrdinalIgnoreCase))
        {
            limitations.Add("A Jira issue omitted a required or matching project identity and was skipped.");
            return false;
        }

        var issueType = fields.TryGetProperty("issuetype", out var typeElement) ? StringProperty(typeElement, "name") : null;
        var summary = StringProperty(fields, "summary");
        var statusElement = fields.TryGetProperty("status", out var status) ? status : default;
        var statusId = StringProperty(statusElement, "id");
        var statusName = StringProperty(statusElement, "name");
        if (issueType is null || summary is null || statusId is null || statusName is null)
        {
            limitations.Add("A Jira issue omitted a required core field and was skipped.");
            return false;
        }

        var identity = new TrackerWorkItemIdentity(
            TrackerProviderKind.Jira,
            configuredProject.ProjectId,
            key,
            id,
            TryReferenceUri(StringProperty(issue, "self")));
        var statusCategory = statusElement.TryGetProperty("statusCategory", out var category)
            ? StringProperty(category, "key") ?? StringProperty(category, "name")
            : null;

        DateTimeOffset? updatedAt = null;
        var updatedText = StringProperty(issue, "updated") ?? StringProperty(fields, "updated");
        if (updatedText is not null && DateTimeOffset.TryParse(updatedText, out var parsedUpdated))
        {
            updatedAt = parsedUpdated;
        }
        else if (updatedText is not null)
        {
            limitations.Add("Jira issue updated time was malformed.");
        }

        TrackerHierarchyReference? parent = null;
        if (fields.TryGetProperty("parent", out var parentElement) && parentElement.ValueKind == JsonValueKind.Object)
        {
            var parentKey = StringProperty(parentElement, "key") ?? StringProperty(parentElement, "id");
            if (parentKey is not null)
            {
                parent = new TrackerHierarchyReference(new TrackerWorkItemIdentity(TrackerProviderKind.Jira, configuredProject.ProjectId, parentKey, StringProperty(parentElement, "id")));
            }
            else
            {
                limitations.Add("Jira parent metadata was malformed.");
            }
        }

        var links = ParseLinks(fields, identity, configuredProject, limitations);
        var comments = ParseComments(fields, limitations);
        try
        {
            snapshot = new TrackerWorkItemSnapshot(
                identity,
                configuredProject,
                issueType,
                summary,
                new TrackerStatusSnapshot(statusId, statusName, statusCategory),
                updatedAt,
                parent,
                links,
                comments,
                identity.ReferenceUri);
            return true;
        }
        catch (ArgumentException)
        {
            limitations.Add("Jira issue normalized data exceeded a supported bound.");
            return false;
        }
    }

    private static IReadOnlyList<TrackerDependencyLink> ParseLinks(
        JsonElement fields,
        TrackerWorkItemIdentity current,
        TrackerProjectIdentity configuredProject,
        ICollection<string> limitations)
    {
        if (!fields.TryGetProperty("issuelinks", out var linksElement))
        {
            return Array.Empty<TrackerDependencyLink>();
        }

        if (linksElement.ValueKind != JsonValueKind.Array)
        {
            limitations.Add("Jira issue links were present in an unsupported shape.");
            return Array.Empty<TrackerDependencyLink>();
        }

        var links = new List<TrackerDependencyLink>();
        var index = 0;
        foreach (var link in linksElement.EnumerateArray())
        {
            if (index++ >= TrackerLimits.MaxLinks)
            {
                limitations.Add("Jira issue links were capped by the adapter bound.");
                break;
            }

            if (!link.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.Object)
            {
                limitations.Add("A Jira issue link had no usable relationship type.");
                continue;
            }

            var linkId = StringProperty(link, "id");
            if (link.TryGetProperty("outwardIssue", out var outward) && outward.ValueKind == JsonValueKind.Object)
            {
                var related = ParseLinkedIdentity(outward, configuredProject);
                var relationship = StringProperty(type, "outward");
                if (related is null || relationship is null)
                {
                    limitations.Add("A Jira outward issue link was malformed.");
                    continue;
                }

                links.Add(new TrackerDependencyLink(current, related, relationship, TrackerLinkDirection.Outward, linkId, IsKnownDependency(relationship)));
            }
            else if (link.TryGetProperty("inwardIssue", out var inward) && inward.ValueKind == JsonValueKind.Object)
            {
                var related = ParseLinkedIdentity(inward, configuredProject);
                var relationship = StringProperty(type, "inward");
                if (related is null || relationship is null)
                {
                    limitations.Add("A Jira inward issue link was malformed.");
                    continue;
                }

                links.Add(new TrackerDependencyLink(related, current, relationship, TrackerLinkDirection.Inward, linkId, IsKnownDependency(relationship)));
            }
            else
            {
                limitations.Add("A Jira issue link had neither an inward nor outward issue.");
            }
        }

        return links;
    }

    private static IReadOnlyList<TrackerCommentMetadata> ParseComments(
        JsonElement fields,
        ICollection<string> limitations)
    {
        if (!fields.TryGetProperty("comment", out var commentField))
        {
            return Array.Empty<TrackerCommentMetadata>();
        }

        var commentArray = commentField.ValueKind == JsonValueKind.Array
            ? commentField
            : commentField.ValueKind == JsonValueKind.Object && commentField.TryGetProperty("comments", out var nested)
                ? nested
                : default;
        if (commentArray.ValueKind != JsonValueKind.Array)
        {
            limitations.Add("Jira comments were present in an unsupported shape.");
            return Array.Empty<TrackerCommentMetadata>();
        }

        var comments = new List<TrackerCommentMetadata>();
        var index = 0;
        foreach (var comment in commentArray.EnumerateArray())
        {
            if (index++ >= TrackerLimits.MaxComments)
            {
                limitations.Add("Jira comments were capped by the adapter bound.");
                break;
            }

            try
            {
                var id = StringProperty(comment, "id");
                var body = comment.TryGetProperty("body", out var bodyElement) ? ExtractBodyText(bodyElement) : null;
                if (id is null || body is null)
                {
                    limitations.Add("A Jira comment was malformed and skipped.");
                    continue;
                }

                var authorElement = comment.TryGetProperty("author", out var author) ? author : default;
                var authorName = StringProperty(authorElement, "displayName") ?? StringProperty(authorElement, "accountId");
                comments.Add(new TrackerCommentMetadata(
                    id,
                    authorName,
                    ParseTime(StringProperty(comment, "created")),
                    ParseTime(StringProperty(comment, "updated")),
                    body,
                    TryReferenceUri(StringProperty(comment, "self"))));
            }
            catch (ArgumentException)
            {
                limitations.Add("A Jira comment exceeded a supported bound and was skipped.");
            }
        }

        if (commentField.ValueKind == JsonValueKind.Object &&
            commentField.TryGetProperty("total", out var total) && total.ValueKind == JsonValueKind.Number &&
            total.TryGetInt32(out var totalCount) && totalCount > comments.Count)
        {
            limitations.Add("Jira returned only a bounded comment page.");
        }

        return comments;
    }

    private static TrackerWorkItemIdentity? ParseLinkedIdentity(JsonElement issue, TrackerProjectIdentity configuredProject)
    {
        var key = StringProperty(issue, "key") ?? StringProperty(issue, "id");
        if (key is null)
        {
            return null;
        }

        var project = issue.TryGetProperty("fields", out var fields) && fields.ValueKind == JsonValueKind.Object &&
                      fields.TryGetProperty("project", out var projectElement) && projectElement.ValueKind == JsonValueKind.Object
            ? StringProperty(projectElement, "key") ?? StringProperty(projectElement, "id")
            : configuredProject.ProjectId;
        return project is null ? null : new TrackerWorkItemIdentity(TrackerProviderKind.Jira, project, key, StringProperty(issue, "id"), TryReferenceUri(StringProperty(issue, "self")));
    }

    private static string? ExtractBodyText(JsonElement body)
    {
        if (body.ValueKind == JsonValueKind.String)
        {
            return Limit(body.GetString(), TrackerLimits.MaxStringLength);
        }

        if (body.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var builder = new StringBuilder();
        AppendBodyText(body, builder, 0);
        return builder.Length == 0 ? null : builder.ToString();
    }

    private static void AppendBodyText(JsonElement value, StringBuilder builder, int depth)
    {
        if (depth > 32)
        {
            throw new ArgumentException("Jira comment body nesting exceeded its bound.");
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            if (value.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
            {
                builder.Append(text.GetString());
                if (builder.Length > TrackerLimits.MaxStringLength)
                {
                    throw new ArgumentException("Jira comment body exceeded its bound.");
                }
            }

            if (value.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in content.EnumerateArray().Take(128))
                {
                    AppendBodyText(child, builder, depth + 1);
                }
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in value.EnumerateArray().Take(128))
            {
                AppendBodyText(child, builder, depth + 1);
            }
        }
    }

    private static bool LinkMatches(TrackerDependencyLink link, TrackerMutationTarget target) =>
        string.Equals(link.Source.CanonicalIdentity, target.LinkDirection == TrackerLinkDirection.Outward
            ? target.WorkItem.CanonicalIdentity
            : target.RelatedWorkItem!.CanonicalIdentity, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(link.Target.CanonicalIdentity, target.LinkDirection == TrackerLinkDirection.Outward
            ? target.RelatedWorkItem!.CanonicalIdentity
            : target.WorkItem.CanonicalIdentity, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(link.Relationship, target.LinkType, StringComparison.OrdinalIgnoreCase) &&
        link.Direction == target.LinkDirection;

    private static bool IdentityMatches(TrackerWorkItemIdentity expected, TrackerWorkItemIdentity actual) =>
        expected.Provider == actual.Provider &&
        string.Equals(expected.ProjectId, actual.ProjectId, StringComparison.OrdinalIgnoreCase) &&
        (string.Equals(expected.KeyOrId, actual.KeyOrId, StringComparison.OrdinalIgnoreCase) ||
         expected.RemoteId is not null && string.Equals(expected.RemoteId, actual.RemoteId, StringComparison.OrdinalIgnoreCase));

    private static bool IsKnownDependency(string relationship) =>
        relationship.Trim().ToLowerInvariant() is "blocks" or "is blocked by" or "depends on" or "is depended by" or "is dependent on";

    private static string? StringProperty(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? Limit(value.GetString(), TrackerLimits.MaxStringLength)
            : null;

    private static DateTimeOffset? ParseTime(string? value) =>
        value is not null && DateTimeOffset.TryParse(value, out var result) ? result : null;

    private static Uri? TryReferenceUri(string? value) =>
        value is not null && Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
        string.IsNullOrEmpty(uri.UserInfo) && uri.Query.Length == 0 && uri.Fragment.Length == 0 ? uri : null;

    private static string? Limit(string? value, int maximum) =>
        value is null ? null : value.Length <= maximum ? value : throw new ArgumentException("Remote tracker text exceeded its bound.");

    private static TrackerMutationOutcome MapConfigurationOutcome(TrackerEvidenceState state) => state switch
    {
        TrackerEvidenceState.AuthenticationRequired => TrackerMutationOutcome.AuthenticationRequired,
        TrackerEvidenceState.Unsupported => TrackerMutationOutcome.Unsupported,
        _ => TrackerMutationOutcome.InvalidAuthority
    };

    private static TrackerMutationOutcome MapCredentialOutcome(TrackerEvidenceState state) => state switch
    {
        TrackerEvidenceState.PermissionDenied => TrackerMutationOutcome.PermissionDenied,
        TrackerEvidenceState.Unsupported => TrackerMutationOutcome.Unsupported,
        _ => TrackerMutationOutcome.AuthenticationRequired
    };

    private static TrackerMutationOutcome MapReadOutcome(TrackerEvidenceState state) => state switch
    {
        TrackerEvidenceState.AuthenticationRequired => TrackerMutationOutcome.AuthenticationRequired,
        TrackerEvidenceState.PermissionDenied => TrackerMutationOutcome.PermissionDenied,
        TrackerEvidenceState.NotFound => TrackerMutationOutcome.NotFound,
        TrackerEvidenceState.RateLimited => TrackerMutationOutcome.RateLimited,
        TrackerEvidenceState.Cancelled => TrackerMutationOutcome.Cancelled,
        TrackerEvidenceState.InvalidResponse => TrackerMutationOutcome.InvalidResponse,
        _ => TrackerMutationOutcome.Unavailable
    };

    private static (TrackerMutationOutcome Outcome, TrackerEvidenceState State, string Message) MapStatus(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized => (TrackerMutationOutcome.AuthenticationRequired, TrackerEvidenceState.AuthenticationRequired, "Jira authentication was rejected."),
        HttpStatusCode.Forbidden => (TrackerMutationOutcome.PermissionDenied, TrackerEvidenceState.PermissionDenied, "Jira permission was denied."),
        HttpStatusCode.NotFound => (TrackerMutationOutcome.NotFound, TrackerEvidenceState.NotFound, "The Jira target was not found."),
        (HttpStatusCode)429 => (TrackerMutationOutcome.RateLimited, TrackerEvidenceState.RateLimited, "Jira rate-limited the request."),
        >= HttpStatusCode.InternalServerError => (TrackerMutationOutcome.Unavailable, TrackerEvidenceState.Unavailable, "Jira was unavailable."),
        _ => (TrackerMutationOutcome.InvalidResponse, TrackerEvidenceState.InvalidResponse, "Jira rejected the request." )
    };

    private static TrackerReadResult<IReadOnlyList<TrackerWorkItemSnapshot>> ListFailure(
        TrackerProjectIdentity project,
        ResponseResult response,
        IReadOnlyList<TrackerWorkItemSnapshot> existing,
        DateTimeOffset capturedAt,
        IReadOnlyList<string> limitations) =>
        ListResult(response.State is TrackerEvidenceState.AuthenticationRequired or TrackerEvidenceState.PermissionDenied or TrackerEvidenceState.NotFound or TrackerEvidenceState.RateLimited
            ? response.State
            : existing.Count > 0 ? TrackerEvidenceState.Stale : response.State,
            project,
            null,
            response.ErrorMessage,
            capturedAt,
            limitations,
            value: null,
            lastKnown: existing.Count > 0 ? existing : null);

    private static TrackerReadResult<IReadOnlyList<TrackerWorkItemSnapshot>> ListResult(
        TrackerEvidenceState state,
        TrackerProjectIdentity project,
        TrackerWorkItemIdentity? target,
        string? error,
        DateTimeOffset? capturedAt = null,
        IReadOnlyList<string>? limitations = null,
        IReadOnlyList<TrackerWorkItemSnapshot>? value = null,
        IReadOnlyList<TrackerWorkItemSnapshot>? lastKnown = null) =>
        new(state, project, target, capturedAt ?? DateTimeOffset.UtcNow, value ?? Array.Empty<TrackerWorkItemSnapshot>(), lastKnown, limitations, error);

    private static TrackerReadResult<TrackerWorkItemSnapshot> SingleFailure(
        TrackerProjectIdentity project,
        TrackerWorkItemIdentity target,
        TrackerWorkItemSnapshot? lastKnown,
        ResponseResult response) =>
        SingleResult(response.State is TrackerEvidenceState.AuthenticationRequired or TrackerEvidenceState.PermissionDenied or TrackerEvidenceState.NotFound or TrackerEvidenceState.RateLimited
            ? response.State
            : lastKnown is not null ? TrackerEvidenceState.Stale : response.State,
            project,
            target,
            null,
            response.ErrorMessage,
            lastKnown is null ? null : ["Last-known tracker evidence is stale; the fresh read failed."],
            lastKnown);

    private static TrackerReadResult<TrackerWorkItemSnapshot> SingleResult(
        TrackerEvidenceState state,
        TrackerProjectIdentity project,
        TrackerWorkItemIdentity target,
        TrackerWorkItemSnapshot? value,
        string? error,
        IReadOnlyList<string>? limitations = null,
        TrackerWorkItemSnapshot? lastKnownValue = null) =>
        new(state, project, target, DateTimeOffset.UtcNow, value, lastKnownValue, limitations, error);

    private sealed record AuthorizationResult(bool Success, TrackerEvidenceState State, AuthenticationHeaderValue? Authorization, string? ErrorMessage);
    private sealed record ResponseResult(bool Success, TrackerMutationOutcome Outcome, TrackerEvidenceState State, string? ErrorMessage, string HttpOutcome, string? Body, bool MayHaveModifiedRemote);
    private sealed record TransitionValue(string TransitionId, string TargetStatusId, bool Available);
    private sealed record TransitionResult(bool Success, TrackerMutationOutcome Outcome, TrackerEvidenceState State, string? ErrorMessage, string HttpOutcome, IReadOnlyList<TransitionValue>? Values = null);
    private sealed class ResponseTooLargeException : Exception;

}
