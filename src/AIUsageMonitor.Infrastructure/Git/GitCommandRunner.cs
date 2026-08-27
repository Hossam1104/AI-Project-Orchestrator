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

/// <summary>
/// Infrastructure-internal process seam. It accepts an argument list so a registered local path
/// can never be reinterpreted as shell syntax.
/// </summary>
internal interface IGitCommandRunner
{
    Task<GitCommandResult> RunAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default);
}

internal sealed class SystemGitCommandRunner : IGitCommandRunner
{
    internal const int MaxCapturedOutputCharacters = 64 * 1024;
    private static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan DefaultDrainTimeout = TimeSpan.FromSeconds(2);

    private readonly TimeSpan _commandTimeout;
    private readonly TimeSpan _drainTimeout;
    private readonly string _fileName;

    public SystemGitCommandRunner()
        : this(DefaultCommandTimeout, DefaultDrainTimeout)
    {
    }

    internal SystemGitCommandRunner(TimeSpan commandTimeout, TimeSpan drainTimeout, string fileName = "git")
    {
        _commandTimeout = commandTimeout;
        _drainTimeout = drainTimeout;
        _fileName = fileName;
    }

    public async Task<GitCommandResult> RunAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);

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
        timeoutCancellation.CancelAfter(_commandTimeout);

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
            standardOutputTask.IsCompletedSuccessfully ? standardOutputTask.Result : string.Empty,
            standardErrorTask.IsCompletedSuccessfully ? standardErrorTask.Result : string.Empty,
            OutputTruncated: (standardOutputTask.IsCompletedSuccessfully && standardOutputTask.Result.Length >= MaxCapturedOutputCharacters) ||
                             (standardErrorTask.IsCompletedSuccessfully && standardErrorTask.Result.Length >= MaxCapturedOutputCharacters));
    }

    private static async Task<string> ReadBoundedAsync(StreamReader reader)
    {
        var builder = new StringBuilder(MaxCapturedOutputCharacters);
        var buffer = new char[4096];
        while (builder.Length < MaxCapturedOutputCharacters)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, MaxCapturedOutputCharacters - builder.Length))).ConfigureAwait(false);
            if (read == 0) break;
            builder.Append(buffer, 0, read);
        }

        return builder.ToString();
    }

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

        startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
        startInfo.Environment["GIT_OPTIONAL_LOCKS"] = "0";
        // Deterministic, English-language Git error text so Infrastructure can safely classify
        // bounded failures (e.g. "not a git repository") without depending on the user's locale.
        startInfo.Environment["LC_ALL"] = "C";
        startInfo.Environment["LANG"] = "C";
        return startInfo;
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
