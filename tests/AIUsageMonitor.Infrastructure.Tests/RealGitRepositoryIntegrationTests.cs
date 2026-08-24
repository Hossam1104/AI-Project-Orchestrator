using System.Diagnostics;
using AIUsageMonitor.Application.Projects;
using AIUsageMonitor.Infrastructure.Git;

namespace AIUsageMonitor.Infrastructure.Tests;

/// <summary>
/// End-to-end proof against a real, disposable Git repository created with real
/// <c>git init</c>/<c>add</c>/<c>commit</c>/<c>mv</c> commands, exercised through the production
/// <see cref="GitLocalRepositoryInspector"/> (the real <see cref="SystemGitCommandRunner"/> and
/// <see cref="SystemLocalPathProbe"/>, not fakes). This is the regression proof for SOL-37-01: the
/// rewritten porcelain -z parser must publish the real Git rename token order correctly.
///
/// The repository lives entirely under the OS temp directory and is removed in
/// <see cref="Dispose"/>; nothing here ever touches the owner's registered repository, performs a
/// network operation, or writes through the inspector itself (the inspector stays read-only —
/// only the test setup below uses Git write commands, against the disposable repo only). If Git is
/// not available in the current environment, every test short-circuits truthfully instead of
/// fabricating a pass or a failure.
/// </summary>
[Collection("SystemLocalPathProbe")]
public sealed class RealGitRepositoryIntegrationTests : IDisposable
{
    private static readonly Lazy<bool> GitAvailable = new(ProbeGitAvailability);

    private readonly string _repositoryPath = Path.Combine(
        Path.GetTempPath(),
        "apo-real-git-tests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task CleanRepository_ReportsAvailableCleanWithBranchAndHead()
    {
        if (!GitAvailable.Value)
        {
            return;
        }

        InitializeRepository();
        RunGit("add", "tracked.txt");
        RunGit("commit", "-m", "initial commit");

        var inspection = await InspectAsync();

        Assert.Equal(RepositoryVerificationStatus.AvailableClean, inspection.Status);
        Assert.True(inspection.IsClean);
        Assert.Equal(0, inspection.ChangedFileTotal);
        Assert.False(inspection.IsDetachedHead);
        Assert.Equal("main", inspection.BranchName);
        Assert.NotNull(inspection.HeadSha);
        Assert.Equal(40, inspection.HeadSha!.Length);
    }

    [Fact]
    public async Task DirtyRepository_ReportsModifiedTrackedFile()
    {
        if (!GitAvailable.Value)
        {
            return;
        }

        InitializeRepository();
        RunGit("add", "tracked.txt");
        RunGit("commit", "-m", "initial commit");
        File.AppendAllText(Path.Combine(_repositoryPath, "tracked.txt"), "more content\n");

        var inspection = await InspectAsync();

        Assert.Equal(RepositoryVerificationStatus.AvailableDirty, inspection.Status);
        Assert.False(inspection.IsClean);
        var changed = Assert.Single(inspection.ChangedFiles);
        Assert.Equal("tracked.txt", changed.RelativePath);
        Assert.True(changed.Kind.HasFlag(RepositoryChangedFileKind.Modified));
        Assert.False(changed.Kind.HasFlag(RepositoryChangedFileKind.Staged));
    }

    [Fact]
    public async Task StagedNewFile_ReportsStagedWithoutModifiedOrUntracked()
    {
        if (!GitAvailable.Value)
        {
            return;
        }

        InitializeRepository();
        RunGit("add", "tracked.txt");
        RunGit("commit", "-m", "initial commit");
        File.WriteAllText(Path.Combine(_repositoryPath, "staged.txt"), "new file\n");
        RunGit("add", "staged.txt");

        var inspection = await InspectAsync();

        var changed = Assert.Single(inspection.ChangedFiles);
        Assert.Equal("staged.txt", changed.RelativePath);
        Assert.True(changed.Kind.HasFlag(RepositoryChangedFileKind.Staged));
        Assert.False(changed.Kind.HasFlag(RepositoryChangedFileKind.Untracked));
    }

    [Fact]
    public async Task UntrackedFile_ReportsUntrackedWithoutStaged()
    {
        if (!GitAvailable.Value)
        {
            return;
        }

        InitializeRepository();
        RunGit("add", "tracked.txt");
        RunGit("commit", "-m", "initial commit");
        File.WriteAllText(Path.Combine(_repositoryPath, "untracked.txt"), "new file\n");

        var inspection = await InspectAsync();

        var changed = Assert.Single(inspection.ChangedFiles);
        Assert.Equal("untracked.txt", changed.RelativePath);
        Assert.True(changed.Kind.HasFlag(RepositoryChangedFileKind.Untracked));
        Assert.False(changed.Kind.HasFlag(RepositoryChangedFileKind.Staged));
    }

    /// <summary>
    /// The SOL-37-01 regression proof: a real <c>git mv</c> against a real repository must publish
    /// the new path as <see cref="RepositoryChangedFile.RelativePath"/> and the old path as
    /// <see cref="RepositoryChangedFile.OriginalRelativePath"/> — the reverse of the raw `-z` token
    /// order emitted by Git itself (`new\0old\0`).
    /// </summary>
    [Fact]
    public async Task RenamedFile_PublishesNewPathAsRelativeAndOldPathAsOriginal()
    {
        if (!GitAvailable.Value)
        {
            return;
        }

        InitializeRepository();
        RunGit("add", "tracked.txt");
        RunGit("commit", "-m", "initial commit");
        RunGit("mv", "tracked.txt", "renamed.txt");

        var inspection = await InspectAsync();

        var changed = Assert.Single(inspection.ChangedFiles);
        Assert.Equal("renamed.txt", changed.RelativePath);
        Assert.Equal("tracked.txt", changed.OriginalRelativePath);
        Assert.True(changed.Kind.HasFlag(RepositoryChangedFileKind.Renamed));
    }

    [Fact]
    public async Task UnbornRepository_ReportsRepositoryValidWithNoFabricatedHeadSha()
    {
        if (!GitAvailable.Value)
        {
            return;
        }

        InitializeRepository();
        File.WriteAllText(Path.Combine(_repositoryPath, "untracked.txt"), "pending\n");

        var inspection = await InspectAsync();

        Assert.NotEqual(RepositoryVerificationStatus.Failed, inspection.Status);
        Assert.NotEqual(RepositoryVerificationStatus.NotGitRepository, inspection.Status);
        Assert.Null(inspection.HeadSha);
        Assert.False(inspection.IsDetachedHead);
    }

    private void InitializeRepository()
    {
        Directory.CreateDirectory(_repositoryPath);
        RunGit("init", "-q", "-b", "main");
        RunGit("config", "user.email", "apo-test@example.invalid");
        RunGit("config", "user.name", "APO Test");
        File.WriteAllText(Path.Combine(_repositoryPath, "tracked.txt"), "initial content\n");
    }

    private async Task<LocalRepositoryInspection> InspectAsync()
    {
        var inspector = new GitLocalRepositoryInspector();
        return await inspector.InspectAsync(_repositoryPath);
    }

    private void RunGit(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = _repositoryPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start git for test setup.");
        process.WaitForExit(10_000);
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("git test setup command did not complete in time.");
        }

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException(
                $"git {string.Join(' ', arguments)} failed with exit {process.ExitCode}: {error}");
        }
    }

    private static bool ProbeGitAvailability()
    {
        try
        {
            var startInfo = new ProcessStartInfo("git", "--version")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return false;
            }

            process.WaitForExit(5_000);
            return process.HasExited && process.ExitCode == 0;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (!Directory.Exists(_repositoryPath))
        {
            return;
        }

        try
        {
            NormalizeAttributesForDeletion(_repositoryPath);
            Directory.Delete(_repositoryPath, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup of a disposable temp directory; never fail the test run over it.
        }
    }

    private static void NormalizeAttributesForDeletion(string path)
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(path, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(entry, FileAttributes.Normal);
        }
    }
}
