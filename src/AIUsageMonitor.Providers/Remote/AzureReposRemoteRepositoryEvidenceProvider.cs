using System.Net.Http.Headers;
using System.Text.Json;
using AIUsageMonitor.Application.RemoteEvidence;
using AIUsageMonitor.Application.Security;
using AIUsageMonitor.Application.Time;

namespace AIUsageMonitor.Providers.Remote;

public sealed class AzureReposRemoteRepositoryEvidenceProvider : IRemoteRepositoryEvidenceProvider
{
    public const string HttpClientName = "AIUsageMonitor.Remote.AzureRepos";

    private readonly IClock _clock;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISecureCredentialStore _credentials;

    public AzureReposRemoteRepositoryEvidenceProvider(
        IClock clock,
        IHttpClientFactory httpClientFactory,
        ISecureCredentialStore credentials)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
    }

    public RemoteRepositoryProvider Provider => RemoteRepositoryProvider.AzureRepos;

    public async Task<RemoteRepositoryEvidence> InspectAsync(
        RemoteRepositoryEvidenceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var draft = new RemoteEvidenceDraft(request.ProjectId, RemoteEvidenceSource.AzureDevOpsRest, _clock.UtcNow);
        if (!IsAzureProvider(request.RepositoryProvider) ||
            !RemoteEvidenceUrl.TryAzure(request.RepositoryUrl, out var target) || target is null)
        {
            return draft.Failure(RemoteEvidenceState.Unsupported,
                "The configured repository is not a supported Azure Repos identity.");
        }

        try
        {
            var authorization = await ResolveAuthorizationAsync(request, cancellationToken).ConfigureAwait(false);
            if (authorization.State is not RemoteEvidenceState.Available)
            {
                return draft.Failure(authorization.State, authorization.Error!);
            }

            var auth = authorization.Value ?? throw new InvalidOperationException("Azure authorization was unavailable.");

            var client = _httpClientFactory.CreateClient(HttpClientName);
            var repositoryResponse = await RemoteEvidenceHttp.GetJsonAsync(
                client,
                target.RepositoryApi(),
                auth,
                cancellationToken).ConfigureAwait(false);
            if (repositoryResponse.State is not RemoteEvidenceState.Available)
            {
                return draft.Failure(repositoryResponse.State, repositoryResponse.ErrorMessage!);
            }

            using var repositoryDocument = RemoteEvidenceJson.Parse(repositoryResponse.Body, out var repositoryError);
            if (repositoryDocument is null)
            {
                return draft.Failure(RemoteEvidenceState.InvalidResponse, repositoryError!);
            }

            string repositoryId;
            string repositoryName;
            string defaultBranch;
            string projectName;
            string projectId;
            try
            {
                var root = repositoryDocument.RootElement;
                repositoryId = RemoteEvidenceJson.Required(root, "id");
                repositoryName = RemoteEvidenceJson.Required(root, "name");
                defaultBranch = RemoteEvidenceJson.Required(root, "defaultBranch");
                var project = root.GetProperty("project");
                projectId = RemoteEvidenceJson.Required(project, "id");
                projectName = RemoteEvidenceJson.String(project, "name") ?? target.Project;
                if (!repositoryName.Equals(target.Repository, StringComparison.OrdinalIgnoreCase) ||
                    request.RepositoryId is not null && !request.RepositoryId.Equals(repositoryId, StringComparison.OrdinalIgnoreCase) ||
                    request.RepositoryMetadata.TryGetValue("projectId", out var configuredProjectId) &&
                    !string.IsNullOrWhiteSpace(configuredProjectId) &&
                    !configuredProjectId.Equals(projectId, StringComparison.OrdinalIgnoreCase))
                {
                    return draft.Failure(RemoteEvidenceState.InvalidResponse,
                        "Azure returned a repository identity different from the configured project target.");
                }

                if (!RemoteEvidenceBranches.TryNormalize(defaultBranch, out defaultBranch))
                {
                    return draft.Failure(RemoteEvidenceState.InvalidResponse,
                        "Azure returned an invalid default branch.");
                }

                draft.Repository = new RemoteRepositoryIdentity(
                    Provider,
                    RemoteEvidenceSource.AzureDevOpsRest,
                    repositoryId,
                    $"{target.Organization}/{projectName}/{repositoryName}",
                    target.Organization,
                    repositoryName,
                    defaultBranch,
                    projectName,
                    new Uri($"https://dev.azure.com/{Uri.EscapeDataString(target.Organization)}/{Uri.EscapeDataString(projectName)}/_git/{Uri.EscapeDataString(repositoryName)}"));
                draft.RepositoryState = RemoteEvidenceState.Available;
            }
            catch (ArgumentException)
            {
                return draft.Failure(RemoteEvidenceState.InvalidResponse,
                    "Azure repository metadata was malformed.");
            }

            if (!RemoteEvidenceBranches.TryNormalize(request.RequestedBranch ?? draft.Repository!.DefaultBranch, out var branch))
            {
                draft.BranchState = RemoteEvidenceState.InvalidResponse;
                draft.Error = "The requested Azure branch was invalid.";
                return draft.Build();
            }

            var branchTarget = new AzureTarget(target.Organization, target.Project, repositoryId);
            var branchResponse = await RemoteEvidenceHttp.GetJsonAsync(
                client,
                branchTarget.Api($"refs?filter={Uri.EscapeDataString(RemoteEvidenceBranches.Ref(branch))}&%24top=1&api-version=7.1"),
                authorization.Value,
                cancellationToken).ConfigureAwait(false);
            if (branchResponse.State is not RemoteEvidenceState.Available)
            {
                draft.BranchState = branchResponse.State;
                draft.Error ??= branchResponse.ErrorMessage;
                return draft.Build();
            }

            using var branchDocument = RemoteEvidenceJson.Parse(branchResponse.Body, out var branchError);
            if (branchDocument is null ||
                !branchDocument.RootElement.TryGetProperty("value", out var refs) ||
                refs.ValueKind is not JsonValueKind.Array)
            {
                draft.BranchState = RemoteEvidenceState.InvalidResponse;
                draft.Error ??= branchError ?? "Azure branch response was malformed.";
                return draft.Build();
            }

            try
            {
                var reference = refs.EnumerateArray().FirstOrDefault();
                if (reference.ValueKind is not JsonValueKind.Object)
                {
                    draft.BranchState = RemoteEvidenceState.Unavailable;
                    draft.Error ??= "The requested Azure branch was unavailable.";
                    return draft.Build();
                }

                var commit = RemoteEvidenceJson.Required(reference, "objectId");
                var actualBranch = RemoteEvidenceJson.String(reference, "name") ?? RemoteEvidenceBranches.Ref(branch);
                if (!RemoteEvidenceBranches.TryNormalize(actualBranch, out actualBranch))
                {
                    throw new ArgumentException("Azure returned an invalid branch.");
                }

                draft.Branch = new RemoteBranchEvidence(
                    actualBranch,
                    commit,
                    actualBranch.Equals(draft.Repository.DefaultBranch, StringComparison.OrdinalIgnoreCase));
                draft.BranchState = RemoteEvidenceState.Available;
            }
            catch (ArgumentException)
            {
                draft.BranchState = RemoteEvidenceState.InvalidResponse;
                draft.Error ??= "Azure branch metadata was malformed.";
                return draft.Build();
            }

            if (request.PullRequestNumber is { } pullRequestNumber)
            {
                await ReadPullRequestAsync(draft, branchTarget, pullRequestNumber, auth, client, cancellationToken)
                    .ConfigureAwait(false);
            }

            var evidenceCommit = draft.PullRequest?.HeadCommitId ?? draft.Branch.CommitId;
            await ReadCommitStatusesAsync(draft, branchTarget, evidenceCommit, auth, client, cancellationToken).ConfigureAwait(false);
            if (draft.PullRequest is not null)
            {
                await ReadPullRequestStatusesAsync(draft, branchTarget, draft.PullRequest.Id, auth, client, cancellationToken)
                    .ConfigureAwait(false);
            }

            await ReadBuildsAsync(
                draft,
                target,
                repositoryId,
                branch,
                evidenceCommit,
                auth,
                client,
                cancellationToken).ConfigureAwait(false);
            return draft.Build();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return draft.Failure(RemoteEvidenceState.Cancelled, "Remote Azure evidence was cancelled by the caller.");
        }
        catch (ArgumentException)
        {
            return draft.Failure(RemoteEvidenceState.InvalidResponse, "Azure returned an unusable evidence response.");
        }
        catch (KeyNotFoundException)
        {
            return draft.Failure(RemoteEvidenceState.InvalidResponse, "Azure returned an incomplete evidence response.");
        }
    }

    private async Task ReadPullRequestAsync(
        RemoteEvidenceDraft draft,
        AzureTarget target,
        int number,
        AuthenticationHeaderValue authorization,
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var response = await RemoteEvidenceHttp.GetJsonAsync(
            client,
            target.Api($"pullRequests/{number.ToString(System.Globalization.CultureInfo.InvariantCulture)}?api-version=7.1"),
            authorization,
            cancellationToken).ConfigureAwait(false);
        if (response.State is not RemoteEvidenceState.Available)
        {
            draft.PullRequestState = response.State;
            draft.Error ??= response.ErrorMessage;
            return;
        }

        using var document = RemoteEvidenceJson.Parse(response.Body, out var error);
        if (document is null)
        {
            draft.PullRequestState = RemoteEvidenceState.InvalidResponse;
            draft.Error ??= error;
            return;
        }

        try
        {
            var root = document.RootElement;
            var repository = root.GetProperty("repository");
            if (RemoteEvidenceJson.String(repository, "id") is { } prRepositoryId &&
                draft.Repository is not null &&
                !prRepositoryId.Equals(draft.Repository.ProviderRepositoryId, StringComparison.OrdinalIgnoreCase))
            {
                draft.PullRequestState = RemoteEvidenceState.InvalidResponse;
                draft.Error ??= "Azure pull request repository identity did not match the configured repository.";
                return;
            }

            var lastSource = root.TryGetProperty("lastMergeSourceCommit", out var sourceCommit)
                ? RemoteEvidenceJson.String(sourceCommit, "commitId")
                : null;
            var lastTarget = root.TryGetProperty("lastMergeTargetCommit", out var targetCommit)
                ? RemoteEvidenceJson.String(targetCommit, "commitId")
                : null;
            draft.PullRequest = new RemotePullRequestEvidence(
                RemoteEvidenceJson.Required(root, "pullRequestId"),
                RemoteEvidenceJson.Required(root, "status"),
                RemoteEvidenceJson.Boolean(root, "isDraft"),
                StripRef(RemoteEvidenceJson.String(root, "sourceRefName")),
                StripRef(RemoteEvidenceJson.String(root, "targetRefName")),
                lastSource,
                lastTarget,
                RemoteEvidenceJson.String(root, "mergeStatus")?.ToLowerInvariant() switch
                {
                    "succeeded" => RemoteMergeability.Available,
                    "conflicts" => RemoteMergeability.Conflicting,
                    "checking" => RemoteMergeability.Calculating,
                    _ => RemoteMergeability.Unknown
                },
                SafeAzureUri(root, "url"));
            draft.PullRequestState = RemoteEvidenceState.Available;

            if (root.TryGetProperty("reviewers", out var reviewers) && reviewers.ValueKind == JsonValueKind.Array)
            {
                var index = 0;
                foreach (var reviewer in reviewers.EnumerateArray())
                {
                    if (index++ >= RemoteEvidenceLimits.MaxItems)
                    {
                        draft.Limitations.Add("Azure pull-request reviewers were capped by the adapter bound.");
                        break;
                    }

                    var name = RemoteEvidenceJson.String(reviewer, "displayName") ??
                        RemoteEvidenceJson.String(reviewer, "uniqueName") ??
                        RemoteEvidenceJson.String(reviewer, "id");
                    if (name is null)
                    {
                        continue;
                    }

                    var vote = RemoteEvidenceJson.String(reviewer, "vote");
                    draft.Reviews.Add(new RemoteReviewEvidence(
                        name,
                        vote switch
                        {
                            "10" => "approved",
                            "-10" => "changes requested",
                            "0" => "pending",
                            _ => "unknown"
                        },
                        requested: true,
                        reviewId: RemoteEvidenceJson.String(reviewer, "id")));
                }
            }

            draft.ReviewState = RemoteEvidenceState.Available;
        }
        catch (ArgumentException)
        {
            draft.PullRequestState = RemoteEvidenceState.InvalidResponse;
            draft.Error ??= "Azure pull-request metadata was malformed.";
        }
    }

    private async Task ReadCommitStatusesAsync(
        RemoteEvidenceDraft draft,
        AzureTarget target,
        string commit,
        AuthenticationHeaderValue authorization,
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var response = await RemoteEvidenceHttp.GetJsonAsync(
            client,
            target.Api($"commits/{Uri.EscapeDataString(commit)}/statuses?%24top={RemoteEvidenceLimits.MaxItems}&api-version=7.1"),
            authorization,
            cancellationToken).ConfigureAwait(false);
        var (state, values, truncated) = ParseAzureStatuses(response, RemoteStatusKind.CommitStatus);
        draft.StatusState = state;
        draft.Statuses.AddRange(values);
        if (truncated)
        {
            draft.Limitations.Add("Azure commit statuses were capped by the adapter bound.");
        }
        draft.Error ??= response.State is not RemoteEvidenceState.Available ? response.ErrorMessage : null;
    }

    private async Task ReadPullRequestStatusesAsync(
        RemoteEvidenceDraft draft,
        AzureTarget target,
        string pullRequestId,
        AuthenticationHeaderValue authorization,
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var response = await RemoteEvidenceHttp.GetJsonAsync(
            client,
            target.Api($"pullRequests/{Uri.EscapeDataString(pullRequestId)}/statuses?api-version=7.1"),
            authorization,
            cancellationToken).ConfigureAwait(false);
        var (state, values, truncated) = ParseAzureStatuses(response, RemoteStatusKind.PullRequestStatus);
        if (draft.StatusState == RemoteEvidenceState.Available && state != RemoteEvidenceState.Available)
        {
            draft.StatusState = state;
        }

        draft.Statuses.AddRange(values);
        if (truncated)
        {
            draft.Limitations.Add("Azure pull-request statuses were capped by the adapter bound.");
        }
        draft.Error ??= response.State is not RemoteEvidenceState.Available ? response.ErrorMessage : null;
    }

    private async Task ReadBuildsAsync(
        RemoteEvidenceDraft draft,
        AzureTarget target,
        string repositoryId,
        string branch,
        string commit,
        AuthenticationHeaderValue authorization,
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var uri = target.BuildApi(
            $"builds?repositoryId={Uri.EscapeDataString(repositoryId)}&branchName={Uri.EscapeDataString(RemoteEvidenceBranches.Ref(branch))}&queryOrder=finishTimeDescending&%24top={RemoteEvidenceLimits.MaxItems}&api-version=7.1");
        var response = await RemoteEvidenceHttp.GetJsonAsync(client, uri, authorization, cancellationToken).ConfigureAwait(false);
        if (response.State is not RemoteEvidenceState.Available)
        {
            draft.CiState = response.State;
            draft.CiResult = RemoteCiState.Unknown;
            draft.Error ??= response.ErrorMessage;
            return;
        }

        using var document = RemoteEvidenceJson.Parse(response.Body, out var error);
        if (document is null || !document.RootElement.TryGetProperty("value", out var builds) || builds.ValueKind is not JsonValueKind.Array)
        {
            draft.CiState = RemoteEvidenceState.InvalidResponse;
            draft.CiResult = RemoteCiState.Unknown;
            draft.Error ??= error ?? "Azure build response was malformed.";
            return;
        }

        try
        {
            var index = 0;
            foreach (var build in builds.EnumerateArray())
            {
                if (index++ >= RemoteEvidenceLimits.MaxItems)
                {
                    draft.Limitations.Add("Azure builds were capped by the adapter bound.");
                    break;
                }

                var sourceVersion = RemoteEvidenceJson.String(build, "sourceVersion");
                if (sourceVersion is null || !sourceVersion.Equals(commit, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var id = RemoteEvidenceJson.Required(build, "id");
                var name = build.TryGetProperty("definition", out var definition) &&
                    RemoteEvidenceJson.String(definition, "name") is { } definitionName
                    ? definitionName
                    : RemoteEvidenceJson.String(build, "buildNumber") ?? "Azure build";
                draft.CiRuns.Add(new RemoteCiRunEvidence(
                    RemoteStatusKind.Build,
                    id,
                    name,
                    FromBuild(RemoteEvidenceJson.String(build, "status"), RemoteEvidenceJson.String(build, "result")),
                    RemoteEvidenceJson.String(build, "result"),
                    StripRef(RemoteEvidenceJson.String(build, "sourceBranch")) ?? branch,
                    sourceVersion,
                    RemoteEvidenceJson.Time(build, "queueTime"),
                    RemoteEvidenceJson.Time(build, "finishTime"),
                    RemoteEvidenceJson.Time(build, "finishTime"),
                    SafeAzureUri(build, "_links", "web", "href")));
            }

            draft.CiState = RemoteEvidenceState.Available;
            draft.CiResult = RemoteEvidenceCi.Aggregate(draft.CiRuns);
        }
        catch (ArgumentException)
        {
            draft.CiState = RemoteEvidenceState.InvalidResponse;
            draft.CiResult = RemoteCiState.Unknown;
            draft.Error ??= "Azure build evidence was malformed.";
        }
    }

    private async Task<(RemoteEvidenceState State, AuthenticationHeaderValue? Value, string? Error)> ResolveAuthorizationAsync(
        RemoteRepositoryEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CredentialReference))
        {
            return (RemoteEvidenceState.AuthenticationRequired, null, "Azure Repos requires a configured credential reference.");
        }

        var token = await _credentials.RetrieveAsync(request.CredentialReference, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(token)
            ? (RemoteEvidenceState.AuthenticationRequired, null, "The configured Azure credential is missing.")
            : (RemoteEvidenceState.Available,
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(":" + token))),
                null);
    }

    private static (RemoteEvidenceState State, List<RemoteStatusEvidence> Values, bool Truncated) ParseAzureStatuses(
        RemoteHttpResult response,
        RemoteStatusKind kind)
    {
        if (response.State is not RemoteEvidenceState.Available)
        {
            return (response.State, [], false);
        }

        using var document = RemoteEvidenceJson.Parse(response.Body, out _);
        if (document is null)
        {
            return (RemoteEvidenceState.InvalidResponse, [], false);
        }

        var root = document.RootElement;
        var values = root.ValueKind == JsonValueKind.Array
            ? root
            : root.TryGetProperty("value", out var nested) && nested.ValueKind == JsonValueKind.Array
                ? nested
                : default;
        if (values.ValueKind is not JsonValueKind.Array)
        {
            return (RemoteEvidenceState.InvalidResponse, [], false);
        }

        try
        {
            var result = new List<RemoteStatusEvidence>();
            var index = 0;
            var truncated = false;
            foreach (var value in values.EnumerateArray())
            {
                if (index++ >= RemoteEvidenceLimits.MaxItems)
                {
                    truncated = true;
                    break;
                }

                var context = value.TryGetProperty("context", out var contextElement) ? contextElement : default;
                var name = RemoteEvidenceJson.String(context, "name") ?? "Azure status";
                result.Add(new RemoteStatusEvidence(
                    kind,
                    name,
                    RemoteEvidenceJson.String(value, "state") ?? "unknown",
                    RemoteEvidenceJson.String(value, "description"),
                    RemoteEvidenceJson.String(value, "commitId"),
                    RemoteEvidenceJson.Time(value, "creationDate"),
                    RemoteEvidenceJson.Time(value, "updatedDate"),
                    SafeAzureUri(value, "targetUrl")));
            }

            return (RemoteEvidenceState.Available, result, truncated);
        }
        catch (ArgumentException)
        {
            return (RemoteEvidenceState.InvalidResponse, [], false);
        }
    }

    private static RemoteCiState FromBuild(string? status, string? result) =>
        result?.ToLowerInvariant() switch
        {
            "succeeded" => RemoteCiState.Passing,
            "failed" or "partiallysucceeded" => RemoteCiState.Failing,
            "canceled" => RemoteCiState.Cancelled,
            _ when status?.Equals("completed", StringComparison.OrdinalIgnoreCase) == true => RemoteCiState.Unknown,
            _ => RemoteCiState.Pending
        };

    private static string? StripRef(string? value) =>
        value?.StartsWith("refs/heads/", StringComparison.OrdinalIgnoreCase) == true ? value[11..] : value;

    private static Uri? SafeAzureUri(JsonElement element, string propertyName, params string[] nestedProperties)
    {
        var value = element;
        foreach (var property in nestedProperties)
        {
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(property, out value))
            {
                return null;
            }
        }

        var raw = nestedProperties.Length > 0
            ? value.ValueKind == JsonValueKind.String ? RemoteEvidenceJson.Limit(value.GetString()) : null
            : RemoteEvidenceJson.String(value, propertyName);

        return raw is not null && Uri.TryCreate(raw, UriKind.Absolute, out var uri) &&
            RemoteEvidenceUrl.IsSafeResponseUri(uri, "dev.azure.com", "*.visualstudio.com")
            ? uri
            : null;
    }

    private static bool IsAzureProvider(string? value)
    {
        var normalized = value?.Replace(" ", string.Empty, StringComparison.Ordinal);
        return normalized is not null &&
            (normalized.Equals("AzureRepos", StringComparison.OrdinalIgnoreCase) ||
             normalized.Equals("AzureDevOps", StringComparison.OrdinalIgnoreCase));
    }
}
