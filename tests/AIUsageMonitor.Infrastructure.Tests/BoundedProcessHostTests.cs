using AIUsageMonitor.Infrastructure.Execution;

namespace AIUsageMonitor.Infrastructure.Tests;

public sealed class BoundedProcessHostTests
{
    [Fact]
    public async Task FastProcess_ReportsSuccessfulExit()
    {
        var result = await CreateHost().RunAsync(CreateDotnetRequest("--version", TimeSpan.FromSeconds(5)));

        Assert.Equal(BoundedProcessOutcome.ExitedSuccessfully, result.Outcome);
        Assert.Equal(0, result.ExitCode);
        Assert.False(result.StandardOutputTruncated);
        Assert.True(result.ProcessTerminationConfirmed);
    }

    [Fact]
    public async Task NonZeroProcess_IsDistinguished()
    {
        var result = await CreateHost().RunAsync(CreateDotnetRequest("--definitely-not-a-real-dotnet-command", TimeSpan.FromSeconds(5)));

        Assert.Equal(BoundedProcessOutcome.NonZeroExit, result.Outcome);
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task Timeout_KillsWaitingProcessAndReturnsBoundedly()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await CreateHost().RunAsync(new BoundedProcessRequest(
            "ping",
            ["127.0.0.1", "-n", "10"],
            Environment.CurrentDirectory,
            TimeSpan.FromMilliseconds(150),
            drainTimeout: TimeSpan.FromMilliseconds(200)));
        stopwatch.Stop();

        Assert.Equal(BoundedProcessOutcome.TimedOut, result.Outcome);
        Assert.True(result.ProcessWasKilled);
        Assert.True(result.ProcessTerminationConfirmed);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3), $"Process host was not bounded: {stopwatch.Elapsed}.");
    }

    [Fact]
    public async Task CallerCancellation_IsDistinguishedAndKillsProcess()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        var result = await CreateHost().RunAsync(new BoundedProcessRequest(
            "ping",
            ["127.0.0.1", "-n", "10"],
            Environment.CurrentDirectory,
            TimeSpan.FromSeconds(5),
            drainTimeout: TimeSpan.FromMilliseconds(200)), cancellation.Token);

        Assert.Equal(BoundedProcessOutcome.Cancelled, result.Outcome);
        Assert.True(result.ProcessWasKilled);
        Assert.True(result.ProcessTerminationConfirmed);
    }

    [Fact]
    public async Task MissingExecutable_ReportsStartFailure()
    {
        var result = await CreateHost().RunAsync(new BoundedProcessRequest(
            Path.Combine(Path.GetTempPath(), "does-not-exist-" + Guid.NewGuid().ToString("N") + ".exe"),
            [],
            Environment.CurrentDirectory,
            TimeSpan.FromSeconds(1)));

        Assert.Equal(BoundedProcessOutcome.StartFailed, result.Outcome);
        Assert.False(result.ProcessTerminationConfirmed);
    }

    [Fact]
    public async Task StandardOutput_IsBoundedAndFlagged()
    {
        var result = await CreateHost().RunAsync(CreateDotnetRequest("--info", TimeSpan.FromSeconds(5), maxStdoutBytes: 32));

        Assert.True(result.StandardOutputTruncated);
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(result.StandardOutput) <= 32);
    }

    [Fact]
    public async Task StandardError_IsBoundedAndFlagged()
    {
        var result = await CreateHost().RunAsync(CreateDotnetRequest("--definitely-not-a-real-dotnet-command", TimeSpan.FromSeconds(5), maxStderrBytes: 16));

        Assert.True(result.StandardErrorTruncated);
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(result.StandardError) <= 16);
    }

    [Fact]
    public void ShellExecutables_AreRejectedBeforeStart()
    {
        Assert.Throws<ArgumentException>(() => new BoundedProcessRequest(
            "cmd.exe",
            ["/c", "echo unsafe"],
            Environment.CurrentDirectory,
            TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void StructuredArguments_AreKeptAsSeparateValues()
    {
        var request = new BoundedProcessRequest(
            "git",
            ["--version", "literal && not shell syntax"],
            Environment.CurrentDirectory,
            TimeSpan.FromSeconds(1));

        Assert.Equal("literal && not shell syntax", request.Arguments[1]);
    }

    private static BoundedProcessHost CreateHost() => new();

    private static BoundedProcessRequest CreateDotnetRequest(
        string argument,
        TimeSpan timeout,
        int maxStdoutBytes = 32 * 1024,
        int maxStderrBytes = 32 * 1024) => new(
        Environment.ProcessPath ?? "dotnet",
        [argument],
        Environment.CurrentDirectory,
        timeout,
        maxStdoutBytes,
        maxStderrBytes,
        TimeSpan.FromSeconds(1));
}
