namespace AIUsageMonitor.Application.Orchestration;

/// <summary>
/// Bounded, provider-independent information about a history-read problem. It intentionally
/// contains a partition identifier and safe summary only; no exception, absolute path, or record
/// payload is exposed through the Application contract.
/// </summary>
public sealed class HistoryReadIssue
{
    private const int MaxPartitionLength = 128;
    private const int MaxMessageLength = 256;

    public HistoryReadIssue(
        HistoryReadIssueKind kind,
        string partition,
        string nonSecretMessage)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "History issue kind is undefined.");
        }

        if (string.IsNullOrWhiteSpace(partition))
        {
            throw new ArgumentException("History issue partition is required.", nameof(partition));
        }

        if (partition.Length > MaxPartitionLength)
        {
            throw new ArgumentException("History issue partition is too long.", nameof(partition));
        }

        if (string.IsNullOrWhiteSpace(nonSecretMessage))
        {
            throw new ArgumentException("History issue message is required.", nameof(nonSecretMessage));
        }

        if (nonSecretMessage.Length > MaxMessageLength)
        {
            throw new ArgumentException("History issue message is too long.", nameof(nonSecretMessage));
        }

        Kind = kind;
        Partition = partition.Trim();
        NonSecretMessage = nonSecretMessage.Trim();
    }

    public HistoryReadIssueKind Kind { get; }

    public string Partition { get; }

    public string NonSecretMessage { get; }
}
