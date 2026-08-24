namespace AIUsageMonitor.Infrastructure.Git;

/// <summary>
/// Truthful, bounded outcome of probing whether a registered local path is an available
/// directory. This is deliberately narrower than <c>RepositoryVerificationStatus</c>: it only
/// answers "can Git even be invoked against this path", not repository semantics.
/// </summary>
internal enum LocalPathProbeStatus
{
    AvailableDirectory,
    Missing,
    NotADirectory,
    Unavailable
}

internal readonly record struct LocalPathProbeResult(LocalPathProbeStatus Status, bool TimedOut = false);

/// <summary>
/// Infrastructure-only seam over the local filesystem probe used before any Git process is
/// started. Kept separate from <see cref="IGitCommandRunner"/> so it can be faked deterministically
/// in tests without touching the filesystem or a Git process.
/// </summary>
internal interface ILocalPathProbe
{
    Task<LocalPathProbeResult> ProbeAsync(string path, CancellationToken cancellationToken = default);
}

/// <summary>
/// The underlying OS filesystem attribute lookup is not reliably cancellable (a UNC/offline path
/// can block the calling thread indefinitely), so the probe runs on a background thread and the
/// caller only ever waits up to <see cref="ProbeTimeout"/> for it, regardless of whether that
/// background work ever actually completes.
/// </summary>
internal sealed class SystemLocalPathProbe : ILocalPathProbe
{
    private static readonly TimeSpan DefaultProbeTimeout = TimeSpan.FromSeconds(4);
    private static readonly object InFlightGate = new();
    private static InFlightProbe? _inFlightProbe;

    private readonly TimeSpan _probeTimeout;
    private readonly Func<string, LocalPathProbeResult> _probeCore;

    public SystemLocalPathProbe()
        : this(DefaultProbeTimeout, ProbeCore)
    {
    }

    /// <summary>
    /// Test-only seam: <paramref name="probeCore"/> lets a test substitute a deterministic
    /// slow/hanging delegate for the real (uncancellable) filesystem call, so the bounded-timeout
    /// logic below can be proven without depending on real slow I/O.
    /// </summary>
    internal SystemLocalPathProbe(TimeSpan probeTimeout, Func<string, LocalPathProbeResult>? probeCore = null)
    {
        _probeTimeout = probeTimeout;
        _probeCore = probeCore ?? ProbeCore;
    }

    public async Task<LocalPathProbeResult> ProbeAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();

        InFlightProbe inFlight;
        lock (InFlightGate)
        {
            // A completed task may still be waiting for its observer to run. It no longer
            // represents an unresolved OS probe, so it can be evicted before starting a new one.
            if (_inFlightProbe is { Task.IsCompleted: true })
            {
                _inFlightProbe = null;
            }

            if (_inFlightProbe is null)
            {
                var probeTask = Task.Run(() => ExecuteProbe(path), CancellationToken.None);
                inFlight = new InFlightProbe(path, probeTask);
                _inFlightProbe = inFlight;
                _ = ObserveAndReleaseAsync(inFlight);
            }
            else
            {
                inFlight = _inFlightProbe;
            }
        }

        // A single unresolved probe is shared only by callers inspecting that same path. A
        // different path must not receive the first path's eventual result and must not start a
        // second potentially stuck OS operation.
        if (!string.Equals(inFlight.Path, path, StringComparison.Ordinal))
        {
            return new LocalPathProbeResult(LocalPathProbeStatus.Unavailable, TimedOut: true);
        }

        var delayTask = Task.Delay(_probeTimeout, cancellationToken);
        var completed = await Task.WhenAny(inFlight.Task, delayTask).ConfigureAwait(false);
        if (completed == inFlight.Task)
        {
            return await inFlight.Task.ConfigureAwait(false);
        }

        // The delay task won the race: either an external cancellation fired, or the probe
        // genuinely exceeded its bound. Caller cancellation never cancels or evicts the shared
        // underlying operation; it may continue until the OS returns.
        cancellationToken.ThrowIfCancellationRequested();
        return new LocalPathProbeResult(LocalPathProbeStatus.Unavailable, TimedOut: true);
    }

    private LocalPathProbeResult ExecuteProbe(string path)
    {
        try
        {
            return _probeCore(path);
        }
        catch (Exception)
        {
            // The real core translates known filesystem failures. Keep an unexpected core
            // failure bounded and truthful at this infrastructure seam as well.
            return new LocalPathProbeResult(LocalPathProbeStatus.Unavailable);
        }
    }

    private static async Task ObserveAndReleaseAsync(InFlightProbe inFlight)
    {
        try
        {
            // Always observe completion, including an unexpected exception from a test seam or
            // future core implementation, so no abandoned task becomes unobserved.
            await inFlight.Task.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // ExecuteProbe currently converts core exceptions to Unavailable. This catch keeps
            // the coordinator safe if task creation or that implementation changes later.
        }
        finally
        {
            lock (InFlightGate)
            {
                if (ReferenceEquals(_inFlightProbe, inFlight))
                {
                    _inFlightProbe = null;
                }
            }
        }
    }

    private static LocalPathProbeResult ProbeCore(string path)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            return attributes.HasFlag(FileAttributes.Directory)
                ? new LocalPathProbeResult(LocalPathProbeStatus.AvailableDirectory)
                : new LocalPathProbeResult(LocalPathProbeStatus.NotADirectory);
        }
        catch (FileNotFoundException)
        {
            return new LocalPathProbeResult(LocalPathProbeStatus.Missing);
        }
        catch (DirectoryNotFoundException)
        {
            return new LocalPathProbeResult(LocalPathProbeStatus.Missing);
        }
        catch (UnauthorizedAccessException)
        {
            return new LocalPathProbeResult(LocalPathProbeStatus.Unavailable);
        }
        catch (IOException)
        {
            return new LocalPathProbeResult(LocalPathProbeStatus.Unavailable);
        }
        catch (ArgumentException)
        {
            return new LocalPathProbeResult(LocalPathProbeStatus.Unavailable);
        }
        catch (NotSupportedException)
        {
            return new LocalPathProbeResult(LocalPathProbeStatus.Unavailable);
        }
    }

    private sealed record InFlightProbe(string Path, Task<LocalPathProbeResult> Task);
}
