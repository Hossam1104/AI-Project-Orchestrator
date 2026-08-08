namespace AIUsageMonitor.Infrastructure.Persistence;

public enum FileReadStatus
{
    Missing,
    Empty,
    Valid,
    UnsupportedSchema,
    Corrupt,
    IoFailure,
    PermissionFailure
}

public sealed record FileReadResult<T>(
    FileReadStatus Status,
    T? Value,
    string FilePath,
    string? ErrorMessage = null)
{
    public bool IsUsable => Status == FileReadStatus.Valid && Value is not null;
}
