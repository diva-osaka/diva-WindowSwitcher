using System.Runtime.InteropServices;

namespace WindowSwitcher;

public static partial class WindowActivator
{
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(nint hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool BringWindowToTop(nint hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(nint hWnd, int nCmdShow);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsIconic(nint hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

    [LibraryImport("user32.dll")]
    private static partial nint GetForegroundWindow();

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    [LibraryImport("kernel32.dll")]
    private static partial uint GetCurrentThreadId();

    private const int SW_RESTORE = 9;

    public static void Activate(nint handle)
    {
        if (IsIconic(handle))
            ShowWindow(handle, SW_RESTORE);

        var foreground = GetForegroundWindow();
        var currentThreadId = GetCurrentThreadId();
        GetWindowThreadProcessId(foreground, out _);
        GetWindowThreadProcessId(handle, out _);
        var targetThreadId = GetWindowThreadProcessId(handle, out _);

        if (currentThreadId != targetThreadId)
        {
            AttachThreadInput(currentThreadId, targetThreadId, true);
            BringWindowToTop(handle);
            SetForegroundWindow(handle);
            AttachThreadInput(currentThreadId, targetThreadId, false);
        }
        else
        {
            BringWindowToTop(handle);
            SetForegroundWindow(handle);
        }
    }
}
