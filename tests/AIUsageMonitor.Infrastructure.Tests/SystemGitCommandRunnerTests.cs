using System.Diagnostics;
using AIUsageMonitor.Infrastructure.Git;

namespace AIUsageMonitor.Infrastructure.Tests;

/// <summary>
/// Exercises the real <see cref="SystemGitCommandRunner"/> against a real long-running OS process
/// (Windows <c>ping</c>, not Git) so the end-to-end bounded-timeout/cancellation path is proven
/// without depending on Git being installed and without a real ten-second wait: the injectable
/// command/drain timeouts are configured short for the test only, never in production.
/// </summary>
public sealed class SystemGitCommandRunnerTests
{
    [Fact]
    public async Task RunAsync_ReturnsBoundedResult_WhenProcessExceedsCommandTimeout()
    {
        var runner = new SystemGitCommandRunner(
            commandTimeout: TimeSpan.FromMilliseconds(300),
            drainTimeout: TimeSpan.FromMilliseconds(300),
            fileName: "ping");
        var stopwatch = Stopwatch.StartNew();

        var result = await runner.RunAsync(["-n", "30", "127.0.0.1"]);

        stopwatch.Stop();
        Assert.True(result.TimedOut);
        Assert.False(result.Cancelled);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"Expected a bounded return; took {stopwatch.Elapsed}.");
    }

    [Fact]
    public async Task RunAsync_ReturnsBoundedResult_WhenExternallyCancelled()
    {
        var runner = new SystemGitCommandRunner(
            commandTimeout: TimeSpan.FromSeconds(10),
            drainTimeout: TimeSpan.FromMilliseconds(300),
            fileName: "ping");
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        var stopwatch = Stopwatch.StartNew();

        var result = await runner.RunAsync(["-n", "30", "127.0.0.1"], cancellation.Token);

        stopwatch.Stop();
        Assert.True(result.Cancelled);
        Assert.False(result.TimedOut);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"Expected a bounded return; took {stopwatch.Elapsed}.");
    }

    [Fact]
    public async Task RunAsync_CompletesNormally_ForAFastProcess()
    {
        var runner = new SystemGitCommandRunner(
            commandTimeout: TimeSpan.FromSeconds(10),
            drainTimeout: TimeSpan.FromSeconds(2),
            fileName: "ping");

        var result = await runner.RunAsync(["-n", "1", "127.0.0.1"]);

        Assert.False(result.TimedOut);
        Assert.False(result.Cancelled);
        Assert.False(result.CouldNotStart);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void CreateStartInfo_SetsDeterministicGitEnvironment()
    {
        var startInfo = SystemGitCommandRunner.CreateStartInfo(["status"]);

        Assert.Equal("C", startInfo.Environment["LC_ALL"]);
        Assert.Equal("C", startInfo.Environment["LANG"]);
    }
}
