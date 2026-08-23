using System.Collections.ObjectModel;
using System.Globalization;
using AIUsageMonitor.Application.Projects;

namespace AIUsageMonitor.Desktop.ViewModels;

/// <summary>
/// Bounded project registry workspace state. It performs in-memory presentation filtering and
/// delegates all persistence semantics to the Application project registry service.
/// </summary>
public sealed class ProjectsViewModel : ObservableObject
{
    private readonly IProjectRegistryService? _registryService;
    private Guid? _editingProjectId;
    private bool _isStorageAvailable;
    private bool _hasLoadedSuccessfully;
    private bool _hasLoadError;
    private bool _isLoading;
    private bool _isSaving;
    private bool _isEditing;
    private bool _isCreating;
    private string _searchText = string.Empty;
    private ProjectStatusFilter _selectedStatusFilter = ProjectStatusFilter.All;
    private ProjectCardViewModel? _selectedProjectCard;
    private Project? _selectedProject;
    private string? _errorMessage;
    private string? _validationMessage;
    private string _editorName = string.Empty;
    private string _editorLocalPath = string.Empty;
    private ProjectStatus _editorStatus = ProjectStatus.Active;
    private string _editorRepositoryProvider = string.Empty;
    private string _editorRepositoryUrl = string.Empty;
    private string _editorRepositoryId = string.Empty;
    private string _editorDefaultBranch = string.Empty;
    private string _editorTrackerType = string.Empty;
    private string _editorTrackerId = string.Empty;
    private string _editorGovernanceReferencesText = string.Empty;
    private string _editorRoutingPolicyReference = string.Empty;
    private string _editorSafetyPolicyReference = string.Empty;

    public ProjectsViewModel()
        : this(null)
    {
    }

    public ProjectsViewModel(IProjectRegistryService? registryService)
    {
        _registryService = registryService;
        _isStorageAvailable = registryService is not null;

        RefreshCommand = new AsyncCommand(
            () => RefreshAsync(),
            () => _registryService is not null && !IsSaving && !IsLoading && !IsEditing);
        NewProjectCommand = new RelayCommand(NewProject, CanInteractWithRegistry);
        EditProjectCommand = new RelayCommand(EditSelectedProject, CanEditSelectedProject);
        SaveProjectCommand = new AsyncCommand(SaveAsync, CanSaveProject);
        CancelEditCommand = new RelayCommand(CancelEdit, () => IsEditing && !IsSaving);
    }

    public ObservableCollection<Project> Projects { get; } = [];

    public ObservableCollection<ProjectCardViewModel> FilteredProjects { get; } = [];

    public IReadOnlyList<ProjectStatusFilter> StatusFilterOptions { get; } =
        Enum.GetValues<ProjectStatusFilter>();

    public IReadOnlyList<ProjectStatus> ProjectStatusOptions { get; } =
        Enum.GetValues<ProjectStatus>();

    public AsyncCommand RefreshCommand { get; }

    public RelayCommand NewProjectCommand { get; }

    public RelayCommand EditProjectCommand { get; }

    public AsyncCommand SaveProjectCommand { get; }

    public RelayCommand CancelEditCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value ?? string.Empty))
            {
                ApplyFilter();
            }
        }
    }

    public ProjectStatusFilter SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if (SetProperty(ref _selectedStatusFilter, value))
            {
                ApplyFilter();
            }
        }
    }

    public ProjectCardViewModel? SelectedProjectCard
    {
        get => _selectedProjectCard;
        set
        {
            if (!SetProperty(ref _selectedProjectCard, value))
            {
                return;
            }

            _selectedProject = value?.Project;
            OnPropertyChanged(nameof(SelectedProject));
            OnPropertyChanged(nameof(HasSelectedProject));
            OnPropertyChanged(nameof(SelectedGovernanceReferencesDisplay));
            OnPropertyChanged(nameof(SelectedCreatedAtText));
            OnPropertyChanged(nameof(SelectedUpdatedAtText));
            EditProjectCommand.NotifyCanExecuteChanged();
        }
    }

    public Project? SelectedProject => _selectedProject;

    public bool HasSelectedProject => SelectedProject is not null;

    public bool HasProjects => Projects.Count > 0;

    public bool HasFilteredProjects => FilteredProjects.Count > 0;

    public bool IsStorageAvailable
    {
        get => _isStorageAvailable;
        private set
        {
            if (SetProperty(ref _isStorageAvailable, value))
            {
                OnWorkspaceStateChanged();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(ShowLoadingState));
                OnPropertyChanged(nameof(ShowStorageUnavailableState));
                OnPropertyChanged(nameof(ShowRegistrySurface));
                OnPropertyChanged(nameof(ShowLoadErrorState));
                OnWorkspaceStateChanged();
            }
        }
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (SetProperty(ref _isSaving, value))
            {
                OnPropertyChanged(nameof(SavingStateText));
                OnWorkspaceStateChanged();
            }
        }
    }

    public bool IsEditing
    {
        get => _isEditing;
        private set
        {
            if (SetProperty(ref _isEditing, value))
            {
                OnPropertyChanged(nameof(IsEditorVisible));
                OnWorkspaceStateChanged();
            }
        }
    }

    public bool IsCreating
    {
        get => _isCreating;
        private set
        {
            if (SetProperty(ref _isCreating, value))
            {
                OnPropertyChanged(nameof(EditorTitle));
            }
        }
    }

    public bool HasLoadError
    {
        get => _hasLoadError;
        private set
        {
            if (SetProperty(ref _hasLoadError, value))
            {
                OnPropertyChanged(nameof(ShowLoadErrorState));
                OnPropertyChanged(nameof(ShowRegistrySurface));
                OnWorkspaceStateChanged();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set
        {
            if (SetProperty(ref _validationMessage, value))
            {
                OnPropertyChanged(nameof(HasValidationMessage));
            }
        }
    }

    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

    public bool ShowLoadingState => IsLoading;

    public bool ShowStorageUnavailableState => !IsLoading && !IsStorageAvailable;

    public bool ShowLoadErrorState => !IsLoading && IsStorageAvailable && HasLoadError;

    public bool ShowRegistrySurface =>
        !IsLoading && IsStorageAvailable && !HasLoadError;

    public bool ShowEmptyRegistryState =>
        ShowRegistrySurface && !HasProjects;

    public bool ShowNoMatchState =>
        ShowRegistrySurface && HasProjects && !HasFilteredProjects;

    public bool IsEditorVisible => IsEditing;

    public bool IsRegistryInteractionEnabled => !IsEditing && !IsSaving && !IsLoading;

    public string SavingStateText => IsSaving ? "Saving project…" : string.Empty;

    public string EditorTitle => IsCreating ? "Register a project" : "Edit project";

    public string? SelectedGovernanceReferencesDisplay => SelectedProject is null
        ? null
        : SelectedProject.GovernanceReferences.Count == 0
            ? "Not configured"
            : string.Join(Environment.NewLine, SelectedProject.GovernanceReferences);

    public string SelectedCreatedAtText => FormatDate(SelectedProject?.CreatedAt);

    public string SelectedUpdatedAtText => FormatDate(SelectedProject?.UpdatedAt);

    public string EditorName
    {
        get => _editorName;
        set => SetProperty(ref _editorName, value ?? string.Empty);
    }

    public string EditorLocalPath
    {
        get => _editorLocalPath;
        set => SetProperty(ref _editorLocalPath, value ?? string.Empty);
    }

    public ProjectStatus EditorStatus
    {
        get => _editorStatus;
        set => SetProperty(ref _editorStatus, value);
    }

    public string EditorRepositoryProvider
    {
        get => _editorRepositoryProvider;
        set => SetProperty(ref _editorRepositoryProvider, value ?? string.Empty);
    }

    public string EditorRepositoryUrl
    {
        get => _editorRepositoryUrl;
        set => SetProperty(ref _editorRepositoryUrl, value ?? string.Empty);
    }

    public string EditorRepositoryId
    {
        get => _editorRepositoryId;
        set => SetProperty(ref _editorRepositoryId, value ?? string.Empty);
    }

    public string EditorDefaultBranch
    {
        get => _editorDefaultBranch;
        set => SetProperty(ref _editorDefaultBranch, value ?? string.Empty);
    }

    public string EditorTrackerType
    {
        get => _editorTrackerType;
        set => SetProperty(ref _editorTrackerType, value ?? string.Empty);
    }

    public string EditorTrackerId
    {
        get => _editorTrackerId;
        set => SetProperty(ref _editorTrackerId, value ?? string.Empty);
    }

    public string EditorGovernanceReferencesText
    {
        get => _editorGovernanceReferencesText;
        set => SetProperty(ref _editorGovernanceReferencesText, value ?? string.Empty);
    }

    public string EditorRoutingPolicyReference
    {
        get => _editorRoutingPolicyReference;
        set => SetProperty(ref _editorRoutingPolicyReference, value ?? string.Empty);
    }

    public string EditorSafetyPolicyReference
    {
        get => _editorSafetyPolicyReference;
        set => SetProperty(ref _editorSafetyPolicyReference, value ?? string.Empty);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default) =>
        await RefreshAsync(cancellationToken).ConfigureAwait(true);

    public void SetPersistenceAvailability(bool persistenceAvailable)
    {
        IsStorageAvailable = persistenceAvailable && _registryService is not null;
        if (!IsStorageAvailable)
        {
            HasLoadError = false;
            ErrorMessage = "Project storage unavailable.";
        }
        else if (ErrorMessage == "Project storage unavailable.")
        {
            ErrorMessage = null;
        }

        OnWorkspaceStateChanged();
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_registryService is null)
        {
            SetPersistenceAvailability(false);
            return;
        }

        if (IsLoading || IsSaving)
        {
            return;
        }

        IsLoading = true;
        HasLoadError = false;
        ErrorMessage = null;

        try
        {
            var projects = await _registryService
                .GetProjectsAsync(cancellationToken)
                .ConfigureAwait(true);

            Projects.Clear();
            foreach (var project in projects)
            {
                Projects.Add(project);
            }

            _hasLoadedSuccessfully = true;
            IsStorageAvailable = true;
            ApplyFilter();
            if (SelectedProjectCard is null && FilteredProjects.Count > 0)
            {
                SelectedProjectCard = FilteredProjects[0];
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            _hasLoadedSuccessfully = false;
            HasLoadError = true;
            ErrorMessage = "Projects could not be loaded.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void SelectProject(Guid projectId)
    {
        var card = FilteredProjects.FirstOrDefault(item => item.Project.Id == projectId)
            ?? Projects.Select(static project => new ProjectCardViewModel(project))
                .FirstOrDefault(item => item.Project.Id == projectId);

        if (card is not null)
        {
            SelectedProjectCard = card;
        }
    }

    private void NewProject()
    {
        _editingProjectId = null;
        SelectedProjectCard = null;
        IsCreating = true;
        IsEditing = true;
        ClearEditor();
        ValidationMessage = null;
        ErrorMessage = null;
    }

    private void EditSelectedProject()
    {
        var target = SelectedProject;
        if (target is null)
        {
            return;
        }

        _editingProjectId = target.Id;
        IsCreating = false;
        IsEditing = true;
        EditorName = target.Name;
        EditorLocalPath = target.LocalPath;
        EditorStatus = target.Status;
        EditorRepositoryProvider = target.RepositoryProvider ?? string.Empty;
        EditorRepositoryUrl = target.RepositoryUrl ?? string.Empty;
        EditorRepositoryId = target.RepositoryId ?? string.Empty;
        EditorDefaultBranch = target.DefaultBranch ?? string.Empty;
        EditorTrackerType = target.TrackerType ?? string.Empty;
        EditorTrackerId = target.TrackerId ?? string.Empty;
        EditorGovernanceReferencesText = string.Join(
            Environment.NewLine,
            target.GovernanceReferences);
        EditorRoutingPolicyReference = target.RoutingPolicyReference ?? string.Empty;
        EditorSafetyPolicyReference = target.SafetyPolicyReference ?? string.Empty;
        ValidationMessage = null;
        ErrorMessage = null;
    }

    public async Task SaveAsync()
    {
        if (_registryService is null || !IsStorageAvailable || !IsEditing || IsSaving)
        {
            return;
        }

        if (!ValidateEditor())
        {
            return;
        }

        if (!IsCreating && _editingProjectId is null)
        {
            ErrorMessage = "Project could not be saved because the edit target is no longer available.";
            return;
        }

        var edit = BuildEditorRequest();
        var targetProjectId = _editingProjectId;
        IsSaving = true;
        ValidationMessage = null;
        ErrorMessage = null;

        try
        {
            var saved = IsCreating
                ? await _registryService.CreateProjectAsync(edit).ConfigureAwait(true)
                : await _registryService.UpdateProjectAsync(targetProjectId!.Value, edit).ConfigureAwait(true);

            ReplaceProject(saved);
            IsCreating = false;
            IsEditing = false;
            _editingProjectId = null;
            _hasLoadedSuccessfully = true;
        }
        catch (ArgumentException exception)
        {
            ValidationMessage = IsDefaultBranchError(exception)
                ? "Default branch is required when repository information is configured."
                : "Project details are invalid.";
        }
        catch (KeyNotFoundException)
        {
            ErrorMessage = "Project could not be saved because it no longer exists.";
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Project could not be saved.";
        }
        catch
        {
            ErrorMessage = "Project could not be saved.";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void CancelEdit()
    {
        _editingProjectId = null;
        IsCreating = false;
        IsEditing = false;
        ValidationMessage = null;
        ErrorMessage = null;
    }

    private void ReplaceProject(Project saved)
    {
        var existingIndex = Projects
            .Select((project, index) => (project, index))
            .FirstOrDefault(item => item.project.Id == saved.Id);

        if (existingIndex.project is null)
        {
            Projects.Add(saved);
        }
        else
        {
            Projects[existingIndex.index] = saved;
        }

        SortProjectsInPlace();
        ApplyFilter();
        SelectedProjectCard = FilteredProjects.FirstOrDefault(item => item.Project.Id == saved.Id)
            ?? new ProjectCardViewModel(saved);
    }

    private void ApplyFilter()
    {
        var search = SearchText.Trim();
        var filtered = Projects
            .Where(project => MatchesStatus(project) && MatchesSearch(project, search))
            .Select(static project => new ProjectCardViewModel(project))
            .ToArray();

        FilteredProjects.Clear();
        foreach (var project in filtered)
        {
            FilteredProjects.Add(project);
        }

        if (SelectedProjectCard is not null &&
            FilteredProjects.All(item => item.Project.Id != SelectedProjectCard.Project.Id))
        {
            SelectedProjectCard = null;
        }

        OnPropertyChanged(nameof(HasProjects));
        OnPropertyChanged(nameof(HasFilteredProjects));
        OnPropertyChanged(nameof(ShowEmptyRegistryState));
        OnPropertyChanged(nameof(ShowNoMatchState));
    }

    private bool MatchesStatus(Project project) => SelectedStatusFilter switch
    {
        ProjectStatusFilter.All => true,
        ProjectStatusFilter.Active => project.Status == ProjectStatus.Active,
        ProjectStatusFilter.Paused => project.Status == ProjectStatus.Paused,
        ProjectStatusFilter.Blocked => project.Status == ProjectStatus.Blocked,
        ProjectStatusFilter.Archived => project.Status == ProjectStatus.Archived,
        _ => false
    };

    private static bool MatchesSearch(Project project, string search) =>
        search.Length == 0 ||
        project.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
        project.LocalPath.Contains(search, StringComparison.OrdinalIgnoreCase);

    private ProjectEdit BuildEditorRequest() => new()
    {
        Name = EditorName,
        LocalPath = EditorLocalPath,
        Status = EditorStatus,
        RepositoryProvider = EditorRepositoryProvider,
        RepositoryUrl = EditorRepositoryUrl,
        RepositoryId = EditorRepositoryId,
        DefaultBranch = EditorDefaultBranch,
        TrackerType = EditorTrackerType,
        TrackerId = EditorTrackerId,
        GovernanceReferences = EditorGovernanceReferencesText
            .Split(["\r\n", "\n", "\r"], StringSplitOptions.None),
        RoutingPolicyReference = EditorRoutingPolicyReference,
        SafetyPolicyReference = EditorSafetyPolicyReference
    };

    private bool ValidateEditor()
    {
        if (string.IsNullOrWhiteSpace(EditorName))
        {
            ValidationMessage = "Project name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(EditorLocalPath))
        {
            ValidationMessage = "Local path is required.";
            return false;
        }

        if (HasRepositoryInput() && string.IsNullOrWhiteSpace(EditorDefaultBranch))
        {
            ValidationMessage = "Default branch is required when repository information is configured.";
            return false;
        }

        ValidationMessage = null;
        return true;
    }

    private bool HasRepositoryInput() =>
        !string.IsNullOrWhiteSpace(EditorRepositoryProvider) ||
        !string.IsNullOrWhiteSpace(EditorRepositoryUrl) ||
        !string.IsNullOrWhiteSpace(EditorRepositoryId);

    private bool CanEditWorkspace() =>
        _registryService is not null &&
        IsStorageAvailable &&
        _hasLoadedSuccessfully &&
        !IsLoading &&
        !IsSaving;

    private bool CanEditSelectedProject() => CanEditWorkspace() && SelectedProject is not null;

    private bool CanInteractWithRegistry() => CanEditWorkspace() && !IsEditing;

    private bool CanSaveProject() =>
        CanEditWorkspace() && IsEditing && !IsSaving;

    private void ClearEditor()
    {
        EditorName = string.Empty;
        EditorLocalPath = string.Empty;
        EditorStatus = ProjectStatus.Active;
        EditorRepositoryProvider = string.Empty;
        EditorRepositoryUrl = string.Empty;
        EditorRepositoryId = string.Empty;
        EditorDefaultBranch = string.Empty;
        EditorTrackerType = string.Empty;
        EditorTrackerId = string.Empty;
        EditorGovernanceReferencesText = string.Empty;
        EditorRoutingPolicyReference = string.Empty;
        EditorSafetyPolicyReference = string.Empty;
    }

    private void SortProjectsInPlace()
    {
        var sorted = Projects
            .OrderBy(project => project.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(project => project.Id)
            .ToArray();
        Projects.Clear();
        foreach (var project in sorted)
        {
            Projects.Add(project);
        }
    }

    private void OnWorkspaceStateChanged()
    {
        OnPropertyChanged(nameof(ShowStorageUnavailableState));
        OnPropertyChanged(nameof(ShowLoadErrorState));
        OnPropertyChanged(nameof(ShowRegistrySurface));
        OnPropertyChanged(nameof(ShowEmptyRegistryState));
        OnPropertyChanged(nameof(ShowNoMatchState));
        OnPropertyChanged(nameof(IsRegistryInteractionEnabled));
        RefreshCommand.NotifyCanExecuteChanged();
        NewProjectCommand.NotifyCanExecuteChanged();
        EditProjectCommand.NotifyCanExecuteChanged();
        SaveProjectCommand.NotifyCanExecuteChanged();
        CancelEditCommand.NotifyCanExecuteChanged();
    }

    private static bool IsDefaultBranchError(ArgumentException exception) =>
        string.Equals(exception.ParamName, "defaultBranch", StringComparison.Ordinal) ||
        exception.Message.Contains("default branch", StringComparison.OrdinalIgnoreCase);

    private static string FormatDate(DateTimeOffset? value) => value is null
        ? "Not available"
        : value.Value.ToLocalTime().ToString("MMM d, yyyy h:mm tt", CultureInfo.CurrentCulture);
}
