using System.Security.Cryptography;
using System.Text;
using AIUsageMonitor.Application.Security;

namespace AIUsageMonitor.Infrastructure.Security;

/// <summary>
/// <see cref="ISecureCredentialStore"/> backed by Windows Credential Manager Generic Credentials.
/// Credentials are written with <c>CRED_PERSIST_LOCAL_MACHINE</c> persistence: per-user, local to
/// this machine, and durable across logon sessions -- the mode documented for a value the current
/// Windows user should keep seeing on this machine every time they sign in, without requiring a
/// roaming profile (<c>CRED_PERSIST_ENTERPRISE</c>) or losing it at logoff (<c>CRED_PERSIST_SESSION</c>).
/// The secret is never written to JSON, JSONL, logs, or any APO file; only the opaque
/// <c>credentialReference</c> is expected to be persisted by callers (BRD &#167;31).
/// </summary>
public sealed class WindowsCredentialManagerStore : ISecureCredentialStore
{
    // Namespaces this application's Generic Credentials within the shared Windows Credential
    // Manager vault so APO entries are identifiable and do not collide with unrelated
    // applications' credentials for the same logical target. This is the permanent APO V1
    // vault namespace; changing it requires an explicit planner-approved migration Story.
    private const string TargetNamePrefix = "AIProjectOrchestrator:Credential:";

    // Generic Credential blob limit (CRED_MAX_CREDENTIAL_BLOB_SIZE = 5 * 512 bytes). Validated
    // here, before any native call, so callers get a clear ArgumentException instead of a
    // confusing native Win32/RPC failure for an input already known to be too large.
    private const int MaxCredentialBlobSizeBytes = 2560;

    private readonly ICredentialManagerNativeStore _nativeStore;
    private readonly Func<bool> _isWindows;

    public WindowsCredentialManagerStore()
        : this(new WindowsCredentialManagerNativeStore(), OperatingSystem.IsWindows)
    {
    }

    internal WindowsCredentialManagerStore(ICredentialManagerNativeStore nativeStore)
        : this(nativeStore, OperatingSystem.IsWindows)
    {
    }

    internal WindowsCredentialManagerStore(ICredentialManagerNativeStore nativeStore, Func<bool> isWindows)
    {
        _nativeStore = nativeStore ?? throw new ArgumentNullException(nameof(nativeStore));
        _isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
    }

    public Task StoreAsync(string credentialReference, string secret, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialReference);
        ArgumentNullException.ThrowIfNull(secret);
        EnsureWindows();
        cancellationToken.ThrowIfCancellationRequested();

        var targetName = BuildTargetName(credentialReference);
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        try
        {
            if (secretBytes.Length > MaxCredentialBlobSizeBytes)
            {
                throw new ArgumentException(
                    $"Encoded secret is {secretBytes.Length} bytes, which exceeds the Windows " +
                    $"Credential Manager Generic Credential blob limit of {MaxCredentialBlobSizeBytes} bytes.",
                    nameof(secret));
            }

            _nativeStore.Write(targetName, secretBytes);
        }
        finally
        {
            // The unmanaged copy of the secret bytes handed to the native layer is zeroed and
            // freed by that layer; this clears the managed transient copy we still hold here.
            // The `secret` System.String parameter itself cannot be reliably zeroed -- managed
            // strings are immutable and may already have been copied by the runtime/GC.
            CryptographicOperations.ZeroMemory(secretBytes);
        }

        return Task.CompletedTask;
    }

    public Task<string?> RetrieveAsync(string credentialReference, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialReference);
        EnsureWindows();
        cancellationToken.ThrowIfCancellationRequested();

        var targetName = BuildTargetName(credentialReference);
        if (!_nativeStore.TryRead(targetName, out var secretBytes))
        {
            return Task.FromResult<string?>(null);
        }

        try
        {
            return Task.FromResult<string?>(Encoding.UTF8.GetString(secretBytes));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secretBytes);
        }
    }

    public Task RemoveAsync(string credentialReference, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialReference);
        EnsureWindows();
        cancellationToken.ThrowIfCancellationRequested();

        var targetName = BuildTargetName(credentialReference);
        _nativeStore.Delete(targetName);
        return Task.CompletedTask;
    }

    private static string BuildTargetName(string credentialReference) =>
        TargetNamePrefix + credentialReference.ToUpperInvariant();

    private void EnsureWindows()
    {
        if (!_isWindows())
        {
            throw new PlatformNotSupportedException(
                "Secure credential storage requires Windows Credential Manager, which is only available on Windows.");
        }
    }
}
