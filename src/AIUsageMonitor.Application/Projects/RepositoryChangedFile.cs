namespace AIUsageMonitor.Application.Projects;

/// <summary>
/// Bounded, repository-relative status evidence. File contents and diffs are intentionally
/// absent.
/// </summary>
public sealed record RepositoryChangedFile(
    string RelativePath,
    RepositoryChangedFileKind Kind,
    string? OriginalRelativePath = null);
