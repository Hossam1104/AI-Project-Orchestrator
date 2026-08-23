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
using AIUsageMonitor.Providers.Claude;
using AIUsageMonitor.Providers.Common;
using AIUsageMonitor.Providers.Copilot;

namespace AIUsageMonitor.Providers.Kimi;

/// <summary>
/// Kimi Code adapter for the documented, authenticated local CLI server usage endpoint. It does
/// not inspect Kimi browser state or private auth files and does not use the separate Moonshot
/// Open Platform billing channel.
/// </summary>
public sealed class KimiProvider : ProviderAdapterBase
{
    public const string HttpClientName = "AIUsageMonitor.Kimi";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISecureCredentialStore _credentialStore;
    private readonly IExecutableLocator _executableLocator;
    private readonly IProviderRuntimeSettingsAccessor _settings;

    public KimiProvider(
        IClock clock,
        IHttpClientFactory httpClientFactory,
        ISecureCredentialStore credentialStore,
        IExecutableLocator executableLocator,
        KimiOptions options)
        : this(clock, httpClientFactory, credentialStore, executableLocator,
            new ProviderRuntimeSettingsAccessor(new CopilotOptions(), new AnthropicOptions(), options))
    {
    }

    public KimiProvider(
        IClock clock,
        IHttpClientFactory httpClientFactory,
        ISecureCredentialStore credentialStore,
        IExecutableLocator executableLocator,
        IProviderRuntimeSettingsAccessor settings)
        : base(clock)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _credentialStore = credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));
        _executableLocator = executableLocator ?? throw new ArgumentNullException(nameof(executableLocator));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public override ProviderCode Code => ProviderCode.Kimi;

    public override Task<ProviderDetectionResult> DetectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var options = _settings.Current.Kimi;
        var cliDetected = _executableLocator.Find("kimi") is not null;
        var configured = IsAllowedServerAddress(options.ServerAddress) &&
                         !string.IsNullOrWhiteSpace(options.CredentialReference);
        return Task.FromResult(new ProviderDetectionResult(
            Code,
            cliDetected || configured,
            configured
                ? "Documented Kimi Code local usage API configured."
                : !IsAllowedServerAddress(options.ServerAddress)
                    ? "Kimi Code local usage API configuration is invalid."
                : cliDetected
                    ? "Official Kimi Code CLI detected; configure its documented local API token for structured usage."
                    : "No official Kimi Code CLI or local usage API credential detected.",
            UtcNow));
    }

    public override async Task<ProviderConnectionStatus> GetConnectionStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var options = _settings.Current.Kimi;
        if (!IsAllowedServerAddress(options.ServerAddress))
        {
            return ProviderConnectionStatus.Error;
        }

        if (!string.IsNullOrWhiteSpace(options.CredentialReference))
        {
            var token = await _credentialStore.RetrieveAsync(options.CredentialReference, cancellationToken)
                .ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(token)
                ? ProviderConnectionStatus.AuthenticationRequired
                : ProviderConnectionStatus.Connected;
        }

        return _executableLocator.Find("kimi") is not null
            ? ProviderConnectionStatus.LocalDetected
            : ProviderConnectionStatus.Unsupported;
    }

    protected override async Task<ProviderRefreshResult> RefreshCoreAsync(CancellationToken cancellationToken)
    {
        var options = _settings.Current.Kimi;
        if (!IsAllowedServerAddress(options.ServerAddress))
        {
            return Failure(
                ProviderErrorCodes.InvalidConfiguration,
                "Kimi Code local usage API requires a loopback HTTP address.");
        }

        if (string.IsNullOrWhiteSpace(options.CredentialReference))
        {
            return Unsupported();
        }

        var token = await _credentialStore.RetrieveAsync(options.CredentialReference, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticationRequired();
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var usage = await FetchAsync<KimiUsageData>(
            client,
            options,
            "api/v1/oauth/usage?provider=managed%3Akimi-code",
            cancellationToken).ConfigureAwait(false);
        if (usage.Malformed)
        {
            return Failure(ProviderErrorCodes.MalformedResponse, "Kimi returned malformed usage data.");
        }

        if (usage.StatusCode.HasValue)
        {
            return MapHttpFailure(usage.StatusCode.Value);
        }

        if (usage.Envelope?.Code != 0 || usage.Envelope.Data is null ||
            !string.Equals(usage.Envelope.Data.Kind, "ok", StringComparison.OrdinalIgnoreCase))
        {
            return MapBusinessFailure(usage.Envelope?.Data?.Status);
        }

        var quotaWindows = MapQuotaWindows(usage.Envelope.Data);
        ProviderAccount? account = null;
        string? metadataWarning = null;

        var userInfo = await FetchAsync<KimiUserInfoData>(
            client,
            options,
            "api/v1/oauth/userinfo?provider=managed%3Akimi-code",
            cancellationToken).ConfigureAwait(false);
        if (!userInfo.Malformed && !userInfo.StatusCode.HasValue && userInfo.Envelope?.Code == 0 &&
            userInfo.Envelope.Data is { Kind: var kind, UserInfo: not null } &&
            string.Equals(kind, "ok", StringComparison.OrdinalIgnoreCase))
        {
            var info = userInfo.Envelope.Data.UserInfo!;
            var externalId = FirstNonBlank(info.UserId, info.Nickname, "kimi-code-account");
            account = new ProviderAccount(
                ProviderIdentity.ForAccount(Code, externalId),
                ProviderIdentity.ForProvider(Code),
                info.Nickname,
                info.UserId,
                DataSource.OfficialCli,
                ConfidenceLevel.Official,
                UtcNow);

        }
        else if (userInfo.Malformed)
        {
            metadataWarning = "Kimi capacity was retrieved, but account metadata was malformed.";
        }
        else if (userInfo.StatusCode.HasValue)
        {
            metadataWarning = "Kimi capacity was retrieved, but account metadata was unavailable.";
        }

        if (quotaWindows.Count == 0)
        {
            return Partial(account, null, quotaWindows,
                metadataWarning ?? "Kimi returned no structured quota rows.");
        }

        return metadataWarning is null
            ? ProviderRefreshResult.Success(Code, account, null, quotaWindows, UtcNow)
            : Partial(account, null, quotaWindows, metadataWarning);
    }

    private async Task<KimiCallResult<T>> FetchAsync<T>(
        HttpClient client,
        KimiOptions options,
        string path,
        CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(new Uri(options.ServerAddress, path), cancellationToken)
            .ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return new KimiCallResult<T>(null, response.StatusCode, false);
        }

        try
        {
            var envelope = await response.Content.ReadFromJsonAsync<KimiEnvelope<T>>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return new KimiCallResult<T>(envelope, null, envelope is null);
        }
        catch (JsonException)
        {
            return new KimiCallResult<T>(null, null, true);
        }
    }

    private IReadOnlyList<QuotaWindow> MapQuotaWindows(KimiUsageData data)
    {
        IEnumerable<KimiQuotaRow> rows = data.Limits is { Count: > 0 }
            ? data.Limits
            : data.Summary is null
                ? Array.Empty<KimiQuotaRow>()
                : new[] { data.Summary };

        var windows = rows
            .Select(MapQuotaRow)
            .Where(window => window is not null)
            .Cast<QuotaWindow>()
            .ToList();

        // Extra usage is a monetary wallet and includes a provider currency code. The current
        // QuotaWindow contract cannot preserve that identity, so it is intentionally not
        // normalized into a generic currency window.

        return windows;
    }

    private QuotaWindow? MapQuotaRow(KimiQuotaRow row)
    {
        var used = TryReadNumber(row.Used);
        var limit = TryReadNumber(row.Limit);
        if (!used.HasValue && !limit.HasValue)
        {
            return null;
        }

        var type = ResolveQuotaType(row.Window);
        var key = FirstNonBlank(row.Name, $"window:{row.Window?.Duration}:{row.Window?.Unit}");
        return QuotaWindow.Create(
            $"kimi-code:{key}",
            type,
            QuotaUnit.Custom,
            used,
            remainingValue: null,
            limit,
            usedPercentage: null,
            remainingPercentage: null,
            windowStart: null,
            resetAt: TryReadDateTime(row.ResetAt),
            DataSource.OfficialCli,
            ConfidenceLevel.Official,
            UtcNow);
    }

    private static QuotaType ResolveQuotaType(KimiQuotaWindow? window)
    {
        if (window is null)
        {
            return QuotaType.Custom;
        }

        var duration = TryReadNumber(window.Duration);
        var unit = window.Unit?.Trim().ToLowerInvariant();
        return (unit, duration) switch
        {
            ("hour", 5) => QuotaType.Rolling5Hour,
            ("day", 7) => QuotaType.Rolling7Day,
            ("day", 1) => QuotaType.Daily,
            ("week", 1) => QuotaType.Weekly,
            _ => QuotaType.Custom
        };
    }

    private ProviderRefreshResult MapHttpFailure(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized => AuthenticationRequired(),
        HttpStatusCode.Forbidden => FailureOrPartial(
            ProviderErrorCodes.PermissionDenied,
            "Kimi denied access to the configured local usage API."),
        HttpStatusCode.NotFound => HasLastKnownGood
            ? Failure(ProviderErrorCodes.UnsupportedScope, "The configured Kimi Code local usage API was not found.")
            : Unsupported(),
        (HttpStatusCode)429 => Failure(ProviderErrorCodes.RateLimited, "Kimi rate-limited the usage request."),
        >= HttpStatusCode.InternalServerError => Failure(
            ProviderErrorCodes.ProviderServerError, "Kimi failed the usage request."),
        _ => Failure(ProviderErrorCodes.ProviderError, "Kimi rejected the usage request.")
    };

    private ProviderRefreshResult MapBusinessFailure(int? status) => status switch
    {
        401 => AuthenticationRequired(),
        403 => FailureOrPartial(ProviderErrorCodes.PermissionDenied, "Kimi denied the usage request."),
        404 => HasLastKnownGood
            ? Failure(ProviderErrorCodes.UnsupportedScope, "Kimi did not expose the configured usage scope.")
            : Unsupported(),
        429 => Failure(ProviderErrorCodes.RateLimited, "Kimi rate-limited the usage request."),
        >= 500 => Failure(ProviderErrorCodes.ProviderServerError, "Kimi failed the usage request."),
        _ => Failure(ProviderErrorCodes.ProviderError, "Kimi returned an unsuccessful usage result.")
    };

    private static double? TryReadNumber(JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
        {
            return number >= 0 && double.IsFinite(number) ? number : throw new JsonException("Invalid Kimi quota quantity.");
        }

        if (value.ValueKind == JsonValueKind.String &&
            double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number))
        {
            return number >= 0 && double.IsFinite(number) ? number : throw new JsonException("Invalid Kimi quota quantity.");
        }

        return null;
    }

    private static DateTimeOffset? TryReadDateTime(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String ||
            !DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
        {
            return null;
        }

        return result;
    }

    private static string FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "quota";

    private static bool IsAllowedServerAddress(Uri? address) =>
        address is { IsAbsoluteUri: true, IsLoopback: true } &&
        (address.Scheme == Uri.UriSchemeHttp || address.Scheme == Uri.UriSchemeHttps);

    private sealed record KimiCallResult<T>(KimiEnvelope<T>? Envelope, HttpStatusCode? StatusCode, bool Malformed);

    private sealed class KimiEnvelope<T>
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    private sealed class KimiUsageData
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("status")]
        public int? Status { get; set; }

        [JsonPropertyName("summary")]
        public KimiQuotaRow? Summary { get; set; }

        [JsonPropertyName("limits")]
        public List<KimiQuotaRow>? Limits { get; set; }

        [JsonPropertyName("extra_usage")]
        public KimiExtraUsage? ExtraUsage { get; set; }
    }

    private sealed class KimiUserInfoData
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("status")]
        public int? Status { get; set; }

        [JsonPropertyName("userInfo")]
        public KimiUserInfo? UserInfo { get; set; }
    }

    private sealed class KimiUserInfo
    {
        [JsonPropertyName("userId")]
        public string? UserId { get; set; }

        [JsonPropertyName("nickname")]
        public string? Nickname { get; set; }

    }

    private sealed class KimiQuotaRow
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("window")]
        public KimiQuotaWindow? Window { get; set; }

        [JsonPropertyName("used")]
        public JsonElement Used { get; set; }

        [JsonPropertyName("limit")]
        public JsonElement Limit { get; set; }

        [JsonPropertyName("reset_at")]
        public JsonElement ResetAt { get; set; }
    }

    private sealed class KimiQuotaWindow
    {
        [JsonPropertyName("duration")]
        public JsonElement Duration { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }
    }

    private sealed class KimiExtraUsage
    {
        [JsonPropertyName("monthly_charge_limit_enabled")]
        public bool MonthlyChargeLimitEnabled { get; set; }

        [JsonPropertyName("monthly_charge_limit_cents")]
        public double? MonthlyChargeLimitCents { get; set; }

        [JsonPropertyName("monthly_used_cents")]
        public double? MonthlyUsedCents { get; set; }
    }
}
