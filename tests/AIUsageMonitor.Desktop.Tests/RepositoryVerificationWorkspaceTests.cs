using AIUsageMonitor.Application.Projects;
using AIUsageMonitor.Application.Time;
using AIUsageMonitor.Desktop.ViewModels;

namespace AIUsageMonitor.Desktop.Tests;

public sealed class RepositoryVerificationWorkspaceTests
{
    [Fact]
    public async Task ProjectSelectionStartsNotInspected()
    {
        var project = CreateProject("Alpha", "C:\\alpha");
        var viewModel = CreateViewModel(new FakeRepositoryStateService(), project);
        await viewModel.InitializeAsync();

        Assert.Equal(RepositoryVerificationStatus.NotInspected, viewModel.RepositoryVerificationStatus);
        Assert.Equal("Not inspected", viewModel.RepositoryVerificationStatusText);
    }

    [Fact]
    public async Task VerifyCommandRequiresSelectedProject()
    {
        var viewModel = CreateViewModel(new FakeRepositoryStateService());
        await viewModel.InitializeAsync();

        Assert.Null(viewModel.SelectedProject);
        Assert.False(viewModel.VerifyRepositoryCommand.CanExecute(null));
    }

    [Fact]
    public async Task VerifyCommandIsDisabledWhileEditing()
    {
        var project = CreateProject("Alpha", "C:\\alpha");
        var viewModel = CreateViewModel(new FakeRepositoryStateService(), project);
        await viewModel.InitializeAsync();

        viewModel.EditProjectCommand.Execute(null);

        Assert.True(viewModel.IsEditing);
        Assert.False(viewModel.VerifyRepositoryCommand.CanExecute(null));
    }

    [Fact]
    public async Task VerifyCommandIsDisabledWhileAlreadyVerifying()
    {
        var project = CreateProject("Alpha", "C:\\alpha");
        var service = new FakeRepositoryStateService();
        var pending = service.AddPending(project.Id);
        var viewModel = CreateViewModel(service, project);
        await viewModel.InitializeAsync();

        var verification = viewModel.VerifyRepositoryAsync();

        Assert.True(viewModel.IsVerifying);
        Assert.False(viewModel.VerifyRepositoryCommand.CanExecute(null));
        pending.SetResult(CreateSnapshot(project, RepositoryVerificationStatus.AvailableClean, isClean: true));
        await verification;
    }

    [Fact]
    public async Task VerificationSuccessShowsCleanState()
    {
        var project = CreateProject("Alpha", "C:\\alpha");
        var service = new FakeRepositoryStateService
        {
            Factory = current => CreateSnapshot(current, RepositoryVerificationStatus.AvailableClean, isClean: true)
        };
        var viewModel = CreateViewModel(service, project);
        await viewModel.InitializeAsync();

        await viewModel.VerifyRepositoryAsync();

        Assert.False(viewModel.IsVerifying);
        Assert.Equal(RepositoryVerificationStatus.AvailableClean, viewModel.RepositoryVerificationStatus);
        Assert.Equal("Repository verified — clean", viewModel.RepositoryVerificationStatusText);
        Assert.Equal("Working tree clean", viewModel.RepositoryWorkingTreeText);
        Assert.True(viewModel.ShowRepositoryStateDetails);
    }

    [Fact]
    public async Task DirtyStateShowsChangeCountAndBoundedEvidence()
    {
        var project = CreateProject("Alpha", "C:\\alpha");
        var service = new FakeRepositoryStateService
        {
            Factory = current => CreateSnapshot(
                current,
                RepositoryVerificationStatus.AvailableDirty,
                isClean: false,
                changedFileTotal: 105,
                changedFilesTruncated: true,
                changedFiles: Enumerable.Range(1, 100)
                    .Select(index => new RepositoryChangedFile($"file-{index}.cs", RepositoryChangedFileKind.Modified))
                    .ToArray())
        };
        var viewModel = CreateViewModel(service, project);
        await viewModel.InitializeAsync();

        await viewModel.VerifyRepositoryAsync();

        Assert.Equal(105, viewModel.RepositoryState!.ChangedFileTotal);
        Assert.Equal(100, viewModel.ChangedFiles.Count);
        Assert.True(viewModel.RepositoryState.ChangedFilesTruncated);
        Assert.Equal("105 changes (showing first 100)", viewModel.ChangedFilesSummaryText);
        Assert.True(viewModel.ShowChangedFiles);
    }

    [Theory]
    [InlineData(RepositoryVerificationStatus.PathMissing, "Local path missing")]
    [InlineData(RepositoryVerificationStatus.PathUnavailable, "Local path unavailable")]
    [InlineData(RepositoryVerificationStatus.GitUnavailable, "Git unavailable")]
    [InlineData(RepositoryVerificationStatus.NotGitRepository, "Not a Git repository")]
    public async Task VerificationStatesRemainTruthful(
        RepositoryVerificationStatus status,
        string expectedText)
    {
        var project = CreateProject("Alpha", "C:\\alpha");
        var viewModel = CreateViewModel(new FakeRepositoryStateService
        {
            Factory = current => CreateSnapshot(current, status)
        }, project);
        await viewModel.InitializeAsync();

        await viewModel.VerifyRepositoryAsync();

        Assert.Equal(status, viewModel.RepositoryVerificationStatus);
        Assert.Equal(expectedText, viewModel.RepositoryVerificationStatusText);
        Assert.False(viewModel.ShowRepositoryStateDetails);
    }

    [Fact]
    public async Task FailureShowsBoundedError()
    {
        var project = CreateProject("Alpha", "C:\\alpha");
        var viewModel = CreateViewModel(new FakeRepositoryStateService
        {
            Exception = new InvalidOperationException("private command details")
        }, project);
        await viewModel.InitializeAsync();

        await viewModel.VerifyRepositoryAsync();

        Assert.Equal(RepositoryVerificationStatus.Failed, viewModel.RepositoryVerificationStatus);
        Assert.Equal("Repository verification failed", viewModel.RepositoryVerificationStatusText);
        Assert.Equal("Repository verification could not be completed.", viewModel.RepositoryVerificationErrorText);
        Assert.DoesNotContain("private command details", viewModel.RepositoryVerificationErrorText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SelectionChangeCancelsOrObsoletesOldResult()
    {
        var alpha = CreateProject("Alpha", "C:\\alpha");
        var beta = CreateProject("Beta", "C:\\beta");
        var service = new FakeRepositoryStateService();
        var alphaPending = service.AddPending(alpha.Id);
        var betaPending = service.AddPending(beta.Id);
        var viewModel = CreateViewModel(service, alpha, beta);
        await viewModel.InitializeAsync();
        viewModel.SelectProject(alpha.Id);

        var alphaVerification = viewModel.VerifyRepositoryAsync();
        viewModel.SelectProject(beta.Id);

        Assert.False(viewModel.IsVerifying);
        Assert.Equal(RepositoryVerificationStatus.NotInspected, viewModel.RepositoryVerificationStatus);

        var betaVerification = viewModel.VerifyRepositoryAsync();
        betaPending.SetResult(CreateSnapshot(beta, RepositoryVerificationStatus.AvailableClean, isClean: true));
        await betaVerification;
        alphaPending.SetResult(CreateSnapshot(alpha, RepositoryVerificationStatus.AvailableDirty, isClean: false, changedFileTotal: 1));
        await alphaVerification;

        Assert.Equal(beta.Id, viewModel.SelectedProject!.Id);
        Assert.Equal(beta.Id, viewModel.RepositoryState!.ProjectId);
        Assert.Equal(RepositoryVerificationStatus.AvailableClean, viewModel.RepositoryVerificationStatus);
    }

    [Fact]
    public async Task SuccessfulProjectEditResetsRepositoryState()
    {
        var project = CreateProject("Alpha", "C:\\alpha");
        var viewModel = CreateViewModel(new FakeRepositoryStateService
        {
            Factory = current => CreateSnapshot(current, RepositoryVerificationStatus.AvailableClean, isClean: true)
        }, project);
        await viewModel.InitializeAsync();
        await viewModel.VerifyRepositoryAsync();

        viewModel.EditProjectCommand.Execute(null);
        viewModel.EditorLocalPath = "C:\\alpha-updated";
        await viewModel.SaveAsync();

        Assert.False(viewModel.IsEditing);
        Assert.Null(viewModel.RepositoryState);
        Assert.Equal(RepositoryVerificationStatus.NotInspected, viewModel.RepositoryVerificationStatus);
    }

    [Fact]
    public void CredentialBearingRegisteredRepositoryUrlIsNeverShownVerbatim()
    {
        var project = new Project(
            Guid.NewGuid(),
            "Credential URL project",
            "C:\\credential-url",
            "main",
            ProjectStatus.Active,
            new DateTimeOffset(2026, 8, 23, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 23, 10, 0, 0, TimeSpan.Zero),
            repositoryProvider: "Git",
            repositoryUrl: "https://user:secret-token@example.invalid/org/repo.git?key=query-secret");

        var card = new ProjectCardViewModel(project);

        Assert.DoesNotContain("secret-token", card.RepositorySummary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("query-secret", card.RepositorySummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://example.invalid/org/repo.git", card.RepositorySummary, StringComparison.Ordinal);
    }

    private static ProjectsViewModel CreateViewModel(
        FakeRepositoryStateService stateService,
        params Project[] projects) =>
        new(new ProjectRegistryService(
                new FakeProjectRepository(projects),
                new FixedClock()),
            stateService);

    private static Project CreateProject(string name, string localPath) =>
        new(
            Guid.NewGuid(),
            name,
            localPath,
            null,
            ProjectStatus.Active,
            new DateTimeOffset(2026, 8, 23, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 23, 10, 0, 0, TimeSpan.Zero));

    private static RepositoryStateSnapshot CreateSnapshot(
        Project project,
        RepositoryVerificationStatus status,
        bool? isClean = null,
        int changedFileTotal = 0,
        bool changedFilesTruncated = false,
        IReadOnlyList<RepositoryChangedFile>? changedFiles = null) =>
        new(
            project.Id,
            project.LocalPath,
            new LocalRepositoryInspection(
                status,
                project.LocalPath,
                repositoryRoot: status is RepositoryVerificationStatus.AvailableClean or RepositoryVerificationStatus.AvailableDirty
                    ? project.LocalPath
                    : null,
                localPathIsRepositoryRoot: status is RepositoryVerificationStatus.AvailableClean or RepositoryVerificationStatus.AvailableDirty
                    ? true
                    : null,
                branchName: "main",
                headSha: status is RepositoryVerificationStatus.AvailableClean or RepositoryVerificationStatus.AvailableDirty
                    ? "abcdef1234567890"
                    : null,
                isClean: isClean,
                changedFileTotal: changedFileTotal,
                changedFiles: changedFiles,
                changedFilesTruncated: changedFilesTruncated,
                capturedAt: new DateTimeOffset(2026, 8, 24, 12, 0, 0, TimeSpan.Zero)));

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeProjectRepository : IProjectRepository
    {
        private readonly List<Project> _projects;

        public FakeProjectRepository(params Project[] projects) => _projects = projects.ToList();

        public Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Project>>(_projects.ToArray());

        public Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_projects.FirstOrDefault(project => project.Id == projectId));

        public Task UpsertAsync(Project project, CancellationToken cancellationToken = default)
        {
            var index = _projects.FindIndex(existing => existing.Id == project.Id);
            if (index >= 0)
            {
                _projects[index] = project;
            }
            else
            {
                _projects.Add(project);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeRepositoryStateService : IProjectRepositoryStateService
    {
        private readonly Dictionary<Guid, TaskCompletionSource<RepositoryStateSnapshot>> _pending = [];

        public Func<Project, RepositoryStateSnapshot> Factory { get; init; } =
            project => CreateSnapshot(project, RepositoryVerificationStatus.AvailableClean, isClean: true);

        public Exception? Exception { get; init; }

        public TaskCompletionSource<RepositoryStateSnapshot> AddPending(Guid projectId)
        {
            var pending = new TaskCompletionSource<RepositoryStateSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[projectId] = pending;
            return pending;
        }

        public Task<RepositoryStateSnapshot> VerifyAsync(Project project, CancellationToken cancellationToken = default)
        {
            if (Exception is not null)
            {
                return Task.FromException<RepositoryStateSnapshot>(Exception);
            }

            if (_pending.TryGetValue(project.Id, out var pending))
            {
                return pending.Task;
            }

            return Task.FromResult(Factory(project));
        }
    }
}
