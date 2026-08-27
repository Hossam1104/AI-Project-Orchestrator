using AIUsageMonitor.Application.Workspaces;

namespace AIUsageMonitor.Infrastructure.Workspaces;

/// <summary>
/// Fixed-command Git adapter for APO-46. Discovery uses only bounded read-only commands. The
/// worktree add method is the only method in this adapter that can mutate Git state.
/// </summary>
public sealed class GitWorkspaceRepository : IWorkspaceRepository
{
    private readonly AIUsageMonitor.Infrastructure.Git.IGitCommandRunner _runner;

    public GitWorkspaceRepository()
        : this(new AIUsageMonitor.Infrastructure.Git.SystemGitCommandRunner())
    {
    }

    internal GitWorkspaceRepository(AIUsageMonitor.Infrastructure.Git.IGitCommandRunner runner) =>
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));

    public async Task<WorkspaceRepositoryDiscovery> DiscoverAsync(string registeredPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(registeredPath)) throw new ArgumentException("Registered repository path is required.", nameof(registeredPath));
        var capturedPath = Path.GetFullPath(registeredPath);
        if (!Directory.Exists(capturedPath)) return new(WorkspaceRepositoryDiscoveryStatus.RepositoryMissing, capturedPath, errorMessage: "Repository path is missing.");

        var rootResult = await RunAsync(["-C", capturedPath, "rev-parse", "--show-toplevel"], cancellationToken).ConfigureAwait(false);
        if (rootResult.ExitCode != 0)
            return new(IsNotRepository(rootResult) ? WorkspaceRepositoryDiscoveryStatus.NotGitRepository : WorkspaceRepositoryDiscoveryStatus.Unavailable, capturedPath, errorMessage: "Git repository root could not be discovered.");
        var root = rootResult.StandardOutput.Trim();
        if (string.IsNullOrWhiteSpace(root) || rootResult.OutputTruncated) return new(WorkspaceRepositoryDiscoveryStatus.Unavailable, capturedPath, errorMessage: "Git repository root evidence was unavailable.");
        root = Path.GetFullPath(root);

        var commonResult = await RunAsync(["-C", capturedPath, "rev-parse", "--git-common-dir"], cancellationToken).ConfigureAwait(false);
        if (!Succeeded(commonResult)) return Unavailable(capturedPath);
        var common = commonResult.StandardOutput.Trim();
        if (string.IsNullOrWhiteSpace(common) || commonResult.OutputTruncated) return Unavailable(capturedPath);
        common = Path.GetFullPath(Path.IsPathRooted(common) ? common : Path.Combine(capturedPath, common));

        var bareResult = await RunAsync(["-C", capturedPath, "rev-parse", "--is-bare-repository"], cancellationToken).ConfigureAwait(false);
        if (!Succeeded(bareResult)) return Unavailable(capturedPath);
        var isBare = string.Equals(bareResult.StandardOutput.Trim(), "true", StringComparison.OrdinalIgnoreCase);

        var headResult = await RunAsync(["-C", capturedPath, "rev-parse", "--verify", "HEAD"], cancellationToken).ConfigureAwait(false);
        if (!Succeeded(headResult)) return Unavailable(capturedPath);
        var head = headResult.StandardOutput.Trim();
        if (!WorkspacePreparationIntegrity.IsGitObjectId(head)) return Unavailable(capturedPath);

        var branchResult = await RunAsync(["-C", capturedPath, "symbolic-ref", "--quiet", "--short", "HEAD"], cancellationToken).ConfigureAwait(false);
        if (branchResult.Cancelled) throw new OperationCanceledException(cancellationToken);
        var detached = branchResult.ExitCode != 0;
        var branch = detached ? null : branchResult.StandardOutput.Trim();
        if (!detached && string.IsNullOrWhiteSpace(branch)) return Unavailable(capturedPath);

        var statusResult = await RunAsync(["-C", capturedPath, "status", "--porcelain=v1", "-z", "--untracked-files=all"], cancellationToken).ConfigureAwait(false);
        if (!Succeeded(statusResult)) return Unavailable(capturedPath);
        var changedFileCount = CountStatusEntries(statusResult.StandardOutput);

        var worktreeResult = await RunAsync(["-C", capturedPath, "worktree", "list", "--porcelain"], cancellationToken).ConfigureAwait(false);
        if (!Succeeded(worktreeResult)) return Unavailable(capturedPath);
        var worktrees = ParseWorktreeList(worktreeResult.StandardOutput);
        var branchListResult = await RunAsync(["-C", capturedPath, "for-each-ref", "--format=%(refname:short)", "refs/heads"], cancellationToken).ConfigureAwait(false);
        if (!Succeeded(branchListResult)) return Unavailable(capturedPath);
        var localBranches = branchListResult.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Take(WorkspacePreparationLimits.MaxWorktrees).ToArray();
        return new(WorkspaceRepositoryDiscoveryStatus.Available, capturedPath, root, common, isBare, head, branch, detached, changedFileCount == 0, changedFileCount, worktrees, localBranches);
    }

    public async Task<WorkspaceRepositoryMutationResult> AddExactWorktreeAsync(string commonDirectory, string workspaceBranch, string managedWorkspacePath, string exactBaseCommitSha, CancellationToken cancellationToken = default)
    {
        if (!Path.IsPathFullyQualified(commonDirectory) || !Path.IsPathFullyQualified(managedWorkspacePath) || !WorkspacePreparationIntegrity.IsGitObjectId(exactBaseCommitSha))
            return new(false, ErrorMessage: "Mutation arguments failed the exact repository/path/SHA safety checks.");
        if (string.IsNullOrWhiteSpace(workspaceBranch) || workspaceBranch.Length > WorkspacePreparationLimits.MaxBranchLength)
            return new(false, ErrorMessage: "Mutation branch failed the bounded validation.");
        var branchCheck = await RunAsync(["--git-dir", commonDirectory, "check-ref-format", "--branch", workspaceBranch], cancellationToken).ConfigureAwait(false);
        if (branchCheck.Cancelled) throw new OperationCanceledException(cancellationToken);
        if (!Succeeded(branchCheck)) return new(false, ErrorMessage: "Workspace branch is not a valid Git branch name.");

        // This is intentionally the sole mutating command emitted by APO-46.
        var result = await RunAsync(["--git-dir", commonDirectory, "worktree", "add", "-b", workspaceBranch, managedWorkspacePath, exactBaseCommitSha], cancellationToken).ConfigureAwait(false);
        if (result.Cancelled) throw new OperationCanceledException(cancellationToken);
        return result.ExitCode == 0 && !result.TimedOut && !result.CouldNotStart && !result.OutputTruncated
            ? new(true)
            : new(false, CommandFailed: true, ErrorMessage: "Git worktree creation failed.");
    }

    internal static IReadOnlyList<WorkspaceWorktreeEvidence> ParseWorktreeList(string output)
    {
        var result = new List<WorkspaceWorktreeEvidence>();
        var path = (string?)null; var head = (string?)null; string? branch = null; var detached = false; var locked = false; var prunable = false;
        void Flush()
        {
            if (path is not null && head is not null && WorkspacePreparationIntegrity.IsGitObjectId(head))
                result.Add(new WorkspaceWorktreeEvidence(path, head, branch, detached, locked, prunable));
            path = null; head = null; branch = null; detached = false; locked = false; prunable = false;
        }
        foreach (var line in (output ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            if (line.Length == 0) { Flush(); continue; }
            if (line.StartsWith("worktree ", StringComparison.Ordinal)) { if (path is not null) Flush(); path = line[9..]; }
            else if (line.StartsWith("HEAD ", StringComparison.Ordinal)) head = line[5..].Trim();
            else if (line.StartsWith("branch ", StringComparison.Ordinal) && line.Length > 7) branch = line[7..].StartsWith("refs/heads/", StringComparison.Ordinal) ? line[18..] : line[7..];
            else if (string.Equals(line, "detached", StringComparison.Ordinal)) detached = true;
            else if (line.StartsWith("locked", StringComparison.Ordinal)) locked = true;
            else if (line.StartsWith("prunable", StringComparison.Ordinal)) prunable = true;
        }
        Flush();
        return result.Take(WorkspacePreparationLimits.MaxWorktrees).ToArray();
    }

    private async Task<AIUsageMonitor.Infrastructure.Git.GitCommandResult> RunAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var result = await _runner.RunAsync(arguments, cancellationToken).ConfigureAwait(false);
        if (result.Cancelled || cancellationToken.IsCancellationRequested) throw new OperationCanceledException(cancellationToken);
        return result;
    }

    private static WorkspaceRepositoryDiscovery Unavailable(string path) => new(WorkspaceRepositoryDiscoveryStatus.Unavailable, path, errorMessage: "Git repository evidence could not be read.");
    private static bool Succeeded(AIUsageMonitor.Infrastructure.Git.GitCommandResult result) => result.ExitCode == 0 && !result.TimedOut && !result.CouldNotStart && !result.OutputTruncated;
    private static bool IsNotRepository(AIUsageMonitor.Infrastructure.Git.GitCommandResult result) => result.StandardError.Contains("not a git repository", StringComparison.OrdinalIgnoreCase);
    private static int CountStatusEntries(string output) => string.IsNullOrEmpty(output) ? 0 : output.Split('\0', StringSplitOptions.RemoveEmptyEntries).Count(value => value.Length >= 2 && value[0] != '#');
}
