namespace AIUsageMonitor.Application.Projects;

/// <summary>
/// Safe display representation for untrusted repository URL metadata. This is deliberately a
/// display helper; it does not validate reachability or change the persisted registration value.
/// </summary>
public static class RepositoryUrlSanitizer
{
    private const int MaxLength = 512;

    public static string Sanitize(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return "Not configured";
        }

        var value = RemoveControlCharacters(rawUrl).Trim();
        if (value.Length == 0)
        {
            return "Remote URL unavailable";
        }

        var at = value.IndexOf('@');
        var schemeSeparator = value.IndexOf("://", StringComparison.Ordinal);
        if (schemeSeparator < 0 && at > 0 && value.IndexOf(':', at) > at)
        {
            // SCP-style SSH syntax (user@host:path). Keep only host/path; discard the user
            // portion and any trailing query-string- or fragment-like suffix, neither of which
            // Git itself treats as syntax here but which must never be echoed verbatim.
            var afterUser = value[(at + 1)..];
            var end = FindScpPathEnd(afterUser);
            return Limit(afterUser[..end]);
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

    private static int FindScpPathEnd(string value)
    {
        var end = value.Length;
        var queryIndex = value.IndexOf('?');
        if (queryIndex >= 0 && queryIndex < end)
        {
            end = queryIndex;
        }

        var fragmentIndex = value.IndexOf('#');
        if (fragmentIndex >= 0 && fragmentIndex < end)
        {
            end = fragmentIndex;
        }

        return end;
    }

    private static string RemoveControlCharacters(string value)
    {
        var hasControlCharacters = false;
        foreach (var character in value)
        {
            if (char.IsControl(character))
            {
                hasControlCharacters = true;
                break;
            }
        }

        if (!hasControlCharacters)
        {
            return value;
        }

        var buffer = new char[value.Length];
        var count = 0;
        foreach (var character in value)
        {
            if (!char.IsControl(character))
            {
                buffer[count++] = character;
            }
        }

        return new string(buffer, 0, count);
    }

    private static string Limit(string value) => value.Length <= MaxLength
        ? value
        : value[..(MaxLength - 1)] + "…";
}
