using AIUsageMonitor.Application.Agents;

namespace AIUsageMonitor.Application.Projects;

/// <summary>Semantic version of the initial, bounded APO-39 project context contract.</summary>
public static class ProjectContextContract
{
    public const int CurrentVersion = 1;
}

public enum RepositorySelectionState
{
    Inspect,
    Skipped
}

public enum TrackerReferenceState
{
    Skipped,
    ConfiguredUnverified,
    NotConfigured
}

public enum CurrentWorkState
{
    NotSelected
}

public enum ProjectNextSafeAction
{
    ReadyForPlanning,
    ReviewRepository,
    ReviewProjectContext
}

/// <summary>Sanitized local Git configuration evidence, never remote reachability evidence.</summary>
public sealed class RepositoryRemoteReference
{
    public RepositoryRemoteReference(string name, string sanitizedUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Remote name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(sanitizedUrl))
        {
            throw new ArgumentException("Sanitized remote URL is required.", nameof(sanitizedUrl));
        }

        Name = name.Trim();
        SanitizedUrl = sanitizedUrl.Trim();
    }

    public string Name { get; }

    public string SanitizedUrl { get; }
}

/// <summary>Bounded repository reference captured during project onboarding.</summary>
public sealed class ProjectRepositoryContextReference
{
    public ProjectRepositoryContextReference(
        Guid projectId,
        string registeredLocalPath,
        RepositorySelectionState selection,
        RepositoryVerificationStatus verificationStatus,
        string? repositoryRoot = null,
        bool? localPathIsRepositoryRoot = null,
        string? branchName = null,
        bool isDetachedHead = false,
        IReadOnlyList<RepositoryRemoteReference>? configuredRemotes = null,
        DateTimeOffset? capturedAt = null)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        if (string.IsNullOrWhiteSpace(registeredLocalPath))
        {
            throw new ArgumentException("Registered local path is required.", nameof(registeredLocalPath));
        }

        if (!Enum.IsDefined(selection))
        {
            throw new ArgumentException("Repository selection is undefined.", nameof(selection));
        }

        if (!Enum.IsDefined(verificationStatus))
        {
            throw new ArgumentException("Repository verification status is undefined.", nameof(verificationStatus));
        }

        ProjectId = projectId;
        RegisteredLocalPath = registeredLocalPath.Trim();
        Selection = selection;
        VerificationStatus = verificationStatus;
        RepositoryRoot = NormalizeOptional(repositoryRoot);
        LocalPathIsRepositoryRoot = localPathIsRepositoryRoot;
        BranchName = NormalizeOptional(branchName);
        IsDetachedHead = isDetachedHead;
        ConfiguredRemotes = (configuredRemotes ?? Array.Empty<RepositoryRemoteReference>()).ToArray();
        CapturedAt = capturedAt ?? DateTimeOffset.UtcNow;
    }

    public Guid ProjectId { get; }
    public string RegisteredLocalPath { get; }
    public RepositorySelectionState Selection { get; }
    public RepositoryVerificationStatus VerificationStatus { get; }
    public string? RepositoryRoot { get; }
    public bool? LocalPathIsRepositoryRoot { get; }
    public string? BranchName { get; }
    public bool IsDetachedHead { get; }
    public IReadOnlyList<RepositoryRemoteReference> ConfiguredRemotes { get; }
    public DateTimeOffset CapturedAt { get; }

    public static ProjectRepositoryContextReference Skipped(Guid projectId, string localPath) =>
        new(projectId, localPath, RepositorySelectionState.Skipped, RepositoryVerificationStatus.NotInspected);

    public static ProjectRepositoryContextReference FromInspection(
        Guid projectId,
        LocalRepositoryInspection inspection) =>
        new(
            projectId,
            inspection.RegisteredLocalPath,
            RepositorySelectionState.Inspect,
            inspection.Status,
            inspection.RepositoryRoot,
            inspection.LocalPathIsRepositoryRoot,
            inspection.BranchName,
            inspection.IsDetachedHead,
            inspection.Remotes
                .Select(remote => new RepositoryRemoteReference(remote.Name, remote.SanitizedUrl))
                .ToArray(),
            inspection.CapturedAt);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Bounded project-scoped model/role reference; it does not grant capabilities.</summary>
public sealed class ProjectModelRoleReference
{
    public ProjectModelRoleReference(
        Guid agentId,
        string displayName,
        IReadOnlyList<AgentRole> roles,
        bool enabled,
        AgentAvailability availability,
        AgentAuthenticationState authentication,
        AgentEntitlementState entitlement)
    {
        if (agentId == Guid.Empty)
        {
            throw new ArgumentException("Agent id cannot be empty.", nameof(agentId));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Agent display name is required.", nameof(displayName));
        }

        AgentId = agentId;
        DisplayName = displayName.Trim();
        Roles = (roles ?? throw new ArgumentNullException(nameof(roles))).Distinct().ToArray();
        Enabled = enabled;
        Availability = availability;
        Authentication = authentication;
        Entitlement = entitlement;
    }

    public Guid AgentId { get; }
    public string DisplayName { get; }
    public IReadOnlyList<AgentRole> Roles { get; }
    public bool Enabled { get; }
    public AgentAvailability Availability { get; }
    public AgentAuthenticationState Authentication { get; }
    public AgentEntitlementState Entitlement { get; }
}

public sealed class ProjectTrackerContextReference
{
    public ProjectTrackerContextReference(
        TrackerReferenceState state,
        string? type = null,
        string? reference = null)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentException("Tracker reference state is undefined.", nameof(state));
        }

        if (state == TrackerReferenceState.ConfiguredUnverified &&
            (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(reference)))
        {
            throw new ArgumentException("A configured tracker requires a bounded type and reference.");
        }

        State = state;
        Type = NormalizeOptional(type);
        Reference = NormalizeOptional(reference);
    }

    public TrackerReferenceState State { get; }
    public string? Type { get; }
    public string? Reference { get; }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class ProjectCurrentWorkReference
{
    public ProjectCurrentWorkReference(CurrentWorkState state)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentException("Current-work state is undefined.", nameof(state));
        }

        State = state;
    }

    public CurrentWorkState State { get; }
}

/// <summary>
/// Initial durable project context seed. It is metadata/reference truth, not an execution
/// checkpoint and contains no prompts, source, issue payloads, or credentials.
/// </summary>
public sealed class ProjectContextReference
{
    public ProjectContextReference(
        Guid projectId,
        Guid contextId,
        int contractVersion,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        ProjectRepositoryContextReference repository,
        ProjectTrackerContextReference tracker,
        IReadOnlyList<ProjectModelRoleReference> modelRoleReferences,
        ProjectCurrentWorkReference currentWork,
        IReadOnlyList<string>? governanceReferences,
        string? routingPolicyReference,
        string? safetyPolicyReference,
        ProjectNextSafeAction nextSafeAction)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        if (contextId == Guid.Empty)
        {
            throw new ArgumentException("Context id cannot be empty.", nameof(contextId));
        }

        if (contractVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(contractVersion));
        }

        if (updatedAt < createdAt)
        {
            throw new ArgumentException("Context UpdatedAt cannot precede CreatedAt.", nameof(updatedAt));
        }

        if (!Enum.IsDefined(nextSafeAction))
        {
            throw new ArgumentException("Next-safe-action value is undefined.", nameof(nextSafeAction));
        }

        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(tracker);
        ArgumentNullException.ThrowIfNull(modelRoleReferences);
        ArgumentNullException.ThrowIfNull(currentWork);

        if (repository.ProjectId != projectId)
        {
            throw new ArgumentException("Repository reference belongs to another project.", nameof(repository));
        }

        ProjectId = projectId;
        ContextId = contextId;
        ContractVersion = contractVersion;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        Repository = repository;
        Tracker = tracker;
        ModelRoleReferences = modelRoleReferences.ToArray();
        CurrentWork = currentWork;
        GovernanceReferences = (governanceReferences ?? Array.Empty<string>())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .ToArray();
        RoutingPolicyReference = NormalizeOptional(routingPolicyReference);
        SafetyPolicyReference = NormalizeOptional(safetyPolicyReference);
        NextSafeAction = nextSafeAction;
    }

    public Guid ProjectId { get; }
    public Guid ContextId { get; }
    public int ContractVersion { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; }
    public ProjectRepositoryContextReference Repository { get; }
    public ProjectTrackerContextReference Tracker { get; }
    public IReadOnlyList<ProjectModelRoleReference> ModelRoleReferences { get; }
    public ProjectCurrentWorkReference CurrentWork { get; }
    public IReadOnlyList<string> GovernanceReferences { get; }
    public string? RoutingPolicyReference { get; }
    public string? SafetyPolicyReference { get; }
    public ProjectNextSafeAction NextSafeAction { get; }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
