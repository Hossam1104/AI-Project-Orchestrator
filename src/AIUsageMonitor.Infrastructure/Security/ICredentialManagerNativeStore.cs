using System.Diagnostics.CodeAnalysis;

namespace AIUsageMonitor.Infrastructure.Security;

/// <summary>
/// Minimal seam over the Windows Credential Manager Generic Credential native calls so
/// <see cref="WindowsCredentialManagerStore"/> can be unit tested without touching the owner's
/// real Windows Credential Manager vault.
/// </summary>
internal interface ICredentialManagerNativeStore
{
    /// <summary>
    /// Creates or replaces the Generic Credential for <paramref name="targetName"/>. Throws
    /// <see cref="CredentialManagerNativeException"/> on any native failure.
    /// </summary>
    void Write(string targetName, byte[] secretBytes);

    /// <summary>
    /// Reads the Generic Credential for <paramref name="targetName"/>. Returns <see langword="false"/>
    /// when no credential exists. Throws <see cref="CredentialManagerNativeException"/> for any other
    /// native failure.
    /// </summary>
    bool TryRead(string targetName, [NotNullWhen(true)] out byte[]? secretBytes);

    /// <summary>
    /// Deletes the Generic Credential for <paramref name="targetName"/>. A missing target is treated
    /// as an idempotent no-op. Throws <see cref="CredentialManagerNativeException"/> for any other
    /// native failure.
    /// </summary>
    void Delete(string targetName);
}
