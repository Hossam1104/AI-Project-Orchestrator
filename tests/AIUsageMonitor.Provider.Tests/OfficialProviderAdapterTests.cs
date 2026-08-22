using System.Net;
using AIUsageMonitor.Application.Providers;
using AIUsageMonitor.Domain.Common;
using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Quotas;
using AIUsageMonitor.Providers.Antigravity;
using AIUsageMonitor.Providers.Claude;
using AIUsageMonitor.Providers.Codex;
using AIUsageMonitor.Providers.Copilot;
using AIUsageMonitor.Providers.Kimi;

namespace AIUsageMonitor.Provider.Tests;

public sealed class OfficialProviderAdapterTests
{
    private const string TestSecret = "apo-test-secret-do-not-log-123";

    [Fact]
    public async Task Copilot_MapsOfficialUsageWithoutFabricatingAllowance()
    {
        var handler = DelegateHttpMessageHandler.Json(
            """
            {
              "usageItems": [
                {
                  "product": "Copilot",
                  "sku": "Copilot AI Credits",
                  "model": "GPT-5",
                  "unitType": "ai-credits",
                  "grossQuantity": 123,
                  "netQuantity": 123
                }
              ]
            }
            """);
        var credentials = new TestCredentialStore();
        credentials.Add("github-copilot", TestSecret);
        var provider = new CopilotProvider(
            new TestClock(),
            new TestHttpClientFactory(handler),
            credentials,
            new CopilotOptions
            {
                CredentialReference = "github-copilot",
                Username = "octocat"
            });

        var result = await provider.RefreshAsync();

        Assert.Equal(ProviderRefreshOutcome.Partial, result.Outcome);
        var quota = Assert.Single(result.QuotaWindows);
        Assert.Equal(123, quota.UsedValue);
        Assert.Null(quota.LimitValue);
        Assert.Null(quota.RemainingValue);
        Assert.Equal(QuotaUnit.Credits, quota.Unit);
        Assert.Equal(DataSource.OfficialApi, quota.Source);
        Assert.DoesNotContain(TestSecret, result.ErrorMessage ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Copilot_401_IsAuthenticationRequiredWithoutSecretInResult()
    {
        var credentials = new TestCredentialStore();
        credentials.Add("github-copilot", TestSecret);
        var provider = new CopilotProvider(
            new TestClock(),
            new TestHttpClientFactory(DelegateHttpMessageHandler.Json("{}", HttpStatusCode.Unauthorized)),
            credentials,
            new CopilotOptions { CredentialReference = "github-copilot", Username = "octocat" });

        var result = await provider.RefreshAsync();

        Assert.Equal(ProviderRefreshOutcome.AuthenticationRequired, result.Outcome);
        Assert.DoesNotContain(TestSecret, result.ErrorMessage ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain(TestSecret, result.ErrorCode ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Copilot_403_IsPermissionAwarePartial()
    {
        var credentials = new TestCredentialStore();
        credentials.Add("github-copilot", TestSecret);
        var provider = new CopilotProvider(
            new TestClock(),
            new TestHttpClientFactory(DelegateHttpMessageHandler.Json("{}", HttpStatusCode.Forbidden)),
            credentials,
            new CopilotOptions { CredentialReference = "github-copilot", Username = "octocat" });

        var result = await provider.RefreshAsync();

        Assert.Equal(ProviderRefreshOutcome.Partial, result.Outcome);
        Assert.Contains("permission", result.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(TestSecret, result.ErrorMessage ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Copilot_RateLimitAfterSuccess_RetainsLastKnownUsageAsStale()
    {
        var responseNumber = 0;
        var handler = new DelegateHttpMessageHandler((_, _) =>
        {
            responseNumber++;
            return Task.FromResult(responseNumber == 1
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "{\"usageItems\":[{\"sku\":\"credits\",\"unitType\":\"credits\",\"grossQuantity\":10}]}",
                        System.Text.Encoding.UTF8,
                        "application/json")
                }
                : new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        });
        var credentials = new TestCredentialStore();
        credentials.Add("github-copilot", TestSecret);
        var provider = new CopilotProvider(
            new TestClock(),
            new TestHttpClientFactory(handler),
            credentials,
            new CopilotOptions { CredentialReference = "github-copilot", Username = "octocat" });

        var first = await provider.RefreshAsync();
        var second = await provider.RefreshAsync();

        Assert.Equal(ProviderRefreshOutcome.Partial, first.Outcome);
        Assert.Equal(ProviderRefreshOutcome.Stale, second.Outcome);
        Assert.Equal("rate_limited", second.ErrorCode);
        Assert.Equal(10, Assert.Single(second.QuotaWindows).UsedValue);
        Assert.DoesNotContain(TestSecret, second.ErrorMessage ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Copilot_MalformedResponse_IsTypedAndDoesNotLeakCredential()
    {
        var credentials = new TestCredentialStore();
        credentials.Add("github-copilot", TestSecret);
        var provider = new CopilotProvider(
            new TestClock(),
            new TestHttpClientFactory(DelegateHttpMessageHandler.Json("{\"usageItems\":\"not-an-array\"}")),
            credentials,
            new CopilotOptions { CredentialReference = "github-copilot", Username = "octocat" });

        var result = await provider.RefreshAsync();

        Assert.Equal(ProviderRefreshOutcome.ProviderError, result.Outcome);
        Assert.Equal("malformed_response", result.ErrorCode);
        Assert.DoesNotContain(TestSecret, result.ErrorMessage ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Copilot_CancellationIsPropagated()
    {
        var handler = new DelegateHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var credentials = new TestCredentialStore();
        credentials.Add("github-copilot", TestSecret);
        var provider = new CopilotProvider(
            new TestClock(),
            new TestHttpClientFactory(handler),
            credentials,
            new CopilotOptions { CredentialReference = "github-copilot", Username = "octocat" });
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.RefreshAsync(cancellation.Token));
    }

    [Fact]
    public async Task Claude_AdminApiUsageIsSeparateFromConsumerSubscription()
    {
        var handler = new DelegateHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/v1/organizations/usage_report/messages", request.RequestUri!.AbsolutePath);
            return Task.FromResult(DelegateHttpMessageHandler.JsonResponse(
                """
                {
                  "data": [
                    {
                      "starting_at": "2026-08-21T00:00:00Z",
                      "ending_at": "2026-08-22T00:00:00Z",
                      "results": [
                        {
                          "uncached_input_tokens": 100,
                          "cache_read_input_tokens": 10,
                          "output_tokens": 20,
                          "cache_creation": {
                            "ephemeral_1h_input_tokens": 5,
                            "ephemeral_5m_input_tokens": 2
                          }
                        }
                      ]
                    }
                  ]
                }
                """));
        });
        var credentials = new TestCredentialStore();
        credentials.Add("anthropic-admin", TestSecret);
        var provider = new ClaudeProvider(
            new TestClock(),
            new TestHttpClientFactory(handler),
            credentials,
            new TestExecutableLocator(),
            new AnthropicOptions { CredentialReference = "anthropic-admin" });

        var result = await provider.RefreshAsync();

        Assert.Equal(ProviderRefreshOutcome.Partial, result.Outcome);
        Assert.Null(result.Subscription);
        var quota = Assert.Single(result.QuotaWindows);
        Assert.Equal(137, quota.UsedValue);
        Assert.Equal(QuotaUnit.Tokens, quota.Unit);
        Assert.Null(quota.LimitValue);
        Assert.StartsWith("anthropic-api:", quota.ExternalKey, StringComparison.Ordinal);
        Assert.DoesNotContain(TestSecret, result.ErrorMessage ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Claude_OfficialCliDetectionLeavesSubscriptionCapacityUnsupported()
    {
        var provider = new ClaudeProvider(
            new TestClock(),
            new TestHttpClientFactory(DelegateHttpMessageHandler.Json("{}")),
            new TestCredentialStore(),
            new TestExecutableLocator("claude"),
            new AnthropicOptions());

        var detection = await provider.DetectAsync();
        var status = await provider.GetConnectionStatusAsync();
        var result = await provider.RefreshAsync();

        Assert.True(detection.IsDetected);
        Assert.Equal(ProviderConnectionStatus.LocalDetected, status);
        Assert.Equal(ProviderRefreshOutcome.Unsupported, result.Outcome);
    }

    [Fact]
    public async Task Kimi_DocumentedLocalUsageApiMapsLimitsResetAndPlan()
    {
        var handler = new DelegateHttpMessageHandler((request, _) =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/oauth/usage", StringComparison.Ordinal))
            {
                return Task.FromResult(DelegateHttpMessageHandler.JsonResponse(
                    """
                    {
                      "code": 0,
                      "data": {
                        "kind": "ok",
                        "summary": {
                          "name": "weekly",
                          "window": { "duration": 1, "unit": "week" },
                          "used": "20",
                          "limit": "100",
                          "reset_at": "2026-08-29T12:00:00Z"
                        },
                        "limits": [],
                        "extra_usage": {
                          "monthly_charge_limit_enabled": true,
                          "monthly_charge_limit_cents": 1000,
                          "monthly_used_cents": 250
                        }
                      }
                    }
                    """));
            }

            return Task.FromResult(DelegateHttpMessageHandler.JsonResponse(
                """
                {
                  "code": 0,
                  "data": {
                    "kind": "ok",
                    "userInfo": {
                      "userId": "kimi-user",
                      "nickname": "Kimi User",
                      "userLevelName": "Allegretto"
                    }
                  }
                }
                """));
        });
        var credentials = new TestCredentialStore();
        credentials.Add("kimi-server", TestSecret);
        var provider = new KimiProvider(
            new TestClock(),
            new TestHttpClientFactory(handler),
            credentials,
            new TestExecutableLocator("kimi"),
            new KimiOptions { CredentialReference = "kimi-server" });

        var result = await provider.RefreshAsync();

        Assert.Equal(ProviderRefreshOutcome.Success, result.Outcome);
        Assert.Equal("Allegretto", result.Subscription!.PlanName);
        Assert.Equal("Kimi User", result.Account!.DisplayName);
        var weekly = Assert.Single(result.QuotaWindows, window => window.Type == QuotaType.Weekly);
        Assert.Equal(20, weekly.UsedValue);
        Assert.Null(weekly.RemainingValue);
        Assert.Equal(80, weekly.RemainingPercentage);
        Assert.Equal(100, weekly.LimitValue);
        Assert.Equal(new DateTimeOffset(2026, 8, 29, 12, 0, 0, TimeSpan.Zero), weekly.ResetAt);
        Assert.DoesNotContain(TestSecret, result.ErrorMessage ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Kimi_MissingLocalServerCredentialIsAuthenticationRequired()
    {
        var provider = new KimiProvider(
            new TestClock(),
            new TestHttpClientFactory(DelegateHttpMessageHandler.Json("{}")),
            new TestCredentialStore(),
            new TestExecutableLocator("kimi"),
            new KimiOptions { CredentialReference = "kimi-server" });

        var result = await provider.RefreshAsync();

        Assert.Equal(ProviderRefreshOutcome.AuthenticationRequired, result.Outcome);
        Assert.Equal(ProviderConnectionStatus.AuthenticationRequired, await provider.GetConnectionStatusAsync());
    }

    [Fact]
    public async Task Kimi_MalformedUsageResponseIsTyped()
    {
        var credentials = new TestCredentialStore();
        credentials.Add("kimi-server", TestSecret);
        var provider = new KimiProvider(
            new TestClock(),
            new TestHttpClientFactory(DelegateHttpMessageHandler.Json("not-json")),
            credentials,
            new TestExecutableLocator("kimi"),
            new KimiOptions { CredentialReference = "kimi-server" });

        var result = await provider.RefreshAsync();

        Assert.Equal(ProviderRefreshOutcome.ProviderError, result.Outcome);
        Assert.Equal("malformed_response", result.ErrorCode);
        Assert.DoesNotContain(TestSecret, result.ErrorMessage ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Codex_And_Antigravity_DetectCliButDoNotScrapeInteractiveUsage()
    {
        var locator = new TestExecutableLocator("codex", "agy");
        var clock = new TestClock();
        var codex = new CodexProvider(clock, locator);
        var antigravity = new AntigravityProvider(clock, locator);

        Assert.True((await codex.DetectAsync()).IsDetected);
        Assert.Equal(ProviderConnectionStatus.LocalDetected, await codex.GetConnectionStatusAsync());
        Assert.Equal(ProviderRefreshOutcome.Unsupported, (await codex.RefreshAsync()).Outcome);

        Assert.True((await antigravity.DetectAsync()).IsDetected);
        Assert.Equal(ProviderConnectionStatus.LocalDetected, await antigravity.GetConnectionStatusAsync());
        Assert.Equal(ProviderRefreshOutcome.Unsupported, (await antigravity.RefreshAsync()).Outcome);
    }
}
