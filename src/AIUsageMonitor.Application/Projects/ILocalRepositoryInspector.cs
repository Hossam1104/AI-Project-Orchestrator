namespace AIUsageMonitor.Application.Projects;

/// <summary>
/// Infrastructure boundary for inspecting one local path without reading file contents or
/// contacting a remote service.
/// </summary>
public interface ILocalRepositoryInspector
{
    Task<LocalRepositoryInspection> InspectAsync(
        string registeredLocalPath,
        string? registeredRepositoryUrl = null,
        CancellationToken cancellationToken = default);
}
