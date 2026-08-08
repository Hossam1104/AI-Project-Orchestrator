using AIUsageMonitor.Infrastructure;
using AIUsageMonitor.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;
using System.Windows;

namespace AIUsageMonitor.Desktop;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private ApplicationDataPaths? _paths;

    public App()
    {
        InitializeComponent();

        _paths = ApplicationDataPaths.CreateDefault();
        _paths.EnsureDirectories();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Debug()
            .WriteTo.File(
                Path.Combine(_paths.LogsDirectory, "aiusagemonitor-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)
            .CreateLogger();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            _paths ??= ApplicationDataPaths.CreateDefault();
            _paths.EnsureDirectories();

            _host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices(services =>
                {
                    services.AddInfrastructure(_paths.RootDirectory);
                    services.AddSingleton<MainWindow>();
                })
                .Build();

            await _host.StartAsync();
            Log.Information("AI Usage Monitor started using LocalAppData at {RootDirectory}", _paths.RootDirectory);

            MainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow.Show();
        }
        catch (Exception exception)
        {
            // Optional state and provider failures must not prevent the empty shell from opening.
            Log.Error(exception, "Application startup completed with a recoverable infrastructure error");
            MainWindow = new MainWindow();
            MainWindow.Show();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(2));
            _host.Dispose();
        }

        Log.Information("AI Usage Monitor stopped");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
