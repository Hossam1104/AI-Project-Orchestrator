using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIUsageMonitor.Application.Providers;
using AIUsageMonitor.Application.Security;
using AIUsageMonitor.Application.Time;
using AIUsageMonitor.Domain.Common;
using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Quotas;
using AIUsageMonitor.Providers.Common;

namespace AIUsageMonitor.Providers.Claude;

/// <summary>
/// Claude adapter with a separately labelled Anthropic organization Admin API usage channel.
/// Claude consumer subscription usage remains manual/unsupported because the documented
/// subscription status surface is interactive.
/// </summary>
public sealed class ClaudeProvider : ProviderAdapterBase
{
    public const string HttpClientName = "AIUsageMonitor.Anthropic";

    private readonly IClock _clock;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISecureCredentialStore _credentialStore;
    private readonly IExecutableLocator _executableLocator;
    private readonly AnthropicOptions _options;

    public ClaudeProvider(
        IClock clock,
        IHttpClientFactory httpClientFactory,
        ISecureCredentialStore credentialStore,
        IExecutableLocator executableLocator,
        AnthropicOptions options)
        : base(clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _credentialStore = credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));
        _executableLocator = executableLocator ?? throw new ArgumentNullException(nameof(executableLocator));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override ProviderCode Code => ProviderCode.Claude;

    public override Task<ProviderDetectionResult> DetectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var apiConfigured = !string.IsNullOrWhiteSpace(_options.CredentialReference);
        var cliDetected = _executableLocator.Find("claude") is not null;
        return Task.FromResult(new ProviderDetectionResult(
            Code,
            apiConfigured || cliDetected,
            apiConfigured
                ? "Anthropic organization Admin API configured; Claude subscription remains separate."
                : cliDetected
                    ? "Official Claude CLI detected; subscription capacity is not machine-readable."
                    : "No configured Admin API credential or official Claude CLI detected.",
            UtcNow));
    }

    public override async Task<ProviderConnectionStatus> GetConnectionStatusAsync(
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_options.CredentialReference))
        {
            var token = await _credentialStore.RetrieveAsync(_options.CredentialReference, cancellationToken)
                .ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(token)
                ? ProviderConnectionStatus.AuthenticationRequired
                : ProviderConnectionStatus.Connected;
        }

        return _executableLocator.Find("claude") is not null
            ? ProviderConnectionStatus.LocalDetected
            : ProviderConnectionStatus.Unsupported;
    }

    protected override async Task<ProviderRefreshResult> RefreshCoreAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.CredentialReference))
        {
            return Unsupported();
        }

        var adminKey = await _credentialStore.RetrieveAsync(_options.CredentialReference, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(adminKey))
        {
            return AuthenticationRequired();
        }

        var tokenCount = 0d;
        var nextPage = (string?)null;
        var seenCursors = new HashSet<string>(StringComparer.Ordinal);
        var startingAt = (_options.StartingAt ?? _clock.UtcNow.AddDays(-1)).ToUniversalTime();
        var client = _httpClientFactory.CreateClient(HttpClientName);

        while (true)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                BuildUsagePath(startingAt, nextPage));
            request.Headers.Add("x-api-key", adminKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await client.SendAsync(
                request,
                cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return MapHttpFailure(response.StatusCode);
            }

            var payload = await response.Content.ReadFromJsonAsync<MessagesUsageReport>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (payload?.Data is null)
            {
                return Failure(ProviderErrorCodes.MalformedResponse, "Anthropic returned an unusable messages usage report.");
            }

            tokenCount += payload.Data
                .SelectMany(bucket => bucket.Results ?? Enumerable.Empty<MessagesUsageResult>())
                .SelectMany(result => result.TokenValues())
                .Sum();

            if (!payload.HasMore)
            {
                break;
            }

            nextPage = payload.NextPage;
            if (string.IsNullOrWhiteSpace(nextPage))
            {
                return Failure(ProviderErrorCodes.MalformedResponse, "Anthropic returned a missing pagination cursor.");
            }

            if (!seenCursors.Add(nextPage))
            {
                return Failure(ProviderErrorCodes.MalformedResponse, "Anthropic returned a repeated pagination cursor.");
            }

        }

        if (tokenCount <= 0)
        {
            return Partial(null, null, Array.Empty<QuotaWindow>(),
                "Anthropic returned no organization message usage; Claude subscription capacity remains unavailable.");
        }

        var window = QuotaWindow.Create(
            "anthropic-api:messages:tokens",
            QuotaType.Tokens,
            QuotaUnit.Tokens,
            usedValue: tokenCount,
            remainingValue: null,
            limitValue: null,
            usedPercentage: null,
            remainingPercentage: null,
            windowStart: startingAt,
            resetAt: null,
            DataSource.OfficialApi,
            ConfidenceLevel.Official,
            UtcNow);

        return Partial(null, null, new[] { window },
            "Anthropic organization API usage was retrieved; Claude Pro/Max subscription allowance is a separate unavailable channel.");
    }

    // All pages must preserve the same non-pagination query parameters. If future filters are
    // added, keep them in this shared base query for both the initial and cursor requests.
    private static string BuildUsagePath(DateTimeOffset startingAt, string? page)
    {
        var path = $"v1/organizations/usage_report/messages?starting_at={Uri.EscapeDataString(startingAt.ToString("O"))}&limit=1000";
        return page is null
            ? path
            : $"{path}&page={Uri.EscapeDataString(page)}";
    }

    private ProviderRefreshResult MapHttpFailure(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized => AuthenticationRequired(),
        HttpStatusCode.Forbidden => FailureOrPartial(
            ProviderErrorCodes.PermissionDenied,
            "Anthropic denied access to the organization messages usage report."),
        HttpStatusCode.NotFound => HasLastKnownGood
            ? Failure(ProviderErrorCodes.UnsupportedScope, "The Anthropic messages usage report is unavailable for this organization.")
            : Unsupported(),
        (HttpStatusCode)429 => Failure(ProviderErrorCodes.RateLimited, "Anthropic rate-limited the organization usage request."),
        >= HttpStatusCode.InternalServerError => Failure(
            ProviderErrorCodes.ProviderServerError, "Anthropic failed the organization usage request."),
        _ => Failure(ProviderErrorCodes.ProviderError, "Anthropic rejected the organization usage request.")
    };

    private sealed class MessagesUsageReport
    {
        [JsonPropertyName("data")]
        public List<MessagesUsageBucket>? Data { get; set; }

        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }

        [JsonPropertyName("next_page")]
        public string? NextPage { get; set; }
    }

    private sealed class MessagesUsageBucket
    {
        [JsonPropertyName("starting_at")]
        public DateTimeOffset? StartingAt { get; set; }

        [JsonPropertyName("ending_at")]
        public DateTimeOffset? EndingAt { get; set; }

        [JsonPropertyName("results")]
        public List<MessagesUsageResult>? Results { get; set; }
    }

    private sealed class MessagesUsageResult
    {
        [JsonPropertyName("uncached_input_tokens")]
        public JsonElement UncachedInputTokens { get; set; }

        [JsonPropertyName("cache_read_input_tokens")]
        public JsonElement CacheReadInputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public JsonElement OutputTokens { get; set; }

        [JsonPropertyName("cache_creation")]
        public CacheCreationTokens? CacheCreation { get; set; }

        public IEnumerable<double> TokenValues()
        {
            foreach (var element in new[] { UncachedInputTokens, CacheReadInputTokens, OutputTokens })
            {
                if (TryReadNumber(element) is { } value)
                {
                    yield return value;
                }
            }

            if (CacheCreation is not null)
            {
                if (TryReadNumber(CacheCreation.Ephemeral1hInputTokens) is { } oneHour)
                {
                    yield return oneHour;
                }

                if (TryReadNumber(CacheCreation.Ephemeral5mInputTokens) is { } fiveMinutes)
                {
                    yield return fiveMinutes;
                }
            }
        }
    }

    private sealed class CacheCreationTokens
    {
        [JsonPropertyName("ephemeral_1h_input_tokens")]
        public JsonElement Ephemeral1hInputTokens { get; set; }

        [JsonPropertyName("ephemeral_5m_input_tokens")]
        public JsonElement Ephemeral5mInputTokens { get; set; }
    }

    private static double? TryReadNumber(JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
        {
            return number >= 0 && double.IsFinite(number) ? number : throw new JsonException("Invalid token quantity.");
        }

        if (value.ValueKind == JsonValueKind.String &&
            double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number))
        {
            return number >= 0 && double.IsFinite(number) ? number : throw new JsonException("Invalid token quantity.");
        }

        return null;
    }
}
