using AIUsageMonitor.Application.Agents;
using AIUsageMonitor.Application.Projects;

namespace AIUsageMonitor.Infrastructure.Persistence;

internal sealed class ProjectContextReferenceRecord
{
    public Guid ProjectId { get; set; }
    public Guid ContextId { get; set; }
    public int ContractVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ProjectRepositoryContextReferenceRecord Repository { get; set; } = new();
    public ProjectTrackerContextReferenceRecord Tracker { get; set; } = new();
    public List<ProjectModelRoleReferenceRecord> ModelRoleReferences { get; set; } = [];
    public ProjectCurrentWorkReferenceRecord CurrentWork { get; set; } = new();
    public List<string> GovernanceReferences { get; set; } = [];
    public string? RoutingPolicyReference { get; set; }
    public string? SafetyPolicyReference { get; set; }
    public ProjectNextSafeAction NextSafeAction { get; set; }

    public static ProjectContextReferenceRecord FromApplication(ProjectContextReference value) => new()
    {
        ProjectId = value.ProjectId,
        ContextId = value.ContextId,
        ContractVersion = value.ContractVersion,
        CreatedAt = value.CreatedAt,
        UpdatedAt = value.UpdatedAt,
        Repository = ProjectRepositoryContextReferenceRecord.FromApplication(value.Repository),
        Tracker = ProjectTrackerContextReferenceRecord.FromApplication(value.Tracker),
        ModelRoleReferences = value.ModelRoleReferences
            .Select(ProjectModelRoleReferenceRecord.FromApplication)
            .ToList(),
        CurrentWork = ProjectCurrentWorkReferenceRecord.FromApplication(value.CurrentWork),
        GovernanceReferences = value.GovernanceReferences.ToList(),
        RoutingPolicyReference = value.RoutingPolicyReference,
        SafetyPolicyReference = value.SafetyPolicyReference,
        NextSafeAction = value.NextSafeAction
    };

    public ProjectContextReference ToApplication() => new(
        ProjectId,
        ContextId,
        ContractVersion,
        CreatedAt,
        UpdatedAt,
        Repository.ToApplication(ProjectId),
        Tracker.ToApplication(),
        (ModelRoleReferences ?? []).Select(value => value.ToApplication()).ToArray(),
        CurrentWork.ToApplication(),
        GovernanceReferences,
        RoutingPolicyReference,
        SafetyPolicyReference,
        NextSafeAction);
}

internal sealed class ProjectRepositoryContextReferenceRecord
{
    public string RegisteredLocalPath { get; set; } = string.Empty;
    public RepositorySelectionState Selection { get; set; }
    public RepositoryVerificationStatus VerificationStatus { get; set; }
    public string? RepositoryRoot { get; set; }
    public bool? LocalPathIsRepositoryRoot { get; set; }
    public string? BranchName { get; set; }
    public bool IsDetachedHead { get; set; }
    public List<RepositoryRemoteReferenceRecord> ConfiguredRemotes { get; set; } = [];
    public DateTimeOffset CapturedAt { get; set; }

    public static ProjectRepositoryContextReferenceRecord FromApplication(ProjectRepositoryContextReference value) => new()
    {
        RegisteredLocalPath = value.RegisteredLocalPath,
        Selection = value.Selection,
        VerificationStatus = value.VerificationStatus,
        RepositoryRoot = value.RepositoryRoot,
        LocalPathIsRepositoryRoot = value.LocalPathIsRepositoryRoot,
        BranchName = value.BranchName,
        IsDetachedHead = value.IsDetachedHead,
        ConfiguredRemotes = value.ConfiguredRemotes.Select(RepositoryRemoteReferenceRecord.FromApplication).ToList(),
        CapturedAt = value.CapturedAt
    };

    public ProjectRepositoryContextReference ToApplication(Guid projectId) => new(
        projectId,
        RegisteredLocalPath,
        Selection,
        VerificationStatus,
        RepositoryRoot,
        LocalPathIsRepositoryRoot,
        BranchName,
        IsDetachedHead,
        (ConfiguredRemotes ?? []).Select(value => value.ToApplication()).ToArray(),
        CapturedAt);
}

internal sealed class RepositoryRemoteReferenceRecord
{
    public string Name { get; set; } = string.Empty;
    public string SanitizedUrl { get; set; } = string.Empty;

    public static RepositoryRemoteReferenceRecord FromApplication(RepositoryRemoteReference value) => new()
    {
        Name = value.Name,
        SanitizedUrl = value.SanitizedUrl
    };

    public RepositoryRemoteReference ToApplication() => new(Name, SanitizedUrl);
}

internal sealed class ProjectTrackerContextReferenceRecord
{
    public TrackerReferenceState State { get; set; }
    public string? Type { get; set; }
    public string? Reference { get; set; }

    public static ProjectTrackerContextReferenceRecord FromApplication(ProjectTrackerContextReference value) => new()
    {
        State = value.State,
        Type = value.Type,
        Reference = value.Reference
    };

    public ProjectTrackerContextReference ToApplication() => new(State, Type, Reference);
}

internal sealed class ProjectModelRoleReferenceRecord
{
    public Guid AgentId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public List<AgentRole> Roles { get; set; } = [];
    public bool Enabled { get; set; }
    public AgentAvailability Availability { get; set; }
    public AgentAuthenticationState Authentication { get; set; }
    public AgentEntitlementState Entitlement { get; set; }

    public static ProjectModelRoleReferenceRecord FromApplication(ProjectModelRoleReference value) => new()
    {
        AgentId = value.AgentId,
        DisplayName = value.DisplayName,
        Roles = value.Roles.ToList(),
        Enabled = value.Enabled,
        Availability = value.Availability,
        Authentication = value.Authentication,
        Entitlement = value.Entitlement
    };

    public ProjectModelRoleReference ToApplication() => new(
        AgentId,
        DisplayName,
        Roles,
        Enabled,
        Availability,
        Authentication,
        Entitlement);
}

internal sealed class ProjectCurrentWorkReferenceRecord
{
    public CurrentWorkState State { get; set; }

    public static ProjectCurrentWorkReferenceRecord FromApplication(ProjectCurrentWorkReference value) => new()
    {
        State = value.State
    };

    public ProjectCurrentWorkReference ToApplication() => new(State);
}
