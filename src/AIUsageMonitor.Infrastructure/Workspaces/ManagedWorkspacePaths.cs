using AIUsageMonitor.Application.Workspaces;
using AIUsageMonitor.Infrastructure.Persistence;

namespace AIUsageMonitor.Infrastructure.Workspaces;

public sealed class ManagedWorkspacePathProvider : IManagedWorkspacePathProvider
{
    private readonly ApplicationDataPaths _paths;
    private readonly IManagedWorkspacePathProbe _probe;

    public ManagedWorkspacePathProvider(ApplicationDataPaths paths)
        : this(paths, new SystemManagedWorkspacePathProbe())
    {
    }

    internal ManagedWorkspacePathProvider(ApplicationDataPaths paths, IManagedWorkspacePathProbe probe)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _probe = probe ?? throw new ArgumentNullException(nameof(probe));
    }

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

        // The root itself is part of the trust boundary. Checking descendants alone is unsafe:
        // a junction/symlink at WorkspacesDirectory can redirect every lexical child elsewhere.
        if (!InspectDirectoryEntry(root, path, out errorMessage))
        {
            return false;
        }

        var relative = Path.GetRelativePath(root, path);
        var current = root;
        foreach (var component in relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, component);
            if (!InspectDirectoryEntry(current, path, out errorMessage)) return false;
        }

        return true;
    }

    private bool InspectDirectoryEntry(string current, string target, out string? errorMessage)
    {
        errorMessage = null;
        PathEntryInspection inspection;
        try
        {
            inspection = _probe.Inspect(current);
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

        if (inspection == PathEntryInspection.Unavailable)
        {
            errorMessage = "Managed workspace path could not be inspected safely.";
            return false;
        }

        if (inspection == PathEntryInspection.ReparsePoint)
        {
            errorMessage = "An existing managed workspace path component is a reparse point.";
            return false;
        }

        if (inspection == PathEntryInspection.File && !string.Equals(current, target, StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "A managed workspace parent is a file.";
            return false;
        }

        return true;
    }
}

internal enum PathEntryInspection
{
    Missing,
    Directory,
    File,
    ReparsePoint,
    Unavailable
}

internal interface IManagedWorkspacePathProbe
{
    PathEntryInspection Inspect(string path);
}

internal sealed class SystemManagedWorkspacePathProbe : IManagedWorkspacePathProbe
{
    public PathEntryInspection Inspect(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path)) return PathEntryInspection.Missing;
        var attributes = File.GetAttributes(path);
        if (attributes.HasFlag(FileAttributes.ReparsePoint)) return PathEntryInspection.ReparsePoint;
        return attributes.HasFlag(FileAttributes.Directory) ? PathEntryInspection.Directory : PathEntryInspection.File;
    }
}
