namespace AIUsageMonitor.Application.Projects;

public enum ProjectContextReadState
{
    Missing,
    Valid,
    UnsupportedVersion,
    Invalid,
    Unavailable
}

public sealed record ProjectContextReadResult(
    ProjectContextReadState State,
    ProjectContextReference? Context = null,
    string? ErrorMessage = null);

/// <summary>Project GUID-scoped persistence for the single current onboarding context reference.</summary>
public interface IProjectContextReferenceRepository
{
    Task<ProjectContextReadResult> GetAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        ProjectContextReference context,
        CancellationToken cancellationToken = default);
}
