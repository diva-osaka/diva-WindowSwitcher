using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace WindowSwitcher;

public class GlobalHotkey : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_NOREPEAT = 0x4000;
    private const int WM_HOTKEY = 0x0312;

    private const uint VK_1 = 0x31;

    // ID ranges: 9000-9008 = VS Code (Ctrl+Alt), 9100-9108 = Terminal (Ctrl+Shift)
    private const int VSCODE_BASE_ID = 9000;
    private const int TERMINAL_BASE_ID = 9100;

    private readonly nint _hwnd;
    private readonly HwndSource _source;
    private readonly Action<int> _vsCodeCallback;
    private readonly Action<int> _terminalCallback;
    private readonly List<int> _registeredIds = [];

    public GlobalHotkey(nint windowHandle, Action<int> vsCodeCallback, Action<int> terminalCallback)
    {
        _hwnd = windowHandle;
        _vsCodeCallback = vsCodeCallback;
        _terminalCallback = terminalCallback;
        _source = HwndSource.FromHwnd(windowHandle)!;
        _source.AddHook(WndProc);

        for (int i = 0; i < 9; i++)
        {
            // Ctrl+Alt+1~9 for VS Code
            var vcId = VSCODE_BASE_ID + i;
            if (RegisterHotKey(_hwnd, vcId, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, VK_1 + (uint)i))
                _registeredIds.Add(vcId);

            // Ctrl+Shift+1~9 for Terminal
            var tmId = TERMINAL_BASE_ID + i;
            if (RegisterHotKey(_hwnd, tmId, MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, VK_1 + (uint)i))
                _registeredIds.Add(tmId);
        }
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (id >= VSCODE_BASE_ID && id < VSCODE_BASE_ID + 9)
            {
                _vsCodeCallback(id - VSCODE_BASE_ID);
                handled = true;
            }
            else if (id >= TERMINAL_BASE_ID && id < TERMINAL_BASE_ID + 9)
            {
                _terminalCallback(id - TERMINAL_BASE_ID);
                handled = true;
            }
        }
        return nint.Zero;
    }

    public void Dispose()
    {
        foreach (var id in _registeredIds)
            UnregisterHotKey(_hwnd, id);
        _source.RemoveHook(WndProc);
    }
}
