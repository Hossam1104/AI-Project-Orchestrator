namespace AIUsageMonitor.Domain.Providers;

public sealed class Provider
{
    public Guid Id { get; }
    public ProviderCode Code { get; }
    public string DisplayName { get; }
    public bool Enabled { get; }
    public int SortOrder { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; }

    public Provider(
        Guid id,
        ProviderCode code,
        string displayName,
        bool enabled,
        int sortOrder,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Provider id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Provider display name is required.", nameof(displayName));
        }

        if (updatedAt < createdAt)
        {
            throw new ArgumentException("UpdatedAt cannot precede CreatedAt.", nameof(updatedAt));
        }

        Id = id;
        Code = code;
        DisplayName = displayName;
        Enabled = enabled;
        SortOrder = sortOrder;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}
