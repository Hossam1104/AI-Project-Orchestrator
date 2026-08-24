using System.Diagnostics;
using AIUsageMonitor.Infrastructure.Git;

namespace AIUsageMonitor.Infrastructure.Tests;

/// <summary>
/// Deterministic proof that once a Git command times out or is cancelled, the runner regains
/// control within a bounded drain window even when termination fails and stdout/stderr never
/// reach EOF. No real process or ten-second sleep is used.
/// </summary>
public sealed class BoundedProcessWaitTests
{
    [Fact]
    public async Task HandleTimeoutOrCancellation_ReturnsBoundedResult_WhenKillThrowsAndStreamsNeverComplete()
    {
        var neverCompletingOutput = new TaskCompletionSource<string>();
        var neverCompletingError = new TaskCompletionSource<string>();
        var stopwatch = Stopwatch.StartNew();

        var result = await BoundedProcessWait.HandleTimeoutOrCancellationAsync(
            killAction: () => throw new InvalidOperationException("kill failed"),
            neverCompletingOutput.Task,
            neverCompletingError.Task,
            drainTimeout: TimeSpan.FromMilliseconds(50),
            wasExternallyCancelled: false);

        stopwatch.Stop();

        Assert.True(result.TimedOut);
        Assert.False(result.Cancelled);
        Assert.Equal(string.Empty, result.StandardOutput);
        Assert.Equal(string.Empty, result.StandardError);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"Expected a bounded return; took {stopwatch.Elapsed}.");
    }

    [Fact]
    public async Task HandleTimeoutOrCancellation_ReportsCancelled_WhenExternallyCancelled()
    {
        var neverCompletingOutput = new TaskCompletionSource<string>();
        var neverCompletingError = new TaskCompletionSource<string>();

        var result = await BoundedProcessWait.HandleTimeoutOrCancellationAsync(
            killAction: () => { },
            neverCompletingOutput.Task,
            neverCompletingError.Task,
            drainTimeout: TimeSpan.FromMilliseconds(50),
            wasExternallyCancelled: true);

        Assert.True(result.Cancelled);
        Assert.False(result.TimedOut);
    }

    [Fact]
    public async Task AwaitOutputWithBound_ReturnsWithoutWaitingForHungStreams()
    {
        var neverCompletingOutput = new TaskCompletionSource<string>();
        var neverCompletingError = new TaskCompletionSource<string>();
        var stopwatch = Stopwatch.StartNew();

        await BoundedProcessWait.AwaitOutputWithBoundAsync(
            neverCompletingOutput.Task, neverCompletingError.Task, TimeSpan.FromMilliseconds(50));

        stopwatch.Stop();
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1), $"Expected a bounded return; took {stopwatch.Elapsed}.");
    }

    [Fact]
    public async Task AwaitOutputWithBound_CompletesImmediatelyWhenStreamsAreAlreadyDone()
    {
        var output = Task.FromResult("out");
        var error = Task.FromResult("err");

        await BoundedProcessWait.AwaitOutputWithBoundAsync(output, error, TimeSpan.FromSeconds(5));

        Assert.Equal("out", await output);
        Assert.Equal("err", await error);
    }
}
