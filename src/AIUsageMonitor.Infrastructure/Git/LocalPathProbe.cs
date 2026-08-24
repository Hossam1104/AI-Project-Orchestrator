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

        var probeTask = Task.Run(() => _probeCore(path), CancellationToken.None);
        var delayTask = Task.Delay(_probeTimeout, cancellationToken);

        var completed = await Task.WhenAny(probeTask, delayTask).ConfigureAwait(false);
        if (completed == probeTask)
        {
            return await probeTask.ConfigureAwait(false);
        }

        // The delay task won the race: either an external cancellation fired, or the probe
        // genuinely exceeded its bound. Either way the caller must regain control now; the
        // abandoned probe task (if any) is left to complete on its own thread-pool thread.
        cancellationToken.ThrowIfCancellationRequested();
        return new LocalPathProbeResult(LocalPathProbeStatus.Unavailable, TimedOut: true);
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
}
