using System.Diagnostics;
using System.ComponentModel;
using System.Text;

namespace AIUsageMonitor.Infrastructure.Git;

internal sealed record GitCommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut = false,
    bool Cancelled = false,
    bool CouldNotStart = false,
    bool OutputTruncated = false);

internal enum GitCommandExecutionProfile
{
    ReadOnly,
    WorktreeMutation
}

/// <summary>
/// Infrastructure-internal process seam. It accepts an argument list so a registered local path
/// can never be reinterpreted as shell syntax.
/// </summary>
internal interface IGitCommandRunner
{
    Task<GitCommandResult> RunAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default);

    Task<GitCommandResult> RunAsync(
        IReadOnlyList<string> arguments,
        GitCommandExecutionProfile profile,
        CancellationToken cancellationToken = default) =>
        RunAsync(arguments, cancellationToken);
}

internal sealed class SystemGitCommandRunner : IGitCommandRunner
{
    internal const int MaxCapturedOutputCharacters = 64 * 1024;
    internal static readonly TimeSpan DefaultReadOnlyCommandTimeout = TimeSpan.FromSeconds(10);
    internal static readonly TimeSpan DefaultWorktreeMutationCommandTimeout = TimeSpan.FromSeconds(120);
    internal static readonly TimeSpan DefaultDrainTimeout = TimeSpan.FromSeconds(2);

    private readonly TimeSpan _readOnlyCommandTimeout;
    private readonly TimeSpan _worktreeMutationCommandTimeout;
    private readonly TimeSpan _drainTimeout;
    private readonly string _fileName;

    public SystemGitCommandRunner()
        : this(DefaultReadOnlyCommandTimeout, DefaultWorktreeMutationCommandTimeout, DefaultDrainTimeout)
    {
    }

    // Kept as a bounded compatibility seam for existing read-only runner tests. The new
    // profile-aware constructor below is the production/test authority for both timeouts.
    internal SystemGitCommandRunner(TimeSpan commandTimeout, TimeSpan drainTimeout, string fileName = "git")
    {
        ValidateTimeout(commandTimeout, nameof(commandTimeout));
        ValidateTimeout(drainTimeout, nameof(drainTimeout));
        _readOnlyCommandTimeout = commandTimeout;
        _worktreeMutationCommandTimeout = DefaultWorktreeMutationCommandTimeout;
        _drainTimeout = drainTimeout;
        _fileName = fileName;
    }

    internal SystemGitCommandRunner(
        TimeSpan readOnlyCommandTimeout,
        TimeSpan worktreeMutationCommandTimeout,
        TimeSpan drainTimeout,
        string fileName = "git")
    {
        ValidateTimeout(readOnlyCommandTimeout, nameof(readOnlyCommandTimeout));
        ValidateTimeout(worktreeMutationCommandTimeout, nameof(worktreeMutationCommandTimeout));
        ValidateTimeout(drainTimeout, nameof(drainTimeout));
        _readOnlyCommandTimeout = readOnlyCommandTimeout;
        _worktreeMutationCommandTimeout = worktreeMutationCommandTimeout;
        _drainTimeout = drainTimeout;
        _fileName = fileName;
    }

    public async Task<GitCommandResult> RunAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
        => await RunAsync(arguments, GitCommandExecutionProfile.ReadOnly, cancellationToken).ConfigureAwait(false);

    public async Task<GitCommandResult> RunAsync(
        IReadOnlyList<string> arguments,
        GitCommandExecutionProfile profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        var commandTimeout = profile switch
        {
            GitCommandExecutionProfile.ReadOnly => _readOnlyCommandTimeout,
            GitCommandExecutionProfile.WorktreeMutation => _worktreeMutationCommandTimeout,
            _ => throw new ArgumentOutOfRangeException(nameof(profile))
        };

        var startInfo = CreateStartInfo(arguments);
        startInfo.FileName = _fileName;

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        try
        {
            if (!process.Start())
            {
                return new GitCommandResult(-1, string.Empty, string.Empty, CouldNotStart: true);
            }
        }
        catch (Win32Exception)
        {
            return new GitCommandResult(-1, string.Empty, string.Empty, CouldNotStart: true);
        }
        catch (InvalidOperationException)
        {
            return new GitCommandResult(-1, string.Empty, string.Empty, CouldNotStart: true);
        }

        // Stop capturing at a bounded size. Git output is diagnostics/evidence only and must not
        // become an unbounded memory or persistence channel.
        var standardOutputTask = ReadBoundedAsync(process.StandardOutput);
        var standardErrorTask = ReadBoundedAsync(process.StandardError);
        using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(commandTimeout);

        try
        {
            await process.WaitForExitAsync(timeoutCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            var wasExternallyCancelled = cancellationToken.IsCancellationRequested;
            return await BoundedProcessWait.HandleTimeoutOrCancellationAsync(
                () => KillProcessTree(process),
                standardOutputTask,
                standardErrorTask,
                _drainTimeout,
                wasExternallyCancelled).ConfigureAwait(false);
        }

        await BoundedProcessWait.AwaitOutputWithBoundAsync(
            standardOutputTask, standardErrorTask, _drainTimeout).ConfigureAwait(false);

        return new GitCommandResult(
            process.ExitCode,
            standardOutputTask.IsCompletedSuccessfully ? TrimCapturedOutput(standardOutputTask.Result) : string.Empty,
            standardErrorTask.IsCompletedSuccessfully ? TrimCapturedOutput(standardErrorTask.Result) : string.Empty,
            OutputTruncated: (standardOutputTask.IsCompletedSuccessfully && standardOutputTask.Result.Length > MaxCapturedOutputCharacters) ||
                             (standardErrorTask.IsCompletedSuccessfully && standardErrorTask.Result.Length > MaxCapturedOutputCharacters));
    }

    private static async Task<string> ReadBoundedAsync(StreamReader reader)
    {
        // Capture one sentinel character beyond the public bound so output exactly at the bound
        // is not falsely classified as truncated. Continue draining without retaining anything
        // beyond the sentinel to keep the child process from blocking on a full pipe.
        var builder = new StringBuilder(MaxCapturedOutputCharacters + 1);
        var buffer = new char[4096];
        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory()).ConfigureAwait(false);
            if (read == 0) break;
            if (builder.Length < MaxCapturedOutputCharacters + 1)
            {
                builder.Append(buffer, 0, Math.Min(read, MaxCapturedOutputCharacters + 1 - builder.Length));
            }
        }

        return builder.ToString();
    }

    private static string TrimCapturedOutput(string value) =>
        value.Length <= MaxCapturedOutputCharacters ? value : value[..MaxCapturedOutputCharacters];

    internal static ProcessStartInfo CreateStartInfo(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Environment.CurrentDirectory
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        GitProcessEnvironment.Sanitize(startInfo.Environment);
        return startInfo;
    }

    private static void ValidateTimeout(TimeSpan value, string parameterName)
    {
        if (value <= TimeSpan.Zero || value == Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Timeouts must be positive and bounded.");
        }
    }

    private static void KillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The process exited between the check and Kill.
        }
        catch (Win32Exception)
        {
            // A platform-specific inability to kill is still bounded by the drain wait below.
        }
    }
}

internal static class GitProcessEnvironment
{
    private static readonly string[] RepositoryRedirectionVariables =
    [
        "GIT_DIR",
        "GIT_WORK_TREE",
        "GIT_INDEX_FILE",
        "GIT_OBJECT_DIRECTORY",
        "GIT_ALTERNATE_OBJECT_DIRECTORIES",
        "GIT_CEILING_DIRECTORIES",
        "GIT_COMMON_DIR",
        "GIT_NAMESPACE"
    ];

    internal static void Sanitize(IDictionary<string, string?> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        foreach (var key in environment.Keys
                     .Where(key => RepositoryRedirectionVariables.Any(variable =>
                         string.Equals(variable, key, StringComparison.OrdinalIgnoreCase)))
                     .ToArray())
        {
            environment.Remove(key);
        }

        // Deterministic, English-language Git error text keeps Infrastructure classification
        // independent of the user's locale. PATH and all unrelated parent-process values remain.
        environment["GIT_TERMINAL_PROMPT"] = "0";
        environment["GIT_OPTIONAL_LOCKS"] = "0";
        environment["LC_ALL"] = "C";
        environment["LANG"] = "C";
    }
}

/// <summary>
/// Isolated, testable bounded-wait logic used once a Git command is cancelled or has timed out.
/// Termination is always best-effort: the caller must regain control in bounded time even when
/// the kill attempt throws or the process ignores it, and even when stdout/stderr never reach EOF.
/// </summary>
internal static class BoundedProcessWait
{
    internal static async Task<GitCommandResult> HandleTimeoutOrCancellationAsync(
        Action killAction,
        Task<string> standardOutputTask,
        Task<string> standardErrorTask,
        TimeSpan drainTimeout,
        bool wasExternallyCancelled)
    {
        try
        {
            killAction();
        }
        catch
        {
            // Best-effort termination only; the caller must regain control regardless of outcome.
        }

        await AwaitOutputWithBoundAsync(standardOutputTask, standardErrorTask, drainTimeout).ConfigureAwait(false);

        return wasExternallyCancelled
            ? new GitCommandResult(-1, string.Empty, string.Empty, Cancelled: true)
            : new GitCommandResult(-1, string.Empty, string.Empty, TimedOut: true);
    }

    internal static async Task AwaitOutputWithBoundAsync(
        Task<string> standardOutputTask,
        Task<string> standardErrorTask,
        TimeSpan bound)
    {
        var combined = SafeWhenAllAsync(standardOutputTask, standardErrorTask);
        var delay = Task.Delay(bound);
        await Task.WhenAny(combined, delay).ConfigureAwait(false);
        // If the drain bound elapsed first, stop waiting for output. Raw process diagnostics are
        // never promoted to a caller-facing result either way.
    }

    private static async Task SafeWhenAllAsync(Task<string> standardOutputTask, Task<string> standardErrorTask)
    {
        try
        {
            await Task.WhenAll(standardOutputTask, standardErrorTask).ConfigureAwait(false);
        }
        catch
        {
            // The caller receives a bounded operation result; process diagnostics are never
            // promoted to user-facing text.
        }
    }
}
