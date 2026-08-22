namespace AIUsageMonitor.Application.Security;

/// <summary>
/// Abstracts secret storage behind an opaque reference string (e.g. "GitHub:Copilot:Primary").
/// JSON/JSONL persistence only stores the reference, never the secret itself (BRD §31). The
/// approved Windows secure-storage adapter is added when provider connection flows are built.
/// <c>credentialReference</c> is a case-insensitive identifier: "GitHub:Copilot:Primary",
/// "github:copilot:primary", and "GITHUB:COPILOT:PRIMARY" all identify the same credential.
/// Casing must never be used to distinguish two credentials.
/// </summary>
public interface ISecureCredentialStore
{
    Task StoreAsync(string credentialReference, string secret, CancellationToken cancellationToken = default);

    Task<string?> RetrieveAsync(string credentialReference, CancellationToken cancellationToken = default);

    Task RemoveAsync(string credentialReference, CancellationToken cancellationToken = default);
}
