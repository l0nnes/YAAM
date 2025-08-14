using System.Runtime.InteropServices;

namespace YAAM.Core.Utils.Native;

internal static class CommandLineHelper
{
    internal static string GetFileName(string cmdLine)
    {
        var argvPtr = CommandLineToArgvW(cmdLine, out _);

        if (argvPtr == IntPtr.Zero)
        {
            return cmdLine.Trim('"', ' ');
        }

        try
        {
            return Marshal.PtrToStringUni(Marshal.ReadIntPtr(argvPtr, 0)) ?? string.Empty;
        }
        finally
        {
            LocalFree(argvPtr);
        }
    }

    [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CommandLineToArgvW(string lpCmdLine, out int pNumArgs);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);
}
