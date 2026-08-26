using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Planning;

namespace AIUsageMonitor.Connection.Tests;

public sealed class PlanningExecutionContractTests
{
    [Fact]
    public void ValidContractSeparatesSchemaVersionFromRevision()
    {
        var contract = ContractFixture.Create(
            revision: 3,
            previousRevision: 2,
            previousContentHash: new string('a', 64));

        Assert.Equal(PlanningExecutionContractSchema.CurrentVersion, contract.SchemaVersion);
        Assert.Equal(3, contract.Revision);
        Assert.Equal(contract.ContractId, contract.Reference.ContractId);
        Assert.Equal(contract.Revision, contract.Reference.Revision);
        Assert.Equal(contract.SchemaVersion, contract.Reference.SchemaVersion);
    }

    [Theory]
    [InlineData("project")]
    [InlineData("contract")]
    [InlineData("owner")]
    public void EmptyIdentityIsRejected(string identity)
    {
        var exception = Assert.Throws<ArgumentException>(() => identity switch
        {
            "project" => ContractFixture.Create(projectId: Guid.Empty),
            "contract" => ContractFixture.Create(contractId: Guid.Empty),
            _ => ContractFixture.Create(ownerReference: " ")
        });

        Assert.True(
            exception.Message.Contains("empty", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EmptyPlannerAgentIsRejected() =>
        Assert.Throws<ArgumentException>(() => ContractFixture.Create(plannerAgentId: Guid.Empty));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveRevisionIsRejected(int revision) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ContractFixture.Create(revision: revision));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveSchemaIsRejected(int schemaVersion) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ContractFixture.Create(schemaVersion: schemaVersion));

    [Fact]
    public void UndefinedWorkItemSourceIsRejected() =>
        Assert.Throws<ArgumentException>(() => new PlanningWorkItem((PlanningWorkItemSource)999, "APO-40", "Contract"));

    [Fact]
    public void BlankWorkItemReferenceAndTitleAreRejected()
    {
        Assert.Throws<ArgumentException>(() => new PlanningWorkItem(PlanningWorkItemSource.Jira, " ", "Contract"));
        Assert.Throws<ArgumentException>(() => new PlanningWorkItem(PlanningWorkItemSource.Jira, "APO-40", " "));
    }

    [Fact]
    public void RepositoryTargetTruthfullyDistinguishesNoneAndLocalGit()
    {
        var none = new PlanningRepositoryTarget(PlanningRepositoryMode.None);
        var local = new PlanningRepositoryTarget(
            PlanningRepositoryMode.LocalGit,
            @"D:\workspace",
            "main",
            new string('a', 40));

        Assert.Null(none.RegisteredLocalPath);
        Assert.Equal(PlanningRepositoryMode.LocalGit, local.Mode);
        Assert.Equal("main", local.ExpectedBranch);
        Assert.Throws<ArgumentException>(() => new PlanningRepositoryTarget(PlanningRepositoryMode.None, @"D:\workspace"));
        Assert.Throws<ArgumentException>(() => new PlanningRepositoryTarget(
            PlanningRepositoryMode.LocalGit,
            @"D:\workspace",
            "main",
            "not-a-full-object-id"));
    }

    [Fact]
    public void EmptyIncludedScopeIsRejected() =>
        Assert.Throws<ArgumentException>(() => ContractFixture.Create(includedScope: []));

    [Fact]
    public void DuplicateAndNullScopeClausesAreRejected()
    {
        var duplicate = new PlanningScopeClause("scope", "first");
        Assert.Throws<ArgumentException>(() => ContractFixture.Create(includedScope: [duplicate, new("scope", "second")]));
        Assert.Throws<ArgumentException>(() => ContractFixture.Create(includedScope: [null!]));
        Assert.Throws<ArgumentException>(() => ContractFixture.Create(forbiddenScope: [duplicate, new("scope", "second")]));
    }

    [Fact]
    public void ScopeClausesHaveDeterministicOrdering()
    {
        var contract = ContractFixture.Create(
            includedScope: [new("z", "last"), new("a", "first")],
            constraints: [new("z", "last"), new("a", "first")],
            forbiddenScope: [new("z", "last"), new("a", "first")]);

        Assert.Equal(["a", "z"], contract.IncludedScope.Select(value => value.Id));
        Assert.Equal(["a", "z"], contract.Constraints.Select(value => value.Id));
        Assert.Equal(["a", "z"], contract.ForbiddenScope.Select(value => value.Id));
    }

    [Fact]
    public void DeliverablesRequireOneRequiredUniqueNonBlankEntry()
    {
        Assert.Throws<ArgumentException>(() => ContractFixture.Create(
            deliverables: [new("d1", "deliverable", required: false)]));
        Assert.Throws<ArgumentException>(() => ContractFixture.Create(
            deliverables: [new("d1", "one", true), new("d1", "two", true)]));
        Assert.Throws<ArgumentException>(() => new PlanningDeliverable("d1", " ", true));
    }

    [Fact]
    public void ValidationRequirementsAreTypedDataAndIdentifiersAreUnique()
    {
        Assert.Throws<ArgumentException>(() => new PlanningValidationRequirement(
            "v1",
            (PlanningValidationKind)999,
            "validation",
            true));
        Assert.Throws<ArgumentException>(() => ContractFixture.Create(
            validationRequirements:
            [
                new("v1", PlanningValidationKind.Build, "build", true),
                new("v1", PlanningValidationKind.Test, "test", true)
            ]));
        Assert.Throws<ArgumentException>(() => new PlanningValidationRequirement(
            "v1",
            PlanningValidationKind.Custom,
            " ",
            true));

        var value = new PlanningValidationRequirement(
            "v1",
            PlanningValidationKind.Custom,
            "Data only",
            true,
            "dotnet test");
        Assert.Equal("dotnet test", value.CommandOrReference);
    }

    [Fact]
    public void AcceptanceCriteriaRequireOneRequiredUniqueNonBlankEntry()
    {
        Assert.Throws<ArgumentException>(() => ContractFixture.Create(
            acceptanceCriteria: [new("c1", "criterion", required: false)]));
        Assert.Throws<ArgumentException>(() => ContractFixture.Create(
            acceptanceCriteria: [new("c1", "one", true), new("c1", "two", true)]));
        Assert.Throws<ArgumentException>(() => new PlanningAcceptanceCriterion("c1", " ", true));
    }

    [Fact]
    public void BudgetsRequirePositiveUniqueKindsAndAreSorted()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlanningExecutionBudget(PlanningBudgetKind.Attempts, 0));
        Assert.Throws<ArgumentException>(() => new PlanningExecutionBudget((PlanningBudgetKind)999, 1));
        Assert.Throws<ArgumentException>(() => ContractFixture.Create(
            executionBudgets:
            [
                new(PlanningBudgetKind.ModelTurns, 2),
                new(PlanningBudgetKind.ModelTurns, 3)
            ]));

        var contract = ContractFixture.Create(
            executionBudgets:
            [
                new(PlanningBudgetKind.ModelTurns, 2),
                new(PlanningBudgetKind.Attempts, 1)
            ]);
        Assert.Equal([PlanningBudgetKind.Attempts, PlanningBudgetKind.ModelTurns], contract.ExecutionBudgets.Select(value => value.Kind));
    }

    [Fact]
    public void StopConditionsRequireBoundaryKindsAndUniqueDescriptions()
    {
        Assert.Throws<ArgumentException>(() => new PlanningStopCondition("s1", (PlanningStopConditionKind)999, "stop"));
        Assert.Throws<ArgumentException>(() => new PlanningStopCondition("s1", PlanningStopConditionKind.ScopeViolation, " "));
        Assert.Throws<ArgumentException>(() => ContractFixture.Create(
            stopConditions:
            [
                new("same", PlanningStopConditionKind.ImmutableTargetMoved, "target"),
                new("same", PlanningStopConditionKind.ScopeViolation, "scope"),
                new("budget", PlanningStopConditionKind.BudgetExceeded, "budget")
            ]));
        Assert.Throws<ArgumentException>(() => ContractFixture.Create(
            stopConditions:
            [
                new("target", PlanningStopConditionKind.ImmutableTargetMoved, "target"),
                new("scope", PlanningStopConditionKind.ScopeViolation, "scope")
            ]));
    }

    [Fact]
    public void RevisionOneCannotCarryPredecessorEvidence()
    {
        Assert.Throws<ArgumentException>(() => ContractFixture.Create(
            revision: 1,
            previousRevision: 0,
            previousContentHash: new string('a', 64)));
    }

    [Fact]
    public void RevisionsRequireImmediatePredecessorAndHash()
    {
        Assert.Throws<ArgumentException>(() => ContractFixture.Create(revision: 2));
        Assert.Throws<ArgumentException>(() => ContractFixture.Create(
            revision: 2,
            previousRevision: 1,
            previousContentHash: "not-a-hash"));
    }

    [Fact]
    public void EquivalentSemanticPayloadHasStableHash()
    {
        var first = ContractFixture.Create(
            includedScope: [new("z", "last"), new("a", "first")],
            executionBudgets:
            [
                new(PlanningBudgetKind.ModelTurns, 2),
                new(PlanningBudgetKind.Attempts, 1)
            ]);
        var second = ContractFixture.Create(
            projectId: first.ProjectId,
            contractId: first.ContractId,
            includedScope: [new("a", "first"), new("z", "last")],
            executionBudgets:
            [
                new(PlanningBudgetKind.Attempts, 1),
                new(PlanningBudgetKind.ModelTurns, 2)
            ]);

        Assert.Equal(first.ContentHash, second.ContentHash);
        Assert.Equal(first.ContentHash, first.Reference.ContentHash);
        Assert.DoesNotContain("signature", first.Reference.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ChangedSemanticPayloadChangesHash()
    {
        var first = ContractFixture.Create();
        var second = ContractFixture.Create(
            projectId: first.ProjectId,
            contractId: first.ContractId,
            includedScope: [new("include", "changed statement")]);

        Assert.NotEqual(first.ContentHash, second.ContentHash);
        Assert.Equal(64, first.ContentHash.Length);
    }

    private static class ContractFixture
    {
        public static PlanningExecutionContract Create(
            Guid? projectId = null,
            Guid? contractId = null,
            int schemaVersion = PlanningExecutionContractSchema.CurrentVersion,
            int revision = 1,
            string ownerReference = "owner-ref",
            Guid? plannerAgentId = null,
            PlanningWorkItem? workItem = null,
            PlanningRepositoryTarget? repositoryTarget = null,
            IReadOnlyList<PlanningScopeClause>? includedScope = null,
            IReadOnlyList<PlanningScopeClause>? constraints = null,
            IReadOnlyList<PlanningScopeClause>? forbiddenScope = null,
            IReadOnlyList<PlanningDeliverable>? deliverables = null,
            IReadOnlyList<PlanningValidationRequirement>? validationRequirements = null,
            IReadOnlyList<PlanningAcceptanceCriterion>? acceptanceCriteria = null,
            IReadOnlyList<PlanningExecutionBudget>? executionBudgets = null,
            IReadOnlyList<PlanningStopCondition>? stopConditions = null,
            int? previousRevision = null,
            string? previousContentHash = null,
            string? contentHash = null)
        {
            return new PlanningExecutionContract(
                projectId ?? Guid.Parse("11111111-1111-1111-1111-111111111111"),
                contractId ?? Guid.Parse("22222222-2222-2222-2222-222222222222"),
                schemaVersion,
                revision,
                new DateTimeOffset(2026, 8, 26, 0, 0, 0, TimeSpan.Zero),
                ownerReference,
                plannerAgentId ?? Guid.Parse("33333333-3333-3333-3333-333333333333"),
                new PlanningContextBinding(
                    Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    1),
                workItem ?? new PlanningWorkItem(PlanningWorkItemSource.Jira, "APO-40", "Define contracts"),
                repositoryTarget ?? new PlanningRepositoryTarget(PlanningRepositoryMode.None),
                includedScope ?? [new("include", "Implement the contract boundary")],
                constraints ?? [new("constraint", "Remain local and bounded")],
                forbiddenScope ?? [new("forbid", "Do not execute model work")],
                deliverables ?? [new("deliverable", "Immutable contract data", true)],
                validationRequirements ?? [new("build", PlanningValidationKind.Build, "Build the solution", true)],
                acceptanceCriteria ?? [new("accept", "Contract can be inspected", true)],
                executionBudgets ?? [new(PlanningBudgetKind.Attempts, 1)],
                stopConditions ?? [
                    new("target-moved", PlanningStopConditionKind.ImmutableTargetMoved, "Stop if target changes"),
                    new("scope", PlanningStopConditionKind.ScopeViolation, "Stop on scope violation"),
                    new("budget", PlanningStopConditionKind.BudgetExceeded, "Stop when budget is exceeded")
                ],
                ["governance/ref"],
                "routing/ref",
                "safety/ref",
                previousRevision,
                previousContentHash,
                contentHash);
        }
    }
}
