using AIUsageMonitor.Application.Workspaces;
using AIUsageMonitor.Infrastructure.Persistence;

namespace AIUsageMonitor.Infrastructure.Workspaces;

public sealed class ManagedWorkspacePathProvider : IManagedWorkspacePathProvider
{
    private readonly ApplicationDataPaths _paths;

    public ManagedWorkspacePathProvider(ApplicationDataPaths paths) => _paths = paths ?? throw new ArgumentNullException(nameof(paths));

    public string GetWorkspacePath(Guid projectId, Guid workspaceId) =>
        _paths.GetManagedWorkspaceRepositoryPath(projectId, workspaceId);

    public bool IsSafeManagedWorkspacePath(Guid projectId, Guid workspaceId, out string path, out string? errorMessage)
    {
        path = GetWorkspacePath(projectId, workspaceId);
        errorMessage = null;
        var root = Path.GetFullPath(_paths.WorkspacesDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        path = Path.GetFullPath(path);
        var prefix = root + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "Workspace path escaped the managed workspace root.";
            return false;
        }

        var relative = Path.GetRelativePath(root, path);
        var current = root;
        foreach (var component in relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, component);
            if (!File.Exists(current) && !Directory.Exists(current)) continue;
            try
            {
                var attributes = File.GetAttributes(current);
                if (attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    errorMessage = "An existing managed workspace path component is a reparse point.";
                    return false;
                }

                if (File.Exists(current) && !string.Equals(current, path, StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = "A managed workspace parent is a file.";
                    return false;
                }
            }
            catch (UnauthorizedAccessException)
            {
                errorMessage = "Managed workspace path attributes could not be inspected safely.";
                return false;
            }
            catch (IOException)
            {
                errorMessage = "Managed workspace path could not be inspected safely.";
                return false;
            }
        }

        return true;
    }
}
