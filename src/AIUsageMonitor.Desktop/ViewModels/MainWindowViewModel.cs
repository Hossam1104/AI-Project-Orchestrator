using System.Windows.Input;

namespace AIUsageMonitor.Desktop.ViewModels;

public sealed class OverviewViewModel
{
    public string PersistenceText { get; set; } = "LocalAppData is available for the foundation.";

    public string PersistenceStateText { get; set; } = "Ready";

    public string ShellStatusText { get; set; } =
        "The branded shell is ready. AI Capacity is the first usable APO workspace.";
}

public sealed class MainWindowViewModel : ObservableObject
{
    private object _activeWorkspace;
    private bool _isOverviewSelected;
    private bool _isAiCapacitySelected;

    public MainWindowViewModel()
        : this(new AiCapacityViewModel())
    {
    }

    public MainWindowViewModel(AiCapacityViewModel aiCapacity)
    {
        AiCapacity = aiCapacity ?? throw new ArgumentNullException(nameof(aiCapacity));
        Overview = new OverviewViewModel();
        _activeWorkspace = AiCapacity;
        _isAiCapacitySelected = true;
        ShowOverviewCommand = new RelayCommand(ShowOverview);
        ShowAiCapacityCommand = new RelayCommand(ShowAiCapacity);
    }

    public OverviewViewModel Overview { get; }

    public AiCapacityViewModel AiCapacity { get; }

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

    public ICommand ShowOverviewCommand { get; }

    public ICommand ShowAiCapacityCommand { get; }

    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        AiCapacity.InitializeAsync(cancellationToken);

    public void SetPersistenceAvailability(bool persistenceAvailable)
    {
        Overview.PersistenceStateText = persistenceAvailable ? "Ready" : "Degraded mode";
        Overview.PersistenceText = persistenceAvailable
            ? "LocalAppData is available for the foundation."
            : "LocalAppData is unavailable; no local state will be written.";
        Overview.ShellStatusText = persistenceAvailable
            ? "The branded shell is ready. AI Capacity is the first usable APO workspace."
            : "Local persistence is unavailable. AI Capacity is running in safe degraded mode.";
        OnPropertyChanged(nameof(Overview));
    }

    private void ShowOverview()
    {
        ActiveWorkspace = Overview;
        IsOverviewSelected = true;
        IsAiCapacitySelected = false;
    }

    private void ShowAiCapacity()
    {
        ActiveWorkspace = AiCapacity;
        IsOverviewSelected = false;
        IsAiCapacitySelected = true;
    }
}
