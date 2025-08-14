using System.ComponentModel;
using System.Runtime.InteropServices;
using YAAM.Core.Models;
using YAAM.Core.Utils.Native;

namespace YAAM.Core.Utils;

internal static class ServiceManager
{
    internal static void CreateService(AutostartItem item)
    {
        var command = $"\"{item.ExecutablePath}\" {item.Arguments}".Trim();

        var scmHandle = ServiceManagerNative.OpenSCManager(null, null, ServiceManagerNative.SC_MANAGER_ALL_ACCESS);
        if (scmHandle == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        
        try
        {
            var serviceHandle = ServiceManagerNative.CreateService(
                scmHandle,
                item.Name,
                item.Name, 
                ServiceManagerNative.SERVICE_ALL_ACCESS,
                ServiceManagerNative.SERVICE_WIN32_OWN_PROCESS,
                item.IsEnabled ? ServiceManagerNative.SERVICE_AUTO_START : ServiceManagerNative.SERVICE_DISABLED,
                ServiceManagerNative.SERVICE_ERROR_NORMAL,
                command,
                null, IntPtr.Zero, null, null, null);

            if (serviceHandle == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            ServiceManagerNative.CloseServiceHandle(serviceHandle);
        }
        finally
        {
            ServiceManagerNative.CloseServiceHandle(scmHandle);
        }
    }

    internal static void DeleteService(string serviceName)
    {
        var scmHandle = ServiceManagerNative.OpenSCManager(null, null, ServiceManagerNative.SC_MANAGER_ALL_ACCESS);
        if (scmHandle == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        try
        {
            var serviceHandle = ServiceManagerNative.OpenService(scmHandle, serviceName, ServiceManagerNative.SERVICE_ALL_ACCESS);
            if (serviceHandle == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            try
            {
                if (!ServiceManagerNative.DeleteService(serviceHandle))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                ServiceManagerNative.CloseServiceHandle(serviceHandle);
            }
        }
        finally
        {
            ServiceManagerNative.CloseServiceHandle(scmHandle);
        }
    }

    internal static void ModifyService(AutostartItem item)
    {
        var command = $"\"{item.ExecutablePath}\" {item.Arguments}".Trim();

        var scmHandle = ServiceManagerNative.OpenSCManager(null, null, ServiceManagerNative.SC_MANAGER_ALL_ACCESS);
        if (scmHandle == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        try
        {
            var serviceHandle = ServiceManagerNative.OpenService(scmHandle, item.Location, ServiceManagerNative.SERVICE_ALL_ACCESS);
            if (serviceHandle == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            try
            {
                if (!ServiceManagerNative.ChangeServiceConfig(
                        serviceHandle, ServiceManagerNative.SERVICE_NO_CHANGE, ServiceManagerNative.SERVICE_NO_CHANGE, ServiceManagerNative.SERVICE_NO_CHANGE,
                        command, null, IntPtr.Zero, null, null, null, item.Name))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                ServiceManagerNative.CloseServiceHandle(serviceHandle);
            }
        }
        finally
        {
            ServiceManagerNative.CloseServiceHandle(scmHandle);
        }
    }
}