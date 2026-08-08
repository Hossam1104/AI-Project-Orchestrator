using AIUsageMonitor.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIUsageMonitor.Infrastructure.Tests;

internal sealed class TemporaryStore : IDisposable
{
    public TemporaryStore()
    {
        RootDirectory = Path.Combine(
            Path.GetTempPath(),
            "AIUsageMonitorTests",
            Guid.NewGuid().ToString("N"));
        Paths = new ApplicationDataPaths(RootDirectory);
        Files = new JsonFileStore(NullLogger<JsonFileStore>.Instance);
        Paths.EnsureDirectories();
    }

    public string RootDirectory { get; }

    public ApplicationDataPaths Paths { get; }

    public JsonFileStore Files { get; }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
        }
        catch (IOException)
        {
            // Test cleanup should not obscure the assertion that already ran.
        }
        catch (UnauthorizedAccessException)
        {
            // Test cleanup should not obscure the assertion that already ran.
        }
    }
}
