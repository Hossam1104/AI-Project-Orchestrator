using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace AIUsageMonitor.Infrastructure.Security;

/// <summary>
/// Direct P/Invoke wrapper over the documented Windows Credential Manager Generic Credential APIs
/// (wincred.h: <c>CredWriteW</c>, <c>CredReadW</c>, <c>CredDeleteW</c>, <c>CredFree</c>). Every
/// pointer-sized field is declared as <see cref="IntPtr"/> so the struct layout is correct on
/// win-x86, win-x64, and win-arm64 without any manual pointer-size arithmetic. This type performs
/// no I/O other than the native calls; callers are responsible for the <see cref="OperatingSystem.IsWindows"/>
/// guard (see <see cref="WindowsCredentialManagerStore"/>).
/// </summary>
internal sealed class WindowsCredentialManagerNativeStore : ICredentialManagerNativeStore
{
    // wincred.h CRED_TYPE_GENERIC (1): a generic credential not tied to an authentication package.
    private const int CredTypeGeneric = 1;

    // wincred.h CRED_PERSIST_LOCAL_MACHINE (2): persists across logon sessions for this Windows
    // user on this machine only. Selected over CRED_PERSIST_SESSION (lost at logoff, unsuitable
    // for a desktop app that must remember provider credentials between sessions) and
    // CRED_PERSIST_ENTERPRISE (roams with the user profile and caps CredentialBlobSize far below
    // CRED_MAX_CREDENTIAL_BLOB_SIZE, which APO does not need and would only add attack surface).
    private const int CredPersistLocalMachine = 2;

    // winerror.h ERROR_NOT_FOUND (1168 / 0x490): "Element not found." Returned by CredReadW and
    // CredDeleteW when no credential exists for the target name.
    private const int ErrorNotFound = 1168;

    public void Write(string targetName, byte[] secretBytes)
    {
        var blobHandle = Marshal.AllocHGlobal(Math.Max(secretBytes.Length, 1));
        try
        {
            if (secretBytes.Length > 0)
            {
                Marshal.Copy(secretBytes, 0, blobHandle, secretBytes.Length);
            }

            var credential = new NativeCredential
            {
                Flags = 0,
                Type = CredTypeGeneric,
                TargetName = targetName,
                Comment = null,
                LastWritten = default,
                CredentialBlobSize = secretBytes.Length,
                CredentialBlob = blobHandle,
                Persist = CredPersistLocalMachine,
                AttributeCount = 0,
                Attributes = IntPtr.Zero,
                TargetAlias = null,
                UserName = null
            };

            if (!CredWriteW(ref credential, 0))
            {
                var errorCode = Marshal.GetLastWin32Error();
                throw new CredentialManagerNativeException("write", targetName, errorCode);
            }
        }
        finally
        {
            ZeroAndFree(blobHandle, secretBytes.Length);
        }
    }

    public bool TryRead(string targetName, [NotNullWhen(true)] out byte[]? secretBytes)
    {
        if (!CredReadW(targetName, CredTypeGeneric, 0, out var credentialPtr))
        {
            var errorCode = Marshal.GetLastWin32Error();
            if (errorCode == ErrorNotFound)
            {
                secretBytes = null;
                return false;
            }

            throw new CredentialManagerNativeException("read", targetName, errorCode);
        }

        try
        {
            var credential = Marshal.PtrToStructure<NativeCredential>(credentialPtr);
            if (credential.CredentialBlobSize <= 0 || credential.CredentialBlob == IntPtr.Zero)
            {
                secretBytes = Array.Empty<byte>();
                return true;
            }

            var buffer = new byte[credential.CredentialBlobSize];
            Marshal.Copy(credential.CredentialBlob, buffer, 0, credential.CredentialBlobSize);
            secretBytes = buffer;
            return true;
        }
        finally
        {
            CredFree(credentialPtr);
        }
    }

    public void Delete(string targetName)
    {
        if (!CredDeleteW(targetName, CredTypeGeneric, 0))
        {
            var errorCode = Marshal.GetLastWin32Error();
            if (errorCode == ErrorNotFound)
            {
                return;
            }

            throw new CredentialManagerNativeException("delete", targetName, errorCode);
        }
    }

    private static void ZeroAndFree(IntPtr handle, int length)
    {
        if (handle == IntPtr.Zero)
        {
            return;
        }

        for (var i = 0; i < length; i++)
        {
            Marshal.WriteByte(handle, i, 0);
        }

        Marshal.FreeHGlobal(handle);
    }

    // Mirrors the documented CREDENTIALW layout exactly (field order, names, and types) per
    // https://learn.microsoft.com/windows/win32/api/wincred/ns-wincred-credentialw. DWORD fields
    // are `int` (always 32-bit); pointer/string fields are IntPtr or LPWStr-marshaled strings so
    // the struct is correctly sized on win-x86, win-x64, and win-arm64.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public int Flags;
        public int Type;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string TargetName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? Comment;

        public FILETIME LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? TargetAlias;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? UserName;
    }

    [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredWriteW(ref NativeCredential credential, int flags);

    [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredReadW(string targetName, int type, int flags, out IntPtr credentialPtr);

    [DllImport("Advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredDeleteW(string targetName, int type, int flags);

    [DllImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = false)]
    private static extern void CredFree(IntPtr buffer);
}
