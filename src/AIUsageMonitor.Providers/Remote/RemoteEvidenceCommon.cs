using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AIUsageMonitor.Application.RemoteEvidence;

namespace AIUsageMonitor.Providers.Remote;

internal static class RemoteEvidenceLimits
{
    public const int MaxResponseBytes = 512 * 1024;
    public const int MaxItems = 100;
    public const int MaxStringLength = 1_024;
    public const int MaxBranchLength = 256;
}

internal sealed record RemoteHttpResult(
    RemoteEvidenceState State,
    string? Body = null,
    string? ErrorMessage = null);

internal static class RemoteEvidenceHttp
{
    public static async Task<RemoteHttpResult> GetJsonAsync(
        HttpClient client,
        Uri uri,
        AuthenticationHeaderValue? authorization,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        if (authorization is not null)
        {
            request.Headers.Authorization = authorization;
        }

        try
        {
            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new(MapStatus(response.StatusCode), ErrorMessage(response.StatusCode));
            }

            if (response.Content.Headers.ContentLength > RemoteEvidenceLimits.MaxResponseBytes)
            {
                return new(RemoteEvidenceState.InvalidResponse, ErrorMessage: "Remote response exceeded its bounded size.");
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

                if (memory.Length + read > RemoteEvidenceLimits.MaxResponseBytes)
                {
                    return new(RemoteEvidenceState.InvalidResponse, ErrorMessage: "Remote response exceeded its bounded size.");
                }

                memory.Write(buffer, 0, read);
            }

            return new(RemoteEvidenceState.Available, Encoding.UTF8.GetString(memory.ToArray()));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new(RemoteEvidenceState.Cancelled, ErrorMessage: "Remote evidence was cancelled by the caller.");
        }
        catch (OperationCanceledException)
        {
            return new(RemoteEvidenceState.Unavailable, ErrorMessage: "Remote evidence timed out or was interrupted.");
        }
        catch (HttpRequestException)
        {
            return new(RemoteEvidenceState.Unavailable, ErrorMessage: "Remote evidence was unavailable.");
        }
        catch (IOException)
        {
            return new(RemoteEvidenceState.Unavailable, ErrorMessage: "Remote response could not be read.");
        }
    }

    private static RemoteEvidenceState MapStatus(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized => RemoteEvidenceState.AuthenticationRequired,
        HttpStatusCode.Forbidden => RemoteEvidenceState.PermissionDenied,
        (HttpStatusCode)429 => RemoteEvidenceState.RateLimited,
        HttpStatusCode.NotFound => RemoteEvidenceState.Unavailable,
        >= HttpStatusCode.InternalServerError => RemoteEvidenceState.Unavailable,
        _ => RemoteEvidenceState.InvalidResponse
    };

    private static string ErrorMessage(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized => "Remote authentication was rejected or is required.",
        HttpStatusCode.Forbidden => "Remote permission was denied.",
        (HttpStatusCode)429 => "The remote provider rate-limited the request.",
        HttpStatusCode.NotFound => "The remote resource was unavailable or inaccessible.",
        >= HttpStatusCode.InternalServerError => "The remote provider was unavailable.",
        _ => "The remote provider returned an unusable response."
    };
}

internal static class RemoteEvidenceJson
{
    public static JsonDocument? Parse(string? body, out string? error)
    {
        error = null;
        if (body is null)
        {
            error = "Remote response did not contain JSON.";
            return null;
        }

        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            error = "Remote response JSON was malformed.";
            return null;
        }
    }

    public static string? String(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return Limit(value.GetString());
        }

        if (value.ValueKind == JsonValueKind.Number)
        {
            var raw = value.GetRawText();
            return Limit(raw);
        }

        return null;
    }

    public static bool? Boolean(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var value))
        {
            return value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => null
            };
        }

        return null;
    }

    public static DateTimeOffset? Time(JsonElement element, string propertyName)
    {
        var value = String(element, propertyName);
        return value is not null && DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var parsed)
            ? parsed
            : null;
    }

    public static Uri? SafeUri(JsonElement element, string propertyName, string allowedHost)
    {
        var value = String(element, propertyName);
        return value is not null && Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            uri.Scheme == Uri.UriSchemeHttps &&
            uri.Host.Equals(allowedHost, StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrEmpty(uri.UserInfo) &&
            uri.Query.Length == 0 &&
            uri.Fragment.Length == 0
            ? uri
            : null;
    }

    public static string Required(JsonElement element, string propertyName)
    {
        var value = String(element, propertyName);
        return value is null
            ? throw new ArgumentException("Remote response omitted a required field.")
            : value;
    }

    public static string? Limit(string? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value.Any(char.IsControl) || value.Length > RemoteEvidenceLimits.MaxStringLength)
        {
            throw new ArgumentException("Remote response contained an oversized or unsafe field.");
        }

        return value.Trim();
    }
}

internal static class RemoteEvidenceUrl
{
    public static bool TryGitHub(string? raw, out GitHubTarget? target)
    {
        target = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var value = raw.Trim();
        string? host = null;
        string? path = null;
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeSsh ||
                uri.Port != -1 && (uri.Scheme == Uri.UriSchemeHttps && uri.Port != 443 ||
                    uri.Scheme == Uri.UriSchemeSsh && uri.Port != 22) ||
                uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
                !string.IsNullOrEmpty(uri.UserInfo) &&
                (uri.Scheme != Uri.UriSchemeSsh || !uri.UserInfo.Equals("git", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            host = uri.Host;
            path = uri.AbsolutePath;
        }
        else if (!TryScp(value, out host, out path))
        {
            return false;
        }

        if (!string.Equals(host, "github.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var parts = PathParts(path);
        if (parts is null || parts.Count != 2 || !TrySegment(parts[0], out var owner))
        {
            return false;
        }

        var repositoryPart = parts[1].EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? parts[1][..^4]
            : parts[1];
        if (!TrySegment(repositoryPart, out var repository))
        {
            return false;
        }

        target = new GitHubTarget(owner, repository, new Uri($"https://github.com/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}"));
        return true;
    }

    public static bool TryAzure(string? raw, out AzureTarget? target)
    {
        target = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var value = raw.Trim();
        string? host;
        string? path;
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeSsh ||
                uri.Port != -1 && (uri.Scheme == Uri.UriSchemeHttps && uri.Port != 443 ||
                    uri.Scheme == Uri.UriSchemeSsh && uri.Port != 22) ||
                uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
                !string.IsNullOrEmpty(uri.UserInfo) &&
                (uri.Scheme != Uri.UriSchemeSsh || !uri.UserInfo.Equals("git", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            host = uri.Host;
            path = uri.AbsolutePath;
        }
        else if (!TryScp(value, out host, out path))
        {
            return false;
        }

        var parts = PathParts(path);
        string? organization;
        string? project;
        string? repository;
        if (host.Equals("dev.azure.com", StringComparison.OrdinalIgnoreCase))
        {
            if (parts is null || parts.Count != 4 || !parts[2].Equals("_git", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            organization = parts[0];
            project = parts[1];
            repository = StripGitSuffix(parts[3]);
        }
        else if (host.Equals("ssh.dev.azure.com", StringComparison.OrdinalIgnoreCase))
        {
            if (parts is null || parts.Count != 4 || !parts[0].Equals("v3", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            organization = parts[1];
            project = parts[2];
            repository = StripGitSuffix(parts[3]);
        }
        else if (host.EndsWith(".visualstudio.com", StringComparison.OrdinalIgnoreCase))
        {
            if (parts is null || parts.Count != 3 || !parts[1].Equals("_git", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            organization = host[..^".visualstudio.com".Length];
            project = parts[0];
            repository = StripGitSuffix(parts[2]);
        }
        else
        {
            return false;
        }

        if (!TrySegment(organization, out var safeOrganization) ||
            !TrySegment(project, out var safeProject) ||
            !TrySegment(repository, out var safeRepository))
        {
            return false;
        }

        target = new AzureTarget(safeOrganization, safeProject, safeRepository);
        return true;
    }

    public static bool IsSafeResponseUri(Uri? uri, params string[] allowedHosts) =>
        uri is not null && uri.Scheme == Uri.UriSchemeHttps &&
        string.IsNullOrEmpty(uri.UserInfo) && uri.Query.Length == 0 && uri.Fragment.Length == 0 &&
        allowedHosts.Any(host => uri.Host.Equals(host, StringComparison.OrdinalIgnoreCase) ||
            host.StartsWith("*.", StringComparison.Ordinal) && uri.Host.EndsWith(host[1..], StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<string>? PathParts(string? path)
    {
        if (path is null)
        {
            return null;
        }

        var parts = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var normalized = new List<string>(parts.Length);
        foreach (var part in parts)
        {
            if (!TrySegment(part, out var value))
            {
                return null;
            }

            normalized.Add(value);
        }

        return normalized;
    }

    private static bool TrySegment(string? raw, out string value)
    {
        value = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        try
        {
            value = Uri.UnescapeDataString(raw);
        }
        catch (UriFormatException)
        {
            return false;
        }

        return value.Length is > 0 and <= RemoteEvidenceLimits.MaxStringLength &&
            !value.Any(char.IsControl) && !value.Contains('/') && !value.Contains('\\') && value is not ("." or "..");
    }

    private static string StripGitSuffix(string value) =>
        value.EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? value[..^4] : value;

    private static bool TryScp(string value, out string host, out string path)
    {
        host = string.Empty;
        path = string.Empty;
        var at = value.IndexOf('@');
        var colon = value.IndexOf(':', at + 1);
        if (at <= 0 || colon <= at + 1 || !value[..at].Equals("git", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        host = value[(at + 1)..colon];
        path = value[(colon + 1)..];
        return host.Length > 0 && path.Length > 0;
    }
}

internal sealed record GitHubTarget(string Owner, string Repository, Uri CanonicalUrl)
{
    public Uri Api(string path) => new($"https://api.github.com/repos/{Uri.EscapeDataString(Owner)}/{Uri.EscapeDataString(Repository)}{(string.IsNullOrEmpty(path) ? string.Empty : "/" + path)}");
}

internal sealed record AzureTarget(string Organization, string Project, string Repository)
{
    public Uri Api(string path) => new($"https://dev.azure.com/{Uri.EscapeDataString(Organization)}/{Uri.EscapeDataString(Project)}/_apis/git/repositories/{Uri.EscapeDataString(Repository)}/{path}");

    public Uri RepositoryApi() => new($"https://dev.azure.com/{Uri.EscapeDataString(Organization)}/{Uri.EscapeDataString(Project)}/_apis/git/repositories/{Uri.EscapeDataString(Repository)}?api-version=7.1");

    public Uri BuildApi(string query) => new($"https://dev.azure.com/{Uri.EscapeDataString(Organization)}/{Uri.EscapeDataString(Project)}/_apis/build/{query}");
}

internal sealed class RemoteEvidenceDraft
{
    public RemoteEvidenceDraft(Guid projectId, RemoteEvidenceSource source, DateTimeOffset capturedAt)
    {
        ProjectId = projectId;
        Source = source;
        CapturedAt = capturedAt;
    }

    public Guid ProjectId { get; }
    public RemoteEvidenceSource Source { get; }
    public DateTimeOffset CapturedAt { get; }
    public RemoteRepositoryIdentity? Repository { get; set; }
    public RemoteEvidenceState RepositoryState { get; set; } = RemoteEvidenceState.NotConfigured;
    public RemoteBranchEvidence? Branch { get; set; }
    public RemoteEvidenceState BranchState { get; set; } = RemoteEvidenceState.NotConfigured;
    public RemotePullRequestEvidence? PullRequest { get; set; }
    public RemoteEvidenceState PullRequestState { get; set; } = RemoteEvidenceState.NotConfigured;
    public List<RemoteReviewEvidence> Reviews { get; } = [];
    public RemoteEvidenceState ReviewState { get; set; } = RemoteEvidenceState.NotConfigured;
    public List<RemoteStatusEvidence> Statuses { get; } = [];
    public RemoteEvidenceState StatusState { get; set; } = RemoteEvidenceState.NotConfigured;
    public List<RemoteStatusEvidence> Checks { get; } = [];
    public RemoteEvidenceState CheckState { get; set; } = RemoteEvidenceState.NotConfigured;
    public List<RemoteCiRunEvidence> CiRuns { get; } = [];
    public RemoteEvidenceState CiState { get; set; } = RemoteEvidenceState.NotConfigured;
    public RemoteCiState CiResult { get; set; } = RemoteCiState.Unknown;
    public List<string> Limitations { get; } = [];
    public string? Error { get; set; }

    public RemoteRepositoryEvidence Build()
    {
        var states = new[] { RepositoryState, BranchState, PullRequestState, ReviewState, StatusState, CheckState, CiState };
        var hasFailure = Limitations.Count > 0 || states.Any(state => state is not (RemoteEvidenceState.Available or RemoteEvidenceState.NotConfigured));
        var state = RepositoryState is not RemoteEvidenceState.Available
            ? RepositoryState
            : hasFailure ? RemoteEvidenceState.Partial : RemoteEvidenceState.Available;
        return new(
            ProjectId,
            state,
            Source,
            CapturedAt,
            Repository,
            RepositoryState,
            Branch,
            BranchState,
            PullRequest,
            PullRequestState,
            Reviews,
            ReviewState,
            Statuses,
            StatusState,
            Checks,
            CheckState,
            CiRuns,
            CiState,
            CiResult,
            Limitations,
            Error);
    }

    public RemoteRepositoryEvidence Failure(RemoteEvidenceState state, string error)
    {
        RepositoryState = state;
        Error = error;
        return Build();
    }
}

internal static class RemoteEvidenceBranches
{
    public static bool TryNormalize(string? value, out string branch)
    {
        branch = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        branch = value.Trim();
        if (branch.StartsWith("refs/heads/", StringComparison.OrdinalIgnoreCase))
        {
            branch = branch[11..];
        }

        return branch.Length is > 0 and <= RemoteEvidenceLimits.MaxBranchLength &&
            !branch.Any(char.IsControl) && !branch.Contains("..", StringComparison.Ordinal) &&
            !branch.Contains('\\') && !branch.StartsWith('/') && !branch.EndsWith('/');
    }

    public static string Ref(string branch) => $"refs/heads/{branch}";
}

internal static class RemoteEvidenceCi
{
    public static RemoteCiState Aggregate(IEnumerable<RemoteCiRunEvidence> runs)
    {
        var materialized = runs.ToArray();
        if (materialized.Length == 0)
        {
            return RemoteCiState.NoEvidence;
        }

        if (materialized.Any(run => run.State == RemoteCiState.Failing))
        {
            return RemoteCiState.Failing;
        }

        if (materialized.Any(run => run.State == RemoteCiState.Pending))
        {
            return RemoteCiState.Pending;
        }

        if (materialized.Any(run => run.State == RemoteCiState.Cancelled))
        {
            return RemoteCiState.Cancelled;
        }

        return materialized.All(run => run.State == RemoteCiState.Passing)
            ? RemoteCiState.Passing
            : RemoteCiState.Unknown;
    }

    public static RemoteCiState FromStatus(string? status, string? conclusion) =>
        !string.IsNullOrWhiteSpace(conclusion) && conclusion.Equals("success", StringComparison.OrdinalIgnoreCase)
            ? RemoteCiState.Passing
            : conclusion?.ToLowerInvariant() switch
            {
                "failure" or "timed_out" or "action_required" => RemoteCiState.Failing,
                "cancelled" => RemoteCiState.Cancelled,
                "skipped" or "neutral" => RemoteCiState.Unknown,
                _ when status?.Equals("completed", StringComparison.OrdinalIgnoreCase) == true => RemoteCiState.Unknown,
                _ => RemoteCiState.Pending
            };
}
