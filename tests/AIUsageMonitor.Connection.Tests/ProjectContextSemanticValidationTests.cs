using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Projects;

namespace AIUsageMonitor.Connection.Tests;

public sealed class ProjectContextSemanticValidationTests
{
    [Fact]
    public void SkippedRepositoryCannotCarryVerifiedEvidence()
    {
        var projectId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new ProjectRepositoryContextReference(
            projectId,
            "C:\\workspace",
            RepositorySelectionState.Skipped,
            RepositoryVerificationStatus.AvailableClean));
        Assert.Throws<ArgumentException>(() => new ProjectRepositoryContextReference(
            projectId,
            "C:\\workspace",
            RepositorySelectionState.Skipped,
            RepositoryVerificationStatus.NotInspected,
            branchName: "main"));
        Assert.Throws<ArgumentException>(() => new ProjectRepositoryContextReference(
            projectId,
            "C:\\workspace",
            RepositorySelectionState.Skipped,
            RepositoryVerificationStatus.NotInspected,
            configuredRemotes: [new RepositoryRemoteReference("origin", "https://example.test/repo.git")]));
    }

    [Fact]
    public void TrackerStatesCannotCarryContradictoryIdentity()
    {
        Assert.Throws<ArgumentException>(() => new ProjectTrackerContextReference(
            TrackerReferenceState.Skipped,
            "Jira",
            "APO-39"));
        Assert.Throws<ArgumentException>(() => new ProjectTrackerContextReference(
            TrackerReferenceState.NotConfigured,
            "Jira",
            "APO-39"));
        Assert.Throws<ArgumentException>(() => new ProjectTrackerContextReference(
            TrackerReferenceState.ConfiguredUnverified,
            reference: "APO-39"));
        Assert.Throws<ArgumentException>(() => new ProjectTrackerContextReference(
            TrackerReferenceState.ConfiguredUnverified,
            type: "Jira"));
    }

    [Fact]
    public void UndefinedModelRoleTruthIsRejected()
    {
        var agentId = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => CreateModel(agentId, roles: [(AgentRole)999]));
        Assert.Throws<ArgumentException>(() => CreateModel(agentId, availability: (AgentAvailability)999));
        Assert.Throws<ArgumentException>(() => CreateModel(agentId, authentication: (AgentAuthenticationState)999));
        Assert.Throws<ArgumentException>(() => CreateModel(agentId, entitlement: (AgentEntitlementState)999));
    }

    [Fact]
    public void DuplicateModelAgentReferencesAreRejectedAndRolesNormalizeDeterministically()
    {
        var agentId = Guid.NewGuid();
        var first = CreateModel(agentId, roles: [AgentRole.Reviewer, AgentRole.Executor, AgentRole.Reviewer]);
        var second = CreateModel(agentId);

        Assert.Equal([AgentRole.Executor, AgentRole.Reviewer], first.Roles);
        Assert.Throws<ArgumentException>(() => CreateContext([first, second]));
    }

    private static ProjectModelRoleReference CreateModel(
        Guid agentId,
        IReadOnlyList<AgentRole>? roles = null,
        AgentAvailability availability = AgentAvailability.Unknown,
        AgentAuthenticationState authentication = AgentAuthenticationState.Unknown,
        AgentEntitlementState entitlement = AgentEntitlementState.Unknown) =>
        new(
            agentId,
            "Test agent",
            roles ?? [AgentRole.Executor],
            enabled: true,
            availability,
            authentication,
            entitlement);

    private static ProjectContextReference CreateContext(IReadOnlyList<ProjectModelRoleReference> models)
    {
        var projectId = Guid.NewGuid();
        return new(
            projectId,
            Guid.NewGuid(),
            ProjectContextContract.CurrentVersion,
            new DateTimeOffset(2026, 8, 26, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 26, 0, 0, 0, TimeSpan.Zero),
            ProjectRepositoryContextReference.Skipped(projectId, "C:\\workspace"),
            new ProjectTrackerContextReference(TrackerReferenceState.Skipped),
            models,
            new ProjectCurrentWorkReference(CurrentWorkState.NotSelected),
            [],
            null,
            null,
            ProjectNextSafeAction.ReadyForPlanning);
    }
}
