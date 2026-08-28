using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Handoffs;
using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Projects;
using AIUsageMonitor.Application.Routing;
using AIUsageMonitor.Application.Time;
using AIUsageMonitor.Application.Workspaces;

namespace AIUsageMonitor.Infrastructure.Tests;

public sealed class BoundedExecutionServiceTests
{
    [Fact]
    public void ProductionTimeoutProvider_EnforcesDeterministicHardCeiling()
    {
        var timeout = new ExecutionBudgetTimeoutProvider().GetTimeout(new ExecutionBudgetEnvelope(1, 100_000));

        Assert.Equal(BoundedExecutionLimits.MaxExecutionTimeout, timeout);
    }

    [Fact]
    public async Task SuccessfulRun_RecordsFullSingleStepLifecycle()
    {
        using var harness = ExecutionHarness.Create();

        var result = await harness.Service.ExecuteAsync(harness.Request);

        Assert.Equal(BoundedExecutionStatus.Succeeded, result.Status);
        Assert.Equal(1, harness.Adapter.InvocationCount);
        Assert.Equal(harness.Agent.Id, harness.Adapter.LastRequest!.SelectedAgent.Id);
        Assert.Equal(harness.Receipt.WorkspacePath, harness.Adapter.LastRequest.WorkspacePath);
        Assert.Equal(harness.Contract.Reference, harness.Adapter.LastRequest.Contract.Reference);
        Assert.Equal(harness.Handoff.Reference, harness.Adapter.LastRequest.Handoff.Reference);
        Assert.Equal(harness.Receipt.ContentHash, result.Authority!.WorkspaceReceiptContentHash);
        Assert.Equal(harness.CurrentCheckpoint.Reference.ContentHash, result.Authority.InputRecoveryCheckpointReference.ContentHash);
        Assert.Equal(
            [ExecutionRunStatus.Planned, ExecutionRunStatus.Running, ExecutionRunStatus.Completed],
            harness.History.Runs.Select(value => value.Status));
        Assert.Equal(2, harness.CheckpointService.Created.Count);
        Assert.Equal(RecoveryCheckpointLifecycleState.Waiting, harness.CheckpointService.Created[0].LifecycleState);
        Assert.Equal(RecoveryCheckpointLifecycleState.Ready, result.TerminalCheckpoint!.LifecycleState);
        Assert.Equal(RecoveryNextSafeAction.RunValidation, result.TerminalCheckpoint.NextSafeAction);
        Assert.NotEqual(ExecutionRunStatus.Accepted, harness.History.Runs[^1].Status);
    }

    [Fact]
    public async Task CallerCancellation_IsRecordedWithoutRetryOrCompletedClaim()
    {
        using var harness = ExecutionHarness.Create(hangAdapter: true);
        using var cancellation = new CancellationTokenSource();
        var execution = harness.Service.ExecuteAsync(harness.Request, cancellation.Token);
        await harness.Adapter.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        cancellation.Cancel();

        var result = await execution;

        Assert.Equal(BoundedExecutionStatus.Cancelled, result.Status);
        Assert.Equal(1, harness.Adapter.InvocationCount);
        Assert.True(harness.Adapter.CancellationObserved);
        Assert.DoesNotContain(harness.History.Runs, value => value.Status == ExecutionRunStatus.Completed);
        Assert.Equal(ExecutionRunStatus.Cancelled, harness.History.Runs[^1].Status);
        Assert.Equal(RecoveryCheckpointLifecycleState.Cancelled, result.TerminalCheckpoint!.LifecycleState);
        Assert.Equal(RecoveryNextSafeAction.ResolveBlocker, result.TerminalCheckpoint.NextSafeAction);
    }

    [Fact]
    public async Task ElapsedBudget_RequestsAdapterCancellationAndDoesNotRetry()
    {
        using var harness = ExecutionHarness.Create(
            hangAdapter: true,
            timeoutProvider: new FixedTimeoutProvider(TimeSpan.FromMilliseconds(80)));

        var result = await harness.Service.ExecuteAsync(harness.Request);

        Assert.Equal(BoundedExecutionStatus.TimedOut, result.Status);
        Assert.Equal(1, harness.Adapter.InvocationCount);
        Assert.True(harness.Adapter.CancellationObserved);
        Assert.DoesNotContain(harness.History.Runs, value => value.Status == ExecutionRunStatus.Completed);
        Assert.Equal(ExecutionRunStatus.Failed, harness.History.Runs[^1].Status);
        Assert.Equal(RecoveryCheckpointLifecycleState.Interrupted, result.TerminalCheckpoint!.LifecycleState);
    }

    [Fact]
    public async Task AdapterFailure_IsTerminalAndDoesNotRetry()
    {
        using var harness = ExecutionHarness.Create(
            adapterResult: new ExecutionAdapterResult(ExecutionAdapterOutcome.Failed, "sanitized failure", "adapter-failed"));

        var result = await harness.Service.ExecuteAsync(harness.Request);

        Assert.Equal(BoundedExecutionStatus.AdapterFailed, result.Status);
        Assert.Equal(1, harness.Adapter.InvocationCount);
        Assert.Equal(ExecutionRunStatus.Failed, harness.History.Runs[^1].Status);
        Assert.Equal("sanitized failure", harness.History.Runs[^1].Outcome);
        Assert.DoesNotContain(harness.History.Runs, value => value.Status == ExecutionRunStatus.Completed);
        Assert.Equal(RecoveryCheckpointLifecycleState.Failed, result.TerminalCheckpoint!.LifecycleState);
    }

    [Fact]
    public async Task ReportedToolBudgetOverrun_IsBudgetExceededWithNoSecondAttempt()
    {
        using var harness = ExecutionHarness.Create(
            adapterResult: new ExecutionAdapterResult(
                ExecutionAdapterOutcome.Succeeded,
                "completed",
                usage: new ExecutionAdapterUsageMetrics(toolInvocations: 11)));

        var result = await harness.Service.ExecuteAsync(harness.Request);

        Assert.Equal(BoundedExecutionStatus.BudgetExceeded, result.Status);
        Assert.Equal(1, harness.Adapter.InvocationCount);
        Assert.Equal(ExecutionRunStatus.Failed, harness.History.Runs[^1].Status);
        Assert.Equal(RecoveryCheckpointLifecycleState.Blocked, result.TerminalCheckpoint!.LifecycleState);
        Assert.Contains("budget", result.TerminalCheckpoint.Blockers[0].Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReportedModelTurnBudgetOverrun_IsBudgetExceededWithNoSecondAttempt()
    {
        using var harness = ExecutionHarness.Create(
            adapterResult: new ExecutionAdapterResult(
                ExecutionAdapterOutcome.Succeeded,
                "completed",
                usage: new ExecutionAdapterUsageMetrics(modelTurns: 3)));

        var result = await harness.Service.ExecuteAsync(harness.Request);

        Assert.Equal(BoundedExecutionStatus.BudgetExceeded, result.Status);
        Assert.Equal(1, harness.Adapter.InvocationCount);
        Assert.Equal(RecoveryCheckpointLifecycleState.Blocked, result.TerminalCheckpoint!.LifecycleState);
    }

    [Fact]
    public async Task ChangedFileAndLineReports_RemainEvidenceConstraints_NotIndependentAcceptance()
    {
        using var harness = ExecutionHarness.Create(
            adapterResult: new ExecutionAdapterResult(
                ExecutionAdapterOutcome.Succeeded,
                "step finished",
                usage: new ExecutionAdapterUsageMetrics(changedFiles: 99, changedLines: 999)));

        var result = await harness.Service.ExecuteAsync(harness.Request);

        Assert.Equal(BoundedExecutionStatus.Succeeded, result.Status);
        Assert.Equal(RecoveryNextSafeAction.RunValidation, result.TerminalCheckpoint!.NextSafeAction);
        Assert.NotEqual(ExecutionRunStatus.Accepted, harness.History.Runs[^1].Status);
    }

    [Fact]
    public async Task MissingAttemptsOrElapsedBudget_IsRejectedBeforeAdapter()
    {
        using var attemptsHarness = ExecutionHarness.Create(budgets: [new PlanningExecutionBudget(PlanningBudgetKind.ElapsedMinutes, 1)]);
        var attemptsResult = await attemptsHarness.Service.ExecuteAsync(attemptsHarness.Request);
        Assert.Equal(BoundedExecutionStatus.BudgetInvalid, attemptsResult.Status);
        Assert.Equal(0, attemptsHarness.Adapter.InvocationCount);

        using var elapsedHarness = ExecutionHarness.Create(budgets: [new PlanningExecutionBudget(PlanningBudgetKind.Attempts, 1)]);
        var elapsedResult = await elapsedHarness.Service.ExecuteAsync(elapsedHarness.Request);
        Assert.Equal(BoundedExecutionStatus.BudgetInvalid, elapsedResult.Status);
        Assert.Equal(0, elapsedHarness.Adapter.InvocationCount);
    }

    [Fact]
    public async Task InteractiveOnlyAgent_IsRejectedBeforeAdapter()
    {
        using var harness = ExecutionHarness.Create(connectionMode: AgentConnectionMode.InteractiveOnly);

        var result = await harness.Service.ExecuteAsync(harness.Request);

        Assert.Equal(BoundedExecutionStatus.ConnectionUnsupported, result.Status);
        Assert.Equal(0, harness.Adapter.InvocationCount);
    }

    [Theory]
    [InlineData(AgentConnectionMode.Manual)]
    [InlineData(AgentConnectionMode.Unsupported)]
    [InlineData(AgentConnectionMode.Unknown)]
    public async Task NonAutonomousConnectionModes_AreRejectedBeforeAdapter(AgentConnectionMode mode)
    {
        using var harness = ExecutionHarness.Create(connectionMode: mode);

        var result = await harness.Service.ExecuteAsync(harness.Request);

        Assert.NotEqual(BoundedExecutionStatus.Succeeded, result.Status);
        Assert.Equal(0, harness.Adapter.InvocationCount);
    }

    [Fact]
    public async Task NoProductionAdapter_FailsClosedBeforeInvocation()
    {
        using var harness = ExecutionHarness.Create(noAdapter: true);

        var result = await harness.Service.ExecuteAsync(harness.Request);

        Assert.Equal(BoundedExecutionStatus.AdapterUnsupported, result.Status);
        Assert.Equal(0, harness.Adapter.InvocationCount);
    }

    [Fact]
    public async Task MultipleExactAdapters_FailsClosedBeforeInvocation()
    {
        using var harness = ExecutionHarness.Create(multipleAdapters: true);

        var result = await harness.Service.ExecuteAsync(harness.Request);

        Assert.Equal(BoundedExecutionStatus.AdapterConfigurationConflict, result.Status);
        Assert.Equal(0, harness.Adapter.InvocationCount);
    }

    [Fact]
    public async Task PreparedWithoutReceipt_IsRejectedBeforeAdapter()
    {
        using var harness = ExecutionHarness.Create(workspaceState: WorkspaceRecoveryState.PreparedWithoutReceipt);

        var result = await harness.Service.ExecuteAsync(harness.Request);

        Assert.Equal(BoundedExecutionStatus.WorkspaceNotPrepared, result.Status);
        Assert.Equal(0, harness.Adapter.InvocationCount);
    }

    [Fact]
    public async Task WorkspaceConflict_IsRejectedBeforeAdapter()
    {
        using var harness = ExecutionHarness.Create(workspaceState: WorkspaceRecoveryState.Conflict);

        var result = await harness.Service.ExecuteAsync(harness.Request);

        Assert.Equal(BoundedExecutionStatus.WorkspaceConflict, result.Status);
        Assert.Equal(0, harness.Adapter.InvocationCount);
    }

    [Fact]
    public async Task WorkspacePlanContractMismatch_IsRejectedBeforeAdapter()
    {
        using var harness = ExecutionHarness.Create();
        var request = new BoundedExecutionRequest(
            harness.Request.ProjectId,
            harness.Request.RunId,
            harness.Request.PlanningContractReference,
            harness.Request.WorkGraphReference,
            harness.Request.WorkGraphNodeId,
            harness.Request.HandoffPackageReference,
            harness.Request.RoutingDecisionReference,
            new WorkspacePreparationPlanReference(
                harness.Plan.PlanId,
                harness.Plan.Reference.SchemaVersion,
                new string('f', 64),
                harness.Request.ProjectId),
            harness.Request.CurrentRecoveryCheckpointReference);

        var result = await harness.Service.ExecuteAsync(request);

        Assert.Equal(BoundedExecutionStatus.WorkspacePlanMismatch, result.Status);
        Assert.Equal(0, harness.Adapter.InvocationCount);
    }

    [Theory]
    [InlineData(RecoveryCheckpointLifecycleState.Blocked, BoundedExecutionStatus.CheckpointBlocked)]
    [InlineData(RecoveryCheckpointLifecycleState.ApprovalRequired, BoundedExecutionStatus.CheckpointApprovalRequired)]
    [InlineData(RecoveryCheckpointLifecycleState.Completed, BoundedExecutionStatus.CheckpointCompleted)]
    public async Task NonExecutableCheckpointStates_AreRejectedBeforeAdapter(
        RecoveryCheckpointLifecycleState state,
        BoundedExecutionStatus expected)
    {
        using var harness = ExecutionHarness.Create(checkpointState: state);

        var result = await harness.Service.ExecuteAsync(harness.Request);

        Assert.Equal(expected, result.Status);
        Assert.Equal(0, harness.Adapter.InvocationCount);
    }

    [Fact]
    public async Task CurrentCheckpointWithUnresolvedBlocker_IsRejectedBeforeAdapter()
    {
        using var harness = ExecutionHarness.Create(checkpointHasBlocker: true);

        var result = await harness.Service.ExecuteAsync(harness.Request);

        Assert.Equal(BoundedExecutionStatus.CheckpointBlocked, result.Status);
        Assert.Equal(0, harness.Adapter.InvocationCount);
    }

    [Fact]
    public async Task AuthorityPersistenceFailure_IsRejectedBeforeAdapter()
    {
        using var harness = ExecutionHarness.Create(authorityPersistenceFailure: true);

        var result = await harness.Service.ExecuteAsync(harness.Request);

        Assert.Equal(BoundedExecutionStatus.PersistenceUnavailable, result.Status);
        Assert.Equal(0, harness.Adapter.InvocationCount);
    }

    [Fact]
    public async Task NonActiveProject_IsRejectedBeforeAdapter()
    {
        using var harness = ExecutionHarness.Create(projectStatus: ProjectStatus.Paused);

        var result = await harness.Service.ExecuteAsync(harness.Request);

        Assert.Equal(BoundedExecutionStatus.ProjectNotExecutable, result.Status);
        Assert.Equal(0, harness.Adapter.InvocationCount);
    }

    [Fact]
    public async Task UnknownProject_IsRejectedBeforeAdapter()
    {
        using var harness = ExecutionHarness.Create();
        var request = new BoundedExecutionRequest(
            Guid.NewGuid(),
            harness.Request.RunId,
            harness.Request.PlanningContractReference,
            harness.Request.WorkGraphReference,
            harness.Request.WorkGraphNodeId,
            harness.Request.HandoffPackageReference,
            harness.Request.RoutingDecisionReference,
            harness.Request.WorkspacePreparationPlanReference,
            harness.Request.CurrentRecoveryCheckpointReference);

        var result = await harness.Service.ExecuteAsync(request);

        Assert.Equal(BoundedExecutionStatus.InvalidRequest, result.Status);
        Assert.Equal(0, harness.Adapter.InvocationCount);
    }

    [Fact]
    public async Task StaleContext_IsRejectedBeforeAdapter()
    {
        using var harness = ExecutionHarness.Create(staleContext: true);

        var result = await harness.Service.ExecuteAsync(harness.Request);

        Assert.Equal(BoundedExecutionStatus.ContractMismatch, result.Status);
        Assert.Equal(0, harness.Adapter.InvocationCount);
    }

    [Fact]
    public async Task NonCurrentCheckpoint_IsRejectedBeforeAdapter()
    {
        using var harness = ExecutionHarness.Create(checkpointIsCurrent: false);

        var result = await harness.Service.ExecuteAsync(harness.Request);

        Assert.Equal(BoundedExecutionStatus.CheckpointNotCurrent, result.Status);
        Assert.Equal(0, harness.Adapter.InvocationCount);
    }

    [Fact]
    public async Task WrongGraphNode_IsRejectedBeforeAdapter()
    {
        using var harness = ExecutionHarness.Create();
        var request = new BoundedExecutionRequest(
            harness.Request.ProjectId,
            harness.Request.RunId,
            harness.Request.PlanningContractReference,
            harness.Request.WorkGraphReference,
            Guid.NewGuid(),
            harness.Request.HandoffPackageReference,
            harness.Request.RoutingDecisionReference,
            harness.Request.WorkspacePreparationPlanReference,
            harness.Request.CurrentRecoveryCheckpointReference);

        var result = await harness.Service.ExecuteAsync(request);

        Assert.Equal(BoundedExecutionStatus.GraphNodeMismatch, result.Status);
        Assert.Equal(0, harness.Adapter.InvocationCount);
    }

    [Fact]
    public async Task WrongImmutableReferenceHash_IsRejectedBeforeAdapter()
    {
        using var harness = ExecutionHarness.Create();
        var request = new BoundedExecutionRequest(
            harness.Request.ProjectId,
            harness.Request.RunId,
            new PlanningExecutionContractReference(
                harness.Contract.ContractId,
                harness.Contract.Revision,
                harness.Contract.SchemaVersion,
                new string('f', 64)),
            harness.Request.WorkGraphReference,
            harness.Request.WorkGraphNodeId,
            harness.Request.HandoffPackageReference,
            harness.Request.RoutingDecisionReference,
            harness.Request.WorkspacePreparationPlanReference,
            harness.Request.CurrentRecoveryCheckpointReference);

        var result = await harness.Service.ExecuteAsync(request);

        Assert.Equal(BoundedExecutionStatus.ContractMismatch, result.Status);
        Assert.Equal(0, harness.Adapter.InvocationCount);
    }

    [Fact]
    public async Task GraphHandoffAndRoutingHashMismatches_AreRejectedBeforeAdapter()
    {
        using var graphHarness = ExecutionHarness.Create();
        var graphRequest = new BoundedExecutionRequest(
            graphHarness.Request.ProjectId,
            graphHarness.Request.RunId,
            graphHarness.Request.PlanningContractReference,
            new WorkGraphReference(
                graphHarness.Graph.GraphId,
                graphHarness.Graph.SchemaVersion,
                new string('f', 64)),
            graphHarness.Request.WorkGraphNodeId,
            graphHarness.Request.HandoffPackageReference,
            graphHarness.Request.RoutingDecisionReference,
            graphHarness.Request.WorkspacePreparationPlanReference,
            graphHarness.Request.CurrentRecoveryCheckpointReference);
        var graphResult = await graphHarness.Service.ExecuteAsync(graphRequest);
        Assert.Equal(BoundedExecutionStatus.GraphInvalid, graphResult.Status);
        Assert.Equal(0, graphHarness.Adapter.InvocationCount);

        using var handoffHarness = ExecutionHarness.Create();
        var handoffRequest = new BoundedExecutionRequest(
            handoffHarness.Request.ProjectId,
            handoffHarness.Request.RunId,
            handoffHarness.Request.PlanningContractReference,
            handoffHarness.Request.WorkGraphReference,
            handoffHarness.Request.WorkGraphNodeId,
            new HandoffPackageReference(handoffHarness.Handoff.PackageId, handoffHarness.Handoff.SchemaVersion, new string('f', 64)),
            handoffHarness.Request.RoutingDecisionReference,
            handoffHarness.Request.WorkspacePreparationPlanReference,
            handoffHarness.Request.CurrentRecoveryCheckpointReference);
        var handoffResult = await handoffHarness.Service.ExecuteAsync(handoffRequest);
        Assert.Equal(BoundedExecutionStatus.HandoffMismatch, handoffResult.Status);
        Assert.Equal(0, handoffHarness.Adapter.InvocationCount);

        using var routingHarness = ExecutionHarness.Create();
        var routingRequest = new BoundedExecutionRequest(
            routingHarness.Request.ProjectId,
            routingHarness.Request.RunId,
            routingHarness.Request.PlanningContractReference,
            routingHarness.Request.WorkGraphReference,
            routingHarness.Request.WorkGraphNodeId,
            routingHarness.Request.HandoffPackageReference,
            new RoutingDecisionReference(routingHarness.Routing.DecisionId, routingHarness.Routing.SchemaVersion, new string('f', 64)),
            routingHarness.Request.WorkspacePreparationPlanReference,
            routingHarness.Request.CurrentRecoveryCheckpointReference);
        var routingResult = await routingHarness.Service.ExecuteAsync(routingRequest);
        Assert.Equal(BoundedExecutionStatus.RoutingMismatch, routingResult.Status);
        Assert.Equal(0, routingHarness.Adapter.InvocationCount);
    }

    [Fact]
    public async Task CheckpointSelectedAgentMismatch_IsRejectedBeforeAdapter()
    {
        using var harness = ExecutionHarness.Create(checkpointExecutorId: Guid.NewGuid());

        var result = await harness.Service.ExecuteAsync(harness.Request);

        Assert.Equal(BoundedExecutionStatus.AgentMismatch, result.Status);
        Assert.Equal(0, harness.Adapter.InvocationCount);
    }

    [Theory]
    [InlineData(false, AgentAvailability.Unavailable, AgentAuthenticationState.NotRequired)]
    [InlineData(false, AgentAvailability.Available, AgentAuthenticationState.AuthenticationRequired)]
    [InlineData(false, AgentAvailability.Available, AgentAuthenticationState.NotRequired)]
    public async Task UnavailableOrDisabledAgent_IsRejectedBeforeAdapter(
        bool enabled,
        AgentAvailability availability,
        AgentAuthenticationState authentication)
    {
        using var harness = ExecutionHarness.Create(
            agentEnabled: enabled,
            agentAvailability: availability,
            authenticationState: authentication);

        var result = await harness.Service.ExecuteAsync(harness.Request);

        Assert.NotEqual(BoundedExecutionStatus.Succeeded, result.Status);
        Assert.Equal(0, harness.Adapter.InvocationCount);
    }

    [Fact]
    public async Task PreRunCheckpointFailure_LeavesDurableAuthorityAndReplayDoesNotInvoke()
    {
        using var harness = ExecutionHarness.Create(failCheckpointCreationNumber: 1);

        var first = await harness.Service.ExecuteAsync(harness.Request);
        var replay = await harness.Service.ExecuteAsync(harness.Request);

        Assert.Equal(BoundedExecutionStatus.PreRunCheckpointFailed, first.Status);
        Assert.Equal(BoundedExecutionStatus.AlreadyStarted, replay.Status);
        Assert.Equal(0, harness.Adapter.InvocationCount);
    }

    [Fact]
    public async Task RunningHistoryFailure_LeavesPreCheckpointAndReplayDoesNotInvoke()
    {
        using var harness = ExecutionHarness.Create(failHistoryStatus: ExecutionRunStatus.Running);

        var first = await harness.Service.ExecuteAsync(harness.Request);
        var replay = await harness.Service.ExecuteAsync(harness.Request);

        Assert.Equal(BoundedExecutionStatus.RunningHistoryFailed, first.Status);
        Assert.Equal(BoundedExecutionStatus.AlreadyStarted, replay.Status);
        Assert.Equal(0, harness.Adapter.InvocationCount);
        Assert.Single(harness.CheckpointService.Created);
    }

    [Fact]
    public async Task TerminalCheckpointFailure_NeverProducesCompletedAndReplayDoesNotInvoke()
    {
        using var harness = ExecutionHarness.Create(failCheckpointCreationNumber: 2);

        var first = await harness.Service.ExecuteAsync(harness.Request);
        var replay = await harness.Service.ExecuteAsync(harness.Request);

        Assert.Equal(BoundedExecutionStatus.TerminalCheckpointFailed, first.Status);
        Assert.Equal(BoundedExecutionStatus.AlreadyStarted, replay.Status);
        Assert.Equal(1, harness.Adapter.InvocationCount);
        Assert.DoesNotContain(harness.History.Runs, value => value.Status == ExecutionRunStatus.Completed);
    }

    [Fact]
    public async Task TerminalHistoryFailure_PreservesCheckpointAndReplayDoesNotInvoke()
    {
        using var harness = ExecutionHarness.Create(failHistoryStatus: ExecutionRunStatus.Completed);

        var first = await harness.Service.ExecuteAsync(harness.Request);
        var replay = await harness.Service.ExecuteAsync(harness.Request);

        Assert.Equal(BoundedExecutionStatus.AuditPersistenceFailed, first.Status);
        Assert.Equal(BoundedExecutionStatus.AlreadyStarted, replay.Status);
        Assert.Equal(1, harness.Adapter.InvocationCount);
        Assert.Equal(2, harness.CheckpointService.Created.Count);
        Assert.Equal(RecoveryCheckpointLifecycleState.Ready, harness.CheckpointService.Created[^1].LifecycleState);
    }

    [Fact]
    public async Task ConcurrentProjectExecution_FailsFastAsProjectBusy()
    {
        using var harness = ExecutionHarness.Create(hangAdapter: true);
        var firstExecution = harness.Service.ExecuteAsync(harness.Request);
        await harness.Adapter.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var second = await harness.Service.ExecuteAsync(
            new BoundedExecutionRequest(
                harness.Request.ProjectId,
                Guid.NewGuid(),
                harness.Request.PlanningContractReference,
                harness.Request.WorkGraphReference,
                harness.Request.WorkGraphNodeId,
                harness.Request.HandoffPackageReference,
                harness.Request.RoutingDecisionReference,
                harness.Request.WorkspacePreparationPlanReference,
                harness.Request.CurrentRecoveryCheckpointReference));
        harness.Adapter.Release(new ExecutionAdapterResult(ExecutionAdapterOutcome.Cancelled, "released", "test-release"));

        var first = await firstExecution;

        Assert.Equal(BoundedExecutionStatus.ProjectBusy, second.Status);
        Assert.Equal(BoundedExecutionStatus.Cancelled, first.Status);
        Assert.Equal(1, harness.Adapter.InvocationCount);
    }

    private sealed class ExecutionHarness : IDisposable
    {
        private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-08-28T10:00:00+00:00");

        private ExecutionHarness(
            Project project,
            ProjectContextReference context,
            PlanningExecutionContract contract,
            WorkGraph graph,
            WorkGraphNode node,
            HandoffPackage handoff,
            RoutingDecision routing,
            WorkspacePreparationPlan plan,
            WorkspacePreparationReceipt receipt,
            ProjectContextReference resolvedContext,
            RecoveryCheckpoint currentCheckpoint,
            BoundedExecutionRequest request,
            FakeAgentRegistry agents,
            FakeExecutionAdapter adapter,
            FakeWorkspaceInspection workspaceInspection,
            IExecutionAdapterResolver adapterResolver,
            FakeCheckpointRepository checkpoints,
            FakeContinuationHeadRepository heads,
            FakeCheckpointService checkpointService,
            FakeRunAuthorityRepository authorities,
            FakeHistory history,
            IExecutionBudgetTimeoutProvider timeoutProvider)
        {
            Project = project;
            Context = context;
            Contract = contract;
            Graph = graph;
            Node = node;
            Handoff = handoff;
            Routing = routing;
            Plan = plan;
            Receipt = receipt;
            CurrentCheckpoint = currentCheckpoint;
            Request = request;
            Agent = agents.Agent;
            Adapter = adapter;
            WorkspaceInspection = workspaceInspection;
            CheckpointService = checkpointService;
            History = history;
            Service = new BoundedExecutionService(
                new FakeContextResolver(project, resolvedContext),
                new FakeContractRepository(contract),
                new FakeGraphRepository(graph),
                new FakeHandoffRepository(handoff),
                new FakeRoutingRepository(routing),
                agents,
                new FakeWorkspacePlanRepository(plan),
                workspaceInspection,
                checkpoints,
                heads,
                checkpointService,
                authorities,
                adapterResolver,
                history,
                new HandoffRedactionService(),
                new FixedClock(Now),
                timeoutProvider);
        }

        public Project Project { get; }
        public ProjectContextReference Context { get; }
        public PlanningExecutionContract Contract { get; }
        public WorkGraph Graph { get; }
        public WorkGraphNode Node { get; }
        public HandoffPackage Handoff { get; }
        public RoutingDecision Routing { get; }
        public WorkspacePreparationPlan Plan { get; }
        public WorkspacePreparationReceipt Receipt { get; }
        public RecoveryCheckpoint CurrentCheckpoint { get; }
        public BoundedExecutionRequest Request { get; }
        public EffectiveAgentDefinition Agent { get; }
        public FakeExecutionAdapter Adapter { get; }
        public FakeCheckpointService CheckpointService { get; }
        public FakeHistory History { get; }
        public BoundedExecutionService Service { get; }

        public static ExecutionHarness Create(
            bool hangAdapter = false,
            ExecutionAdapterResult? adapterResult = null,
            IExecutionBudgetTimeoutProvider? timeoutProvider = null,
            AgentConnectionMode connectionMode = AgentConnectionMode.Cli,
            WorkspaceRecoveryState workspaceState = WorkspaceRecoveryState.PreparedAndRecorded,
            bool checkpointIsCurrent = true,
            int? failCheckpointCreationNumber = null,
            ExecutionRunStatus? failHistoryStatus = null,
            IReadOnlyList<PlanningExecutionBudget>? budgets = null,
            bool noAdapter = false,
            bool multipleAdapters = false,
            RecoveryCheckpointLifecycleState checkpointState = RecoveryCheckpointLifecycleState.Ready,
            bool checkpointHasBlocker = false,
            bool authorityPersistenceFailure = false,
            ProjectStatus projectStatus = ProjectStatus.Active,
            bool staleContext = false,
            Guid? checkpointExecutorId = null,
            bool agentEnabled = true,
            AgentAvailability? agentAvailability = null,
            AgentAuthenticationState authenticationState = AgentAuthenticationState.NotRequired,
            AgentEntitlementState entitlementState = AgentEntitlementState.VerifiedAvailable)
        {
            var projectId = Guid.NewGuid();
            var contextId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var contract = CreateContract(projectId, contextId, agentId, budgets);
            var context = new ProjectContextReference(
                projectId,
                contextId,
                ProjectContextContract.CurrentVersion,
                Now,
                Now,
                ProjectRepositoryContextReference.Skipped(projectId, @"C:\APO-test"),
                new ProjectTrackerContextReference(TrackerReferenceState.Skipped),
                [],
                new ProjectCurrentWorkReference(CurrentWorkState.NotSelected),
                [],
                null,
                null,
                ProjectNextSafeAction.ReadyForPlanning);
            var resolvedContext = staleContext
                ? new ProjectContextReference(
                    projectId,
                    Guid.NewGuid(),
                    ProjectContextContract.CurrentVersion,
                    Now,
                    Now,
                    ProjectRepositoryContextReference.Skipped(projectId, @"C:\APO-test"),
                    new ProjectTrackerContextReference(TrackerReferenceState.Skipped),
                    [],
                    new ProjectCurrentWorkReference(CurrentWorkState.NotSelected),
                    [],
                    null,
                    null,
                    ProjectNextSafeAction.ReadyForPlanning)
                : context;
            var project = new Project(projectId, "Execution project", @"C:\APO-test", null, projectStatus, Now, Now);
            var node = new WorkGraphNode(Guid.NewGuid(), contract.Reference);
            var graph = new WorkGraph(projectId, Guid.NewGuid(), WorkGraphSchema.CurrentVersion, Now, [node], []);
            var definition = new AgentDefinition(
                agentId,
                "Test executor",
                "Executor",
                connectionMode,
                agentAvailability ?? (connectionMode == AgentConnectionMode.Unsupported ? AgentAvailability.Unsupported : AgentAvailability.Available),
                agentEnabled,
                Now,
                Now,
                provider: "TestProvider",
                capabilities: ["bounded"],
                roleCapabilities: [AgentRole.Executor],
                supportedConnectionModes: connectionMode is AgentConnectionMode.Unknown or AgentConnectionMode.Unsupported ? null : [connectionMode],
                authenticationState: authenticationState,
                entitlementState: entitlementState,
                modelIdentifier: "TestModel");
            var agent = new EffectiveAgentDefinition(projectId, definition, null);
            var routing = CreateRouting(projectId, contract, contextId, agent);
            var handoff = CreateHandoff(projectId, contract, context, graph, node, budgets);
            var baseSha = new string('a', 40);
            var workspacePath = $@"C:\APO-managed\{projectId:D}\{workspaceId:D}";
            var discovery = new WorkspaceRepositoryDiscovery(
                WorkspaceRepositoryDiscoveryStatus.Available,
                @"C:\APO-test",
                @"C:\APO-test",
                @"C:\APO-test\.git",
                headCommitSha: baseSha,
                branchName: "main",
                isClean: true);
            var plan = new WorkspacePreparationPlan(
                projectId,
                workspaceId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Now,
                new WorkspaceContextIdentity(projectId, contextId, 1, Now),
                contract.Reference,
                graph.Reference,
                node.NodeId,
                routing.Reference,
                discovery,
                baseSha,
                "apo-run",
                workspacePath,
                WorkspacePreparationPolicy.RequireCleanSource,
                true,
                "prepared");
            var receipt = new WorkspacePreparationReceipt(
                projectId,
                workspaceId,
                plan.CorrelationId,
                Now,
                plan.Reference,
                workspacePath,
                plan.WorkspaceBranch,
                baseSha,
                baseSha,
                @"C:\APO-test",
                "APO-test-owner");
            var currentCheckpoint = new RecoveryCheckpoint(
                projectId,
                Guid.NewGuid(),
                RecoveryCheckpointSchema.CurrentVersion,
                Now,
                checkpointState,
                new RecoveryContextReference(contextId, 1, Now),
                contract.Reference,
                graph.Reference,
                node.NodeId,
                handoff.Reference,
                selectedAgentRoleReferences: [new RecoveryAgentRoleReference(checkpointExecutorId ?? agentId, AgentRole.Executor, routing.Reference.ToString())],
                gateSnapshots: checkpointState == RecoveryCheckpointLifecycleState.Ready ? [] : checkpointState == RecoveryCheckpointLifecycleState.ApprovalRequired ? [new RecoveryGateSnapshot(RecoveryGateKind.Approval, RecoveryGateState.Pending)] : [],
                blockers: checkpointHasBlocker ? [new RecoveryBlocker("test-blocker", RecoveryBlockerKind.Other, "test blocker", ownerActionRequired: true)] : [],
                nextSafeAction: RecoveryNextSafeAction.ContinueFromCheckpoint,
                explanation: "ready");
            var heads = new FakeContinuationHeadRepository(currentCheckpoint, checkpointIsCurrent);
            var checkpoints = new FakeCheckpointRepository(currentCheckpoint);
            var checkpointService = new FakeCheckpointService(context, heads, checkpoints, failCheckpointCreationNumber);
            var workspaceInspection = new FakeWorkspaceInspection(receipt, workspaceState);
            var authorities = new FakeRunAuthorityRepository(authorityPersistenceFailure);
            var history = new FakeHistory(failHistoryStatus);
            var adapter = new FakeExecutionAdapter(
                new ExecutionAdapterDescriptor("test-adapter", [connectionMode], [PlanningBudgetKind.ToolInvocations, PlanningBudgetKind.ModelTurns]),
                hangAdapter,
                adapterResult);
            var adapters = noAdapter
                ? Array.Empty<IExecutionAdapter>()
                : multipleAdapters
                    ? new IExecutionAdapter[] { adapter, new FakeExecutionAdapter(adapter.Descriptor, false, null) }
                    : new IExecutionAdapter[] { adapter };
            var request = new BoundedExecutionRequest(
                projectId,
                Guid.NewGuid(),
                contract.Reference,
                graph.Reference,
                node.NodeId,
                handoff.Reference,
                routing.Reference,
                plan.Reference,
                currentCheckpoint.Reference);
            var harness = new ExecutionHarness(
                project,
                context,
                contract,
                graph,
                node,
                handoff,
                routing,
                plan,
                receipt,
                resolvedContext,
                currentCheckpoint,
                request,
                new FakeAgentRegistry(agent),
                adapter,
                workspaceInspection,
                new ExecutionAdapterResolver(adapters),
                checkpoints,
                heads,
                checkpointService,
                authorities,
                history,
                timeoutProvider ?? new FixedTimeoutProvider(TimeSpan.FromSeconds(5)));
            return harness;
        }

        public FakeWorkspaceInspection WorkspaceInspection { get; }

        public void Dispose() { }

        private static PlanningExecutionContract CreateContract(Guid projectId, Guid contextId, Guid agentId, IReadOnlyList<PlanningExecutionBudget>? budgets) => new(
            projectId,
            Guid.NewGuid(),
            PlanningExecutionContractSchema.CurrentVersion,
            1,
            Now,
            "APO-45 test owner",
            agentId,
            new PlanningContextBinding(contextId, ProjectContextContract.CurrentVersion),
            new PlanningWorkItem(PlanningWorkItemSource.Jira, "APO-45", "Bounded execution"),
            new PlanningRepositoryTarget(PlanningRepositoryMode.None),
            [new PlanningScopeClause("include", "one bounded step")],
            [],
            [new PlanningScopeClause("forbid", "autonomous loop")],
            [new PlanningDeliverable("step", "bounded step", true)],
            [new PlanningValidationRequirement("validate", PlanningValidationKind.Test, "later validation", true)],
            [new PlanningAcceptanceCriterion("accept", "later acceptance", true)],
            budgets ?? [
                new PlanningExecutionBudget(PlanningBudgetKind.Attempts, 1),
                new PlanningExecutionBudget(PlanningBudgetKind.ElapsedMinutes, 1),
                new PlanningExecutionBudget(PlanningBudgetKind.ToolInvocations, 10),
                new PlanningExecutionBudget(PlanningBudgetKind.ModelTurns, 2)],
            [
                new PlanningStopCondition("target", PlanningStopConditionKind.ImmutableTargetMoved, "target"),
                new PlanningStopCondition("scope", PlanningStopConditionKind.ScopeViolation, "scope"),
                new PlanningStopCondition("budget", PlanningStopConditionKind.BudgetExceeded, "budget")],
            [],
            null,
            null);

        private static RoutingDecision CreateRouting(Guid projectId, PlanningExecutionContract contract, Guid contextId, EffectiveAgentDefinition agent)
        {
            var classification = new RoutingTaskClassification(
                RoutingScopeScale.Bounded,
                RoutingTaskRisk.Low,
                RoutingBlastRadius.Local,
                RoutingValidationCost.Low,
                AgentRole.Executor,
                capacityRequirement: RoutingCapacityRequirement.NotApplicable);
            var policy = new RoutingPolicySnapshot(
                "execution-policy",
                AgentRole.Executor,
                capacityRequirement: RoutingCapacityRequirement.NotApplicable);
            var input = new RoutingInputSnapshot(
                projectId,
                contract.Reference,
                new RoutingContextReference(contextId, 1, Now),
                classification,
                policy,
                [RoutingAgentSnapshot.FromEffective(agent)],
                [],
                null,
                Now);
            var evaluation = new RoutingDecisionEngine().Evaluate(input);
            return new RoutingDecision(projectId, Guid.NewGuid(), RoutingDecisionSchema.CurrentVersion, Now, evaluation);
        }

        private static HandoffPackage CreateHandoff(
            Guid projectId,
            PlanningExecutionContract contract,
            ProjectContextReference context,
            WorkGraph graph,
            WorkGraphNode node,
            IReadOnlyList<PlanningExecutionBudget>? budgets)
        {
            var scopeBudgets = budgets ?? [new PlanningExecutionBudget(PlanningBudgetKind.Attempts, 1), new PlanningExecutionBudget(PlanningBudgetKind.ElapsedMinutes, 1), new PlanningExecutionBudget(PlanningBudgetKind.ToolInvocations, 10), new PlanningExecutionBudget(PlanningBudgetKind.ModelTurns, 2)];
            var scope = new HandoffExecutionScope(
                [new PlanningScopeClause("include", "one bounded step")],
                [],
                [new PlanningScopeClause("forbid", "replay")],
                [new PlanningDeliverable("step", "bounded step", true)],
                [new PlanningValidationRequirement("validate", PlanningValidationKind.Test, "later validation", true)],
                scopeBudgets,
                [
                    new PlanningStopCondition("target", PlanningStopConditionKind.ImmutableTargetMoved, "target"),
                    new PlanningStopCondition("scope", PlanningStopConditionKind.ScopeViolation, "scope"),
                    new PlanningStopCondition("budget", PlanningStopConditionKind.BudgetExceeded, "budget")],
                [],
                null,
                null);
            var scopeCount = 1 + 1 + 1 + 1 + scopeBudgets.Count + 3;
            return new HandoffPackage(
                projectId,
                Guid.NewGuid(),
                HandoffPackageSchema.CurrentVersion,
                Now,
                HandoffTransition.PlannerToExecutor,
                HandoffRole.Planner,
                HandoffRole.Executor,
                contract.Reference,
                contract.WorkItem,
                new HandoffContextReference(context.ContextId, context.ContractVersion, Now, Now),
                new PlanningRepositoryTarget(PlanningRepositoryMode.None),
                graph.Reference,
                node.NodeId,
                null,
                scope,
                null,
                null,
                null,
                [],
                [],
                [],
                null,
                [],
                "Execute one bounded step",
                new HandoffRedactionMetadata(false, 0, []),
                new HandoffPackageSizeMetadata(HandoffPackageLimits.MaxCanonicalPayloadBytes, 0, 0, 0, 0, 0, scopeCount));
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class FixedTimeoutProvider(TimeSpan timeout) : IExecutionBudgetTimeoutProvider
    {
        public TimeSpan GetTimeout(ExecutionBudgetEnvelope budgets) => timeout;
    }

    private sealed class FakeContextResolver(Project project, ProjectContextReference context) : IProjectContextResolver
    {
        public Task<ProjectContextResolution> ResolveAsync(Guid projectId, CancellationToken cancellationToken = default) =>
            Task.FromResult(projectId == project.Id
                ? new ProjectContextResolution(ProjectContextResolutionState.Ready, new ProjectContextView(project, context, []))
                : new ProjectContextResolution(ProjectContextResolutionState.ProjectNotFound));
    }

    private sealed class FakeContractRepository(PlanningExecutionContract contract) : IPlanningExecutionContractRepository
    {
        public Task<PlanningContractRepositoryWriteResult> CreateAsync(PlanningExecutionContract value, CancellationToken cancellationToken = default) => Task.FromResult(new PlanningContractRepositoryWriteResult(PlanningContractRepositoryWriteStatus.Created));
        public Task<PlanningContractReadResult> GetAsync(Guid projectId, Guid contractId, int revision, CancellationToken cancellationToken = default) => Task.FromResult(projectId == contract.ProjectId && contractId == contract.ContractId && revision == contract.Revision ? new PlanningContractReadResult(PlanningContractReadState.Valid, contract) : new PlanningContractReadResult(PlanningContractReadState.Missing));
        public Task<PlanningContractReadResult> GetLatestAsync(Guid projectId, Guid contractId, CancellationToken cancellationToken = default) => GetAsync(projectId, contractId, contract.Revision, cancellationToken);
        public Task<PlanningContractRevisionListResult> ListRevisionsAsync(Guid projectId, Guid contractId, CancellationToken cancellationToken = default) => Task.FromResult(new PlanningContractRevisionListResult(PlanningContractReadState.Valid, [contract]));
    }

    private sealed class FakeGraphRepository(WorkGraph graph) : IWorkGraphRepository
    {
        public Task<WorkGraphRepositoryWriteResult> CreateAsync(WorkGraph value, CancellationToken cancellationToken = default) => Task.FromResult(new WorkGraphRepositoryWriteResult(WorkGraphRepositoryWriteStatus.Created));
        public Task<WorkGraphReadResult> GetAsync(Guid projectId, Guid graphId, CancellationToken cancellationToken = default) => Task.FromResult(projectId == graph.ProjectId && graphId == graph.GraphId ? new WorkGraphReadResult(WorkGraphReadState.Valid, graph) : new WorkGraphReadResult(WorkGraphReadState.Missing));
    }

    private sealed class FakeHandoffRepository(HandoffPackage package) : IHandoffPackageRepository
    {
        public Task<HandoffPackageRepositoryWriteResult> CreateAsync(HandoffPackage value, CancellationToken cancellationToken = default) => Task.FromResult(new HandoffPackageRepositoryWriteResult(HandoffPackageRepositoryWriteStatus.Created));
        public Task<HandoffPackageReadResult> GetAsync(Guid projectId, Guid packageId, CancellationToken cancellationToken = default) => Task.FromResult(projectId == package.ProjectId && packageId == package.PackageId ? new HandoffPackageReadResult(HandoffPackageReadState.Valid, package) : new HandoffPackageReadResult(HandoffPackageReadState.Missing));
    }

    private sealed class FakeRoutingRepository(RoutingDecision decision) : IRoutingDecisionRepository
    {
        public Task<RoutingDecisionRepositoryWriteResult> CreateAsync(RoutingDecision value, CancellationToken cancellationToken = default) => Task.FromResult(new RoutingDecisionRepositoryWriteResult(RoutingDecisionRepositoryWriteStatus.Created));
        public Task<RoutingDecisionReadResult> GetAsync(Guid projectId, Guid decisionId, CancellationToken cancellationToken = default) => Task.FromResult(projectId == decision.ProjectId && decisionId == decision.DecisionId ? new RoutingDecisionReadResult(RoutingDecisionReadState.Valid, decision) : new RoutingDecisionReadResult(RoutingDecisionReadState.Missing));
    }

    private sealed class FakeAgentRegistry(EffectiveAgentDefinition agent) : IAgentRegistryService
    {
        public EffectiveAgentDefinition Agent { get; } = agent;
        public Task<IReadOnlyList<EffectiveAgentDefinition>> GetEffectiveAgentsAsync(Guid projectId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<EffectiveAgentDefinition>>([Agent]);
        public Task<AgentRegistryResolution> ResolveAsync(Guid projectId, Guid agentId, CancellationToken cancellationToken = default) => Task.FromResult(projectId == Agent.ProjectId && agentId == Agent.Id ? AgentRegistryResolution.FoundResult(Agent) : AgentRegistryResolution.NotFoundResult());
    }

    private sealed class FakeWorkspacePlanRepository(WorkspacePreparationPlan plan) : IWorkspacePreparationPlanRepository
    {
        public Task<WorkspacePreparationPlanWriteResult> CreateAsync(WorkspacePreparationPlan value, CancellationToken cancellationToken = default) => Task.FromResult(new WorkspacePreparationPlanWriteResult(WorkspacePreparationPlanWriteStatus.Created));
        public Task<WorkspacePreparationPlanReadResult> GetAsync(Guid projectId, Guid planId, CancellationToken cancellationToken = default) => Task.FromResult(projectId == plan.ProjectId && planId == plan.PlanId ? new WorkspacePreparationPlanReadResult(WorkspacePreparationPlanReadState.Valid, plan) : new WorkspacePreparationPlanReadResult(WorkspacePreparationPlanReadState.Missing));
    }

    private sealed class FakeWorkspaceInspection(WorkspacePreparationReceipt receipt, WorkspaceRecoveryState state) : IWorkspaceRecoveryInspectionService
    {
        public WorkspaceRecoveryState State { get; set; } = state;
        public Task<WorkspaceRecoveryInspectionResult> InspectAsync(WorkspacePreparationPlanReference planReference, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorkspaceRecoveryInspectionResult(State, State == WorkspaceRecoveryState.PreparedAndRecorded ? receipt : null));
    }

    private sealed class FakeCheckpointRepository(RecoveryCheckpoint current) : IRecoveryCheckpointRepository
    {
        public Dictionary<Guid, RecoveryCheckpoint> Values { get; } = new() { [current.CheckpointId] = current };
        public Task<RecoveryCheckpointRepositoryWriteResult> CreateAsync(RecoveryCheckpoint checkpoint, CancellationToken cancellationToken = default) { Values[checkpoint.CheckpointId] = checkpoint; return Task.FromResult(new RecoveryCheckpointRepositoryWriteResult(RecoveryCheckpointRepositoryWriteStatus.Created)); }
        public Task<RecoveryCheckpointReadResult> GetAsync(Guid projectId, Guid checkpointId, CancellationToken cancellationToken = default) => Task.FromResult(Values.TryGetValue(checkpointId, out var checkpoint) && checkpoint.ProjectId == projectId ? new RecoveryCheckpointReadResult(RecoveryCheckpointReadState.Valid, checkpoint) : new RecoveryCheckpointReadResult(RecoveryCheckpointReadState.Missing));
    }

    private sealed class FakeContinuationHeadRepository(RecoveryCheckpoint current, bool isCurrent) : IContinuationHeadRepository
    {
        public ContinuationHead? Current { get; set; } = new ContinuationHead(current.ProjectId, ContinuationHeadSchema.CurrentVersion, 1, isCurrent ? current.Reference : new RecoveryCheckpointReference(Guid.NewGuid(), 1, new string('e', 64)), null, current.CreatedAt);
        public Task<ContinuationHeadReadResult> GetAsync(Guid projectId, CancellationToken cancellationToken = default) => Task.FromResult(Current is null ? new ContinuationHeadReadResult(ContinuationHeadReadState.Missing) : new ContinuationHeadReadResult(ContinuationHeadReadState.Valid, Current));
        public Task<ContinuationHeadRepositoryWriteResult> PublishAsync(ContinuationHead head, CancellationToken cancellationToken = default) { Current = head; return Task.FromResult(new ContinuationHeadRepositoryWriteResult(ContinuationHeadRepositoryWriteStatus.Published)); }
    }

    private sealed class FakeCheckpointService(
        ProjectContextReference context,
        FakeContinuationHeadRepository heads,
        FakeCheckpointRepository checkpoints,
        int? failCreationNumber) : IRecoveryCheckpointService
    {
        public List<RecoveryCheckpoint> Created { get; } = [];
        public Task<RecoveryCheckpointCreationResult> CreateAsync(RecoveryCheckpointCreationRequest request, CancellationToken cancellationToken = default)
        {
            if (failCreationNumber == Created.Count + 1)
            {
                return Task.FromResult(new RecoveryCheckpointCreationResult(RecoveryCheckpointCreationStatus.PersistenceUnavailable, ErrorMessage: "forced checkpoint failure"));
            }

            var checkpoint = new RecoveryCheckpoint(
                request.ProjectId,
                request.CheckpointId,
                RecoveryCheckpointSchema.CurrentVersion,
                request.CreatedAt ?? DateTimeOffset.UtcNow,
                request.LifecycleState,
                new RecoveryContextReference(context.ContextId, context.ContractVersion, context.UpdatedAt),
                request.PlanningContractReference,
                request.WorkGraphReference,
                request.WorkGraphNodeId,
                request.HandoffPackageReference,
                request.PreviousCheckpointReference,
                request.SelectedAgentRoleReferences,
                request.EvidenceReferences,
                request.GateSnapshots,
                request.Blockers,
                request.NextSafeAction,
                request.Explanation);
            Created.Add(checkpoint);
            checkpoints.Values[checkpoint.CheckpointId] = checkpoint;
            var generation = (heads.Current?.Generation ?? 0) + 1;
            var head = new ContinuationHead(checkpoint.ProjectId, ContinuationHeadSchema.CurrentVersion, generation, checkpoint.Reference, heads.Current?.LastSafeCheckpointReference, checkpoint.CreatedAt);
            heads.Current = head;
            return Task.FromResult(new RecoveryCheckpointCreationResult(RecoveryCheckpointCreationStatus.Created, checkpoint, head));
        }
    }

    private sealed class FakeRunAuthorityRepository(bool failCreate) : IExecutionRunAuthorityRepository
    {
        private readonly Dictionary<(Guid ProjectId, Guid RunId), ExecutionRunAuthority> _values = new();
        public Task<ExecutionRunAuthorityRepositoryWriteResult> CreateAsync(ExecutionRunAuthority authority, CancellationToken cancellationToken = default) =>
            Task.FromResult(failCreate
                ? new ExecutionRunAuthorityRepositoryWriteResult(ExecutionRunAuthorityRepositoryWriteStatus.Unavailable, "forced authority failure")
                : _values.TryAdd((authority.ProjectId, authority.RunId), authority)
                ? new ExecutionRunAuthorityRepositoryWriteResult(ExecutionRunAuthorityRepositoryWriteStatus.Created)
                : new ExecutionRunAuthorityRepositoryWriteResult(ExecutionRunAuthorityRepositoryWriteStatus.RunConflict));
        public Task<ExecutionRunAuthorityReadResult> GetAsync(Guid projectId, Guid runId, CancellationToken cancellationToken = default) => Task.FromResult(_values.TryGetValue((projectId, runId), out var authority) ? new ExecutionRunAuthorityReadResult(ExecutionRunAuthorityReadState.Valid, authority) : new ExecutionRunAuthorityReadResult(ExecutionRunAuthorityReadState.Missing));
    }

    private sealed class FakeHistory(ExecutionRunStatus? failStatus) : IProjectOrchestrationStore
    {
        public List<ExecutionRun> Runs { get; } = [];
        public Task AppendExecutionRunAsync(ExecutionRun run, CancellationToken cancellationToken = default)
        {
            if (run.Status == failStatus)
            {
                throw new IOException("forced history failure");
            }
            Runs.Add(run);
            return Task.CompletedTask;
        }
        public Task<HistoryReadResult<ExecutionRun>> ReadExecutionRunsAsync(Guid projectId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default) => Task.FromResult(new HistoryReadResult<ExecutionRun>(Runs, HistoryReadStatus.Success));
        public Task AppendEvidenceAsync(EvidenceMetadata evidence, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<HistoryReadResult<EvidenceMetadata>> ReadEvidenceAsync(Guid projectId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default) => Task.FromResult(new HistoryReadResult<EvidenceMetadata>([], HistoryReadStatus.Success));
        public Task AppendReviewAsync(ReviewMetadata review, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<HistoryReadResult<ReviewMetadata>> ReadReviewsAsync(Guid projectId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default) => Task.FromResult(new HistoryReadResult<ReviewMetadata>([], HistoryReadStatus.Success));
        public Task AppendActivityAsync(ActivityAuditRecord activity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<HistoryReadResult<ActivityAuditRecord>> ReadActivityAsync(Guid projectId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default) => Task.FromResult(new HistoryReadResult<ActivityAuditRecord>([], HistoryReadStatus.Success));
    }

    private sealed class FakeExecutionAdapter(
        ExecutionAdapterDescriptor descriptor,
        bool hang,
        ExecutionAdapterResult? result) : IExecutionAdapter
    {
        private TaskCompletionSource<ExecutionAdapterResult>? _release;
        public ExecutionAdapterDescriptor Descriptor { get; } = descriptor;
        public int InvocationCount { get; private set; }
        public ExecutionAdapterRequest? LastRequest { get; private set; }
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool CancellationObserved { get; private set; }

        public Task<ExecutionAdapterResult> ExecuteAsync(ExecutionAdapterRequest request, CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            LastRequest = request;
            Started.TrySetResult(true);
            if (!hang)
            {
                return Task.FromResult(result ?? new ExecutionAdapterResult(ExecutionAdapterOutcome.Succeeded, "completed"));
            }

            _release = new TaskCompletionSource<ExecutionAdapterResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            cancellationToken.Register(() =>
            {
                CancellationObserved = true;
                _release.TrySetResult(new ExecutionAdapterResult(ExecutionAdapterOutcome.Cancelled, "cancelled", "test-cancel"));
            });
            return _release.Task;
        }

        public void Release(ExecutionAdapterResult result) => _release?.TrySetResult(result);
    }
}
