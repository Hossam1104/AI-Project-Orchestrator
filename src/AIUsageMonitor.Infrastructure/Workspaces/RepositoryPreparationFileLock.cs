using AIUsageMonitor.Application.Workspaces;
using AIUsageMonitor.Infrastructure.Persistence;

namespace AIUsageMonitor.Infrastructure.Workspaces;

/// <summary>Cross-process lock keyed by repository identity, never by a repository file.</summary>
public sealed class RepositoryPreparationFileLock : IRepositoryPreparationLock
{
    private readonly ApplicationDataPaths _paths;
    private readonly TimeSpan _retryDelay;
    private readonly TimeSpan _timeout;

    public RepositoryPreparationFileLock(ApplicationDataPaths paths)
        : this(paths, TimeSpan.FromMilliseconds(50), TimeSpan.FromSeconds(30)) { }

    internal RepositoryPreparationFileLock(ApplicationDataPaths paths, TimeSpan retryDelay, TimeSpan timeout)
    { _paths = paths ?? throw new ArgumentNullException(nameof(paths)); _retryDelay = retryDelay; _timeout = timeout; }

    public async Task<IAsyncDisposable> AcquireAsync(string repositoryIdentity, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(repositoryIdentity)) throw new ArgumentException("Repository identity is required.", nameof(repositoryIdentity));
        var normalizedIdentity = WorkspaceRepositoryIdentity.Normalize(repositoryIdentity);
        var path = _paths.GetWorkspaceLockFile(normalizedIdentity);
        await _paths.EnsureDirectoriesAsync(cancellationToken).ConfigureAwait(false);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);
        while (true)
        {
            timeout.Token.ThrowIfCancellationRequested();
            try
            {
                var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.WriteThrough);
                return new FileLockHandle(stream);
            }
            catch (IOException)
            {
                await Task.Delay(_retryDelay, timeout.Token).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException)
            {
                throw new IOException("Repository preparation lock is unavailable.");
            }
        }
    }

    private sealed class FileLockHandle : IAsyncDisposable
    {
        private readonly FileStream _stream;
        public FileLockHandle(FileStream stream) => _stream = stream;
        public ValueTask DisposeAsync() { _stream.Dispose(); return ValueTask.CompletedTask; }
    }
}
