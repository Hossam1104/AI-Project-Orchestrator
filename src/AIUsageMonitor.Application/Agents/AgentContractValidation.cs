namespace AIUsageMonitor.Application.Agents;

internal static class AgentContractValidation
{
    public static IReadOnlyList<T> CopyDistinctEnums<T>(
        IReadOnlyList<T>? values,
        string parameterName,
        T? fallback = null)
        where T : struct, Enum
    {
        if (values is null || values.Count == 0)
        {
            return fallback.HasValue ? [fallback.Value] : Array.Empty<T>();
        }

        var result = new List<T>(values.Count);
        foreach (var value in values)
        {
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentException("The collection contains an undefined enum value.", parameterName);
            }

            if (!result.Contains(value))
            {
                result.Add(value);
            }
        }

        return result.AsReadOnly();
    }

    public static IReadOnlyList<T>? CopyOptionalDistinctEnums<T>(
        IReadOnlyList<T>? values,
        string parameterName)
        where T : struct, Enum
    {
        if (values is null)
        {
            return null;
        }

        return CopyDistinctEnums(values, parameterName);
    }

    public static void RejectUnsupportedModeMix(
        IReadOnlyList<AgentConnectionMode> modes,
        string parameterName)
    {
        if (modes.Contains(AgentConnectionMode.Unsupported) && modes.Count > 1)
        {
            throw new ArgumentException(
                "Unsupported cannot be combined with another supported invocation mode.",
                parameterName);
        }
    }

    public static void RejectUnverifiedOrUnsupportedSupportedModes(
        IReadOnlyList<AgentConnectionMode> modes,
        string parameterName)
    {
        if (modes.Contains(AgentConnectionMode.Unknown))
        {
            throw new ArgumentException(
                "Unknown is not a verified supported invocation mode.",
                parameterName);
        }

        if (modes.Contains(AgentConnectionMode.Unsupported))
        {
            throw new ArgumentException(
                "Unsupported cannot be included in a list of supported invocation modes.",
                parameterName);
        }
    }

    public static IReadOnlyList<string> CopyStrings(
        IReadOnlyList<string>? values,
        string parameterName,
        int maximumLength = 500)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<string>();
        }

        var result = new List<string>(values.Count);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Values cannot be blank.", parameterName);
            }

            var normalized = value.Trim();
            if (normalized.Length > maximumLength)
            {
                throw new ArgumentException($"Values cannot exceed {maximumLength} characters.", parameterName);
            }

            if (!result.Contains(normalized, StringComparer.Ordinal))
            {
                result.Add(normalized);
            }
        }

        return result.AsReadOnly();
    }

    public static string? NormalizeOptional(
        string? value,
        string parameterName,
        int maximumLength = 500)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException($"Value cannot exceed {maximumLength} characters.", parameterName);
        }

        return normalized;
    }
}
