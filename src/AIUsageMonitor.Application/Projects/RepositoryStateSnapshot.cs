namespace AIUsageMonitor.Application.Projects;

/// <summary>
/// Project-scoped repository evidence. The project identity is carried through publication so a
/// late inspection cannot be mistaken for the currently selected project.
/// </summary>
public sealed class RepositoryStateSnapshot
{
    public RepositoryStateSnapshot(
        Guid projectId,
        string registeredLocalPath,
        LocalRepositoryInspection inspection)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        ArgumentNullException.ThrowIfNull(inspection);

        ProjectId = projectId;
        RegisteredLocalPath = registeredLocalPath;
        Status = inspection.Status;
        RepositoryRoot = inspection.RepositoryRoot;
        LocalPathIsRepositoryRoot = inspection.LocalPathIsRepositoryRoot;
        BranchName = inspection.BranchName;
        IsDetachedHead = inspection.IsDetachedHead;
        HeadSha = inspection.HeadSha;
        HeadShortSha = inspection.HeadShortSha;
        UpstreamBranch = inspection.UpstreamBranch;
        IsClean = inspection.IsClean;
        ChangedFileTotal = inspection.ChangedFileTotal;
        ChangedFiles = inspection.ChangedFiles.ToArray();
        ChangedFilesTruncated = inspection.ChangedFilesTruncated;
        Remotes = inspection.Remotes.ToArray();
        RemoteComparison = inspection.RemoteComparison;
        CapturedAt = inspection.CapturedAt;
        SafeErrorMessage = inspection.SafeErrorMessage;
    }

    public Guid ProjectId { get; }

    public string RegisteredLocalPath { get; }

    public RepositoryVerificationStatus Status { get; }

    public string? RepositoryRoot { get; }

    public bool? LocalPathIsRepositoryRoot { get; }

    public string? BranchName { get; }

    public bool IsDetachedHead { get; }

    public string? HeadSha { get; }

    public string? HeadShortSha { get; }

    public string? UpstreamBranch { get; }

    public bool? IsClean { get; }

    public int ChangedFileTotal { get; }

    public IReadOnlyList<RepositoryChangedFile> ChangedFiles { get; }

    public bool ChangedFilesTruncated { get; }

    public IReadOnlyList<RepositoryRemote> Remotes { get; }

    public RepositoryRemoteComparison RemoteComparison { get; }

    public DateTimeOffset CapturedAt { get; }

    public string? SafeErrorMessage { get; }
}
