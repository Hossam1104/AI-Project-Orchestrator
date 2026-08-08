namespace AIUsageMonitor.Application.Security;

/// <summary>
/// Abstracts secret storage behind an opaque reference string (e.g. "GitHub:Copilot:Primary").
/// The database only ever stores the reference, never the secret itself (BRD §31). Implemented
/// against Windows Credential Manager/DPAPI in a later session.
/// </summary>
public interface ISecureCredentialStore
{
    Task StoreAsync(string credentialReference, string secret, CancellationToken cancellationToken = default);

    Task<string?> RetrieveAsync(string credentialReference, CancellationToken cancellationToken = default);

    Task RemoveAsync(string credentialReference, CancellationToken cancellationToken = default);
}
