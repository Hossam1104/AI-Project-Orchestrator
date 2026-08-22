namespace AIUsageMonitor.Infrastructure.Security;

/// <summary>
/// Reports a genuine Windows Credential Manager native failure (Win32 error code from
/// <c>CredWriteW</c>, <c>CredReadW</c>, or <c>CredDeleteW</c>). The message intentionally carries
/// only the operation name, target name, and Win32 error code -- never the secret value.
/// </summary>
public sealed class CredentialManagerNativeException : Exception
{
    public CredentialManagerNativeException(string operation, string targetName, int nativeErrorCode)
        : base($"Windows Credential Manager '{operation}' failed for target '{targetName}' with Win32 error code {nativeErrorCode}.")
    {
        Operation = operation;
        NativeErrorCode = nativeErrorCode;
    }

    public string Operation { get; }

    public int NativeErrorCode { get; }
}
