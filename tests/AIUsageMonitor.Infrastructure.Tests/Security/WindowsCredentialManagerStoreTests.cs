using System.Text;
using AIUsageMonitor.Infrastructure.Security;

namespace AIUsageMonitor.Infrastructure.Tests.Security;

public sealed class WindowsCredentialManagerStoreTests
{
    private const string TargetPrefix = "AIProjectOrchestrator:Credential:";

    [Fact]
    public async Task StoreAsync_PassesCredentialReferenceAndSecretToNativeStore()
    {
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native);

        await store.StoreAsync("GitHub:Copilot:Primary", "super-secret-token");

        var target = Assert.Single(native.WriteCalls);
        Assert.Equal(TargetPrefix + "GITHUB:COPILOT:PRIMARY", target);
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
            WriteException = new CredentialManagerNativeException("write", TargetPrefix + "ANTIGRAVITY:PRIMARY", 5)
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

        var files = Directory
            .EnumerateFiles(temporary.Paths.RootDirectory, "*", SearchOption.AllDirectories)
            .ToArray();

        Assert.Empty(files);
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

    // --- Case identity (Sol decision: credentialReference is case-insensitive; canonical
    // Windows target = ToUpperInvariant()) ---

    [Theory]
    [InlineData("GitHub:Copilot:Primary")]
    [InlineData("github:copilot:primary")]
    [InlineData("GITHUB:COPILOT:PRIMARY")]
    [InlineData("GiThUb:CoPiLoT:PrImArY")]
    public async Task RetrieveAsync_ResolvesTheSameCredential_RegardlessOfReferenceCasing(string casedReference)
    {
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native);
        await store.StoreAsync("GitHub:Copilot:Primary", "shared-secret");

        var retrieved = await store.RetrieveAsync(casedReference);

        Assert.Equal("shared-secret", retrieved);
    }

    [Fact]
    public async Task StoreAsync_UnderDifferentCasing_ReplacesTheSameCredential()
    {
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native);

        await store.StoreAsync("GitHub:Copilot:Primary", "first-secret");
        await store.StoreAsync("github:copilot:primary", "second-secret");

        Assert.Equal("second-secret", await store.RetrieveAsync("GITHUB:COPILOT:PRIMARY"));
        Assert.Equal(2, native.WriteCalls.Count);
        Assert.Equal(native.WriteCalls[0], native.WriteCalls[1]);
    }

    [Fact]
    public async Task RemoveAsync_UnderDifferentCasing_RemovesTheSameCredential()
    {
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native);
        await store.StoreAsync("GitHub:Copilot:Primary", "secret");

        await store.RemoveAsync("GITHUB:COPILOT:PRIMARY");

        Assert.Null(await store.RetrieveAsync("github:copilot:primary"));
    }

    [Fact]
    public async Task BuildTargetName_ProducesTheExactPermanentApoNamespaceAndUppercaseReference()
    {
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native);

        await store.StoreAsync("GitHub:Copilot:Primary", "secret");

        var target = Assert.Single(native.WriteCalls);
        Assert.Equal("AIProjectOrchestrator:Credential:GITHUB:COPILOT:PRIMARY", target);
    }

    // --- Native failure propagation ---

    [Fact]
    public async Task RetrieveAsync_NativeReadFailure_PropagatesWithoutLeakingSecretContent()
    {
        var native = new FakeCredentialManagerNativeStore
        {
            ReadException = new CredentialManagerNativeException("read", TargetPrefix + "CODEX:PRIMARY", 87)
        };
        var store = new WindowsCredentialManagerStore(native);

        var exception = await Record.ExceptionAsync(() => store.RetrieveAsync("Codex:Primary"));

        var nativeException = Assert.IsType<CredentialManagerNativeException>(exception);
        Assert.Equal(87, nativeException.NativeErrorCode);
    }

    [Fact]
    public async Task RemoveAsync_NativeDeleteFailure_Propagates()
    {
        var native = new FakeCredentialManagerNativeStore
        {
            DeleteException = new CredentialManagerNativeException("delete", TargetPrefix + "KIMI:PRIMARY", 5)
        };
        var store = new WindowsCredentialManagerStore(native);

        var exception = await Record.ExceptionAsync(() => store.RemoveAsync("Kimi:Primary"));

        var nativeException = Assert.IsType<CredentialManagerNativeException>(exception);
        Assert.Equal(5, nativeException.NativeErrorCode);
    }

    // --- RetrieveAsync / RemoveAsync validation ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RetrieveAsync_RejectsNullEmptyOrWhitespaceCredentialReference(string? credentialReference)
    {
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.RetrieveAsync(credentialReference!));
        Assert.Empty(native.ReadCalls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RemoveAsync_RejectsNullEmptyOrWhitespaceCredentialReference(string? credentialReference)
    {
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.RemoveAsync(credentialReference!));
        Assert.Empty(native.DeleteCalls);
    }

    // --- Cancellation ---

    [Fact]
    public async Task RetrieveAsync_HonorsCancellationBeforeTheNativeOperation()
    {
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => store.RetrieveAsync("GitHub:Copilot:Primary", cts.Token));

        Assert.Empty(native.ReadCalls);
    }

    [Fact]
    public async Task RemoveAsync_HonorsCancellationBeforeTheNativeOperation()
    {
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => store.RemoveAsync("GitHub:Copilot:Primary", cts.Token));

        Assert.Empty(native.DeleteCalls);
    }

    // --- Empty and Unicode secrets ---

    [Fact]
    public async Task StoreAsync_RoundTripsAnEmptySecretExactly()
    {
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native);

        await store.StoreAsync("Empty:Secret", string.Empty);
        var retrieved = await store.RetrieveAsync("Empty:Secret");

        Assert.Equal(string.Empty, retrieved);
    }

    [Fact]
    public async Task StoreAsync_RoundTripsAUnicodeSecretExactly()
    {
        // Synthetic value combining Latin diacritics, CJK, and an astral-plane emoji to exercise
        // multi-byte and surrogate-pair UTF-8 encoding round-tripping.
        const string secret = "Sÿnthëtic-测试-🚀-value";
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native);

        await store.StoreAsync("Unicode:Secret", secret);
        var retrieved = await store.RetrieveAsync("Unicode:Secret");

        Assert.Equal(secret, retrieved);
    }

    // --- Deterministic oversize validation (2560-byte Generic Credential blob limit) ---

    [Fact]
    public async Task StoreAsync_AcceptsASecretEncodingToExactlyTheMaximumBlobSize()
    {
        var secret = new string('a', 2560);
        Assert.Equal(2560, Encoding.UTF8.GetByteCount(secret));
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native);

        await store.StoreAsync("MaxSize:Secret", secret);

        Assert.Single(native.WriteCalls);
        Assert.Equal(secret, await store.RetrieveAsync("MaxSize:Secret"));
    }

    [Fact]
    public async Task StoreAsync_RejectsASecretEncodingToOneByteOverTheMaximumBlobSize_BeforeAnyNativeCall()
    {
        var secret = new string('a', 2561);
        Assert.Equal(2561, Encoding.UTF8.GetByteCount(secret));
        var native = new FakeCredentialManagerNativeStore();
        var store = new WindowsCredentialManagerStore(native);

        var exception = await Record.ExceptionAsync(() => store.StoreAsync("OverSize:Secret", secret));

        var argumentException = Assert.IsType<ArgumentException>(exception);
        Assert.Equal("secret", argumentException.ParamName);
        Assert.DoesNotContain(secret, argumentException.Message, StringComparison.Ordinal);
        Assert.Empty(native.WriteCalls);
    }
}
