using System.Text.Json;
using AIUsageMonitor.Application.Trackers;
using AIUsageMonitor.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageMonitor.Infrastructure.Tests;

public sealed class TrackerMutationAuditPersistenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 29, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid ProjectId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task Append_WritesProjectScopedVersionedSecretSafeJsonl()
    {
        using var store = new TemporaryStore();
        var events = new JsonlEventStore<TrackerMutationAuditRecord>(
            store.Paths,
            store.Files,
            NullLogger<JsonlEventStore<TrackerMutationAuditRecord>>.Instance);
        var repository = new JsonTrackerMutationAuditRepository(store.Paths, events);
        var receipt = Receipt();

        await repository.AppendAsync(receipt);

        var directory = store.Paths.GetProjectTrackerAuditDirectory(ProjectId);
        var file = Assert.Single(Directory.GetFiles(directory, "*.jsonl"));
        var json = await File.ReadAllTextAsync(file);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal("tracker-mutation-receipt", root.GetProperty("recordType").GetString());
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(ProjectId, root.GetProperty("projectId").GetGuid());
        Assert.Contains(receipt.BodyHash!, json, StringComparison.Ordinal);
        Assert.DoesNotContain("comment body secret", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Bearer owner-token", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Authorization", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Append_DoesNotWriteIntoAnotherProjectDirectory()
    {
        using var store = new TemporaryStore();
        var events = new JsonlEventStore<TrackerMutationAuditRecord>(
            store.Paths,
            store.Files,
            NullLogger<JsonlEventStore<TrackerMutationAuditRecord>>.Instance);
        var repository = new JsonTrackerMutationAuditRepository(store.Paths, events);

        await repository.AppendAsync(Receipt());

        Assert.True(Directory.Exists(store.Paths.GetProjectTrackerAuditDirectory(ProjectId)));
        Assert.False(Directory.Exists(store.Paths.GetProjectTrackerAuditDirectory(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"))));
    }

    [Fact]
    public void TrackerAuditStartsAtOneWithoutChangingGlobalJsonSchema()
    {
        Assert.Equal(1, JsonFileStore.CurrentSchemaVersion);
        Assert.Equal(1, TrackerMutationReceipt.CurrentSchemaVersion);
    }

    private static TrackerMutationReceipt Receipt()
    {
        var tracker = new TrackerProjectIdentity(TrackerProviderKind.Jira, "APO", new Uri("https://jira.example/"));
        var workItem = new TrackerWorkItemIdentity(TrackerProviderKind.Jira, "APO", "APO-47", "10001");
        var target = new TrackerMutationTarget(workItem);
        var authority = new TrackerMutationAuthority(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            ProjectId,
            tracker,
            target,
            TrackerMutationKind.AddComment,
            "expected-fingerprint",
            "actor",
            "run-1",
            Now,
            Now.AddMinutes(5),
            TrackerCommentMetadata.ComputeBodyHash("comment body secret"));

        return new TrackerMutationReceipt(
            ProjectId,
            tracker,
            target,
            TrackerMutationKind.AddComment,
            authority.AuthorityId,
            authority.ContentHash,
            authority.ActorIdentity,
            authority.CorrelationId,
            Now,
            authority.ExpectedStateIdentity,
            "201 Created",
            TrackerEvidenceState.Available,
            TrackerMutationOutcome.Succeeded,
            mayHaveModifiedRemote: true,
            bodyHash: TrackerCommentMetadata.ComputeBodyHash("comment body secret"),
            bodyLength: "comment body secret".Length,
            remoteReference: "300");
    }
}
