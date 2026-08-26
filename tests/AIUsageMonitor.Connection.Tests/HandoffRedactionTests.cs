using AIUsageMonitor.Application.Handoffs;

namespace AIUsageMonitor.Connection.Tests;

public sealed class HandoffRedactionTests
{
    public static IEnumerable<object[]> SecretShapedValues()
    {
        yield return ["password=secret-value", "secret-value", HandoffRedactionCategory.ConnectionStringPassword];
        yield return ["api_key=api-secret-value", "api-secret-value", HandoffRedactionCategory.ApiKeyAssignment];
        yield return ["Authorization: Bearer abcdefghijklmnop", "abcdefghijklmnop", HandoffRedactionCategory.AuthorizationHeader];
        yield return ["Bearer abcdefghijklmnop", "abcdefghijklmnop", HandoffRedactionCategory.BearerToken];
        yield return ["token ghp_12345678901234567890", "ghp_12345678901234567890", HandoffRedactionCategory.PersonalAccessToken];
        yield return ["Server=local;Password=connection-secret;Database=apo", "connection-secret", HandoffRedactionCategory.ConnectionStringPassword];
        yield return ["password=\"my secret value\"", "my secret value", HandoffRedactionCategory.ConnectionStringPassword];
        yield return ["api_key='my secret value'", "my secret value", HandoffRedactionCategory.ApiKeyAssignment];
    }

    [Theory]
    [MemberData(nameof(SecretShapedValues))]
    public void RecognizedSecretValuesAreReplacedAndCategorized(
        string input,
        string secret,
        HandoffRedactionCategory category)
    {
        var result = new HandoffRedactionService().Redact(input);

        Assert.Contains("[REDACTED]", result.Value, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, result.Value, StringComparison.Ordinal);
        Assert.True(result.Count > 0);
        Assert.Contains(category, result.Categories);
    }

    [Fact]
    public void UnsupportedControlCharactersFailClosed()
    {
        Assert.Throws<ArgumentException>(() => new HandoffRedactionService().Redact("unsafe\0text"));
    }

    [Fact]
    public void IdentityValidationReportsSecretsWithoutReturningAChangedIdentity()
    {
        var result = new HandoffRedactionService().ValidateIdentityText("api_key=identity-secret-value");

        Assert.True(result.RequiresRedaction);
        Assert.Equal(1, result.Count);
        Assert.Contains(HandoffRedactionCategory.ApiKeyAssignment, result.Categories);
    }
}
