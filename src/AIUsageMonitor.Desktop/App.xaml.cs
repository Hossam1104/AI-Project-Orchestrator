using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Serilog;

namespace AIUsageMonitor.Desktop;

public partial class App : Application
{
    private readonly IHost _host;
    private Window? _window;

    public App()
    {
        InitializeComponent();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Debug()
            .WriteTo.File(
                Path.Combine(AppContext.BaseDirectory, "logs", "aiusagemonitor-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices(ConfigureServices)
            .Build();
    }

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Log.Information("AI Usage Monitor starting up");

        _window = new MainWindow();
        _window.Activate();
    }
}
