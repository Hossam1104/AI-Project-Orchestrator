using System.Windows;
using System.Windows.Media;

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
            ? "The branded shell is ready. Connectors and orchestration surfaces will arrive through their approved capability stories."
            : "Local persistence is unavailable. The shell is running in degraded no-persistence mode.";

        PersistenceCardStateText.Text = persistenceAvailable ? "Ready" : "Degraded mode";
        PersistenceCardDetailText.Text = persistenceAvailable
            ? "LocalAppData is available for the foundation."
            : "LocalAppData is unavailable; no local state will be written.";
        PersistenceIndicator.Fill = (Brush)FindResource(
            persistenceAvailable ? "BrandSuccessBrush" : "BrandWarningBrush");
    }
}
