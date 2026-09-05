using System.Runtime.InteropServices;

namespace AIUsageMonitor.Infrastructure.Tests;

internal static class DotnetExecutableResolver
{
    public static string Resolve()
    {
        var candidates = new List<string>();
        AddDotnetProcessCandidate(candidates, Environment.ProcessPath);
        AddRootCandidate(candidates, Environment.GetEnvironmentVariable("DOTNET_ROOT"));
        AddRootCandidate(candidates, Environment.GetEnvironmentVariable("DOTNET_ROOT(x86)"));

        var runtimeDirectory = new DirectoryInfo(RuntimeEnvironment.GetRuntimeDirectory());
        AddRootCandidate(candidates, runtimeDirectory.Parent?.Parent?.Parent?.FullName);

        var path = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrWhiteSpace(path))
        {
            foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                AddCandidate(candidates, Path.Combine(directory, ExecutableName));
            }
        }

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        throw new InvalidOperationException("A usable dotnet CLI executable could not be resolved from the test/runtime environment.");
    }

    private static string ExecutableName => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

    private static void AddDotnetProcessCandidate(ICollection<string> candidates, string? processPath)
    {
        if (string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            AddCandidate(candidates, processPath);
        }
    }

    private static void AddRootCandidate(ICollection<string> candidates, string? root)
    {
        if (!string.IsNullOrWhiteSpace(root))
        {
            AddCandidate(candidates, Path.Combine(root, ExecutableName));
        }
    }

    private static void AddCandidate(ICollection<string> candidates, string? candidate)
    {
        if (!string.IsNullOrWhiteSpace(candidate))
        {
            candidates.Add(candidate);
        }
    }
}
