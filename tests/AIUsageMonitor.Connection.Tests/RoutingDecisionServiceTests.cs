using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Handoffs;
using AIUsageMonitor.Application.Planning;
using AIUsageMonitor.Application.Projects;
using AIUsageMonitor.Application.Routing;
using AIUsageMonitor.Application.Time;

namespace AIUsageMonitor.Connection.Tests;

public sealed class RoutingDecisionServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid ProjectId = Guid.Parse("90000000-0000-0000-0000-000000000001");
    private static readonly Guid AgentId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid ContextId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid ContractId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task ServiceUsesExactContractRevisionAndPersistsDecisionWithoutExecutingIt()
    {
        var contract = Contract();
        var contexts = new FakeContextResolver(Context(contract.Context));
        var contracts = new FakeContractRepository(contract);
        var decisions = new FakeDecisionRepository();
        var service = new RoutingDecisionService(
            new RoutingInputAssembler(contexts, contracts, new HandoffRedactionService()),
            new RoutingDecisionEngine(),
            decisions,
            new FixedClock(Now));

        var result = await service.CreateAsync(Request(contract.Reference));

        Assert.True(result.Succeeded);
        Assert.Equal(1, contracts.ExactRevisionCalls);
        Assert.Equal(0, contracts.LatestCalls);
        Assert.NotNull(decisions.Decision);
        Assert.Equal(RoutingDecisionSchema.CurrentVersion, decisions.Decision!.SchemaVersion);
        Assert.Equal(contract.Reference.ContentHash, decisions.Decision.Input.PlanningContractReference.ContentHash);
    }

    [Fact]
    public async Task ContextBindingMismatchFailsClosedBeforeRouting()
    {
        var contract = Contract();
        var mismatchedContext = Context(contract.Context, contextId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var service = new RoutingDecisionService(
            new RoutingInputAssembler(
                new FakeContextResolver(mismatchedContext),
                new FakeContractRepository(contract),
                new HandoffRedactionService()),
            new RoutingDecisionEngine(),
            new FakeDecisionRepository(),
            new FixedClock(Now));

        var result = await service.CreateAsync(Request(contract.Reference));

        Assert.Equal(RoutingDecisionCreationStatus.ContractMismatch, result.Status);
    }

    [Fact]
    public async Task SecretShapedTrustedIdentityIsRejectedRatherThanInventingSafeIdentity()
    {
        var contract = Contract();
        var secretAgent = new AgentDefinition(
            AgentId,
            "password=do-not-store-this",
            "executor",
            AgentConnectionMode.Api,
            AgentAvailability.Available,
            true,
            Now,
            Now,
            provider: "provider",
            capabilities: ["code"],
            roleCapabilities: [AgentRole.Executor],
            supportedConnectionModes: [AgentConnectionMode.Api],
            authenticationState: AgentAuthenticationState.Authenticated,
            entitlementState: AgentEntitlementState.VerifiedAvailable,
            modelIdentifier: "model");
        var effective = new EffectiveAgentDefinition(ProjectId, secretAgent, null);
        var context = Context(contract.Context, effective);
        var service = new RoutingDecisionService(
            new RoutingInputAssembler(
                new FakeContextResolver(context, effective),
                new FakeContractRepository(contract),
                new HandoffRedactionService()),
            new RoutingDecisionEngine(),
            new FakeDecisionRepository(),
            new FixedClock(Now));

        var result = await service.CreateAsync(Request(contract.Reference));

        Assert.Equal(RoutingDecisionCreationStatus.RedactionRejected, result.Status);
    }

    private static RoutingDecisionRequest Request(PlanningExecutionContractReference reference) => new(
        ProjectId,
        reference,
        new RoutingTaskClassification(
            RoutingScopeScale.Bounded,
            RoutingTaskRisk.Low,
            RoutingBlastRadius.Local,
            RoutingValidationCost.Low,
            AgentRole.Executor,
            ["code"]),
        new RoutingPolicySnapshot("policy:service", AgentRole.Executor),
        [new RoutingCapacityEvidence(AgentId, RoutingCapacityState.Sufficient, Now, Now.AddHours(1), "manual:fixture", source: RoutingCapacityEvidenceSource.Manual)]);

    private static ProjectContextReference Context(
        PlanningContextBinding binding,
        EffectiveAgentDefinition? effective = null,
        Guid? contextId = null)
    {
        var agent = effective?.GlobalDefinition ?? new AgentDefinition(
            AgentId,
            "Service Agent",
            "executor",
            AgentConnectionMode.Api,
            AgentAvailability.Available,
            true,
            Now,
            Now,
            provider: "provider",
            capabilities: ["code"],
            roleCapabilities: [AgentRole.Executor],
            supportedConnectionModes: [AgentConnectionMode.Api],
            authenticationState: AgentAuthenticationState.Authenticated,
            entitlementState: AgentEntitlementState.VerifiedAvailable,
            modelIdentifier: "model");
        return new(
            ProjectId,
            contextId ?? ContextId,
            1,
            Now,
            Now,
            ProjectRepositoryContextReference.Skipped(ProjectId, @"D:\\APO-test"),
            new ProjectTrackerContextReference(TrackerReferenceState.Skipped),
            [new ProjectModelRoleReference(agent.Id, agent.Name, [AgentRole.Executor], true, AgentAvailability.Available, AgentAuthenticationState.Authenticated, AgentEntitlementState.VerifiedAvailable)],
            new ProjectCurrentWorkReference(CurrentWorkState.NotSelected),
            [],
            null,
            null,
            ProjectNextSafeAction.ReadyForPlanning);
    }

    private static PlanningExecutionContract Contract() => new(
        ProjectId,
        ContractId,
        PlanningExecutionContractSchema.CurrentVersion,
        1,
        Now,
        "planner:sol",
        AgentId,
        new PlanningContextBinding(ContextId, 1),
        new PlanningWorkItem(PlanningWorkItemSource.Jira, "APO-44", "Routing"),
        new PlanningRepositoryTarget(PlanningRepositoryMode.None),
        [new PlanningScopeClause("include", "Routing contract")],
        [],
        [new PlanningScopeClause("forbid", "Execution")],
        [new PlanningDeliverable("decision", "Decision", true)],
        [new PlanningValidationRequirement("build", PlanningValidationKind.Build, "Build", true)],
        [new PlanningAcceptanceCriterion("accept", "Accept", true)],
        [new PlanningExecutionBudget(PlanningBudgetKind.Attempts, 1)],
        [
            new PlanningStopCondition("target", PlanningStopConditionKind.ImmutableTargetMoved, "Target"),
            new PlanningStopCondition("scope", PlanningStopConditionKind.ScopeViolation, "Scope"),
            new PlanningStopCondition("budget", PlanningStopConditionKind.BudgetExceeded, "Budget")
        ],
        [],
        null,
        null);

    private sealed class FixedClock(DateTimeOffset value) : IClock
    {
        public DateTimeOffset UtcNow { get; } = value;
    }

    private sealed class FakeContextResolver(ProjectContextReference context, EffectiveAgentDefinition? suppliedEffective = null) : IProjectContextResolver
    {
        public Task<ProjectContextResolution> ResolveAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            var project = new Project(ProjectId, "APO", @"D:\\APO-test", null, ProjectStatus.Active, Now, Now);
            var effective = suppliedEffective ?? new EffectiveAgentDefinition(
                ProjectId,
                new AgentDefinition(
                    AgentId,
                    "Service Agent",
                    "executor",
                    AgentConnectionMode.Api,
                    AgentAvailability.Available,
                    true,
                    Now,
                    Now,
                    provider: "provider",
                    capabilities: ["code"],
                    roleCapabilities: [AgentRole.Executor],
                    supportedConnectionModes: [AgentConnectionMode.Api],
                    authenticationState: AgentAuthenticationState.Authenticated,
                    entitlementState: AgentEntitlementState.VerifiedAvailable,
                    modelIdentifier: "model"),
                null);
            return Task.FromResult(new ProjectContextResolution(
                ProjectContextResolutionState.Ready,
                new ProjectContextView(project, context, [effective])));
        }
    }

    private sealed class FakeContractRepository(PlanningExecutionContract contract) : IPlanningExecutionContractRepository
    {
        public int ExactRevisionCalls { get; private set; }
        public int LatestCalls { get; private set; }

        public Task<PlanningContractRepositoryWriteResult> CreateAsync(PlanningExecutionContract value, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PlanningContractRepositoryWriteResult(PlanningContractRepositoryWriteStatus.Created));

        public Task<PlanningContractReadResult> GetAsync(Guid projectId, Guid contractId, int revision, CancellationToken cancellationToken = default)
        {
            ExactRevisionCalls++;
            return Task.FromResult(new PlanningContractReadResult(PlanningContractReadState.Valid, contract));
        }

        public Task<PlanningContractReadResult> GetLatestAsync(Guid projectId, Guid contractId, CancellationToken cancellationToken = default)
        {
            LatestCalls++;
            throw new InvalidOperationException("Latest contract fallback is forbidden by APO-44.");
        }

        public Task<PlanningContractRevisionListResult> ListRevisionsAsync(Guid projectId, Guid contractId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PlanningContractRevisionListResult(PlanningContractReadState.Valid, [contract]));
    }

    private sealed class FakeDecisionRepository : IRoutingDecisionRepository
    {
        public RoutingDecision? Decision { get; private set; }

        public Task<RoutingDecisionRepositoryWriteResult> CreateAsync(RoutingDecision decision, CancellationToken cancellationToken = default)
        {
            Decision = decision;
            return Task.FromResult(new RoutingDecisionRepositoryWriteResult(RoutingDecisionRepositoryWriteStatus.Created));
        }

        public Task<RoutingDecisionReadResult> GetAsync(Guid projectId, Guid decisionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new RoutingDecisionReadResult(RoutingDecisionReadState.Valid, Decision));
    }
}
