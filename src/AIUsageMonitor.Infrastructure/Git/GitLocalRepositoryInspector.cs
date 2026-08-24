using System.Globalization;
using System.Text;
using AIUsageMonitor.Application.Projects;

namespace AIUsageMonitor.Infrastructure.Git;

/// <summary>
/// Read-only local Git inspector. The command set is intentionally small and does not include
/// fetch, pull, push, checkout, config writes, or any other repository mutation.
/// </summary>
public sealed class GitLocalRepositoryInspector : ILocalRepositoryInspector
{
    internal const int MaxChangedFiles = 100;
    internal const int MaxFieldLength = 512;

    private readonly IGitCommandRunner _runner;

    public GitLocalRepositoryInspector()
        : this(new SystemGitCommandRunner())
    {
    }

    internal GitLocalRepositoryInspector(IGitCommandRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    public async Task<LocalRepositoryInspection> InspectAsync(
        string registeredLocalPath,
        string? registeredRepositoryUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(registeredLocalPath))
        {
            throw new ArgumentException("Registered local path is required.", nameof(registeredLocalPath));
        }

        var capturedAt = DateTimeOffset.UtcNow;
        var pathState = CheckPath(registeredLocalPath);
        if (pathState is not null)
        {
            return CreateResult(pathState.Value, registeredLocalPath, capturedAt);
        }

        var version = await RunAsync(["--version"], cancellationToken).ConfigureAwait(false);
        if (version.Cancelled)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        if (version.CouldNotStart)
        {
            return CreateResult(RepositoryVerificationStatus.GitUnavailable, registeredLocalPath, capturedAt);
        }

        if (version.TimedOut)
        {
            return CreateFailure(registeredLocalPath, capturedAt, "Git verification timed out.");
        }

        if (version.ExitCode != 0)
        {
            return CreateFailure(registeredLocalPath, capturedAt, "Git verification could not be completed.");
        }

        var root = await RunAsync(
            ["-C", registeredLocalPath, "rev-parse", "--show-toplevel"],
            cancellationToken).ConfigureAwait(false);
        ThrowIfCancelled(root, cancellationToken);
        if (root.TimedOut)
        {
            return CreateFailure(registeredLocalPath, capturedAt, "Git verification timed out.");
        }

        if (root.CouldNotStart)
        {
            return CreateResult(RepositoryVerificationStatus.GitUnavailable, registeredLocalPath, capturedAt);
        }

        if (root.ExitCode != 0)
        {
            return CreateResult(RepositoryVerificationStatus.NotGitRepository, registeredLocalPath, capturedAt);
        }

        var repositoryRoot = LimitField(root.StandardOutput.Trim());
        if (repositoryRoot.Length == 0)
        {
            return CreateFailure(registeredLocalPath, capturedAt, "Repository root was unavailable.");
        }

        var branch = await RunAsync(
            ["-C", registeredLocalPath, "symbolic-ref", "--quiet", "--short", "HEAD"],
            cancellationToken).ConfigureAwait(false);
        ThrowIfCancelled(branch, cancellationToken);
        if (branch.TimedOut)
        {
            return CreateFailure(registeredLocalPath, capturedAt, "Git verification timed out.");
        }

        var head = await RunAsync(
            ["-C", registeredLocalPath, "rev-parse", "--verify", "HEAD"],
            cancellationToken).ConfigureAwait(false);
        ThrowIfCancelled(head, cancellationToken);
        if (head.TimedOut)
        {
            return CreateFailure(registeredLocalPath, capturedAt, "Git verification timed out.");
        }

        var upstream = await RunAsync(
            ["-C", registeredLocalPath, "rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{upstream}"],
            cancellationToken).ConfigureAwait(false);
        ThrowIfCancelled(upstream, cancellationToken);
        if (upstream.TimedOut)
        {
            return CreateFailure(registeredLocalPath, capturedAt, "Git verification timed out.");
        }

        var status = await RunAsync(
            ["-C", registeredLocalPath, "status", "--porcelain=v1", "-z", "--untracked-files=all"],
            cancellationToken).ConfigureAwait(false);
        ThrowIfCancelled(status, cancellationToken);
        if (status.TimedOut)
        {
            return CreateFailure(registeredLocalPath, capturedAt, "Git verification timed out.");
        }

        if (status.ExitCode != 0)
        {
            return CreateFailure(registeredLocalPath, capturedAt, "Repository status could not be read.");
        }

        var changes = ParseStatus(status.StandardOutput);
        var remotes = await RunAsync(
            ["-C", registeredLocalPath, "remote", "-v"],
            cancellationToken).ConfigureAwait(false);
        ThrowIfCancelled(remotes, cancellationToken);
        if (remotes.TimedOut)
        {
            return CreateFailure(registeredLocalPath, capturedAt, "Git verification timed out.");
        }

        if (remotes.ExitCode != 0)
        {
            return CreateFailure(registeredLocalPath, capturedAt, "Repository remotes could not be read.");
        }

        var remoteEntries = ParseRemotes(remotes.StandardOutput);
        return new LocalRepositoryInspection(
            changes.Total == 0
                ? RepositoryVerificationStatus.AvailableClean
                : RepositoryVerificationStatus.AvailableDirty,
            registeredLocalPath,
            repositoryRoot,
            IsSamePath(registeredLocalPath, repositoryRoot),
            branch.ExitCode == 0 ? LimitField(branch.StandardOutput.Trim()) : null,
            branch.ExitCode != 0,
            head.ExitCode == 0 ? LimitField(head.StandardOutput.Trim()) : null,
            head.ExitCode == 0 ? LimitField(head.StandardOutput.Trim())[..Math.Min(7, LimitField(head.StandardOutput.Trim()).Length)] : null,
            upstream.ExitCode == 0 ? LimitField(upstream.StandardOutput.Trim()) : null,
            changes.Total == 0,
            changes.Total,
            changes.Files,
            changes.Truncated,
            remoteEntries,
            CompareRegisteredRemote(registeredRepositoryUrl, remoteEntries),
            capturedAt);
    }

    private async Task<GitCommandResult> RunAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken) =>
        await _runner.RunAsync(arguments, cancellationToken).ConfigureAwait(false);

    private static RepositoryVerificationStatus? CheckPath(string path)
    {
        if (!Directory.Exists(path))
        {
            return File.Exists(path)
                ? RepositoryVerificationStatus.PathUnavailable
                : RepositoryVerificationStatus.PathMissing;
        }

        try
        {
            // Probe only the directory itself. This does not recursively enumerate or read source
            // files, but distinguishes an inaccessible directory from a missing one where possible.
            using var enumerator = Directory.EnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly).GetEnumerator();
            _ = enumerator.MoveNext();
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return RepositoryVerificationStatus.PathUnavailable;
        }
        catch (IOException)
        {
            return RepositoryVerificationStatus.PathUnavailable;
        }
        catch (ArgumentException)
        {
            return RepositoryVerificationStatus.PathUnavailable;
        }
    }

    private static LocalRepositoryInspection CreateResult(
        RepositoryVerificationStatus status,
        string path,
        DateTimeOffset capturedAt) =>
        new(status, path, capturedAt: capturedAt);

    private static LocalRepositoryInspection CreateFailure(
        string path,
        DateTimeOffset capturedAt,
        string message) =>
        new(
            RepositoryVerificationStatus.Failed,
            path,
            capturedAt: capturedAt,
            safeErrorMessage: message);

    private static void ThrowIfCancelled(GitCommandResult result, CancellationToken cancellationToken)
    {
        if (result.Cancelled)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private static bool IsSamePath(string left, string right)
    {
        try
        {
            var normalizedLeft = Path.TrimEndingDirectorySeparator(Path.GetFullPath(left));
            var normalizedRight = Path.TrimEndingDirectorySeparator(Path.GetFullPath(right));
            return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static ParsedChanges ParseStatus(string output)
    {
        var tokens = output.Split('\0');
        var total = 0;
        var files = new List<RepositoryChangedFile>(Math.Min(MaxChangedFiles, tokens.Length));

        for (var index = 0; index < tokens.Length; index++)
        {
            var token = tokens[index];
            if (token.Length < 4)
            {
                continue;
            }

            var first = token[0];
            var second = token[1];
            var path = token[3..];
            if (path.Length == 0)
            {
                continue;
            }

            string? originalPath = null;
            var isRename = first is 'R' or 'C' || second is 'R' or 'C';
            if (isRename && index + 1 < tokens.Length && tokens[index + 1].Length > 0)
            {
                originalPath = path;
                path = tokens[++index];
            }

            total++;
            var kind = GetChangeKind(first, second, isRename);
            if (files.Count < MaxChangedFiles)
            {
                files.Add(new RepositoryChangedFile(
                    LimitField(path),
                    kind,
                    originalPath is null ? null : LimitField(originalPath)));
            }
        }

        return new ParsedChanges(total, files, total > files.Count);
    }

    private static RepositoryChangedFileKind GetChangeKind(char index, char worktree, bool isRename)
    {
        var kind = RepositoryChangedFileKind.None;
        if (index != ' ' && index != '?')
        {
            kind |= RepositoryChangedFileKind.Staged;
        }

        if (worktree is 'M' or 'T')
        {
            kind |= RepositoryChangedFileKind.Modified;
        }

        if (index is 'D' || worktree is 'D')
        {
            kind |= RepositoryChangedFileKind.Deleted;
        }

        if (isRename)
        {
            kind |= RepositoryChangedFileKind.Renamed;
        }

        if (index == '?' && worktree == '?')
        {
            kind |= RepositoryChangedFileKind.Untracked;
        }

        if (index is 'U' || worktree is 'U' || (index == 'A' && worktree == 'A') || (index == 'D' && worktree == 'D'))
        {
            kind |= RepositoryChangedFileKind.Conflicted;
        }

        return kind;
    }

    private static IReadOnlyList<RepositoryRemote> ParseRemotes(string output)
    {
        var result = new List<RepositoryRemote>();
        foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = line.IndexOf('\t');
            if (separator <= 0)
            {
                continue;
            }

            var name = line[..separator].Trim();
            var remainder = line[(separator + 1)..].Trim();
            var marker = remainder.LastIndexOf(" (", StringComparison.Ordinal);
            var rawUrl = marker > 0 ? remainder[..marker].Trim() : remainder;
            if (name.Length == 0 || rawUrl.Length == 0)
            {
                continue;
            }

            var sanitized = SanitizeRemoteUrl(rawUrl);
            if (!result.Any(remote =>
                    string.Equals(remote.Name, name, StringComparison.Ordinal) &&
                    string.Equals(remote.SanitizedUrl, sanitized, StringComparison.Ordinal)))
            {
                result.Add(new RepositoryRemote(LimitField(name), sanitized));
            }
        }

        return result;
    }

    internal static string SanitizeRemoteUrl(string rawUrl)
        => LimitField(RepositoryUrlSanitizer.Sanitize(rawUrl));

    private static RepositoryRemoteComparison CompareRegisteredRemote(
        string? registeredUrl,
        IReadOnlyList<RepositoryRemote> remotes)
    {
        if (string.IsNullOrWhiteSpace(registeredUrl))
        {
            return RepositoryRemoteComparison.NotConfigured;
        }

        if (remotes.Count == 0)
        {
            return RepositoryRemoteComparison.NoLocalRemote;
        }

        var registeredKey = GetRemoteKey(registeredUrl);
        if (registeredKey is null)
        {
            return RepositoryRemoteComparison.ComparisonUnavailable;
        }

        var localKeys = remotes
            .Select(remote => GetRemoteKey(remote.SanitizedUrl))
            .ToArray();
        if (localKeys.Any(key => key is not null && key == registeredKey))
        {
            return RepositoryRemoteComparison.Match;
        }

        return localKeys.Any(static key => key is null)
            ? RepositoryRemoteComparison.ComparisonUnavailable
            : RepositoryRemoteComparison.Different;
    }

    private static string? GetRemoteKey(string value)
    {
        var sanitized = SanitizeRemoteUrl(value);
        var separator = sanitized.IndexOf(':');
        if (sanitized.Contains("://", StringComparison.Ordinal))
        {
            if (!Uri.TryCreate(sanitized, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
            {
                return null;
            }

            var path = uri.AbsolutePath.Trim('/');
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{uri.Host.ToLowerInvariant()}/{path}");
        }

        if (separator > 0)
        {
            var host = sanitized[..separator].Trim().ToLowerInvariant();
            var path = sanitized[(separator + 1)..].Trim('/');
            return $"{host}/{path}";
        }

        return null;
    }

    private static string LimitField(string value)
    {
        if (value.Length <= MaxFieldLength)
        {
            return value;
        }

        return value[..(MaxFieldLength - 1)] + "…";
    }

    private sealed record ParsedChanges(
        int Total,
        IReadOnlyList<RepositoryChangedFile> Files,
        bool Truncated);
}
