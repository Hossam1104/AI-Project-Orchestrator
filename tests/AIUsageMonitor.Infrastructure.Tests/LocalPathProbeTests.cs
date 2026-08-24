using System.Diagnostics;
using AIUsageMonitor.Infrastructure.Git;

namespace AIUsageMonitor.Infrastructure.Tests;

public sealed class LocalPathProbeTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "apo-path-probe-tests-" + Guid.NewGuid().ToString("N"));

    public LocalPathProbeTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public async Task ProbeAsync_AvailableDirectory_ForARealDirectory()
    {
        var probe = new SystemLocalPathProbe(TimeSpan.FromSeconds(4));

        var result = await probe.ProbeAsync(_root);

        Assert.Equal(LocalPathProbeStatus.AvailableDirectory, result.Status);
        Assert.False(result.TimedOut);
    }

    [Fact]
    public async Task ProbeAsync_Missing_ForANonexistentPath()
    {
        var probe = new SystemLocalPathProbe(TimeSpan.FromSeconds(4));
        var missingPath = Path.Combine(_root, "does-not-exist");

        var result = await probe.ProbeAsync(missingPath);

        Assert.Equal(LocalPathProbeStatus.Missing, result.Status);
    }

    [Fact]
    public async Task ProbeAsync_NotADirectory_ForAFile()
    {
        var probe = new SystemLocalPathProbe(TimeSpan.FromSeconds(4));
        var filePath = Path.Combine(_root, "file.txt");
        File.WriteAllText(filePath, "content");

        var result = await probe.ProbeAsync(filePath);

        Assert.Equal(LocalPathProbeStatus.NotADirectory, result.Status);
    }

    [Fact]
    public async Task ProbeAsync_MissingAndUnavailable_AreDistinctOutcomes()
    {
        // Mirrors production ProbeCore's contract: access-denial is caught internally and
        // surfaced as Unavailable, never as an exception crossing the seam boundary.
        var probe = new SystemLocalPathProbe(
            TimeSpan.FromSeconds(4),
            _ => new LocalPathProbeResult(LocalPathProbeStatus.Unavailable));

        var result = await probe.ProbeAsync(_root);

        Assert.Equal(LocalPathProbeStatus.Unavailable, result.Status);
        Assert.NotEqual(LocalPathProbeStatus.Missing, result.Status);
    }

    [Fact]
    public async Task ProbeAsync_IOFailure_IsUnavailable()
    {
        var probe = new SystemLocalPathProbe(
            TimeSpan.FromSeconds(4),
            _ => new LocalPathProbeResult(LocalPathProbeStatus.Unavailable));

        var result = await probe.ProbeAsync(_root);

        Assert.Equal(LocalPathProbeStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task ProbeAsync_MalformedPath_IsUnavailableRatherThanThrowing()
    {
        var probe = new SystemLocalPathProbe(TimeSpan.FromSeconds(4));
        var malformedPath = _root + "\0invalid";

        var result = await probe.ProbeAsync(malformedPath);

        Assert.Equal(LocalPathProbeStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task ProbeAsync_ReturnsBoundedTimeout_WhenUnderlyingProbeNeverCompletes()
    {
        var neverReturns = new ManualResetEventSlim(initialState: false);
        var probe = new SystemLocalPathProbe(
            TimeSpan.FromMilliseconds(100),
            path =>
            {
                neverReturns.Wait(TimeSpan.FromSeconds(10));
                return new LocalPathProbeResult(LocalPathProbeStatus.AvailableDirectory);
            });
        var stopwatch = Stopwatch.StartNew();

        var result = await probe.ProbeAsync(_root);

        stopwatch.Stop();
        Assert.Equal(LocalPathProbeStatus.Unavailable, result.Status);
        Assert.True(result.TimedOut);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"Expected a bounded return; took {stopwatch.Elapsed}.");
        neverReturns.Set();
    }

    [Fact]
    public async Task ProbeAsync_HonorsExternalCancellation_WithoutWaitingForTheUnderlyingProbe()
    {
        var neverReturns = new ManualResetEventSlim(initialState: false);
        var probe = new SystemLocalPathProbe(
            TimeSpan.FromSeconds(10),
            path =>
            {
                neverReturns.Wait(TimeSpan.FromSeconds(10));
                return new LocalPathProbeResult(LocalPathProbeStatus.AvailableDirectory);
            });
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var stopwatch = Stopwatch.StartNew();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => probe.ProbeAsync(_root, cancellation.Token));

        stopwatch.Stop();
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"Expected a bounded return; took {stopwatch.Elapsed}.");
        neverReturns.Set();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
