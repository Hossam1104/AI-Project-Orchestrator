using AIUsageMonitor.Application.Projects;

namespace AIUsageMonitor.Infrastructure.Tests;

/// <summary>
/// Proves untrusted remote URL strings never surface credentials, query strings, fragments, or
/// injected control characters in the sanitized display value, across both absolute-URI (HTTPS)
/// and SCP-style (SSH shorthand) remote syntaxes.
/// </summary>
public sealed class RepositoryUrlSanitizerTests
{
    [Fact]
    public void Sanitize_HttpsWithUserInfo_StripsCredentials()
    {
        var result = RepositoryUrlSanitizer.Sanitize("https://token:secret@github.com/org/repo.git");

        Assert.DoesNotContain("token", result);
        Assert.DoesNotContain("secret", result);
        Assert.Equal("https://github.com/org/repo.git", result);
    }

    [Fact]
    public void Sanitize_HttpsWithQueryString_StripsQuery()
    {
        var result = RepositoryUrlSanitizer.Sanitize("https://github.com/org/repo.git?token=secret");

        Assert.DoesNotContain("token", result);
        Assert.DoesNotContain("secret", result);
        Assert.DoesNotContain("?", result);
    }

    [Fact]
    public void Sanitize_HttpsWithFragment_StripsFragment()
    {
        var result = RepositoryUrlSanitizer.Sanitize("https://github.com/org/repo.git#access_token=secret");

        Assert.DoesNotContain("secret", result);
        Assert.DoesNotContain("#", result);
    }

    [Fact]
    public void Sanitize_HttpsWithUserInfoQueryAndFragment_StripsAll()
    {
        var result = RepositoryUrlSanitizer.Sanitize(
            "https://token:secret@github.com/org/repo.git?auth=x#frag=y");

        Assert.DoesNotContain("token", result);
        Assert.DoesNotContain("secret", result);
        Assert.DoesNotContain("auth", result);
        Assert.DoesNotContain("frag", result);
        Assert.Equal("https://github.com/org/repo.git", result);
    }

    [Fact]
    public void Sanitize_ScpStyle_StripsUserInfo()
    {
        var result = RepositoryUrlSanitizer.Sanitize("git@github.com:org/repo.git");

        Assert.DoesNotContain("git@", result);
        Assert.Equal("github.com:org/repo.git", result);
    }

    [Fact]
    public void Sanitize_ScpStyleWithQueryLikeSuffix_StripsSuffix()
    {
        var result = RepositoryUrlSanitizer.Sanitize("git@github.com:org/repo.git?token=secret");

        Assert.DoesNotContain("token", result);
        Assert.DoesNotContain("secret", result);
        Assert.DoesNotContain("?", result);
    }

    [Fact]
    public void Sanitize_ScpStyleWithFragmentLikeSuffix_StripsSuffix()
    {
        var result = RepositoryUrlSanitizer.Sanitize("git@github.com:org/repo.git#secret");

        Assert.DoesNotContain("secret", result);
        Assert.DoesNotContain("#", result);
    }

    [Fact]
    public void Sanitize_ScpStyleWithCombinedQueryAndFragment_StripsBoth()
    {
        var result = RepositoryUrlSanitizer.Sanitize("git@github.com:org/repo.git?token=secret#frag=other");

        Assert.DoesNotContain("token", result);
        Assert.DoesNotContain("secret", result);
        Assert.DoesNotContain("frag", result);
        Assert.DoesNotContain("other", result);
        Assert.Equal("github.com:org/repo.git", result);
    }

    [Theory]
    [InlineData("https://github.com/org/repo.git\r\nInjected-Header: evil")]
    [InlineData("https://github.com/org/repo.git\nInjected-Line")]
    [InlineData("git@github.com:org/repo.git\r\nfake line")]
    public void Sanitize_CrLfInjection_NeverProducesMultilineOutput(string maliciousInput)
    {
        var result = RepositoryUrlSanitizer.Sanitize(maliciousInput);

        Assert.DoesNotContain("\r", result);
        Assert.DoesNotContain("\n", result);
    }

    [Fact]
    public void Sanitize_ControlCharacterInjection_RemovesControlCharacters()
    {
        var withControlChars = "https://github.com/org/repo" + (char)0x07 + (char)0x00 + ".git";

        var result = RepositoryUrlSanitizer.Sanitize(withControlChars);

        Assert.DoesNotContain((char)0x07, result);
        Assert.DoesNotContain((char)0x00, result);
        foreach (var character in result)
        {
            Assert.False(char.IsControl(character), $"Unexpected control character in sanitized result.");
        }
    }

    [Fact]
    public void Sanitize_UnparseableInput_ReturnsSafePlaceholder()
    {
        var result = RepositoryUrlSanitizer.Sanitize("not a url at all, just text");

        Assert.Equal("Remote URL unavailable", result);
    }

    [Fact]
    public void Sanitize_NullOrWhitespace_ReturnsNotConfigured()
    {
        Assert.Equal("Not configured", RepositoryUrlSanitizer.Sanitize(null));
        Assert.Equal("Not configured", RepositoryUrlSanitizer.Sanitize("   "));
    }

    [Fact]
    public void Sanitize_ExceedsMaxLength_IsBoundedWithEllipsis()
    {
        var longPath = new string('a', 1000);
        var result = RepositoryUrlSanitizer.Sanitize($"https://github.com/org/{longPath}.git");

        Assert.True(result.Length <= 512);
        Assert.EndsWith("…", result);
    }

    [Theory]
    [InlineData("git@github.com:org/repo.git")]
    [InlineData("https://github.com/org/repo.git")]
    [InlineData("ssh://git@github.com/org/repo.git")]
    public void Sanitize_OrdinarySafeRemotes_AreUnchangedInSubstance(string safeRemote)
    {
        var result = RepositoryUrlSanitizer.Sanitize(safeRemote);

        Assert.Contains("github.com", result);
        Assert.Contains("org/repo.git", result);
    }
}
