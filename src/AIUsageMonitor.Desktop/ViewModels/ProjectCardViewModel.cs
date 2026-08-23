using System.Globalization;
using AIUsageMonitor.Application.Projects;

namespace AIUsageMonitor.Desktop.ViewModels;

/// <summary>
/// Presentation-only projection for a project registry card. It contains no additional state
/// and never probes the registered path or external systems.
/// </summary>
public sealed class ProjectCardViewModel
{
    public ProjectCardViewModel(Project project)
    {
        Project = project ?? throw new ArgumentNullException(nameof(project));
    }

    public Project Project { get; }

    public string Name => Project.Name;

    public string StatusText => Project.Status.ToString();

    public string LocalPath => Project.LocalPath;

    public string RepositorySummary => BuildSummary(
        Project.RepositoryProvider,
        Project.RepositoryUrl,
        Project.RepositoryId,
        Project.RepositoryMetadata.Count > 0);

    public string TrackerSummary => BuildSummary(
        Project.TrackerType,
        Project.TrackerId,
        hasHiddenConfiguration: Project.TrackerMetadata.Count > 0);

    public string UpdatedAtText => Project.UpdatedAt
        .ToLocalTime()
        .ToString("MMM d, yyyy h:mm tt", CultureInfo.CurrentCulture);

    private static string BuildSummary(
        string? first,
        string? second,
        string? third,
        bool hasHiddenConfiguration)
    {
        var values = new[] { first, second, third }
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!);

        var summary = string.Join(" · ", values);
        return summary.Length > 0
            ? summary
            : hasHiddenConfiguration ? "Configured" : "Not configured";
    }

    private static string BuildSummary(string? first, string? second, bool hasHiddenConfiguration) =>
        BuildSummary(first, second, null, hasHiddenConfiguration);
}
