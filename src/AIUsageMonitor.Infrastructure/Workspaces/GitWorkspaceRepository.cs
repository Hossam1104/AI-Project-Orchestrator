using AIUsageMonitor.Application.Workspaces;
using AIUsageMonitor.Infrastructure.Git;

namespace AIUsageMonitor.Infrastructure.Workspaces;

/// <summary>
/// Fixed-command Git adapter for APO-46. Discovery uses only bounded read-only commands. The
/// worktree add method is the only method in this adapter that can mutate Git state.
/// </summary>
public sealed class GitWorkspaceRepository : IWorkspaceRepository, IWorkspaceBranchSafety, IWorkspacePreparedWorkspaceVerifier
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
        if (!detached && (string.IsNullOrWhiteSpace(branch) || branchResult.OutputTruncated)) return Unavailable(capturedPath);

        var statusResult = await RunAsync(["-C", capturedPath, "status", "--porcelain=v1", "-z", "--untracked-files=all"], cancellationToken).ConfigureAwait(false);
        if (!Succeeded(statusResult)) return Unavailable(capturedPath);
        var changedFileCount = CountStatusEntries(statusResult.StandardOutput);
        var workingTreeFingerprint = WorkspacePreparationIntegrity.ComputeWorkingTreeStateFingerprint(statusResult.StandardOutput);
        if (changedFileCount > WorkspacePreparationLimits.MaxChangedFiles)
            return new(WorkspaceRepositoryDiscoveryStatus.EvidenceOverflow, capturedPath, root, common, isBare, head, branch, detached, false, changedFileCount, errorMessage: "Git changed-file evidence exceeded the supported bound.", workingTreeStateFingerprint: workingTreeFingerprint);

        var worktreeResult = await RunAsync(["-C", capturedPath, "worktree", "list", "--porcelain"], cancellationToken).ConfigureAwait(false);
        if (!Succeeded(worktreeResult)) return Unavailable(capturedPath);
        var worktrees = ParseWorktreeList(worktreeResult.StandardOutput, out var worktreeOverflow);
        if (worktreeResult.OutputTruncated || worktreeOverflow)
            return new(WorkspaceRepositoryDiscoveryStatus.EvidenceOverflow, capturedPath, root, common, isBare, head, branch, detached, changedFileCount == 0, changedFileCount, worktrees, errorMessage: "Git worktree evidence exceeded the supported bound.", workingTreeStateFingerprint: workingTreeFingerprint);

        var branchListResult = await RunAsync(["-C", capturedPath, "for-each-ref", "--format=%(refname:short)", "refs/heads"], cancellationToken).ConfigureAwait(false);
        if (!Succeeded(branchListResult)) return Unavailable(capturedPath);
        var localBranches = branchListResult.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (branchListResult.OutputTruncated || localBranches.Length > WorkspacePreparationLimits.MaxWorktrees)
            return new(WorkspaceRepositoryDiscoveryStatus.EvidenceOverflow, capturedPath, root, common, isBare, head, branch, detached, changedFileCount == 0, changedFileCount, worktrees, localBranches, "Git local-branch evidence exceeded the supported bound.", workingTreeFingerprint);

        var divergence = await DiscoverDivergenceAsync(capturedPath, cancellationToken).ConfigureAwait(false);
        if (divergence is null) return Unavailable(capturedPath);

        return new(WorkspaceRepositoryDiscoveryStatus.Available, capturedPath, root, common, isBare, head, branch, detached, changedFileCount == 0, changedFileCount, worktrees, localBranches, workingTreeStateFingerprint: workingTreeFingerprint, divergence: divergence);
    }

    public async Task<WorkspaceBranchValidationResult> ValidateBranchNameAsync(string commonDirectory, string branchName, CancellationToken cancellationToken = default)
    {
        if (!Path.IsPathFullyQualified(commonDirectory) || string.IsNullOrWhiteSpace(branchName))
            return new(WorkspaceBranchValidationStatus.Invalid, "Branch validation arguments are invalid.");
        var result = await RunAsync(["--git-dir", commonDirectory, "check-ref-format", "--branch", branchName], cancellationToken).ConfigureAwait(false);
        if (result.Cancelled) throw new OperationCanceledException(cancellationToken);
        if (result.TimedOut || result.CouldNotStart || result.OutputTruncated) return new(WorkspaceBranchValidationStatus.Unavailable, "Git branch validation was unavailable.");
        return result.ExitCode == 0 ? new(WorkspaceBranchValidationStatus.Valid) : new(WorkspaceBranchValidationStatus.Invalid, "Git rejected the branch name.");
    }

    public async Task<WorkspaceBranchExistenceResult> QueryLocalBranchAsync(string commonDirectory, string branchName, CancellationToken cancellationToken = default)
    {
        if (!Path.IsPathFullyQualified(commonDirectory) || string.IsNullOrWhiteSpace(branchName))
            return new(WorkspaceBranchExistenceStatus.Unavailable, "Branch query arguments are invalid.");
        var result = await RunAsync(["--git-dir", commonDirectory, "show-ref", "--verify", "--quiet", $"refs/heads/{branchName}"], cancellationToken).ConfigureAwait(false);
        if (result.Cancelled) throw new OperationCanceledException(cancellationToken);
        if (result.TimedOut || result.CouldNotStart || result.OutputTruncated) return new(WorkspaceBranchExistenceStatus.Unavailable, "Git branch query was unavailable.");
        return result.ExitCode == 0 ? new(WorkspaceBranchExistenceStatus.Exists) : result.ExitCode == 1 ? new(WorkspaceBranchExistenceStatus.NotFound) : new(WorkspaceBranchExistenceStatus.Unavailable, "Git branch query failed.");
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
        var result = await RunAsync(["--git-dir", commonDirectory, "worktree", "add", "-b", workspaceBranch, managedWorkspacePath, exactBaseCommitSha], GitCommandExecutionProfile.WorktreeMutation, cancellationToken).ConfigureAwait(false);
        if (result.Cancelled) throw new OperationCanceledException(cancellationToken);
        return result.ExitCode == 0 && !result.TimedOut && !result.CouldNotStart && !result.OutputTruncated
            ? new(true)
            : new(false, CommandFailed: true, ErrorMessage: result.TimedOut ? "Git worktree creation timed out within the bounded mutation timeout." : "Git worktree creation failed.", TimedOut: result.TimedOut);
    }

    public async Task<WorkspacePreparedWorkspaceVerification> VerifyPreparedWorkspaceAsync(
        string workspacePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            return new(WorkspacePreparedWorkspaceVerificationStatus.Unavailable, workspacePath ?? string.Empty,
                ErrorMessage: "Workspace path is required.");
        }

        string capturedPath;
        try
        {
            capturedPath = WorkspaceRepositoryIdentity.Normalize(workspacePath);
        }
        catch (ArgumentException)
        {
            return new(WorkspacePreparedWorkspaceVerificationStatus.Unavailable, workspacePath,
                ErrorMessage: "Workspace path could not be normalized safely.");
        }

        if (!Directory.Exists(capturedPath))
        {
            return new(WorkspacePreparedWorkspaceVerificationStatus.WorkspaceMissing, capturedPath,
                ErrorMessage: "Managed workspace directory is missing.");
        }

        var inside = await RunAsync(["-C", capturedPath, "rev-parse", "--is-inside-work-tree"], cancellationToken).ConfigureAwait(false);
        if (!Succeeded(inside))
        {
            return new(WorkspacePreparedWorkspaceVerificationStatus.NotGitWorktree, capturedPath,
                ErrorMessage: "Git did not recognize the managed workspace as a working tree.");
        }

        if (!string.Equals(inside.StandardOutput.Trim(), "true", StringComparison.OrdinalIgnoreCase))
        {
            return new(WorkspacePreparedWorkspaceVerificationStatus.NotGitWorktree, capturedPath,
                ErrorMessage: "Managed workspace is not a Git working tree.");
        }

        var rootResult = await RunAsync(["-C", capturedPath, "rev-parse", "--show-toplevel"], cancellationToken).ConfigureAwait(false);
        if (!Succeeded(rootResult)) return VerificationUnavailable(capturedPath, "Workspace repository root could not be verified.");
        var root = NormalizeGitPath(rootResult.StandardOutput.Trim(), capturedPath);
        if (root is null) return VerificationUnavailable(capturedPath, "Workspace repository root evidence was unavailable.");

        var commonResult = await RunAsync(["-C", capturedPath, "rev-parse", "--git-common-dir"], cancellationToken).ConfigureAwait(false);
        if (!Succeeded(commonResult)) return VerificationUnavailable(capturedPath, "Workspace Git common directory could not be verified.");
        var common = NormalizeGitPath(commonResult.StandardOutput.Trim(), capturedPath);
        if (common is null) return VerificationUnavailable(capturedPath, "Workspace Git common directory evidence was unavailable.");

        var headResult = await RunAsync(["-C", capturedPath, "rev-parse", "--verify", "HEAD"], cancellationToken).ConfigureAwait(false);
        if (!Succeeded(headResult)) return VerificationUnavailable(capturedPath, "Workspace HEAD could not be verified.");
        var head = headResult.StandardOutput.Trim();
        if (!WorkspacePreparationIntegrity.IsGitObjectId(head)) return VerificationUnavailable(capturedPath, "Workspace HEAD evidence was invalid.");

        var branchResult = await RunAsync(["-C", capturedPath, "symbolic-ref", "--quiet", "--short", "HEAD"], cancellationToken).ConfigureAwait(false);
        if (branchResult.Cancelled) throw new OperationCanceledException(cancellationToken);
        var detached = branchResult.ExitCode == 1;
        if (!detached && !Succeeded(branchResult)) return VerificationUnavailable(capturedPath, "Workspace branch evidence could not be verified.");
        var branch = detached ? null : branchResult.StandardOutput.Trim();
        if (!detached && (string.IsNullOrWhiteSpace(branch) || branchResult.OutputTruncated))
            return VerificationUnavailable(capturedPath, "Workspace branch evidence was incomplete.");

        var statusResult = await RunAsync(["-C", capturedPath, "status", "--porcelain=v1", "-z", "--untracked-files=all"], cancellationToken).ConfigureAwait(false);
        if (!Succeeded(statusResult)) return VerificationUnavailable(capturedPath, "Workspace checkout status could not be verified.");
        var changedFileCount = CountStatusEntries(statusResult.StandardOutput);
        var fingerprint = WorkspacePreparationIntegrity.ComputeWorkingTreeStateFingerprint(statusResult.StandardOutput);
        if (changedFileCount > WorkspacePreparationLimits.MaxChangedFiles)
            return new(WorkspacePreparedWorkspaceVerificationStatus.EvidenceOverflow, capturedPath,
                IsGitWorktree: true, RepositoryRoot: root, CommonDirectory: common, HeadCommitSha: head,
                BranchName: branch, IsDetached: detached, ChangedFileCount: changedFileCount,
                WorkingTreeStateFingerprint: fingerprint,
                ErrorMessage: "Workspace changed-file evidence exceeded the supported bound.");

        var worktreeResult = await RunAsync(["-C", capturedPath, "worktree", "list", "--porcelain"], cancellationToken).ConfigureAwait(false);
        if (!Succeeded(worktreeResult)) return VerificationUnavailable(capturedPath, "Workspace worktree registration could not be verified.");
        var worktrees = ParseWorktreeList(worktreeResult.StandardOutput, out var overflow);
        if (worktreeResult.OutputTruncated || overflow)
            return new(WorkspacePreparedWorkspaceVerificationStatus.EvidenceOverflow, capturedPath,
                IsGitWorktree: true, RepositoryRoot: root, CommonDirectory: common, HeadCommitSha: head,
                BranchName: branch, IsDetached: detached, IsClean: changedFileCount == 0,
                ChangedFileCount: changedFileCount, WorkingTreeStateFingerprint: fingerprint,
                ErrorMessage: "Workspace worktree evidence exceeded the supported bound.");

        if (!worktrees.Any(value => WorkspaceRepositoryIdentity.AreEqual(value.Path, capturedPath)))
        {
            return new(WorkspacePreparedWorkspaceVerificationStatus.NotGitWorktree, capturedPath,
                IsGitWorktree: false, RepositoryRoot: root, CommonDirectory: common, HeadCommitSha: head,
                BranchName: branch, IsDetached: detached, IsClean: changedFileCount == 0,
                ChangedFileCount: changedFileCount, WorkingTreeStateFingerprint: fingerprint,
                ErrorMessage: "Git did not register the managed workspace as a linked worktree.");
        }

        return new(WorkspacePreparedWorkspaceVerificationStatus.Verified, capturedPath,
            IsGitWorktree: true, RepositoryRoot: root, CommonDirectory: common, HeadCommitSha: head,
            BranchName: branch, IsDetached: detached, IsClean: changedFileCount == 0,
            ChangedFileCount: changedFileCount, WorkingTreeStateFingerprint: fingerprint);
    }

    internal static IReadOnlyList<WorkspaceWorktreeEvidence> ParseWorktreeList(string output) =>
        ParseWorktreeList(output, out _);

    internal static IReadOnlyList<WorkspaceWorktreeEvidence> ParseWorktreeList(string output, out bool overflow)
    {
        var result = new List<WorkspaceWorktreeEvidence>();
        var localOverflow = false;
        var path = (string?)null; var head = (string?)null; string? branch = null; var detached = false; var locked = false; var prunable = false;
        void Flush()
        {
            if (path is not null && head is not null && WorkspacePreparationIntegrity.IsGitObjectId(head))
            {
                if (result.Count < WorkspacePreparationLimits.MaxWorktrees)
                    result.Add(new WorkspaceWorktreeEvidence(path, head, branch, detached, locked, prunable));
                else
                    localOverflow = true;
            }
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
        overflow = localOverflow;
        return result.ToArray();
    }

    private async Task<WorkspaceRepositoryDivergence?> DiscoverDivergenceAsync(string capturedPath, CancellationToken cancellationToken)
    {
        var upstream = await RunAsync(["-C", capturedPath, "rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{upstream}"], cancellationToken).ConfigureAwait(false);
        if (upstream.Cancelled) throw new OperationCanceledException(cancellationToken);
        if (upstream.OutputTruncated || upstream.TimedOut || upstream.CouldNotStart) return null;
        if (upstream.ExitCode == 1 ||
            (upstream.ExitCode != 0 && (upstream.StandardError.Contains("no upstream", StringComparison.OrdinalIgnoreCase) ||
                                        upstream.StandardError.Contains("does not point to a branch", StringComparison.OrdinalIgnoreCase))))
            return new WorkspaceRepositoryDivergence(WorkspaceDivergenceState.NotConfigured);
        if (upstream.ExitCode != 0) return new WorkspaceRepositoryDivergence(WorkspaceDivergenceState.Unavailable);

        var reference = upstream.StandardOutput.Trim();
        if (string.IsNullOrWhiteSpace(reference) || reference.Length > WorkspacePreparationLimits.MaxDivergenceReferenceLength)
            return new WorkspaceRepositoryDivergence(WorkspaceDivergenceState.Unavailable);

        var counts = await RunAsync(["-C", capturedPath, "rev-list", "--left-right", "--count", $"HEAD...{reference}"], cancellationToken).ConfigureAwait(false);
        if (counts.Cancelled) throw new OperationCanceledException(cancellationToken);
        if (!Succeeded(counts)) return new WorkspaceRepositoryDivergence(WorkspaceDivergenceState.Unavailable, reference);
        var tokens = counts.StandardOutput.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return tokens.Length == 2 && int.TryParse(tokens[0], out var ahead) && int.TryParse(tokens[1], out var behind) && ahead >= 0 && behind >= 0
            ? new WorkspaceRepositoryDivergence(WorkspaceDivergenceState.PointInTime, reference, ahead, behind)
            : new WorkspaceRepositoryDivergence(WorkspaceDivergenceState.Unavailable, reference);
    }

    private async Task<AIUsageMonitor.Infrastructure.Git.GitCommandResult> RunAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken) =>
        await RunAsync(arguments, GitCommandExecutionProfile.ReadOnly, cancellationToken).ConfigureAwait(false);

    private async Task<AIUsageMonitor.Infrastructure.Git.GitCommandResult> RunAsync(
        IReadOnlyList<string> arguments,
        GitCommandExecutionProfile profile,
        CancellationToken cancellationToken)
    {
        var result = await _runner.RunAsync(arguments, profile, cancellationToken).ConfigureAwait(false);
        if (result.Cancelled || cancellationToken.IsCancellationRequested) throw new OperationCanceledException(cancellationToken);
        return result;
    }

    private static WorkspacePreparedWorkspaceVerification VerificationUnavailable(string path, string message) =>
        new(WorkspacePreparedWorkspaceVerificationStatus.Unavailable, path, ErrorMessage: message);

    private static string? NormalizeGitPath(string value, string workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try
        {
            return WorkspaceRepositoryIdentity.Normalize(Path.IsPathRooted(value)
                ? value
                : Path.Combine(workingDirectory, value));
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static WorkspaceRepositoryDiscovery Unavailable(string path) => new(WorkspaceRepositoryDiscoveryStatus.Unavailable, path, errorMessage: "Git repository evidence could not be read.");
    private static bool Succeeded(AIUsageMonitor.Infrastructure.Git.GitCommandResult result) => result.ExitCode == 0 && !result.TimedOut && !result.CouldNotStart && !result.OutputTruncated;
    private static bool IsNotRepository(AIUsageMonitor.Infrastructure.Git.GitCommandResult result) => result.StandardError.Contains("not a git repository", StringComparison.OrdinalIgnoreCase);
    private static int CountStatusEntries(string output) => string.IsNullOrEmpty(output) ? 0 : output.Split('\0', StringSplitOptions.RemoveEmptyEntries).Count(value => value.Length >= 2 && value[0] != '#');
}
