namespace AIUsageMonitor.Application.Security;

/// <summary>
/// Abstracts secret storage behind an opaque reference string (e.g. "GitHub:Copilot:Primary").
/// JSON/JSONL persistence only stores the reference, never the secret itself (BRD §31). The
/// approved Windows secure-storage adapter is added when provider connection flows are built.
/// </summary>
public interface ISecureCredentialStore
{
    Task StoreAsync(string credentialReference, string secret, CancellationToken cancellationToken = default);

    Task<string?> RetrieveAsync(string credentialReference, CancellationToken cancellationToken = default);

    Task RemoveAsync(string credentialReference, CancellationToken cancellationToken = default);
}
