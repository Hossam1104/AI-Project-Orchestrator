namespace AIUsageMonitor.Providers.Common;

/// <summary>
/// Resolves an executable from the current PATH without invoking a shell.
/// </summary>
public interface IExecutableLocator
{
    string? Find(string commandName);
}
