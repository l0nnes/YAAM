using System.Runtime.InteropServices;

namespace YAAM.Core.Utils.Native;

internal sealed class Wow64FsRedirection : IDisposable
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool Wow64DisableWow64FsRedirection(ref IntPtr ptr);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool Wow64RevertWow64FsRedirection(IntPtr ptr);

    private readonly IntPtr _oldValue;
    private readonly bool _reverted;

    public Wow64FsRedirection()
    {
        if (!Environment.Is64BitOperatingSystem || Environment.Is64BitProcess) return;
        _oldValue = IntPtr.Zero;
        _reverted = Wow64DisableWow64FsRedirection(ref _oldValue);
    }

    public void Dispose()
    {
        if (_reverted)
        {
            Wow64RevertWow64FsRedirection(_oldValue);
        }
    }
}
