using System.Text.Json;
using AIUsageMonitor.Application.Projects;
using AIUsageMonitor.Application.Time;
using AIUsageMonitor.Application.Trackers;

namespace AIUsageMonitor.Connection.Tests;

public sealed class TrackerSynchronizationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 29, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void JiraProjectConfiguration_ResolvesExactJiraAdapter()
    {
        var project = CreateProject();
        var adapter = new FakeAdapter(TrackerProviderKind.Jira);

        var result = new WorkItemTrackerAdapterResolver([adapter]).Resolve(project);

        Assert.Equal(TrackerAdapterResolutionStatus.Resolved, result.Status);
        Assert.Same(adapter, result.Adapter);
        Assert.Equal(TrackerProviderKind.Jira, result.Configuration!.Identity.Provider);
    }

    [Fact]
    public void AzureConfiguration_DoesNotResolveJira()
    {
        var project = CreateProject(trackerType: "AzureDevOps", trackerId: "DBSMENA", includeBaseUri: false);

        var result = new WorkItemTrackerAdapterResolver([new FakeAdapter(TrackerProviderKind.Jira)]).Resolve(project);

        Assert.Equal(TrackerAdapterResolutionStatus.Unsupported, result.Status);
        Assert.Null(result.Adapter);
    }

    [Fact]
    public void UnknownTrackerType_FailsTruthfully()
    {
        var result = new WorkItemTrackerAdapterResolver([new FakeAdapter(TrackerProviderKind.Jira)])
            .Resolve(CreateProject(trackerType: "Linear"));

        Assert.Equal(TrackerAdapterResolutionStatus.Unsupported, result.Status);
        Assert.Contains("not supported", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingTrackerConfiguration_IsNotConfigured()
    {
        var result = new WorkItemTrackerAdapterResolver([new FakeAdapter(TrackerProviderKind.Jira)])
            .Resolve(CreateProject(trackerType: null, trackerId: null, includeBaseUri: false));

        Assert.Equal(TrackerAdapterResolutionStatus.NotConfigured, result.Status);
    }

    [Fact]
    public void MultipleExactAdapters_ReturnConfigurationConflict()
    {
        var result = new WorkItemTrackerAdapterResolver([
            new FakeAdapter(TrackerProviderKind.Jira),
            new FakeAdapter(TrackerProviderKind.Jira)])
            .Resolve(CreateProject());

        Assert.Equal(TrackerAdapterResolutionStatus.ConfigurationConflict, result.Status);
        Assert.Null(result.Adapter);
    }

    [Fact]
    public void UnsafeOrConflictingMetadata_IsRejected()
    {
        var project = CreateProject(metadata: new Dictionary<string, string?>
        {
            [TrackerMetadataKeys.BaseUri] = "http://jira.example/",
            [TrackerMetadataKeys.ProjectKey] = "OTHER"
        });

        var result = new WorkItemTrackerAdapterResolver([new FakeAdapter(TrackerProviderKind.Jira)]).Resolve(project);

        Assert.Equal(TrackerAdapterResolutionStatus.ConfigurationConflict, result.Status);
    }

    [Fact]
    public void ProjectTrackerConfigurationContainsOnlyOpaqueCredentialReference()
    {
        var json = JsonSerializer.Serialize(CreateProject());

        Assert.Contains("authRef", json, StringComparison.Ordinal);
        Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IdenticalDesiredState_ProducesNoOpPlan()
    {
        var snapshot = Snapshot();
        var plan = Planner().CreatePlan(new TrackerSynchronizationRequest(
            ProjectId,
            Evidence(snapshot),
            new TrackerSynchronizationDesiredState()));

        Assert.Empty(plan.Operations);
        Assert.True(plan.IsExecutable is false);
        Assert.Empty(plan.Conflicts);
    }

    [Fact]
    public void StatusDifference_ProducesOneBoundedOperation()
    {
        var snapshot = Snapshot();
        var plan = Planner().CreatePlan(new TrackerSynchronizationRequest(
            ProjectId,
            Evidence(snapshot),
            new TrackerSynchronizationDesiredState(statusId: "31")));

        var operation = Assert.Single(plan.Operations);
        Assert.Equal(TrackerMutationKind.TransitionStatus, operation.Kind);
        Assert.Equal("31", operation.StatusId);
        Assert.True(plan.IsExecutable);
    }

    [Fact]
    public void DesiredLinkAddition_IsDeterministicAndBounded()
    {
        var snapshot = Snapshot();
        var related = new TrackerWorkItemIdentity(TrackerProviderKind.Jira, "APO", "APO-48", "10876");
        var link = new TrackerDependencyLink(
            snapshot.Identity,
            related,
            "blocks",
            TrackerLinkDirection.Outward,
            isDependency: true);

        var plan = Planner().CreatePlan(new TrackerSynchronizationRequest(
            ProjectId,
            Evidence(snapshot),
            new TrackerSynchronizationDesiredState(linksToAdd: [link])));

        var operation = Assert.Single(plan.Operations);
        Assert.Equal(TrackerMutationKind.AddDependencyLink, operation.Kind);
        Assert.Equal(link.Source.CanonicalIdentity, operation.Target.WorkItem.CanonicalIdentity);
        Assert.Equal(link.Target.CanonicalIdentity, operation.Target.RelatedWorkItem!.CanonicalIdentity);
        Assert.Equal(link.Relationship, operation.Target.LinkType);
    }

    [Fact]
    public void StaleEvidence_BlocksExecutableMutationPlan()
    {
        var snapshot = Snapshot();
        var evidence = new TrackerReadResult<TrackerWorkItemSnapshot>(
            TrackerEvidenceState.Stale,
            snapshot.Project,
            snapshot.Identity,
            Now,
            value: null,
            lastKnownValue: snapshot,
            limitations: ["fresh read failed"],
            errorMessage: "tracker unavailable");

        var plan = Planner().CreatePlan(new TrackerSynchronizationRequest(
            ProjectId,
            evidence,
            new TrackerSynchronizationDesiredState(statusId: "31")));

        Assert.False(plan.IsExecutable);
        Assert.Empty(plan.Operations);
        Assert.Contains(plan.Blockers, value => value.Contains("fresh", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UnsupportedDesiredChange_IsNotConvertedToGenericPatch()
    {
        var plan = Planner().CreatePlan(new TrackerSynchronizationRequest(
            ProjectId,
            Evidence(Snapshot()),
            new TrackerSynchronizationDesiredState(unsupportedChanges: ["summary"] )));

        Assert.False(plan.IsExecutable);
        Assert.Contains("summary", Assert.Single(plan.UnsupportedChanges));
    }

    [Fact]
    public async Task ProjectMismatch_IsRejectedBeforeAdapterInvocation()
    {
        var adapter = new FakeAdapter(TrackerProviderKind.Jira);
        var service = new TrackerSynchronizationService(
            new FakeProjectRepository(CreateProject(id: OtherProjectId)),
            new WorkItemTrackerAdapterResolver([adapter]));
        var snapshot = Snapshot();
        var operation = new TrackerSynchronizationOperation(
            TrackerMutationKind.TransitionStatus,
            new TrackerMutationTarget(snapshot.Identity),
            snapshot.StateFingerprint,
            statusId: "31");
        var plan = new TrackerSynchronizationPlan(ProjectId, TrackerEvidenceState.Available, snapshot.StateFingerprint, [operation]);
        var authority = Authority(operation, snapshot.StateFingerprint);

        var result = await service.ExecuteAsync(plan, operation, authority);

        Assert.Equal(TrackerMutationOutcome.InvalidAuthority, result.Outcome);
        Assert.Equal(0, adapter.MutationCalls);
    }

    [Fact]
    public void Authority_BindsProjectTargetKindAndContentAndExpires()
    {
        var snapshot = Snapshot();
        var target = new TrackerMutationTarget(snapshot.Identity);
        var authority = new TrackerMutationAuthority(
            Guid.NewGuid(),
            ProjectId,
            snapshot.Project,
            target,
            TrackerMutationKind.TransitionStatus,
            snapshot.StateFingerprint,
            "actor",
            "run-1",
            Now,
            Now.AddMinutes(5),
            contentIdentity: "31");
        var request = new TrackerMutationRequest(ProjectId, snapshot.Project, TrackerMutationKind.TransitionStatus, target, authority, statusId: "31");

        Assert.True(authority.Matches(request, Now.AddMinutes(1)));
        Assert.False(authority.Matches(request, Now.AddMinutes(5)));
        Assert.False(authority.Matches(new TrackerMutationRequest(ProjectId, snapshot.Project, TrackerMutationKind.AddComment, target, authority, commentBody: "comment"), Now));
        Assert.Throws<ArgumentException>(() => new TrackerMutationAuthority(
            authority.AuthorityId,
            ProjectId,
            snapshot.Project,
            target,
            TrackerMutationKind.TransitionStatus,
            snapshot.StateFingerprint,
            "actor",
            "run-1",
            Now,
            Now.AddMinutes(5),
            contentIdentity: "31",
            contentHash: "tampered"));
    }

    [Fact]
    public async Task CancellationBeforeExecution_PerformsNoAdapterCall()
    {
        var adapter = new FakeAdapter(TrackerProviderKind.Jira);
        var service = new TrackerSynchronizationService(
            new FakeProjectRepository(CreateProject()),
            new WorkItemTrackerAdapterResolver([adapter]));
        var snapshot = Snapshot();
        var operation = new TrackerSynchronizationOperation(
            TrackerMutationKind.TransitionStatus,
            new TrackerMutationTarget(snapshot.Identity),
            snapshot.StateFingerprint,
            statusId: "31");
        var plan = new TrackerSynchronizationPlan(ProjectId, TrackerEvidenceState.Available, snapshot.StateFingerprint, [operation]);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var result = await service.ExecuteAsync(plan, operation, Authority(operation, snapshot.StateFingerprint), cancellation.Token);

        Assert.Equal(TrackerMutationOutcome.Cancelled, result.Outcome);
        Assert.Equal(0, adapter.MutationCalls);
    }

    private static readonly Guid ProjectId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherProjectId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static TrackerSynchronizationService Planner() =>
        new(new FakeProjectRepository(CreateProject()), new WorkItemTrackerAdapterResolver([]));

    private static TrackerReadResult<TrackerWorkItemSnapshot> Evidence(TrackerWorkItemSnapshot snapshot) =>
        new(TrackerEvidenceState.Available, snapshot.Project, snapshot.Identity, Now, snapshot);

    private static TrackerWorkItemSnapshot Snapshot() =>
        new(
            new TrackerWorkItemIdentity(TrackerProviderKind.Jira, "APO", "APO-47", "10875"),
            new TrackerProjectIdentity(TrackerProviderKind.Jira, "APO", new Uri("https://jira.example/")),
            "Task",
            "Tracker sync",
            new TrackerStatusSnapshot("11", "To Do", "new"),
            Now);

    private static TrackerMutationAuthority Authority(TrackerSynchronizationOperation operation, string expectedState) =>
        new(
            Guid.NewGuid(),
            ProjectId,
            Snapshot().Project,
            operation.Target,
            operation.Kind,
            expectedState,
            "actor",
            "run-1",
            Now,
            Now.AddMinutes(5),
            operation.StatusId);

    private static Project CreateProject(
        Guid? id = null,
        string? trackerType = "Jira",
        string? trackerId = "APO",
        bool includeBaseUri = true,
        IReadOnlyDictionary<string, string?>? metadata = null) =>
        new(
            id ?? ProjectId,
            "APO",
            @"D:\APO",
            null,
            ProjectStatus.Active,
            Now,
            Now,
            trackerType: trackerType,
            trackerId: trackerId,
            trackerMetadata: metadata ?? (includeBaseUri
                ? new Dictionary<string, string?>
                {
                    [TrackerMetadataKeys.BaseUri] = "https://jira.example/",
                    [TrackerMetadataKeys.ProjectKey] = trackerId,
                    [TrackerMetadataKeys.AuthReference] = "Jira:APO"
                }
                : null));

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeProjectRepository(Project project) : IProjectRepository
    {
        public Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Project>>([project]);

        public Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Project?>(project.Id == projectId ? project : null);

        public Task UpsertAsync(Project project, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeAdapter(TrackerProviderKind provider) : IWorkItemTrackerAdapter
    {
        public TrackerProviderKind Provider => provider;
        public int MutationCalls { get; private set; }

        public Task<TrackerReadResult<IReadOnlyList<TrackerWorkItemSnapshot>>> DiscoverAsync(
            TrackerConfiguration configuration,
            TrackerWorkItemQuery query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new TrackerReadResult<IReadOnlyList<TrackerWorkItemSnapshot>>(
                TrackerEvidenceState.Unsupported,
                configuration.Identity,
                null,
                Now));

        public Task<TrackerReadResult<TrackerWorkItemSnapshot>> ReadAsync(
            TrackerConfiguration configuration,
            TrackerWorkItemIdentity target,
            TrackerWorkItemSnapshot? lastKnownValue = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new TrackerReadResult<TrackerWorkItemSnapshot>(
                TrackerEvidenceState.Unsupported,
                configuration.Identity,
                target,
                Now,
                lastKnownValue: lastKnownValue));

        public Task<TrackerMutationResult> MutateAsync(
            TrackerConfiguration configuration,
            TrackerMutationRequest request,
            CancellationToken cancellationToken = default)
        {
            MutationCalls++;
            return Task.FromResult(new TrackerMutationResult(TrackerMutationOutcome.Succeeded));
        }
    }
}
