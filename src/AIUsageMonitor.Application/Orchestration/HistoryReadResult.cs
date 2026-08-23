namespace AIUsageMonitor.Application.Orchestration;

/// <summary>
/// Read records together with the storage truthfulness state for the requested history range.
/// Normal absence is represented by Success with an empty record collection.
/// </summary>
public sealed class HistoryReadResult<T>
{
    public HistoryReadResult(
        IReadOnlyList<T> records,
        HistoryReadStatus status,
        IReadOnlyList<HistoryReadIssue>? issues = null)
    {
        ArgumentNullException.ThrowIfNull(records);

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "History read status is undefined.");
        }

        Status = status;
        Records = records.ToArray();
        Issues = (issues ?? Array.Empty<HistoryReadIssue>()).ToArray();

        if (Issues.Any(static issue => issue is null))
        {
            throw new ArgumentException("History read issues cannot contain null values.", nameof(issues));
        }

        if (status == HistoryReadStatus.Success && Issues.Count > 0)
        {
            throw new ArgumentException(
                "A successful history read cannot contain read issues.",
                nameof(status));
        }
    }

    public IReadOnlyList<T> Records { get; }

    public HistoryReadStatus Status { get; }

    public IReadOnlyList<HistoryReadIssue> Issues { get; }
}
