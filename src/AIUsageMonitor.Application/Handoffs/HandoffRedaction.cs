using System.Text.RegularExpressions;

namespace AIUsageMonitor.Application.Handoffs;

public sealed record HandoffRedactionResult(
    string Value,
    int Count,
    IReadOnlyList<HandoffRedactionCategory> Categories);

public sealed record HandoffRedactionInspection(
    int Count,
    IReadOnlyList<HandoffRedactionCategory> Categories)
{
    public bool RequiresRedaction => Count > 0;
}

public interface IHandoffRedactionService
{
    HandoffRedactionResult Redact(string value);

    HandoffRedactionInspection ValidateIdentityText(string value);
}

/// <summary>
/// Conservative defense-in-depth redaction for bounded package text. It is not a guarantee that
/// arbitrary secret-shaped text can never evade detection; structured package fields and strict
/// bounds remain the primary safety boundary.
/// </summary>
public sealed class HandoffRedactionService : IHandoffRedactionService
{
    private const string Marker = "[REDACTED]";

    private static readonly Regex ConnectionStringPassword = new(
        @"(?<prefix>\b(?:password|pwd)\s*=\s*)(?:(?<quote>[""'])(?<quotedValue>[^""']+)(?:\k<quote>)|(?<unquotedValue>[^;\s,""']+))",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex AuthorizationHeader = new(
        @"(?<prefix>\bAuthorization\s*:\s*(?:Bearer|Basic)\s+)(?<value>[^\s,;]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex PasswordAssignment = new(
        @"(?<prefix>\b(?:password|passwd|pwd)\b\s*[:=]\s*)(?:(?<quote>[""'])(?<quotedValue>[^""']+)(?:\k<quote>)|(?<unquotedValue>[^\s,;""']+))",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex ApiKeyAssignment = new(
        @"(?<prefix>\b(?:api[_-]?key|apikey|access[_-]?token|secret[_-]?key)\b\s*[:=]\s*)(?:(?<quote>[""'])(?<quotedValue>[^""']+)(?:\k<quote>)|(?<unquotedValue>[^\s,;""']+))",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex BearerToken = new(
        @"(?<prefix>\bBearer\s+)(?<value>[A-Za-z0-9_\-.=+/]{12,})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex PersonalAccessToken = new(
        @"(?<value>(?:ghp_|github_pat_|glpat-|xox[baprs]-|pat_|sk-)[A-Za-z0-9_\-.]{12,})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public HandoffRedactionResult Redact(string value)
    {
        var analysis = AnalyzeAndRedact(value);
        return new(analysis.Value, analysis.Count, analysis.Categories);
    }

    public HandoffRedactionInspection ValidateIdentityText(string value)
    {
        // Use the same ordered rule set as descriptive redaction, but expose only the inspection
        // result so callers can reject a secret-shaped identity without receiving a transformed id.
        var analysis = AnalyzeAndRedact(value);
        return new(analysis.Count, analysis.Categories);
    }

    private static RedactionAnalysis AnalyzeAndRedact(string value)
    {
        ValidateInput(value);

        var categories = new HashSet<HandoffRedactionCategory>();
        var count = 0;
        var result = value;

        result = Replace(result, ConnectionStringPassword, HandoffRedactionCategory.ConnectionStringPassword, categories, ref count);
        result = Replace(result, AuthorizationHeader, HandoffRedactionCategory.AuthorizationHeader, categories, ref count);
        result = Replace(result, ApiKeyAssignment, HandoffRedactionCategory.ApiKeyAssignment, categories, ref count);
        result = Replace(result, PasswordAssignment, HandoffRedactionCategory.PasswordAssignment, categories, ref count);
        result = Replace(result, BearerToken, HandoffRedactionCategory.BearerToken, categories, ref count);
        result = Replace(result, PersonalAccessToken, HandoffRedactionCategory.PersonalAccessToken, categories, ref count);

        return new(result, count, categories.OrderBy(static category => category).ToArray());
    }

    private static void ValidateInput(string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (value.Any(static character => char.IsControl(character) && character is not ('\r' or '\n' or '\t')))
        {
            throw new ArgumentException("Text contains unsupported control characters.", nameof(value));
        }
    }

    private static string Replace(
        string input,
        Regex pattern,
        HandoffRedactionCategory category,
        HashSet<HandoffRedactionCategory> categories,
        ref int count)
    {
        var replacements = 0;
        var result = pattern.Replace(input, match =>
        {
            var value = match.Groups["value"].Success
                ? match.Groups["value"].Value
                : match.Groups["quotedValue"].Success
                    ? match.Groups["quotedValue"].Value
                    : match.Groups["unquotedValue"].Value;
            if (string.IsNullOrEmpty(value) || string.Equals(value, Marker, StringComparison.Ordinal))
            {
                return match.Value;
            }

            categories.Add(category);
            replacements++;
            if (match.Groups["prefix"].Success)
            {
                return match.Groups["prefix"].Value + Marker;
            }

            return Marker;
        });
        count += replacements;
        return result;
    }

    private sealed record RedactionAnalysis(
        string Value,
        int Count,
        IReadOnlyList<HandoffRedactionCategory> Categories);
}
