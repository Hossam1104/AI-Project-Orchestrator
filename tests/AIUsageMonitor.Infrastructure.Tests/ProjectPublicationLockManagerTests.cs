using AIUsageMonitor.Application.Orchestration;

namespace AIUsageMonitor.Infrastructure.Tests;

public sealed class ProjectPublicationLockManagerTests
{
    [Fact]
    public async Task AcquireReferenceCannotBeRetiredWhileWaiterIsPendingAndIdleEntriesEvict()
    {
        var manager = new ProjectPublicationLockManager();
        var projectId = Guid.NewGuid();

        var owner = await manager.AcquireAsync(projectId, CancellationToken.None);
        var waiterTask = manager.AcquireAsync(projectId, CancellationToken.None);

        Assert.Equal(1, manager.EntryCount);
        Assert.False(waiterTask.IsCompleted);

        owner.Dispose();
        var waiter = await waiterTask;

        Assert.Equal(1, manager.EntryCount);

        var subsequentTask = manager.AcquireAsync(projectId, CancellationToken.None);
        Assert.False(subsequentTask.IsCompleted);

        waiter.Dispose();
        var subsequent = await subsequentTask;
        subsequent.Dispose();

        Assert.Equal(0, manager.EntryCount);
    }

    [Fact]
    public async Task CancelledAcquireReleasesItsReferenceAndSubsequentAcquireWorks()
    {
        var manager = new ProjectPublicationLockManager();
        var projectId = Guid.NewGuid();
        var owner = await manager.AcquireAsync(projectId, CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            manager.AcquireAsync(projectId, cancellation.Token));

        Assert.Equal(1, manager.EntryCount);

        owner.Dispose();
        Assert.Equal(0, manager.EntryCount);

        var subsequent = await manager.AcquireAsync(projectId, CancellationToken.None);
        subsequent.Dispose();

        Assert.Equal(0, manager.EntryCount);
    }
}
