using System.Diagnostics.CodeAnalysis;
using System.Text;
using AIUsageMonitor.Infrastructure.Security;

namespace AIUsageMonitor.Infrastructure.Tests.Security;

/// <summary>
/// In-memory <see cref="ICredentialManagerNativeStore"/> double so
/// <see cref="WindowsCredentialManagerStore"/> can be exercised without touching the real Windows
/// Credential Manager vault. Records every call so tests can assert exactly what the store passed
/// to the native layer.
/// </summary>
internal sealed class FakeCredentialManagerNativeStore : ICredentialManagerNativeStore
{
    private readonly Dictionary<string, byte[]> _entries = new(StringComparer.Ordinal);

    public List<string> WriteCalls { get; } = new();

    public List<string> ReadCalls { get; } = new();

    public List<string> DeleteCalls { get; } = new();

    public Exception? WriteException { get; set; }

    public Exception? ReadException { get; set; }

    public Exception? DeleteException { get; set; }

    public void Write(string targetName, byte[] secretBytes)
    {
        WriteCalls.Add(targetName);
        if (WriteException is not null)
        {
            throw WriteException;
        }

        _entries[targetName] = (byte[])secretBytes.Clone();
    }

    public bool TryRead(string targetName, [NotNullWhen(true)] out byte[]? secretBytes)
    {
        ReadCalls.Add(targetName);
        if (ReadException is not null)
        {
            throw ReadException;
        }

        if (_entries.TryGetValue(targetName, out var stored))
        {
            secretBytes = (byte[])stored.Clone();
            return true;
        }

        secretBytes = null;
        return false;
    }

    public void Delete(string targetName)
    {
        DeleteCalls.Add(targetName);
        if (DeleteException is not null)
        {
            throw DeleteException;
        }

        _entries.Remove(targetName);
    }

    public string? GetStoredSecretUtf8(string targetName) =>
        _entries.TryGetValue(targetName, out var bytes) ? Encoding.UTF8.GetString(bytes) : null;
}
