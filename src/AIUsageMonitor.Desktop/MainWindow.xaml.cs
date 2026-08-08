using System.Windows;

namespace AIUsageMonitor.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void SetPersistenceAvailability(bool persistenceAvailable)
    {
        StatusText.Text = persistenceAvailable
            ? "No providers connected yet. Your local workspace is ready."
            : "Local persistence is unavailable. Running in degraded mode.";
    }
}
