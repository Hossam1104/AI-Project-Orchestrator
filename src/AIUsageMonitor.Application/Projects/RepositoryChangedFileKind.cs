using System;

namespace AIUsageMonitor.Application.Projects;

[Flags]
public enum RepositoryChangedFileKind
{
    None = 0,
    Staged = 1,
    Modified = 2,
    Deleted = 4,
    Renamed = 8,
    Untracked = 16,
    Conflicted = 32
}
