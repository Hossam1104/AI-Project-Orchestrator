namespace AIUsageMonitor.Application.Projects;

public sealed class ProjectOnboardingRequest
{
    public string Name { get; init; } = string.Empty;
    public string LocalPath { get; init; } = string.Empty;
    public bool SkipRepository { get; init; }
    public LocalRepositoryInspection? RepositoryInspection { get; init; }
    public string? RepositoryDefaultBranch { get; init; }
    public bool SkipTracker { get; init; } = true;
    public string? TrackerType { get; init; }
    public string? TrackerReference { get; init; }
    public IReadOnlyCollection<Guid>? EnabledAgentIds { get; init; }
}

public sealed class ProjectOnboardingResult
{
    private ProjectOnboardingResult(
        bool succeeded,
        Project? project,
        ProjectContextReference? context,
        string? errorMessage)
    {
        Succeeded = succeeded;
        Project = project;
        Context = context;
        ErrorMessage = errorMessage;
    }

    public bool Succeeded { get; }
    public Project? Project { get; }
    public ProjectContextReference? Context { get; }
    public string? ErrorMessage { get; }

    public static ProjectOnboardingResult Success(Project project, ProjectContextReference context) =>
        new(true, project, context, null);

    public static ProjectOnboardingResult Failure(Project? project, string errorMessage) =>
        new(false, project, null, errorMessage);
}

/// <summary>Coordinates the bounded APO-39 onboarding workflow without routing or execution.</summary>
public interface IProjectOnboardingService
{
    Task<LocalRepositoryInspection> InspectRepositoryAsync(
        string localPath,
        CancellationToken cancellationToken = default);

    Task<ProjectOnboardingResult> CompleteAsync(
        ProjectOnboardingRequest request,
        CancellationToken cancellationToken = default);
}
