using AIUsageMonitor.Application.Providers;
using AIUsageMonitor.Application.Time;
using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Domain.Quotas;
using AIUsageMonitor.Domain.Subscriptions;

namespace AIUsageMonitor.Providers.Common;

/// <summary>
/// Shared lifecycle and last-known-good behavior for provider adapters. Provider-specific
/// classes remain responsible for detection, transport, parsing, and error classification.
/// </summary>
public abstract class ProviderAdapterBase : IAiUsageProvider
{
    private readonly IClock _clock;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private ProviderRefreshResult? _lastKnownGood;

    protected ProviderAdapterBase(IClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public abstract ProviderCode Code { get; }

    protected DateTimeOffset UtcNow => _clock.UtcNow;

    public abstract Task<ProviderDetectionResult> DetectAsync(CancellationToken cancellationToken = default);

    public abstract Task<ProviderConnectionStatus> GetConnectionStatusAsync(
        CancellationToken cancellationToken = default);

    public async Task<ProviderAccount?> GetAccountAsync(CancellationToken cancellationToken = default) =>
        (await RefreshAsync(cancellationToken).ConfigureAwait(false)).Account;

    public async Task<Subscription?> GetSubscriptionAsync(CancellationToken cancellationToken = default) =>
        (await RefreshAsync(cancellationToken).ConfigureAwait(false)).Subscription;

    public async Task<IReadOnlyList<QuotaWindow>> GetQuotasAsync(CancellationToken cancellationToken = default) =>
        (await RefreshAsync(cancellationToken).ConfigureAwait(false)).QuotaWindows;

    public async Task<ProviderRefreshResult> RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            try
            {
                var result = await RefreshCoreAsync(cancellationToken).ConfigureAwait(false);
                return RememberIfUseful(result);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (TaskCanceledException)
            {
                return Failure(ProviderErrorCodes.Timeout, "The provider request timed out.");
            }
            catch (HttpRequestException)
            {
                return Failure(ProviderErrorCodes.Network, "The provider could not be reached.");
            }
            catch (System.Text.Json.JsonException)
            {
                return Failure(ProviderErrorCodes.MalformedResponse, "The provider returned malformed data.");
            }
            catch (Exception)
            {
                return Failure(ProviderErrorCodes.ProviderError, "The provider refresh failed.");
            }
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    protected abstract Task<ProviderRefreshResult> RefreshCoreAsync(CancellationToken cancellationToken);

    protected ProviderRefreshResult Unsupported() =>
        ProviderRefreshResult.Unsupported(Code, UtcNow);

    protected ProviderRefreshResult AuthenticationRequired() =>
        ProviderRefreshResult.AuthenticationRequired(Code, UtcNow);

    protected ProviderRefreshResult Partial(
        ProviderAccount? account,
        Subscription? subscription,
        IReadOnlyList<QuotaWindow> quotaWindows,
        string message) =>
        ProviderRefreshResult.Partial(Code, account, subscription, quotaWindows, message, UtcNow);

    protected ProviderRefreshResult Failure(string errorCode, string errorMessage) =>
        ProviderRefreshResult.Failed(
            Code,
            errorCode,
            errorMessage,
            UtcNow,
            _lastKnownGood?.Account,
            _lastKnownGood?.Subscription,
            _lastKnownGood?.QuotaWindows);

    protected ProviderRefreshResult FailureOrPartial(
        string errorCode,
        string errorMessage,
        ProviderAccount? account = null,
        Subscription? subscription = null,
        IReadOnlyList<QuotaWindow>? quotaWindows = null)
    {
        if (_lastKnownGood is not null)
        {
            return Failure(errorCode, errorMessage);
        }

        return Partial(account, subscription, quotaWindows ?? Array.Empty<QuotaWindow>(), errorMessage);
    }

    protected bool HasLastKnownGood => _lastKnownGood is not null;

    private ProviderRefreshResult RememberIfUseful(ProviderRefreshResult result)
    {
        if ((result.Outcome is ProviderRefreshOutcome.Success or ProviderRefreshOutcome.Partial) &&
            (result.Account is not null || result.Subscription is not null || result.QuotaWindows.Count > 0))
        {
            _lastKnownGood = result;
        }

        return result;
    }
}
