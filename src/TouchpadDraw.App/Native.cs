using System.Runtime.InteropServices;
using System.Text;

namespace TouchpadDraw;

internal static class Native
{
    internal const int WM_HOTKEY = 0x312, WM_KEYDOWN = 0x100, WM_KEYUP = 0x101,
        WM_SYSKEYDOWN = 0x104, WM_SYSKEYUP = 0x105, WM_MOUSEMOVE = 0x200,
        WM_LBUTTONDOWN = 0x201, WM_LBUTTONUP = 0x202;
    internal static readonly nuint InputTag = 0x54445044;
    internal delegate nint HookProc(int code, nint wParam, nint lParam);
    internal delegate void WinEventProc(nint hook, uint evt, nint window, int obj, int child, uint thread, uint time);
    internal delegate bool EnumProc(nint window, nint data);
    [StructLayout(LayoutKind.Sequential)] internal struct POINT { public int X, Y; public readonly Point Value => new(X, Y); }
    [StructLayout(LayoutKind.Sequential)] internal struct RECT { public int Left, Top, Right, Bottom; public readonly Size Size => new(Right-Left, Bottom-Top); }
    [StructLayout(LayoutKind.Sequential)] internal struct KBD { public uint Vk, Scan, Flags, Time; public nuint Extra; }
    [StructLayout(LayoutKind.Sequential)] internal struct MS { public POINT Pt; public uint Data, Flags, Time; public nuint Extra; }
    [StructLayout(LayoutKind.Sequential)] internal struct MOUSEINPUT { public int X, Y; public uint Data, Flags, Time; public nuint Extra; }
    // INPUT's union must include KEYBDINPUT/HARDWAREINPUT padding on both x86 and x64.
    [StructLayout(LayoutKind.Explicit)] internal struct INPUTUNION { [FieldOffset(0)] public MOUSEINPUT Mouse; }
    [StructLayout(LayoutKind.Sequential)] internal struct INPUT { public uint Type; public INPUTUNION Union; }
    [DllImport("user32.dll", SetLastError=true)] internal static extern nint SetWindowsHookEx(int id, HookProc proc, nint module, uint thread);
    [DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("user32.dll")] internal static extern nint CallNextHookEx(nint hook, int code, nint wParam, nint lParam);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode)] internal static extern nint GetModuleHandle(string? module);
    [DllImport("user32.dll", SetLastError=true)] internal static extern uint SendInput(uint count, INPUT[] inputs, int size);
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] internal static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern bool GetCursorPos(out POINT pt);
    [DllImport("user32.dll")] internal static extern nint WindowFromPoint(POINT pt);
    [DllImport("user32.dll")] internal static extern nint GetAncestor(nint window, uint flags);
    [DllImport("user32.dll")] internal static extern bool IsWindow(nint window);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] internal static extern bool IsIconic(nint window);
    [DllImport("user32.dll")] internal static extern bool GetClientRect(nint window, out RECT rect);
    [DllImport("user32.dll")] internal static extern bool ClientToScreen(nint window, ref POINT pt);
    [DllImport("user32.dll")] internal static extern uint GetDpiForWindow(nint window);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(nint window, out uint pid);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] internal static extern int GetWindowText(nint window, StringBuilder text, int length);
    [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumProc callback, nint data);
    [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] internal static extern bool ShowWindow(nint window, int command);
    [DllImport("user32.dll", SetLastError=true)] internal static extern bool RegisterHotKey(nint window, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(nint window, int id);
    [DllImport("user32.dll")] internal static extern uint RegisterWindowMessage(string text);
    [DllImport("user32.dll")] internal static extern bool PostMessage(nint window, uint message, nint wp, nint lp);
    [DllImport("user32.dll")] internal static extern nint SetWinEventHook(uint min, uint max, nint module, WinEventProc proc, uint pid, uint thread, uint flags);
    [DllImport("user32.dll")] internal static extern bool UnhookWinEvent(nint hook);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern nint OpenProcess(uint access, bool inherit, uint pid);
    [DllImport("advapi32.dll", SetLastError=true)] internal static extern bool OpenProcessToken(nint process, uint access, out nint token);
    [DllImport("advapi32.dll", SetLastError=true)] internal static extern bool GetTokenInformation(nint token, int kind, nint buffer, int size, out int needed);
    [DllImport("advapi32.dll")] internal static extern nint GetSidSubAuthorityCount(nint sid);
    [DllImport("advapi32.dll")] internal static extern nint GetSidSubAuthority(nint sid, uint index);
    [DllImport("kernel32.dll")] internal static extern bool CloseHandle(nint handle);
    internal static bool Down(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;
    internal static bool Modifiers => Down(0x10) || Down(0x11) || Down(0x12) || Down(0x5B) || Down(0x5C);
    internal static bool SendButton(bool down)
    {
        var input = new INPUT { Type = 0, Union = new INPUTUNION { Mouse = new MOUSEINPUT { Flags = down ? 2u : 4u, Extra = InputTag } } };
        return SendInput(1, [input], Marshal.SizeOf<INPUT>()) == 1;
    }
    internal static int? Integrity(uint pid)
    {
        var process = OpenProcess(0x1000, false, pid);
        if (process == 0) return null;
        nint token = 0, buffer = 0;
        try
        {
            if (!OpenProcessToken(process, 8, out token)) return null;
            GetTokenInformation(token, 25, 0, 0, out var needed);
            if (needed <= 0) return null;
            buffer = Marshal.AllocHGlobal(needed);
            if (!GetTokenInformation(token, 25, buffer, needed, out _)) return null;
            var sid = Marshal.ReadIntPtr(buffer);
            var count = Marshal.ReadByte(GetSidSubAuthorityCount(sid));
            return Marshal.ReadInt32(GetSidSubAuthority(sid, (uint)(count - 1)));
        }
        finally { if (buffer != 0) Marshal.FreeHGlobal(buffer); if (token != 0) CloseHandle(token); CloseHandle(process); }
    }
}
