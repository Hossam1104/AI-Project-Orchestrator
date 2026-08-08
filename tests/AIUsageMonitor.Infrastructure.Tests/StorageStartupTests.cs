using AIUsageMonitor.Infrastructure.Persistence;

namespace AIUsageMonitor.Infrastructure.Tests;

public sealed class StorageStartupTests
{
    [Fact]
    public void TryInitialize_ReturnsDegradedResultWhenDirectoryCreationFails()
    {
        using var store = new TemporaryStore();
        var failure = new UnauthorizedAccessException("storage denied");

        var result = StorageStartup.TryInitialize(
            () => store.Paths,
            _ => throw failure);

        Assert.False(result.IsAvailable);
        Assert.Null(result.Paths);
        Assert.Same(failure, result.Failure);
    }

    [Fact]
    public void TryInitialize_DoesNotRedirectWhenPathResolutionFails()
    {
        var failure = new InvalidOperationException("LocalAppData unavailable");

        var result = StorageStartup.TryInitialize(
            () => throw failure,
            _ => { });

        Assert.False(result.IsAvailable);
        Assert.Null(result.Paths);
        Assert.Same(failure, result.Failure);
    }

    [Fact]
    public void TryInitialize_ReturnsAvailableForUsablePerUserStorage()
    {
        using var store = new TemporaryStore();

        var result = StorageStartup.TryInitialize(
            () => store.Paths,
            static paths => paths.EnsureDirectories());

        Assert.True(result.IsAvailable);
        Assert.Same(store.Paths, result.Paths);
        Assert.Null(result.Failure);
    }
}
