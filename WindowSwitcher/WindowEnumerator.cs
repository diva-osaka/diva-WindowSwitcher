using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace WindowSwitcher;

public static partial class WindowEnumerator
{
    private static readonly Regex VsCodeTitlePattern = new(
        @"^(.+?) - (.+?) - Visual Studio Code$",
        RegexOptions.Compiled);

    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

    [LibraryImport("user32.dll")]
    private static partial int GetWindowTextLengthW(nint hWnd);

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(nint hWnd, StringBuilder lpClassName, int nMaxCount);

    public static List<WindowEntry> GetVsCodeWindows()
    {
        var codeProcessIds = new HashSet<uint>(
            Process.GetProcessesByName("Code").Select(p => (uint)p.Id));

        var windows = new List<WindowEntry>();

        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd))
                return true;

            GetWindowThreadProcessId(hWnd, out var processId);

            if (!codeProcessIds.Contains(processId))
                return true;

            var title = GetWindowTitle(hWnd);
            if (string.IsNullOrEmpty(title))
                return true;

            var match = VsCodeTitlePattern.Match(title);
            if (!match.Success)
                return true;

            var workspaceName = match.Groups[2].Value;
            var displayName = ShortenName(workspaceName);

            windows.Add(new WindowEntry
            {
                Handle = hWnd,
                FullTitle = title,
                WorkspaceName = workspaceName,
                DisplayName = displayName,
            });

            return true;
        }, nint.Zero);

        return windows;
    }

    public static List<WindowEntry> GetTerminalWindows()
    {
        var windows = new List<WindowEntry>();

        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd))
                return true;

            var className = GetWindowClassName(hWnd);
            if (className != "CASCADIA_HOSTING_WINDOW_CLASS")
                return true;

            var title = GetWindowTitle(hWnd);
            if (string.IsNullOrEmpty(title))
                return true;

            windows.Add(new WindowEntry
            {
                Handle = hWnd,
                FullTitle = title,
                WorkspaceName = title,
                DisplayName = title,
            });

            return true;
        }, nint.Zero);

        return windows;
    }

    private static string GetWindowTitle(nint hWnd)
    {
        var length = GetWindowTextLengthW(hWnd);
        if (length == 0) return "";

        var sb = new StringBuilder(length + 1);
        GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static string GetWindowClassName(nint hWnd)
    {
        var sb = new StringBuilder(256);
        GetClassName(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    // "devsys-BucketCounter" → "BucketCounter"
    public static string ShortenName(string name)
    {
        var dashIndex = name.IndexOf('-');
        return dashIndex >= 0 ? name[(dashIndex + 1)..] : name;
    }
}
