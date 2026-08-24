using System.Diagnostics;
using System.ComponentModel;

namespace AIUsageMonitor.Infrastructure.Git;

internal sealed record GitCommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut = false,
    bool Cancelled = false,
    bool CouldNotStart = false);

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
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(10);

    public async Task<GitCommandResult> RunAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var startInfo = CreateStartInfo(arguments);

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

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(CommandTimeout);

        try
        {
            await process.WaitForExitAsync(timeoutCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            KillProcessTree(process);
            await AwaitOutputAsync(standardOutputTask, standardErrorTask).ConfigureAwait(false);

            return cancellationToken.IsCancellationRequested
                ? new GitCommandResult(-1, string.Empty, string.Empty, Cancelled: true)
                : new GitCommandResult(-1, string.Empty, string.Empty, TimedOut: true);
        }

        await AwaitOutputAsync(standardOutputTask, standardErrorTask).ConfigureAwait(false);
        return new GitCommandResult(
            process.ExitCode,
            standardOutputTask.Result,
            standardErrorTask.Result);
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
        return startInfo;
    }

    private static async Task AwaitOutputAsync(
        Task<string> standardOutputTask,
        Task<string> standardErrorTask)
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
        catch (System.ComponentModel.Win32Exception)
        {
            // A platform-specific inability to kill is still bounded by the process wait above.
        }
    }
}
