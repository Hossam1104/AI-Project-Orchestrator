using AIUsageMonitor.Infrastructure;
using AIUsageMonitor.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Serilog;

namespace AIUsageMonitor.Desktop;

public partial class App : Microsoft.UI.Xaml.Application
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
        services.AddInfrastructure();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        Log.Information("AI Usage Monitor starting up");

        await _host.StartAsync();

        // LocalDB may not be installed/reachable — this must never crash startup (BRD §29).
        // The result is logged now; surfacing it in the UI is a later-session concern.
        var initializer = _host.Services.GetRequiredService<IDatabaseInitializer>();
        var databaseResult = await initializer.InitializeAsync();
        if (!databaseResult.IsReady)
        {
            Log.Warning(
                "Local database unavailable at startup ({Status}): {UserMessage}",
                databaseResult.Status, databaseResult.UserMessage);
        }

        _window = new MainWindow();
        _window.Activate();
    }
}
