namespace AIUsageMonitor.Application.Projects;

/// <summary>
/// Safe display representation for untrusted repository URL metadata. This is deliberately a
/// display helper; it does not validate reachability or change the persisted registration value.
/// </summary>
public static class RepositoryUrlSanitizer
{
    public static string Sanitize(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return "Not configured";
        }

        var value = rawUrl.Trim();
        var at = value.IndexOf('@');
        var schemeSeparator = value.IndexOf("://", StringComparison.Ordinal);
        if (schemeSeparator < 0 && at > 0 && value.IndexOf(':', at) > at)
        {
            // SCP-style SSH syntax. Keep host/path but discard the user portion entirely.
            return Limit(value[(at + 1)..]);
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
        {
            return "Remote URL unavailable";
        }

        try
        {
            var components = uri.GetComponents(
                UriComponents.Scheme | UriComponents.Host | UriComponents.Port | UriComponents.Path,
                UriFormat.UriEscaped);
            return Limit(components);
        }
        catch (UriFormatException)
        {
            return "Remote URL unavailable";
        }
    }

    private static string Limit(string value) => value.Length <= 512
        ? value
        : value[..511] + "…";
}
