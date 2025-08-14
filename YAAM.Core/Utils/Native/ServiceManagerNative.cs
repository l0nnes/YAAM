using System.Runtime.InteropServices;

namespace YAAM.Core.Utils.Native;

internal static class ServiceManagerNative
{
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    internal static extern IntPtr OpenSCManager(string? machineName, string? databaseName, uint dwDesiredAccess);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    internal static extern IntPtr CreateService(
        IntPtr hSCManager,
        string lpServiceName,
        string lpDisplayName,
        uint dwDesiredAccess,
        uint dwServiceType,
        uint dwStartType,
        uint dwErrorControl,
        string lpBinaryPathName,
        string? lpLoadOrderGroup,
        IntPtr lpdwTagId,
        string? lpDependencies,
        string? lpServiceStartName,
        string? lpPassword);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeleteService(IntPtr hService);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ChangeServiceConfig(
        IntPtr hService,
        uint nServiceType,
        uint nStartType,
        uint nErrorControl,
        string? lpBinaryPathName,
        string? lpLoadOrderGroup,
        IntPtr lpdwTagId,
        string? lpDependencies,
        string? lpServiceStartName,
        string? lpPassword,
        string? lpDisplayName);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    internal static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, uint dwDesiredAccess);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseServiceHandle(IntPtr hSCObject);

    internal const uint SC_MANAGER_ALL_ACCESS = 0xF003F;
    internal const uint SERVICE_ALL_ACCESS = 0xF01FF;
    internal const uint SERVICE_WIN32_OWN_PROCESS = 0x00000010;
    internal const uint SERVICE_ERROR_NORMAL = 0x00000001;
    internal const uint SERVICE_AUTO_START = 2;
    internal const uint SERVICE_DISABLED = 4;
    internal const uint SERVICE_NO_CHANGE = 0xFFFFFFFF;
}
