using AIUsageMonitor.Desktop.ViewModels;
using System.Windows;

namespace AIUsageMonitor.Desktop;

public partial class ProviderConnectionEditorWindow : Window
{
    private readonly ProviderConnectionEditorViewModel _viewModel;

    public ProviderConnectionEditorWindow(ProviderConnectionEditorViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;
        InitializeComponent();
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (await _viewModel.SaveAsync(SecretBox.Password))
        {
            SecretBox.Clear();
            DialogResult = true;
        }
    }

    private async void RemoveCredential_Click(object sender, RoutedEventArgs e)
    {
        if (await _viewModel.RemoveCredentialAsync())
        {
            SecretBox.Clear();
            DialogResult = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        SecretBox.Clear();
        DialogResult = false;
    }
}
