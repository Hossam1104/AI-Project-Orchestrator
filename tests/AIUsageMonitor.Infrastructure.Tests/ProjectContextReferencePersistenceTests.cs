using System.Text.Json;
using AIUsageMonitor.Application.Projects;
using AIUsageMonitor.Infrastructure.Persistence;
using AIUsageMonitor.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageMonitor.Infrastructure.Tests;

public sealed class ProjectContextReferencePersistenceTests
{
    [Fact]
    public async Task ContextRoundTripsToGuidScopedPathAndPreservesProjectIsolation()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();
        var contextA = CreateContext(projectA, "C:\\a");
        var contextB = CreateContext(projectB, "C:\\b");

        await repository.UpsertAsync(contextA);
        await repository.UpsertAsync(contextB);

        var loadedA = await repository.GetAsync(projectA);
        var loadedB = await repository.GetAsync(projectB);

        Assert.Equal(ProjectContextReadState.Valid, loadedA.State);
        Assert.Equal(ProjectContextReadState.Valid, loadedB.State);
        Assert.Equal(projectA, loadedA.Context!.ProjectId);
        Assert.Equal("C:\\a", loadedA.Context.Repository.RegisteredLocalPath);
        Assert.Equal(projectB, loadedB.Context!.ProjectId);
        Assert.NotEqual(
            store.Paths.GetProjectContextReferenceFile(projectA),
            store.Paths.GetProjectContextReferenceFile(projectB));
        Assert.True(File.Exists(store.Paths.GetProjectContextReferenceFile(projectA)));
        Assert.True(File.Exists(store.Paths.GetProjectContextReferenceFile(projectB)));
    }

    [Fact]
    public async Task MissingContextIsExplicitlyMissing()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);

        var result = await repository.GetAsync(Guid.NewGuid());

        Assert.Equal(ProjectContextReadState.Missing, result.State);
        Assert.Null(result.Context);
    }

    [Fact]
    public async Task MismatchedEmbeddedProjectIdIsRejected()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var requestedProject = Guid.NewGuid();
        var embeddedProject = Guid.NewGuid();
        var context = CreateContext(embeddedProject, "C:\\embedded");
        var record = ProjectContextReferenceRecord.FromApplication(context);
        var path = store.Paths.GetProjectContextReferenceFile(requestedProject);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(
            path,
            JsonSerializer.Serialize(new { schemaVersion = JsonFileStore.CurrentSchemaVersion, payload = record }, JsonFileStore.SerializerOptions));

        var result = await repository.GetAsync(requestedProject);

        Assert.Equal(ProjectContextReadState.Invalid, result.State);
        Assert.Null(result.Context);
    }

    [Fact]
    public async Task FutureSemanticVersionIsNotReadAsCurrent()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var projectId = Guid.NewGuid();
        var record = ProjectContextReferenceRecord.FromApplication(CreateContext(projectId, "C:\\future"));
        record.ContractVersion = ProjectContextContract.CurrentVersion + 1;
        var path = store.Paths.GetProjectContextReferenceFile(projectId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(
            path,
            JsonSerializer.Serialize(new { schemaVersion = JsonFileStore.CurrentSchemaVersion, payload = record }, JsonFileStore.SerializerOptions));

        var result = await repository.GetAsync(projectId);

        Assert.Equal(ProjectContextReadState.UnsupportedVersion, result.State);
        Assert.Contains("newer", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CorruptContextDoesNotBecomeReady()
    {
        using var store = new TemporaryStore();
        var repository = CreateRepository(store);
        var projectId = Guid.NewGuid();
        var path = store.Paths.GetProjectContextReferenceFile(projectId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, "{ not valid json");

        var result = await repository.GetAsync(projectId);

        Assert.Equal(ProjectContextReadState.Invalid, result.State);
        Assert.Null(result.Context);
    }

    private static JsonProjectContextReferenceRepository CreateRepository(TemporaryStore store) =>
        new(store.Paths, store.Files, NullLogger<JsonProjectContextReferenceRepository>.Instance);

    private static ProjectContextReference CreateContext(Guid projectId, string localPath) =>
        new(
            projectId,
            Guid.NewGuid(),
            ProjectContextContract.CurrentVersion,
            new DateTimeOffset(2026, 8, 26, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 26, 0, 0, 0, TimeSpan.Zero),
            ProjectRepositoryContextReference.Skipped(projectId, localPath),
            new ProjectTrackerContextReference(TrackerReferenceState.Skipped),
            [],
            new ProjectCurrentWorkReference(CurrentWorkState.NotSelected),
            [],
            null,
            null,
            ProjectNextSafeAction.ReadyForPlanning);
}
