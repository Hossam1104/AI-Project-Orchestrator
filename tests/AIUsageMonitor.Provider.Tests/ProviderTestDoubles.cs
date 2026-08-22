using System.Net;
using System.Net.Http;
using AIUsageMonitor.Application.Security;
using AIUsageMonitor.Application.Time;
using AIUsageMonitor.Providers.Common;

namespace AIUsageMonitor.Provider.Tests;

internal sealed class TestClock : IClock
{
    public DateTimeOffset UtcNow { get; init; } = new(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
}

internal sealed class TestCredentialStore : ISecureCredentialStore
{
    private readonly Dictionary<string, string> _secrets = new(StringComparer.OrdinalIgnoreCase);

    public void Add(string reference, string secret) => _secrets[reference] = secret;

    public Task StoreAsync(string credentialReference, string secret, CancellationToken cancellationToken = default)
    {
        _secrets[credentialReference] = secret;
        return Task.CompletedTask;
    }

    public Task<string?> RetrieveAsync(string credentialReference, CancellationToken cancellationToken = default)
    {
        _secrets.TryGetValue(credentialReference, out var secret);
        return Task.FromResult(secret);
    }

    public Task RemoveAsync(string credentialReference, CancellationToken cancellationToken = default)
    {
        _secrets.Remove(credentialReference);
        return Task.CompletedTask;
    }
}

internal sealed class TestExecutableLocator : IExecutableLocator
{
    private readonly HashSet<string> _commands;

    public TestExecutableLocator(params string[] commands)
    {
        _commands = new HashSet<string>(commands, StringComparer.OrdinalIgnoreCase);
    }

    public string? Find(string commandName) =>
        _commands.Contains(commandName) ? $"C:\\test-tools\\{commandName}.exe" : null;
}

internal sealed class TestHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient _client;

    public TestHttpClientFactory(HttpMessageHandler handler)
    {
        _client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://test.invalid/"),
            Timeout = Timeout.InfiniteTimeSpan
        };
    }

    public HttpClient CreateClient(string name) => _client;
}

internal sealed class DelegateHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public DelegateHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    public int RequestCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestCount++;
        return _handler(request, cancellationToken);
    }

    public static DelegateHttpMessageHandler Json(
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new((_, _) => Task.FromResult(JsonResponse(json, statusCode)));

    public static HttpResponseMessage JsonResponse(
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
}
