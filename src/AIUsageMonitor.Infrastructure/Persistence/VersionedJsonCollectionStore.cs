namespace AIUsageMonitor.Infrastructure.Persistence;

internal sealed class VersionedCollection<TRecord>
{
    public List<TRecord> Items { get; set; } = [];
}

/// <summary>
/// Small JSON-document collection storage. It deliberately exposes collection semantics rather
/// than becoming a generic database: each repository owns its document and mapping rules.
/// </summary>
internal sealed class VersionedJsonCollectionStore<TRecord>
{
    private readonly JsonFileStore _files;

    public VersionedJsonCollectionStore(JsonFileStore files)
    {
        _files = files;
    }

    public async Task<IReadOnlyList<TRecord>> ReadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var result = await _files.ReadAsync<VersionedCollection<TRecord>>(path, cancellationToken).ConfigureAwait(false);
        return result.IsUsable && result.Value!.Items is not null ? result.Value.Items : [];
    }

    public Task UpdateAsync(
        string path,
        Func<List<TRecord>, List<TRecord>> update,
        CancellationToken cancellationToken = default) =>
        _files.ExecuteExclusiveAsync(path, async () =>
        {
            var current = await ReadForUpdateAsync(path, cancellationToken).ConfigureAwait(false);
            var updated = update(current.ToList());
            await _files.WriteCoreAsync(path, new VersionedCollection<TRecord> { Items = updated }, cancellationToken)
                .ConfigureAwait(false);
        }, cancellationToken);

    public Task<TResult> UpdateAsync<TResult>(
        string path,
        Func<List<TRecord>, (List<TRecord> Items, TResult Result)> update,
        CancellationToken cancellationToken = default) =>
        _files.ExecuteExclusiveAsync(path, async () =>
        {
            var current = await ReadForUpdateAsync(path, cancellationToken).ConfigureAwait(false);
            var (updated, result) = update(current.ToList());
            await _files.WriteCoreAsync(path, new VersionedCollection<TRecord> { Items = updated }, cancellationToken)
                .ConfigureAwait(false);
            return result;
        }, cancellationToken);

    private async Task<List<TRecord>> ReadForUpdateAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var result = await _files.ReadAsync<VersionedCollection<TRecord>>(path, cancellationToken)
            .ConfigureAwait(false);

        return result.Status switch
        {
            FileReadStatus.Missing or FileReadStatus.Empty => [],
            FileReadStatus.Valid => result.Value?.Items ?? [],
            // JsonFileStore has already quarantined these statuses using the established
            // recovery policy. A later update is allowed to recreate the authoritative file.
            FileReadStatus.Corrupt or FileReadStatus.UnsupportedSchema => [],
            FileReadStatus.IoFailure => throw new IOException(
                "The JSON collection could not be read because of a temporary I/O failure."),
            FileReadStatus.PermissionFailure => throw new UnauthorizedAccessException(
                "The JSON collection could not be read because access was denied."),
            _ => throw new IOException("The JSON collection could not be read safely before update.")
        };
    }
}
