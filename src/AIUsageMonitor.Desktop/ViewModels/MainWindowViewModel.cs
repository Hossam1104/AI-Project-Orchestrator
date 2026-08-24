using System.Windows.Input;

namespace AIUsageMonitor.Desktop.ViewModels;

public sealed class OverviewViewModel : ObservableObject
{
    private string _persistenceText = "LocalAppData is available for the foundation.";
    private string _persistenceStateText = "Ready";
    private string _shellStatusText = "The branded shell is ready. AI Capacity is the first usable APO workspace.";
    private bool _isPersistenceAvailable = true;

    public string PersistenceText
    {
        get => _persistenceText;
        private set => SetProperty(ref _persistenceText, value);
    }

    public string PersistenceStateText
    {
        get => _persistenceStateText;
        private set => SetProperty(ref _persistenceStateText, value);
    }

    public string ShellStatusText
    {
        get => _shellStatusText;
        private set => SetProperty(ref _shellStatusText, value);
    }

    public bool IsPersistenceAvailable
    {
        get => _isPersistenceAvailable;
        private set => SetProperty(ref _isPersistenceAvailable, value);
    }

    internal void SetPersistenceAvailability(bool persistenceAvailable)
    {
        IsPersistenceAvailable = persistenceAvailable;
        PersistenceStateText = persistenceAvailable ? "Ready" : "Degraded mode";
        PersistenceText = persistenceAvailable
            ? "LocalAppData is available for the foundation."
            : "LocalAppData is unavailable; no local state will be written.";
        ShellStatusText = persistenceAvailable
            ? "The branded shell is ready. AI Capacity is the first usable APO workspace."
            : "Local persistence is unavailable. AI Capacity is running in safe degraded mode.";
    }
}

public sealed class MainWindowViewModel : ObservableObject
{
    private object _activeWorkspace;
    private bool _isOverviewSelected;
    private bool _isProjectsSelected;
    private bool _isAiCapacitySelected;

    public MainWindowViewModel()
        : this(new AiCapacityViewModel(), new ProjectsViewModel())
    {
    }

    public MainWindowViewModel(AiCapacityViewModel aiCapacity)
        : this(aiCapacity, new ProjectsViewModel())
    {
    }

    public MainWindowViewModel(
        AiCapacityViewModel aiCapacity,
        ProjectsViewModel projects)
    {
        AiCapacity = aiCapacity ?? throw new ArgumentNullException(nameof(aiCapacity));
        Projects = projects ?? throw new ArgumentNullException(nameof(projects));
        Overview = new OverviewViewModel();
        _activeWorkspace = AiCapacity;
        _isAiCapacitySelected = true;
        ShowOverviewCommand = new RelayCommand(ShowOverview);
        ShowProjectsCommand = new RelayCommand(ShowProjects);
        ShowAiCapacityCommand = new RelayCommand(ShowAiCapacity);
    }

    public OverviewViewModel Overview { get; }

    public AiCapacityViewModel AiCapacity { get; }

    public ProjectsViewModel Projects { get; }

    public object ActiveWorkspace
    {
        get => _activeWorkspace;
        private set => SetProperty(ref _activeWorkspace, value);
    }

    public bool IsOverviewSelected
    {
        get => _isOverviewSelected;
        private set => SetProperty(ref _isOverviewSelected, value);
    }

    public bool IsAiCapacitySelected
    {
        get => _isAiCapacitySelected;
        private set => SetProperty(ref _isAiCapacitySelected, value);
    }

    public bool IsProjectsSelected
    {
        get => _isProjectsSelected;
        private set => SetProperty(ref _isProjectsSelected, value);
    }

    public ICommand ShowOverviewCommand { get; }

    public ICommand ShowProjectsCommand { get; }

    public ICommand ShowAiCapacityCommand { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await Task.WhenAll(
            AiCapacity.InitializeAsync(cancellationToken),
            Projects.InitializeAsync(cancellationToken)).ConfigureAwait(true);
    }

    public async Task InitializeDegradedAsync(CancellationToken cancellationToken = default)
    {
        await AiCapacity.InitializeDegradedAsync(cancellationToken).ConfigureAwait(true);
        Projects.SetPersistenceAvailability(false);
        await Projects.InitializeAsync(cancellationToken).ConfigureAwait(true);
    }

    public void SetPersistenceAvailability(bool persistenceAvailable)
    {
        Overview.SetPersistenceAvailability(persistenceAvailable);
        Projects.SetPersistenceAvailability(persistenceAvailable);
    }

    private void ShowOverview()
    {
        ActiveWorkspace = Overview;
        IsOverviewSelected = true;
        IsProjectsSelected = false;
        IsAiCapacitySelected = false;
    }

    private void ShowProjects()
    {
        ActiveWorkspace = Projects;
        IsOverviewSelected = false;
        IsProjectsSelected = true;
        IsAiCapacitySelected = false;
    }

    private void ShowAiCapacity()
    {
        ActiveWorkspace = AiCapacity;
        IsOverviewSelected = false;
        IsProjectsSelected = false;
        IsAiCapacitySelected = true;
    }
}
