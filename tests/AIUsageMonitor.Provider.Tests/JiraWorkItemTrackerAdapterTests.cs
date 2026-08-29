using System.Net;
using System.Text.Json;
using AIUsageMonitor.Application.Projects;
using AIUsageMonitor.Application.Security;
using AIUsageMonitor.Application.Trackers;
using AIUsageMonitor.Providers.Jira;

namespace AIUsageMonitor.Provider.Tests;

public sealed class JiraWorkItemTrackerAdapterTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 29, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid ProjectId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task Read_NormalizesCoreHierarchyLinksAndComments()
    {
        var handler = DelegateHttpMessageHandler.Json(IssueJson(
            comments: [Comment("300", "trace")],
            links: [BlocksLink()]));
        var adapter = CreateAdapter(handler, out _);

        var result = await adapter.ReadAsync(Configuration(), WorkItem());

        Assert.Equal(TrackerEvidenceState.Available, result.State);
        Assert.Equal(ProjectId, result.ProjectId);
        var snapshot = Assert.IsType<TrackerWorkItemSnapshot>(result.Value);
        Assert.Equal("APO-47", snapshot.Identity.KeyOrId);
        Assert.Equal("10001", snapshot.Identity.RemoteId);
        Assert.Equal("APO-1", snapshot.Parent!.Parent.KeyOrId);
        var link = Assert.Single(snapshot.Links);
        Assert.Equal("blocks", link.Relationship);
        Assert.Equal("10000", link.RemoteTypeId);
        Assert.Equal("Blocks", link.RemoteTypeName);
        Assert.True(link.IsDependency);
        Assert.Equal(TrackerLinkDirection.Outward, link.Direction);
        Assert.Equal("trace", Assert.Single(snapshot.Comments).Body);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task Discover_UsesBoundedJqlSearchPagination()
    {
        var responses = 0;
        var handler = new DelegateHttpMessageHandler((request, _) =>
        {
            responses++;
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.EndsWith("/rest/api/3/search/jql", request.RequestUri!.AbsolutePath, StringComparison.Ordinal);
            return Task.FromResult(DelegateHttpMessageHandler.JsonResponse(
                JsonSerializer.Serialize(new
                {
                    issues = new[] { JsonSerializer.Deserialize<JsonElement>(IssueJson()) },
                    isLast = true
                })));
        });
        var adapter = CreateAdapter(handler, out _);

        var result = await adapter.DiscoverAsync(Configuration(), new TrackerWorkItemQuery(ProjectId, maxResults: 1, maxPages: 1));

        Assert.Equal(TrackerEvidenceState.Available, result.State);
        Assert.Equal(ProjectId, result.ProjectId);
        Assert.Single(result.Value!);
        Assert.Equal(1, responses);
    }

    [Fact]
    public async Task Discover_StopsAtMaximumPageBound()
    {
        var handler = new DelegateHttpMessageHandler((request, _) =>
        {
            return Task.FromResult(DelegateHttpMessageHandler.JsonResponse(
                JsonSerializer.Serialize(new
                {
                    issues = new[] { JsonSerializer.Deserialize<JsonElement>(IssueJson()) },
                    isLast = false,
                    nextPageToken = "next"
                })));
        });
        var adapter = CreateAdapter(handler, out _);

        var result = await adapter.DiscoverAsync(Configuration(), new TrackerWorkItemQuery(ProjectId, maxResults: 100, maxPages: 2));

        Assert.Equal(TrackerEvidenceState.Partial, result.State);
        Assert.Equal(2, handler.RequestCount);
        Assert.Contains(result.Limitations, value => value.Contains("page bound", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Read_MapsHttpFailureStatesWithoutServerDetails()
    {
        foreach (var (statusCode, expected) in new[]
        {
            (HttpStatusCode.Unauthorized, TrackerEvidenceState.AuthenticationRequired),
            (HttpStatusCode.Forbidden, TrackerEvidenceState.PermissionDenied),
            (HttpStatusCode.NotFound, TrackerEvidenceState.NotFound),
            ((HttpStatusCode)429, TrackerEvidenceState.RateLimited),
            (HttpStatusCode.InternalServerError, TrackerEvidenceState.Unavailable)
        })
        {
            var handler = DelegateHttpMessageHandler.Json("{\"error\":\"Bearer TOP-SECRET\"}", statusCode);
            var adapter = CreateAdapter(handler, out _);

            var result = await adapter.ReadAsync(Configuration(), WorkItem());

            Assert.Equal(expected, result.State);
            Assert.Equal(ProjectId, result.ProjectId);
            Assert.DoesNotContain("TOP-SECRET", result.ErrorMessage ?? string.Empty, StringComparison.Ordinal);
            Assert.Equal(1, handler.RequestCount);
        }
    }

    [Fact]
    public async Task Read_MalformedAndOversizedResponsesAreInvalidAndBounded()
    {
        var malformed = CreateAdapter(DelegateHttpMessageHandler.Json("not-json"), out _);
        var malformedResult = await malformed.ReadAsync(Configuration(), WorkItem());
        Assert.Equal(TrackerEvidenceState.InvalidResponse, malformedResult.State);

        var oversized = CreateAdapter(DelegateHttpMessageHandler.Json(new string('x', TrackerLimits.MaxResponseBytes + 1)), out _);
        var oversizedResult = await oversized.ReadAsync(Configuration(), WorkItem());
        Assert.Equal(TrackerEvidenceState.InvalidResponse, oversizedResult.State);
        Assert.Contains("bounded", oversizedResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Read_PartialAuxiliaryDataPreservesCoreSnapshot()
    {
        var handler = DelegateHttpMessageHandler.Json(IssueJson(
            comments: [new { id = "bad", body = new { unsupported = true } }],
            links: [new { id = "bad", type = new { outward = "blocks" } }]));
        var adapter = CreateAdapter(handler, out _);

        var result = await adapter.ReadAsync(Configuration(), WorkItem());

        Assert.Equal(TrackerEvidenceState.Partial, result.State);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Limitations);
        Assert.Empty(result.Value!.Comments);
    }

    [Fact]
    public async Task Read_CallerCancellationIsTyped()
    {
        var handler = DelegateHttpMessageHandler.Json(IssueJson());
        var adapter = CreateAdapter(handler, out _);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var result = await adapter.ReadAsync(Configuration(), WorkItem(), cancellationToken: cancellation.Token);

        Assert.Equal(TrackerEvidenceState.Cancelled, result.State);
        Assert.Equal(ProjectId, result.ProjectId);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task CommentMutation_UsesFreshReadSinglePostIndependentVerificationAndSafeReceipt()
    {
        var calls = new List<HttpRequestMessage>();
        var handler = new DelegateHttpMessageHandler(async (request, _) =>
        {
            calls.Add(request);
            if (request.Method == HttpMethod.Get)
            {
                var withComment = calls.Count >= 3;
                return DelegateHttpMessageHandler.JsonResponse(IssueJson(comments: withComment ? [Comment("301", "trace")] : []));
            }

            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.EndsWith("/comment", request.RequestUri!.AbsolutePath, StringComparison.Ordinal);
            var body = await request.Content!.ReadAsStringAsync();
            Assert.Contains("\"type\":\"doc\"", body, StringComparison.Ordinal);
            Assert.Contains("trace", body, StringComparison.Ordinal);
            return DelegateHttpMessageHandler.JsonResponse("{\"id\":\"301\"}", HttpStatusCode.Created);
        });
        var audit = new MemoryAuditRepository();
        var adapter = CreateAdapter(handler, out _ , audit);
        var current = Assert.IsType<TrackerWorkItemSnapshot>((await adapter.ReadAsync(Configuration(), WorkItem())).Value);
        var target = new TrackerMutationTarget(current.Identity);
        var authority = Authority(current, target, TrackerMutationKind.AddComment, TrackerCommentMetadata.ComputeBodyHash("trace"));

        var result = await adapter.MutateAsync(Configuration(), new TrackerMutationRequest(ProjectId, Configuration().Identity, TrackerMutationKind.AddComment, target, authority, commentBody: "trace"));

        Assert.Equal(TrackerMutationOutcome.Succeeded, result.Outcome);
        Assert.Equal(4, calls.Count);
        var receipt = Assert.Single(audit.Receipts);
        Assert.Equal(TrackerMutationOutcome.Succeeded, receipt.FinalOutcome);
        Assert.Equal(TrackerCommentMetadata.ComputeBodyHash("trace"), receipt.BodyHash);
        Assert.DoesNotContain("trace", JsonSerializer.Serialize(receipt), StringComparison.Ordinal);
    }

    [Fact]
    public async Task StatusMutation_ValidatesTransitionAndPostVerifies()
    {
        var calls = new List<HttpRequestMessage>();
        var handler = new DelegateHttpMessageHandler((request, _) =>
        {
            calls.Add(request);
            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath.EndsWith("/transitions", StringComparison.Ordinal))
            {
                return Task.FromResult(DelegateHttpMessageHandler.JsonResponse("{\"transitions\":[{\"id\":\"21\",\"to\":{\"id\":\"31\",\"name\":\"Done\"},\"isAvailable\":true}]}"));
            }

            if (request.Method == HttpMethod.Post)
            {
                Assert.EndsWith("/transitions", request.RequestUri!.AbsolutePath, StringComparison.Ordinal);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }

            return Task.FromResult(DelegateHttpMessageHandler.JsonResponse(IssueJson(
                statusId: calls.Count >= 4 ? "31" : "11",
                statusName: calls.Count >= 4 ? "Done" : "To Do")));
        });
        var audit = new MemoryAuditRepository();
        var adapter = CreateAdapter(handler, out _ , audit);
        var current = Snapshot();
        var target = new TrackerMutationTarget(current.Identity);
        var authority = Authority(current, target, TrackerMutationKind.TransitionStatus, "31");

        var result = await adapter.MutateAsync(Configuration(), new TrackerMutationRequest(ProjectId, Configuration().Identity, TrackerMutationKind.TransitionStatus, target, authority, statusId: "31"));

        Assert.Equal(TrackerMutationOutcome.Succeeded, result.Outcome);
        Assert.Equal(4, calls.Count);
        Assert.Equal(TrackerMutationKind.TransitionStatus, audit.Receipts[0].MutationKind);
    }

    [Fact]
    public async Task RemoteDriftBeforeMutation_PerformsNoStateChangingCall()
    {
        var handler = DelegateHttpMessageHandler.Json(IssueJson(statusId: "31", statusName: "Done"));
        var adapter = CreateAdapter(handler, out _);
        var current = Snapshot();
        var target = new TrackerMutationTarget(current.Identity);

        var result = await adapter.MutateAsync(Configuration(), new TrackerMutationRequest(
            ProjectId,
            Configuration().Identity,
            TrackerMutationKind.TransitionStatus,
            target,
            Authority(current, target, TrackerMutationKind.TransitionStatus, "31"),
            statusId: "31"));

        Assert.Equal(TrackerMutationOutcome.Conflict, result.Outcome);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task DependencyLinkMutation_UsesExplicitIssueLinkAndVerificationRead()
    {
        var fake = new JiraIssueLinkRoundTripFake("APO-47");
        var handler = new DelegateHttpMessageHandler(fake.HandleAsync);
        var audit = new MemoryAuditRepository();
        var adapter = CreateAdapter(handler, out _, audit);
        var current = Snapshot();
        var related = new TrackerWorkItemIdentity(TrackerProviderKind.Jira, "APO", "APO-48", "10002");
        var target = new TrackerMutationTarget(
            current.Identity,
            related,
            "Blocks",
            TrackerLinkDirection.Outward,
            remoteTypeId: "10000",
            relationship: "blocks");

        var result = await adapter.MutateAsync(Configuration(), new TrackerMutationRequest(
            ProjectId,
            Configuration().Identity,
            TrackerMutationKind.AddDependencyLink,
            target,
            Authority(current, target, TrackerMutationKind.AddDependencyLink, contentIdentity: null)));

        Assert.Equal(TrackerMutationOutcome.Succeeded, result.Outcome);
        Assert.Equal(1, fake.PostCount);
        Assert.Equal("APO-47", fake.PostedInwardIssueKey);
        Assert.Equal("APO-48", fake.PostedOutwardIssueKey);
        Assert.Equal("10000", fake.PostedTypeId);
        Assert.Null(fake.PostedTypeName);
        Assert.Equal(2, fake.ReadIssuePaths.Count);
        Assert.All(fake.ReadIssuePaths, path => Assert.EndsWith("/APO-47", path, StringComparison.Ordinal));

        var verified = await adapter.ReadAsync(Configuration(), WorkItem());
        var link = Assert.Single(Assert.IsType<TrackerWorkItemSnapshot>(verified.Value).Links);
        Assert.Equal("APO-47", link.Source.KeyOrId);
        Assert.Equal("APO-48", link.Target.KeyOrId);
        Assert.Equal(TrackerLinkDirection.Outward, link.Direction);
        Assert.Equal("blocks", link.Relationship);
        Assert.Equal("10000", link.RemoteTypeId);
        Assert.Equal("Blocks", link.RemoteTypeName);
        Assert.Equal(3, fake.ReadIssuePaths.Count);
        Assert.Single(audit.Receipts);
    }

    [Fact]
    public async Task InwardDependencyLinkThroughSynchronizationService_UsesCurrentIssueForReadPostAndVerification()
    {
        var fake = new JiraIssueLinkRoundTripFake("APO-47");
        var handler = new DelegateHttpMessageHandler(fake.HandleAsync);
        var audit = new MemoryAuditRepository();
        var adapter = CreateAdapter(handler, out _, audit);
        var evidence = await adapter.ReadAsync(Configuration(), WorkItem());
        var current = Assert.IsType<TrackerWorkItemSnapshot>(evidence.Value);
        var related = new TrackerWorkItemIdentity(TrackerProviderKind.Jira, "APO", "APO-48", "10002");
        var desiredLink = new TrackerDependencyLink(
            related,
            current.Identity,
            "is blocked by",
            TrackerLinkDirection.Inward,
            isDependency: true,
            remoteTypeId: "10000",
            remoteTypeName: "Blocks");
        var service = new TrackerSynchronizationService(
            new TestProjectRepository(ConfiguredProject()),
            new WorkItemTrackerAdapterResolver([adapter]));

        var plan = service.CreatePlan(new TrackerSynchronizationRequest(
            ProjectId,
            evidence,
            new TrackerSynchronizationDesiredState(linksToAdd: [desiredLink])));

        var operation = Assert.Single(plan.Operations);
        Assert.Equal(current.Identity.CanonicalIdentity, operation.Target.WorkItem.CanonicalIdentity);
        Assert.Equal(related.CanonicalIdentity, operation.Target.RelatedWorkItem!.CanonicalIdentity);
        var result = await service.ExecuteAsync(
            plan,
            operation,
            Authority(current, operation.Target, TrackerMutationKind.AddDependencyLink, contentIdentity: null));

        Assert.Equal(TrackerMutationOutcome.Succeeded, result.Outcome);
        Assert.Equal(1, fake.PostCount);
        Assert.Equal("APO-48", fake.PostedInwardIssueKey);
        Assert.Equal("APO-47", fake.PostedOutwardIssueKey);
        Assert.Equal("10000", fake.PostedTypeId);
        Assert.Null(fake.PostedTypeName);
        Assert.Equal(3, fake.ReadIssuePaths.Count);
        Assert.All(fake.ReadIssuePaths, path => Assert.EndsWith("/APO-47", path, StringComparison.Ordinal));

        var verified = await adapter.ReadAsync(Configuration(), WorkItem());
        var link = Assert.Single(Assert.IsType<TrackerWorkItemSnapshot>(verified.Value).Links);
        Assert.Equal("APO-48", link.Source.KeyOrId);
        Assert.Equal("APO-47", link.Target.KeyOrId);
        Assert.Equal(TrackerLinkDirection.Inward, link.Direction);
        Assert.Equal("is blocked by", link.Relationship);
        Assert.Equal("10000", link.RemoteTypeId);
        Assert.Equal("Blocks", link.RemoteTypeName);
        Assert.Equal(4, fake.ReadIssuePaths.Count);
        Assert.Single(audit.Receipts);
    }

    [Fact]
    public async Task DependencyLinkDirections_UseOppositeJiraEndpointAssignments()
    {
        var outward = await ExecuteDependencyLinkMutationAsync(TrackerLinkDirection.Outward);
        var inward = await ExecuteDependencyLinkMutationAsync(TrackerLinkDirection.Inward);

        Assert.Equal("APO-47", outward.PostedInwardIssueKey);
        Assert.Equal("APO-48", outward.PostedOutwardIssueKey);
        Assert.Equal(outward.PostedInwardIssueKey, inward.PostedOutwardIssueKey);
        Assert.Equal(outward.PostedOutwardIssueKey, inward.PostedInwardIssueKey);
    }

    [Fact]
    public async Task CommentCredentialCancellationBeforePost_IsTypedAndNonMutating()
    {
        using var cancellation = new CancellationTokenSource();
        var credentials = new CancellingCredentialStore(cancellation);
        var handler = DelegateHttpMessageHandler.Json(IssueJson());
        var audit = new MemoryAuditRepository();
        var adapter = CreateAdapter(handler, credentials, audit);
        var current = Snapshot();
        var target = new TrackerMutationTarget(current.Identity);

        var result = await adapter.MutateAsync(
            Configuration(),
            new TrackerMutationRequest(
                ProjectId,
                Configuration().Identity,
                TrackerMutationKind.AddComment,
                target,
                Authority(current, target, TrackerMutationKind.AddComment, TrackerCommentMetadata.ComputeBodyHash("trace")),
                commentBody: "trace"),
            cancellation.Token);

        Assert.Equal(TrackerMutationOutcome.Cancelled, result.Outcome);
        Assert.False(result.MayHaveModifiedRemote);
        Assert.Equal(1, handler.RequestCount);
        Assert.Empty(audit.Receipts);
    }

    [Fact]
    public async Task DependencyLinkCredentialCancellationBeforePost_IsTypedAndNonMutating()
    {
        using var cancellation = new CancellationTokenSource();
        var credentials = new CancellingCredentialStore(cancellation);
        var handler = DelegateHttpMessageHandler.Json(IssueJson());
        var audit = new MemoryAuditRepository();
        var adapter = CreateAdapter(handler, credentials, audit);
        var current = Snapshot();
        var related = new TrackerWorkItemIdentity(TrackerProviderKind.Jira, "APO", "APO-48", "10002");
        var target = new TrackerMutationTarget(
            current.Identity,
            related,
            "Blocks",
            TrackerLinkDirection.Outward,
            remoteTypeId: "10000",
            relationship: "blocks");

        var result = await adapter.MutateAsync(
            Configuration(),
            new TrackerMutationRequest(
                ProjectId,
                Configuration().Identity,
                TrackerMutationKind.AddDependencyLink,
                target,
                Authority(current, target, TrackerMutationKind.AddDependencyLink, contentIdentity: null)),
            cancellation.Token);

        Assert.Equal(TrackerMutationOutcome.Cancelled, result.Outcome);
        Assert.False(result.MayHaveModifiedRemote);
        Assert.Equal(1, handler.RequestCount);
        Assert.Empty(audit.Receipts);
    }

    [Fact]
    public async Task TransitionCredentialCancellationBeforeDiscoveryGet_IsTypedAndNonMutating()
    {
        using var cancellation = new CancellationTokenSource();
        var credentials = new CancellingCredentialStore(cancellation);
        var handler = DelegateHttpMessageHandler.Json(IssueJson());
        var audit = new MemoryAuditRepository(rejectCancelledTokens: true);
        var adapter = CreateAdapter(handler, credentials, audit);
        var current = Snapshot();
        var target = new TrackerMutationTarget(current.Identity);

        var result = await adapter.MutateAsync(
            Configuration(),
            new TrackerMutationRequest(
                ProjectId,
                Configuration().Identity,
                TrackerMutationKind.TransitionStatus,
                target,
                Authority(current, target, TrackerMutationKind.TransitionStatus, contentIdentity: "31"),
                statusId: "31"),
            cancellation.Token);

        Assert.Equal(TrackerMutationOutcome.Cancelled, result.Outcome);
        Assert.False(result.MayHaveModifiedRemote);
        Assert.Equal(1, handler.RequestCount);
        Assert.Empty(audit.Receipts);
    }

    [Fact]
    public async Task TransitionGetCancellationBeforePost_IsTypedAndNonMutating()
    {
        using var cancellation = new CancellationTokenSource();
        var transitionGets = 0;
        var posts = 0;
        var handler = new DelegateHttpMessageHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath.EndsWith("/transitions", StringComparison.Ordinal))
            {
                transitionGets++;
                cancellation.Cancel();
                return Task.FromException<HttpResponseMessage>(new OperationCanceledException(cancellation.Token));
            }

            if (request.Method == HttpMethod.Get)
            {
                return Task.FromResult(DelegateHttpMessageHandler.JsonResponse(IssueJson()));
            }

            posts++;
            return Task.FromResult(DelegateHttpMessageHandler.JsonResponse("", HttpStatusCode.NoContent));
        });
        var audit = new MemoryAuditRepository(rejectCancelledTokens: true);
        var adapter = CreateAdapter(handler, out _, audit);
        var current = Snapshot();
        var target = new TrackerMutationTarget(current.Identity);

        var result = await adapter.MutateAsync(
            Configuration(),
            new TrackerMutationRequest(
                ProjectId,
                Configuration().Identity,
                TrackerMutationKind.TransitionStatus,
                target,
                Authority(current, target, TrackerMutationKind.TransitionStatus, contentIdentity: "31"),
                statusId: "31"),
            cancellation.Token);

        Assert.Equal(TrackerMutationOutcome.Cancelled, result.Outcome);
        Assert.False(result.MayHaveModifiedRemote);
        Assert.Equal(1, transitionGets);
        Assert.Equal(0, posts);
        Assert.Empty(audit.Receipts);
    }

    [Fact]
    public async Task SelfDependencyLink_IsRejectedBeforeRemoteCall()
    {
        var handler = DelegateHttpMessageHandler.Json(IssueJson());
        var adapter = CreateAdapter(handler, out _);
        var current = Snapshot();
        var target = new TrackerMutationTarget(
            current.Identity,
            current.Identity,
            "Blocks",
            TrackerLinkDirection.Outward,
            remoteTypeId: "10000",
            relationship: "blocks");

        var result = await adapter.MutateAsync(
            Configuration(),
            new TrackerMutationRequest(
                ProjectId,
                Configuration().Identity,
                TrackerMutationKind.AddDependencyLink,
                target,
                Authority(current, target, TrackerMutationKind.AddDependencyLink, contentIdentity: null)));

        Assert.Equal(TrackerMutationOutcome.InvalidAuthority, result.Outcome);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task CustomLinkTypeIdentitySurvivesReadAndMutation()
    {
        var calls = 0;
        var handler = new DelegateHttpMessageHandler(async (request, _) =>
        {
            calls++;
            if (request.Method == HttpMethod.Get)
            {
                return DelegateHttpMessageHandler.JsonResponse(IssueJson(
                    links: calls == 1 || calls >= 3 ? [CustomDependsLink()] : []));
            }

            var payload = await request.Content!.ReadAsStringAsync();
            Assert.Contains("\"id\":\"10001\"", payload, StringComparison.Ordinal);
            Assert.DoesNotContain("\"name\":\"depends on\"", payload, StringComparison.Ordinal);
            return DelegateHttpMessageHandler.JsonResponse("", HttpStatusCode.Created);
        });
        var audit = new MemoryAuditRepository();
        var adapter = CreateAdapter(handler, out _, audit);
        var current = Snapshot();
        var related = new TrackerWorkItemIdentity(TrackerProviderKind.Jira, "APO", "APO-48", "10002");
        var target = new TrackerMutationTarget(
            current.Identity,
            related,
            "Depends",
            TrackerLinkDirection.Outward,
            remoteTypeId: "10001",
            relationship: "depends on");

        var read = await adapter.ReadAsync(Configuration(), WorkItem());
        var readLink = Assert.Single(Assert.IsType<TrackerWorkItemSnapshot>(read.Value).Links);
        Assert.Equal("10001", readLink.RemoteTypeId);
        Assert.Equal("Depends", readLink.RemoteTypeName);
        Assert.Equal("depends on", readLink.Relationship);

        var result = await adapter.MutateAsync(Configuration(), new TrackerMutationRequest(
            ProjectId,
            Configuration().Identity,
            TrackerMutationKind.AddDependencyLink,
            target,
            Authority(current, target, TrackerMutationKind.AddDependencyLink, contentIdentity: null)));

        Assert.Equal(TrackerMutationOutcome.Succeeded, result.Outcome);
        Assert.Single(audit.Receipts);
    }

    [Fact]
    public async Task AmbiguousMutationTimeout_IsNotRetriedAndRequiresReconciliation()
    {
        var posts = 0;
        var handler = new DelegateHttpMessageHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return Task.FromResult(DelegateHttpMessageHandler.JsonResponse(IssueJson()));
            }

            posts++;
            return Task.FromException<HttpResponseMessage>(new TaskCanceledException("ambiguous"));
        });
        var audit = new MemoryAuditRepository();
        var adapter = CreateAdapter(handler, out _ , audit);
        var current = Snapshot();
        var target = new TrackerMutationTarget(current.Identity);

        var result = await adapter.MutateAsync(Configuration(), new TrackerMutationRequest(
            ProjectId,
            Configuration().Identity,
            TrackerMutationKind.AddComment,
            target,
            Authority(current, target, TrackerMutationKind.AddComment, TrackerCommentMetadata.ComputeBodyHash("trace")),
            commentBody: "trace"));

        Assert.Equal(TrackerMutationOutcome.ReconciliationRequired, result.Outcome);
        Assert.True(result.MayHaveModifiedRemote);
        Assert.Equal(1, posts);
        Assert.Single(audit.Receipts);
    }

    [Fact]
    public async Task CancellationAfterPostUsesIndependentAuditFinalization()
    {
        using var cancellation = new CancellationTokenSource();
        var posts = 0;
        var handler = new DelegateHttpMessageHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return Task.FromResult(DelegateHttpMessageHandler.JsonResponse(IssueJson()));
            }

            posts++;
            cancellation.Cancel();
            return Task.FromResult(DelegateHttpMessageHandler.JsonResponse("{\"id\":\"301\"}", HttpStatusCode.Created));
        });
        var audit = new MemoryAuditRepository(rejectCancelledTokens: true);
        var adapter = CreateAdapter(handler, out _, audit);
        var current = Snapshot();
        var target = new TrackerMutationTarget(current.Identity);

        var result = await adapter.MutateAsync(
            Configuration(),
            new TrackerMutationRequest(
                ProjectId,
                Configuration().Identity,
                TrackerMutationKind.AddComment,
                target,
                Authority(current, target, TrackerMutationKind.AddComment, TrackerCommentMetadata.ComputeBodyHash("trace")),
                commentBody: "trace"),
            cancellation.Token);

        Assert.Equal(TrackerMutationOutcome.ReconciliationRequired, result.Outcome);
        Assert.True(result.MayHaveModifiedRemote);
        Assert.Equal(1, posts);
        Assert.Single(audit.Receipts);
        Assert.Single(audit.Tokens);
        Assert.False(audit.Tokens[^1].IsCancellationRequested);
        Assert.NotEqual(cancellation.Token, audit.Tokens[^1]);
        Assert.Equal(TrackerMutationOutcome.ReconciliationRequired, audit.Receipts[^1].FinalOutcome);
        Assert.Equal(TrackerEvidenceState.Cancelled, audit.Receipts[^1].VerificationState);
    }

    [Fact]
    public async Task OversizedSuccessfulPostRequiresReconciliationAndAudit()
    {
        var posts = 0;
        var handler = new DelegateHttpMessageHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return Task.FromResult(DelegateHttpMessageHandler.JsonResponse(IssueJson()));
            }

            posts++;
            return Task.FromResult(DelegateHttpMessageHandler.JsonResponse(
                new string('x', TrackerLimits.MaxResponseBytes + 1),
                HttpStatusCode.Created));
        });
        var audit = new MemoryAuditRepository();
        var adapter = CreateAdapter(handler, out _, audit);
        var current = Snapshot();
        var target = new TrackerMutationTarget(current.Identity);

        var result = await adapter.MutateAsync(
            Configuration(),
            new TrackerMutationRequest(
                ProjectId,
                Configuration().Identity,
                TrackerMutationKind.AddComment,
                target,
                Authority(current, target, TrackerMutationKind.AddComment, TrackerCommentMetadata.ComputeBodyHash("trace")),
                commentBody: "trace"));

        Assert.Equal(TrackerMutationOutcome.ReconciliationRequired, result.Outcome);
        Assert.True(result.MayHaveModifiedRemote);
        Assert.Equal(1, posts);
        Assert.Single(audit.Receipts);
        Assert.Equal(TrackerMutationOutcome.ReconciliationRequired, audit.Receipts[0].FinalOutcome);
        Assert.True(audit.Receipts[0].MayHaveModifiedRemote);
    }

    [Fact]
    public async Task PostResponseBodyReadFailureRequiresReconciliationAndAudit()
    {
        var posts = 0;
        var handler = new DelegateHttpMessageHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return Task.FromResult(DelegateHttpMessageHandler.JsonResponse(IssueJson()));
            }

            posts++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StreamContent(new ThrowingReadStream())
            });
        });
        var audit = new MemoryAuditRepository();
        var adapter = CreateAdapter(handler, out _, audit);
        var current = Snapshot();
        var target = new TrackerMutationTarget(current.Identity);

        var result = await adapter.MutateAsync(
            Configuration(),
            new TrackerMutationRequest(
                ProjectId,
                Configuration().Identity,
                TrackerMutationKind.AddComment,
                target,
                Authority(current, target, TrackerMutationKind.AddComment, TrackerCommentMetadata.ComputeBodyHash("trace")),
                commentBody: "trace"));

        Assert.Equal(TrackerMutationOutcome.ReconciliationRequired, result.Outcome);
        Assert.True(result.MayHaveModifiedRemote);
        Assert.Equal(1, posts);
        Assert.Single(audit.Receipts);
    }

    [Fact]
    public async Task LinkTypeSubstitutionDoesNotPassAuthorityOrReachJira()
    {
        var handler = DelegateHttpMessageHandler.Json(IssueJson());
        var adapter = CreateAdapter(handler, out _);
        var current = Snapshot();
        var related = new TrackerWorkItemIdentity(TrackerProviderKind.Jira, "APO", "APO-48", "10002");
        var authorizedTarget = new TrackerMutationTarget(current.Identity, related, "Blocks", TrackerLinkDirection.Outward, relationship: "blocks");
        var substitutedTarget = new TrackerMutationTarget(current.Identity, related, "Depends", TrackerLinkDirection.Outward, relationship: "blocks");
        var authority = Authority(current, authorizedTarget, TrackerMutationKind.AddDependencyLink, contentIdentity: null);

        var result = await adapter.MutateAsync(Configuration(), new TrackerMutationRequest(
            ProjectId,
            Configuration().Identity,
            TrackerMutationKind.AddDependencyLink,
            substitutedTarget,
            authority));

        Assert.Equal(TrackerMutationOutcome.InvalidAuthority, result.Outcome);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task LocalAuditFailureAfterRemoteSuccess_DoesNotRetryRemoteMutation()
    {
        var calls = 0;
        var handler = new DelegateHttpMessageHandler((request, _) =>
        {
            calls++;
            if (request.Method == HttpMethod.Get)
            {
                return Task.FromResult(DelegateHttpMessageHandler.JsonResponse(IssueJson(comments: calls >= 3 ? [Comment("301", "trace")] : [])));
            }

            return Task.FromResult(DelegateHttpMessageHandler.JsonResponse("{\"id\":\"301\"}", HttpStatusCode.Created));
        });
        var adapter = CreateAdapter(handler, out _ , new ThrowingAuditRepository());
        var current = Snapshot();
        var target = new TrackerMutationTarget(current.Identity);

        var result = await adapter.MutateAsync(Configuration(), new TrackerMutationRequest(
            ProjectId,
            Configuration().Identity,
            TrackerMutationKind.AddComment,
            target,
            Authority(current, target, TrackerMutationKind.AddComment, TrackerCommentMetadata.ComputeBodyHash("trace")),
            commentBody: "trace"));

        Assert.Equal(TrackerMutationOutcome.ReconciliationRequired, result.Outcome);
        Assert.True(result.MayHaveModifiedRemote);
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task InvalidAuthorityAndCrossProjectTarget_PerformNoRemoteCall()
    {
        var handler = DelegateHttpMessageHandler.Json(IssueJson());
        var adapter = CreateAdapter(handler, out _);
        var current = Snapshot();
        var target = new TrackerMutationTarget(new TrackerWorkItemIdentity(TrackerProviderKind.Jira, "OTHER", "OTHER-1", "2"));

        var result = await adapter.MutateAsync(Configuration(), new TrackerMutationRequest(
            ProjectId,
            Configuration().Identity,
            TrackerMutationKind.AddComment,
            target));

        Assert.Equal(TrackerMutationOutcome.InvalidAuthority, result.Outcome);
        Assert.Equal(0, handler.RequestCount);
    }

    private static JiraWorkItemTrackerAdapter CreateAdapter(
        HttpMessageHandler handler,
        out TestCredentialStore credentials,
        ITrackerMutationAuditRepository? audit = null)
    {
        credentials = new TestCredentialStore();
        credentials.Add("Jira:APO", "Bearer TEST-TOKEN");
        return CreateAdapter(handler, credentials, audit);
    }

    private static JiraWorkItemTrackerAdapter CreateAdapter(
        HttpMessageHandler handler,
        ISecureCredentialStore credentials,
        ITrackerMutationAuditRepository? audit = null) =>
        new(
            new TestClock { UtcNow = Now },
            new TestHttpClientFactory(handler),
            credentials,
            audit ?? new MemoryAuditRepository());

    private static Project ConfiguredProject() => new(
        ProjectId,
        "APO",
        @"D:\APO",
        null,
        ProjectStatus.Active,
        Now,
        Now,
        trackerType: "Jira",
        trackerId: "APO",
        trackerMetadata: new Dictionary<string, string?>
        {
            [TrackerMetadataKeys.BaseUri] = "https://jira.example/",
            [TrackerMetadataKeys.ProjectKey] = "APO",
            [TrackerMetadataKeys.AuthReference] = "Jira:APO"
        });

    private static TrackerConfiguration Configuration()
    {
        var project = ConfiguredProject();
        Assert.True(TrackerConfiguration.TryCreate(project, out var configuration, out _, out var error), error);
        return configuration!;
    }

    private static TrackerWorkItemIdentity WorkItem() =>
        new(TrackerProviderKind.Jira, "APO", "APO-47", "10001");

    private static TrackerWorkItemSnapshot Snapshot() =>
        new(
            WorkItem(),
            Configuration().Identity,
            "Task",
            "Tracker sync",
            new TrackerStatusSnapshot("11", "To Do", "new"),
            Now.AddMinutes(-10),
            new TrackerHierarchyReference(new TrackerWorkItemIdentity(TrackerProviderKind.Jira, "APO", "APO-1", "10000")));

    private static TrackerMutationAuthority Authority(
        TrackerWorkItemSnapshot current,
        TrackerMutationTarget target,
        TrackerMutationKind kind,
        string? contentIdentity) =>
        new(
            Guid.NewGuid(),
            ProjectId,
            current.Project,
            target,
            kind,
            current.StateFingerprint,
            "actor",
            "run-1",
            Now.AddMinutes(-1),
            Now.AddMinutes(5),
            contentIdentity);

    private async Task<JiraIssueLinkRoundTripFake> ExecuteDependencyLinkMutationAsync(TrackerLinkDirection direction)
    {
        var fake = new JiraIssueLinkRoundTripFake("APO-47");
        var adapter = CreateAdapter(new DelegateHttpMessageHandler(fake.HandleAsync), out _);
        var current = Snapshot();
        var related = new TrackerWorkItemIdentity(TrackerProviderKind.Jira, "APO", "APO-48", "10002");
        var target = new TrackerMutationTarget(
            current.Identity,
            related,
            "Blocks",
            direction,
            remoteTypeId: "10000",
            relationship: direction == TrackerLinkDirection.Outward ? "blocks" : "is blocked by");

        var result = await adapter.MutateAsync(Configuration(), new TrackerMutationRequest(
            ProjectId,
            Configuration().Identity,
            TrackerMutationKind.AddDependencyLink,
            target,
            Authority(current, target, TrackerMutationKind.AddDependencyLink, contentIdentity: null)));

        Assert.Equal(TrackerMutationOutcome.Succeeded, result.Outcome);
        Assert.Equal(1, fake.PostCount);
        return fake;
    }

    private static object BlocksLink() => new
    {
        id = "200",
        type = new { id = "10000", name = "Blocks", inward = "is blocked by", outward = "blocks" },
        outwardIssue = new { id = "10002", key = "APO-48", fields = new { project = new { key = "APO" } } }
    };

    private static object CustomDependsLink() => new
    {
        id = "201",
        type = new { id = "10001", name = "Depends", inward = "is dependency for", outward = "depends on" },
        outwardIssue = new { id = "10002", key = "APO-48", fields = new { project = new { key = "APO" } } }
    };

    private static object Comment(string id, string body) => new
    {
        id,
        author = new { displayName = "Sol" },
        created = "2026-08-29T09:00:00.000+0000",
        updated = "2026-08-29T09:00:00.000+0000",
        self = $"https://jira.example/rest/api/3/issue/10001/comment/{id}",
        body = new { type = "doc", version = 1, content = new[] { new { type = "paragraph", content = new[] { new { type = "text", text = body } } } } }
    };

    private static string IssueJson(
        string statusId = "11",
        string statusName = "To Do",
        object[]? comments = null,
        object[]? links = null) =>
        JsonSerializer.Serialize(new
        {
            id = "10001",
            key = "APO-47",
            self = "https://jira.example/rest/api/3/issue/10001",
            updated = "2026-08-29T09:50:00.000+0000",
            fields = new
            {
                summary = "Tracker sync",
                project = new { id = "10000", key = "APO" },
                issuetype = new { id = "10001", name = "Task" },
                status = new { id = statusId, name = statusName, statusCategory = new { key = "new", name = statusName } },
                parent = new { id = "10000", key = "APO-1" },
                issuelinks = links ?? [],
                comment = new { comments = comments ?? [], total = comments?.Length ?? 0 }
            }
        });

    private sealed class JiraIssueLinkRoundTripFake(string currentIssueKey)
    {
        private string CurrentIssueKey { get; } = currentIssueKey;

        public int PostCount { get; private set; }
        public string? PostedInwardIssueKey { get; private set; }
        public string? PostedOutwardIssueKey { get; private set; }
        public string? PostedTypeId { get; private set; }
        public string? PostedTypeName { get; private set; }
        public List<string> ReadIssuePaths { get; } = [];

        public async Task<HttpResponseMessage> HandleAsync(HttpRequestMessage request, CancellationToken _)
        {
            if (request.Method == HttpMethod.Get)
            {
                ReadIssuePaths.Add(request.RequestUri!.AbsolutePath);
                Assert.EndsWith($"/{CurrentIssueKey}", request.RequestUri.AbsolutePath, StringComparison.Ordinal);
                object[] links = PostedInwardIssueKey is null ? [] : [LinkFromPostedEndpoints()];
                return DelegateHttpMessageHandler.JsonResponse(IssueJson(links: links));
            }

            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/rest/api/3/issueLink", request.RequestUri!.AbsolutePath);
            using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            var root = document.RootElement;
            PostedInwardIssueKey = root.GetProperty("inwardIssue").GetProperty("key").GetString();
            PostedOutwardIssueKey = root.GetProperty("outwardIssue").GetProperty("key").GetString();
            var type = root.GetProperty("type");
            PostedTypeId = type.TryGetProperty("id", out var id) ? id.GetString() : null;
            PostedTypeName = type.TryGetProperty("name", out var name) ? name.GetString() : null;
            PostCount++;
            return DelegateHttpMessageHandler.JsonResponse("", HttpStatusCode.Created);
        }

        private object LinkFromPostedEndpoints()
        {
            var type = new
            {
                id = PostedTypeId,
                name = PostedTypeName ?? "Blocks",
                inward = "is blocked by",
                outward = "blocks"
            };

            if (string.Equals(PostedInwardIssueKey, CurrentIssueKey, StringComparison.OrdinalIgnoreCase))
            {
                return new
                {
                    id = "200",
                    type,
                    outwardIssue = LinkedIssue(PostedOutwardIssueKey!)
                };
            }

            Assert.Equal(PostedOutwardIssueKey, CurrentIssueKey, StringComparer.OrdinalIgnoreCase);
            return new
            {
                id = "200",
                type,
                inwardIssue = LinkedIssue(PostedInwardIssueKey!)
            };
        }

        private static object LinkedIssue(string key) => new
        {
            id = "10002",
            key,
            fields = new { project = new { key = "APO" } }
        };
    }

    private sealed class MemoryAuditRepository : ITrackerMutationAuditRepository
    {
        private readonly bool _rejectCancelledTokens;

        public MemoryAuditRepository(bool rejectCancelledTokens = false) => _rejectCancelledTokens = rejectCancelledTokens;
        public List<TrackerMutationReceipt> Receipts { get; } = [];
        public List<CancellationToken> Tokens { get; } = [];
        public Task AppendAsync(TrackerMutationReceipt receipt, CancellationToken cancellationToken = default)
        {
            Tokens.Add(cancellationToken);
            if (_rejectCancelledTokens && cancellationToken.IsCancellationRequested)
            {
                return Task.FromException(new OperationCanceledException(cancellationToken));
            }

            Receipts.Add(receipt);
            return Task.CompletedTask;
        }
    }

    private sealed class CancellingCredentialStore(CancellationTokenSource cancellation) : ISecureCredentialStore
    {
        public int RetrieveCount { get; private set; }

        public Task StoreAsync(string credentialReference, string secret, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<string?> RetrieveAsync(string credentialReference, CancellationToken cancellationToken = default)
        {
            RetrieveCount++;
            if (RetrieveCount == 2)
            {
                cancellation.Cancel();
                return Task.FromException<string?>(new OperationCanceledException(cancellationToken));
            }

            return Task.FromResult<string?>("Bearer TEST-TOKEN");
        }

        public Task RemoveAsync(string credentialReference, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestProjectRepository(Project project) : IProjectRepository
    {
        public Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Project>>([project]);

        public Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Project?>(project.Id == projectId ? project : null);

        public Task UpsertAsync(Project project, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ThrowingAuditRepository : ITrackerMutationAuditRepository
    {
        public Task AppendAsync(TrackerMutationReceipt receipt, CancellationToken cancellationToken = default) =>
            Task.FromException(new IOException("audit unavailable"));
    }

    private sealed class ThrowingReadStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 1;
        public override long Position { get; set; }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new IOException("response body failed");
        public override int Read(Span<byte> buffer) => throw new IOException("response body failed");
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

}
