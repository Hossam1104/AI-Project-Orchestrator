namespace AIUsageMonitor.Providers.Common;

public sealed class SystemExecutableLocator : IExecutableLocator
{
    public string? Find(string commandName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);

        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var candidates = OperatingSystem.IsWindows() && Path.GetExtension(commandName).Length == 0
            ? new[] { commandName, commandName + ".exe", commandName + ".cmd", commandName + ".bat" }
            : new[] { commandName };

        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var candidate in candidates)
            {
                var fullPath = Path.Combine(directory, candidate);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        return null;
    }
}
