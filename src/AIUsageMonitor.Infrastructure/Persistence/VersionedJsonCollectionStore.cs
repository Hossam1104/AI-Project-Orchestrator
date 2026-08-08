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
            var current = await ReadAsync(path, cancellationToken).ConfigureAwait(false);
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
            var current = await ReadAsync(path, cancellationToken).ConfigureAwait(false);
            var (updated, result) = update(current.ToList());
            await _files.WriteCoreAsync(path, new VersionedCollection<TRecord> { Items = updated }, cancellationToken)
                .ConfigureAwait(false);
            return result;
        }, cancellationToken);
}
