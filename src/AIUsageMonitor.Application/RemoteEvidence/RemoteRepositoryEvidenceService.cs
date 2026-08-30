using AIUsageMonitor.Application.Projects;

namespace AIUsageMonitor.Application.RemoteEvidence;

public sealed class RemoteRepositoryEvidenceService : IRemoteRepositoryEvidenceService
{
    private readonly IReadOnlyDictionary<RemoteRepositoryProvider, IRemoteRepositoryEvidenceProvider> _providers;

    public RemoteRepositoryEvidenceService(IEnumerable<IRemoteRepositoryEvidenceProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        var materialized = providers.ToArray();
        if (materialized.GroupBy(provider => provider.Provider).Any(group => group.Count() != 1))
        {
            throw new InvalidOperationException("Remote evidence provider registration must be unique.");
        }

        _providers = materialized.ToDictionary(provider => provider.Provider);
    }

    public async Task<RemoteRepositoryEvidence> InspectAsync(
        Project project,
        string? requestedBranch = null,
        int? pullRequestNumber = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        var capturedAt = DateTimeOffset.UtcNow;
        if (string.IsNullOrWhiteSpace(project.RepositoryProvider) ||
            string.IsNullOrWhiteSpace(project.RepositoryUrl))
        {
            return Empty(project.Id, RemoteEvidenceState.NotConfigured, capturedAt,
                "The project has no complete remote repository configuration.");
        }

        if (!TryParseProvider(project.RepositoryProvider, out var provider))
        {
            return Empty(project.Id, RemoteEvidenceState.Unsupported, capturedAt,
                "The configured remote repository provider is unsupported.");
        }

        if (!_providers.TryGetValue(provider, out var adapter))
        {
            return Empty(project.Id, RemoteEvidenceState.Unsupported, capturedAt,
                "The configured remote repository provider is unavailable.");
        }

        try
        {
            return await adapter.InspectAsync(
                RemoteRepositoryEvidenceRequest.FromProject(project, requestedBranch, pullRequestNumber),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Empty(project.Id, RemoteEvidenceState.Cancelled, capturedAt,
                "Remote repository evidence was cancelled by the caller.");
        }
        catch (HttpRequestException)
        {
            return Empty(project.Id, RemoteEvidenceState.Unavailable, capturedAt,
                "Remote repository evidence was unavailable.");
        }
        catch (ArgumentException)
        {
            return Empty(project.Id, RemoteEvidenceState.InvalidResponse, capturedAt,
                "Remote repository configuration was invalid.");
        }
    }

    private static bool TryParseProvider(string value, out RemoteRepositoryProvider provider)
    {
        var normalized = value.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
        if (normalized.Equals("GitHub", StringComparison.OrdinalIgnoreCase))
        {
            provider = RemoteRepositoryProvider.GitHub;
            return true;
        }

        if (normalized.Equals("AzureRepos", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("AzureDevOps", StringComparison.OrdinalIgnoreCase))
        {
            provider = RemoteRepositoryProvider.AzureRepos;
            return true;
        }

        provider = default;
        return false;
    }

    private static RemoteRepositoryEvidence Empty(
        Guid projectId,
        RemoteEvidenceState state,
        DateTimeOffset capturedAt,
        string error) =>
        new(projectId, state, RemoteEvidenceSource.Unknown, capturedAt, safeErrorMessage: error);
}
