namespace AIUsageMonitor.Application.Common;

/// <summary>
/// Keeps free-form registry metadata limited to non-secret identifiers and labels. The storage
/// contract deliberately has no token, password, cookie, prompt, source, or authenticated-payload
/// fields; rejecting sensitive-looking keys also prevents accidental future expansion through a
/// generic metadata bag.
/// </summary>
internal static class MetadataValidation
{
    private static readonly string[] ForbiddenKeyFragments =
    [
        "token",
        "secret",
        "password",
        "cookie",
        "credential",
        "authorization",
        "apikey",
        "api-key",
        "refresh",
        "prompt",
        "conversation",
        "sourcecode",
        "payload"
    ];

    public static IReadOnlyDictionary<string, string?> Copy(
        IReadOnlyDictionary<string, string?>? values)
    {
        if (values is null || values.Count == 0)
        {
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        var copy = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in values)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                throw new ArgumentException("Metadata keys cannot be blank.", nameof(values));
            }

            var key = pair.Key.Trim();
            if (ForbiddenKeyFragments.Any(fragment => key.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException(
                    $"Metadata key '{pair.Key}' is not permitted because it may identify secret-bearing data.",
                    nameof(values));
            }

            copy[key] = string.IsNullOrWhiteSpace(pair.Value) ? null : pair.Value.Trim();
        }

        return copy;
    }
}
