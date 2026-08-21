using AIUsageMonitor.Infrastructure.Security;

namespace AIUsageMonitor.Infrastructure.Tests.Security;

public sealed class WindowsCredentialManagerStoreTests
{
    [Fact]
    public async Task StoreAsync_PassesCredentialReferenceAndSecretToNativeStore()
    {
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native);

        await store.StoreAsync("GitHub:Copilot:Primary", "super-secret-token");

        var target = Assert.Single(native.WriteCalls);
        Assert.Contains("GitHub:Copilot:Primary", target, StringComparison.Ordinal);
        Assert.Equal("super-secret-token", native.GetStoredSecretUtf8(target));
    }

    [Fact]
    public async Task StoreAsync_SupportsReplacementOnRepeatedCalls()
    {
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native);

        await store.StoreAsync("GitHub:Copilot:Primary", "first-secret");
        await store.StoreAsync("GitHub:Copilot:Primary", "second-secret");

        var retrieved = await store.RetrieveAsync("GitHub:Copilot:Primary");
        Assert.Equal("second-secret", retrieved);
        Assert.Equal(2, native.WriteCalls.Count);
    }

    [Fact]
    public async Task RetrieveAsync_ReturnsExactStoredValue()
    {
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native);
        await store.StoreAsync("Codex:Primary", "codex-secret-value");

        var retrieved = await store.RetrieveAsync("Codex:Primary");

        Assert.Equal("codex-secret-value", retrieved);
    }

    [Fact]
    public async Task RetrieveAsync_ReturnsNull_WhenCredentialNotFound()
    {
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native);

        var retrieved = await store.RetrieveAsync("Missing:Reference");

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task RemoveAsync_InvokesNativeDeletionAndClearsTheCredential()
    {
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native);
        await store.StoreAsync("Kimi:Primary", "kimi-secret");

        await store.RemoveAsync("Kimi:Primary");

        Assert.Null(await store.RetrieveAsync("Kimi:Primary"));
        Assert.Single(native.DeleteCalls);
    }

    [Fact]
    public async Task RemoveAsync_MissingCredential_IsIdempotent()
    {
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native);

        var exception = await Record.ExceptionAsync(() => store.RemoveAsync("Never:Stored"));

        Assert.Null(exception);
        Assert.Single(native.DeleteCalls);
    }

    [Fact]
    public async Task NativeWriteFailure_PropagatesWithoutExposingTheSecret()
    {
        const string secret = "top-secret-value-should-not-leak";
        var native = new FakeCredentialManagerNativeStore
        {
            WriteException = new CredentialManagerNativeException("write", "AIUsageMonitor:Credential:Antigravity:Primary", 5)
        };
        var store = new WindowsCredentialManagerStore(native);

        var exception = await Record.ExceptionAsync(() => store.StoreAsync("Antigravity:Primary", secret));

        var nativeException = Assert.IsType<CredentialManagerNativeException>(exception);
        Assert.Equal(5, nativeException.NativeErrorCode);
        Assert.DoesNotContain(secret, nativeException.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, nativeException.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task StoreAsync_RejectsNullEmptyOrWhitespaceCredentialReference(string? credentialReference)
    {
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native);

        // ArgumentException.ThrowIfNullOrWhiteSpace throws ArgumentNullException for null and
        // ArgumentException for empty/whitespace; both derive from ArgumentException.
        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.StoreAsync(credentialReference!, "secret"));
        Assert.Empty(native.WriteCalls);
    }

    [Fact]
    public async Task StoreAsync_HonorsCancellationBeforeTheNativeOperation()
    {
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => store.StoreAsync("GitHub:Copilot:Primary", "secret", cts.Token));

        Assert.Empty(native.WriteCalls);
    }

    [Fact]
    public async Task StoreAsync_DoesNotWriteTheSecretToAnyFileUnderApplicationStorage()
    {
        using var temporary = new TemporaryStore();
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native);
        const string secret = "file-persistence-guard-secret";

        await store.StoreAsync("GitHub:Copilot:Primary", secret);

        foreach (var file in Directory.EnumerateFiles(temporary.Paths.RootDirectory, "*", SearchOption.AllDirectories))
        {
            var content = await File.ReadAllTextAsync(file);
            Assert.DoesNotContain(secret, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task NonWindowsPlatform_FailsTruthfullyWithoutCallingTheNativeStore()
    {
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native, isWindows: () => false);

        await Assert.ThrowsAsync<PlatformNotSupportedException>(
            () => store.StoreAsync("GitHub:Copilot:Primary", "secret"));
        await Assert.ThrowsAsync<PlatformNotSupportedException>(
            () => store.RetrieveAsync("GitHub:Copilot:Primary"));
        await Assert.ThrowsAsync<PlatformNotSupportedException>(
            () => store.RemoveAsync("GitHub:Copilot:Primary"));

        Assert.Empty(native.WriteCalls);
        Assert.Empty(native.ReadCalls);
        Assert.Empty(native.DeleteCalls);
    }

    [Fact]
    public void Constructor_RejectsNullNativeStore()
    {
        Assert.Throws<ArgumentNullException>(() => new WindowsCredentialManagerStore(null!));
    }
}
