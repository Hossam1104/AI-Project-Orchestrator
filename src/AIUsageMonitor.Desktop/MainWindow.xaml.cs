using AIUsageMonitor.Desktop.ViewModels;
using System.Windows;

namespace AIUsageMonitor.Desktop;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private bool _persistenceAvailable;

    public MainWindow()
        : this(new MainWindowViewModel())
    {
    }

    public MainWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public void SetPersistenceAvailability(bool persistenceAvailable)
    {
        _persistenceAvailable = persistenceAvailable;
        _viewModel.SetPersistenceAvailability(persistenceAvailable);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (_persistenceAvailable)
        {
            _viewModel.AiCapacity.SetEditorLauncher(OpenConnectionEditorAsync);
            await _viewModel.InitializeAsync();
        }
        else
        {
            await _viewModel.InitializeDegradedAsync();
        }
    }

    private async Task OpenConnectionEditorAsync(ProviderCapacityCardViewModel card)
    {
        if (!card.CanEditConnection || _viewModel.AiCapacity.ConnectionService is null)
        {
            return;
        }

        var connection = await _viewModel.AiCapacity.ConnectionService.GetAsync(card.Code);
        var editor = new ProviderConnectionEditorWindow(
            new ProviderConnectionEditorViewModel(card.Code, connection, _viewModel.AiCapacity.ConnectionService))
        {
            Owner = this
        };

        if (editor.ShowDialog() == true)
        {
            card.SetConnection(await _viewModel.AiCapacity.ConnectionService.GetAsync(card.Code));
        }
    }
}
