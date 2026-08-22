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

namespace AIUsageMonitor.Providers.Copilot;

/// <summary>
/// GitHub Copilot billing-usage adapter. The current official endpoint reports consumed AI
/// credits/request quantities, not a universal personal allowance. This adapter therefore returns
/// explicit usage-only windows as Partial and never invents a limit or remaining percentage.
/// </summary>
public sealed class CopilotProvider : ProviderAdapterBase
{
    public const string HttpClientName = "AIUsageMonitor.GitHub";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISecureCredentialStore _credentialStore;
    private readonly CopilotOptions _options;

    public CopilotProvider(
        IClock clock,
        IHttpClientFactory httpClientFactory,
        ISecureCredentialStore credentialStore,
        CopilotOptions options)
        : base(clock)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _credentialStore = credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override ProviderCode Code => ProviderCode.Copilot;

    private bool HasSupportedScope =>
        _options.Scope == CopilotBillingScope.PersonalUser ||
        (_options.Scope == CopilotBillingScope.Organization && !string.IsNullOrWhiteSpace(_options.Organization));

    public override Task<ProviderDetectionResult> DetectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var configured = HasSupportedScope && !string.IsNullOrWhiteSpace(_options.CredentialReference);
        return Task.FromResult(new ProviderDetectionResult(
            Code,
            configured,
            configured
                ? "GitHub billing usage API configured."
                : "GitHub billing usage API requires an explicit scope and credential reference.",
            UtcNow));
    }

    public override async Task<ProviderConnectionStatus> GetConnectionStatusAsync(
        CancellationToken cancellationToken = default)
    {
        if (!HasSupportedScope)
        {
            return ProviderConnectionStatus.Unsupported;
        }

        if (string.IsNullOrWhiteSpace(_options.CredentialReference))
        {
            return ProviderConnectionStatus.AuthenticationRequired;
        }

        var token = await _credentialStore.RetrieveAsync(_options.CredentialReference, cancellationToken)
            .ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(token)
            ? ProviderConnectionStatus.AuthenticationRequired
            : ProviderConnectionStatus.Connected;
    }

    protected override async Task<ProviderRefreshResult> RefreshCoreAsync(CancellationToken cancellationToken)
    {
        if (!HasSupportedScope)
        {
            return Unsupported();
        }

        if (string.IsNullOrWhiteSpace(_options.CredentialReference))
        {
            return AuthenticationRequired();
        }

        var token = await _credentialStore.RetrieveAsync(_options.CredentialReference, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticationRequired();
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        var subjectResolution = await ResolveSubjectAsync(client, cancellationToken).ConfigureAwait(false);
        if (subjectResolution.StatusCode == HttpStatusCode.Unauthorized)
        {
            return AuthenticationRequired();
        }

        var subject = subjectResolution.Subject;
        if (subject is null)
        {
            return Partial(null, null, Array.Empty<QuotaWindow>(),
                "GitHub identity was unavailable; Copilot billing usage cannot be mapped safely.");
        }

        using var response = await client.GetAsync(BuildUsagePath(subject), cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return MapHttpFailure(response.StatusCode);
        }

        var payload = await response.Content.ReadFromJsonAsync<CopilotUsageResponse>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (payload?.UsageItems is null)
        {
            return Failure(ProviderErrorCodes.MalformedResponse, "GitHub returned an unusable Copilot usage report.");
        }

        var account = new ProviderAccount(
            ProviderIdentity.ForAccount(Code, subject),
            ProviderIdentity.ForProvider(Code),
            subject,
            subject,
            DataSource.OfficialApi,
            ConfidenceLevel.Official,
            UtcNow);

        var windows = payload.UsageItems
            .Select((item, index) => MapUsageItem(item, index))
            .Where(window => window is not null)
            .Cast<QuotaWindow>()
            .ToArray();

        if (windows.Length == 0)
        {
            return Partial(account, null, windows,
                "GitHub returned no metered Copilot quantities; remaining capacity is unavailable.");
        }

        return Partial(account, null, windows,
            "Official Copilot usage was retrieved, but the selected billing surface does not expose a corresponding allowance.");
    }

    private async Task<SubjectResolution> ResolveSubjectAsync(HttpClient client, CancellationToken cancellationToken)
    {
        if (_options.Scope != CopilotBillingScope.PersonalUser)
        {
            return new SubjectResolution(
                _options.Scope == CopilotBillingScope.Organization ? _options.Organization : _options.Enterprise,
                null);
        }

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            return new SubjectResolution(_options.Username, null);
        }

        using var response = await client.GetAsync("user", cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new SubjectResolution(null, response.StatusCode);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new SubjectResolution(null, response.StatusCode);
        }

        var user = await response.Content.ReadFromJsonAsync<GitHubUserResponse>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return new SubjectResolution(
            string.IsNullOrWhiteSpace(user?.Login) ? null : user.Login,
            null);
    }

    private string BuildUsagePath(string subject)
    {
        var escaped = Uri.EscapeDataString(subject);
        return _options.Scope switch
        {
            CopilotBillingScope.PersonalUser => $"users/{escaped}/settings/billing/ai_credit/usage",
            CopilotBillingScope.Organization => $"organizations/{escaped}/settings/billing/ai_credit/usage",
            _ => throw new InvalidOperationException("Enterprise Copilot billing usage is not enabled by this adapter.")
        };
    }

    private ProviderRefreshResult MapHttpFailure(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized => AuthenticationRequired(),
        HttpStatusCode.Forbidden => FailureOrPartial(
            ProviderErrorCodes.PermissionDenied,
            "Permission denied for the configured GitHub Copilot billing scope."),
        HttpStatusCode.NotFound => HasLastKnownGood
            ? Failure(ProviderErrorCodes.UnsupportedScope, "The configured GitHub Copilot billing scope was not found.")
            : Unsupported(),
        (HttpStatusCode)429 => Failure(ProviderErrorCodes.RateLimited, "GitHub rate-limited the Copilot usage request."),
        >= HttpStatusCode.InternalServerError => Failure(
            ProviderErrorCodes.ProviderServerError, "GitHub failed the Copilot usage request."),
        _ => Failure(ProviderErrorCodes.ProviderError, "GitHub rejected the Copilot usage request.")
    };

    private QuotaWindow? MapUsageItem(CopilotUsageItem item, int index)
    {
        var quantity = TryReadNumber(item.GrossQuantity) ?? TryReadNumber(item.NetQuantity);
        if (!quantity.HasValue)
        {
            return null;
        }

        var unitType = item.UnitType?.Trim().ToLowerInvariant();
        var unit = unitType switch
        {
            "ai-credits" or "credits" => QuotaUnit.Credits,
            "requests" or "premium-requests" => QuotaUnit.Requests,
            "tokens" => QuotaUnit.Tokens,
            _ => QuotaUnit.Custom
        };
        var type = unitType switch
        {
            "ai-credits" or "credits" => QuotaType.AiCredits,
            "requests" or "premium-requests" => QuotaType.RequestAllowance,
            "tokens" => QuotaType.Tokens,
            _ => QuotaType.Custom
        };

        var keyPart = FirstNonBlank(item.Sku, item.Model, item.Product, $"item-{index}");
        return QuotaWindow.Create(
            $"copilot:{_options.Scope}:{keyPart}",
            type,
            unit,
            usedValue: quantity,
            remainingValue: null,
            limitValue: null,
            usedPercentage: null,
            remainingPercentage: null,
            windowStart: null,
            resetAt: null,
            DataSource.OfficialApi,
            ConfidenceLevel.Official,
            UtcNow);
    }

    private static double? TryReadNumber(JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
        {
            return number >= 0 && double.IsFinite(number) ? number : throw new JsonException("Invalid usage quantity.");
        }

        if (value.ValueKind == JsonValueKind.String &&
            double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number))
        {
            return number >= 0 && double.IsFinite(number) ? number : throw new JsonException("Invalid usage quantity.");
        }

        return null;
    }

    private static string FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "usage";

    private sealed class GitHubUserResponse
    {
        [JsonPropertyName("login")]
        public string? Login { get; set; }
    }

    private sealed record SubjectResolution(string? Subject, HttpStatusCode? StatusCode);

    private sealed class CopilotUsageResponse
    {
        [JsonPropertyName("usageItems")]
        public List<CopilotUsageItem>? UsageItems { get; set; }
    }

    private sealed class CopilotUsageItem
    {
        [JsonPropertyName("product")]
        public string? Product { get; set; }

        [JsonPropertyName("sku")]
        public string? Sku { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("unitType")]
        public string? UnitType { get; set; }

        [JsonPropertyName("grossQuantity")]
        public JsonElement GrossQuantity { get; set; }

        [JsonPropertyName("netQuantity")]
        public JsonElement NetQuantity { get; set; }
    }
}
