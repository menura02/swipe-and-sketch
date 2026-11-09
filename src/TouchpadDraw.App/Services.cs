using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using TouchpadDraw.Core;

namespace TouchpadDraw;

internal sealed record TargetWindowSelection(nint Handle, uint ProcessId, string Title);

internal sealed class TargetWindowService
{
    public TargetWindowSelection? Selection { get; private set; }
    public Rectangle? Area { get; private set; }
    private Size size;
    private uint dpi;
    public string? Notice { get; private set; }
    public static List<TargetWindowSelection> List()
    {
        var result = new List<TargetWindowSelection>();
        Native.EnumWindows((hwnd, _) =>
        {
            Native.GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == Environment.ProcessId || !Native.IsWindowVisible(hwnd)) return true;
            var title = new StringBuilder(512);
            Native.GetWindowText(hwnd, title, title.Capacity);
            if (title.Length > 0) result.Add(new(hwnd, pid, title.ToString()));
            return true;
        }, 0);
        return result.OrderBy(x => x.Title).ToList();
    }
    public void Select(TargetWindowSelection target) { Selection = target; Area = null; Notice = null; }
    public bool Valid
    {
        get
        {
            if (Selection is not { } s || !Native.IsWindow(s.Handle)) return false;
            Native.GetWindowThreadProcessId(s.Handle, out var pid);
            return pid == s.ProcessId;
        }
    }
    public bool Configured => Valid && Area is not null;
    public bool Foreground => Valid && Native.GetForegroundWindow() == Selection!.Handle &&
        Native.IsWindowVisible(Selection.Handle) && !Native.IsIconic(Selection.Handle);
    public Rectangle ClientScreen
    {
        get
        {
            if (!Valid || !Native.GetClientRect(Selection!.Handle, out var r)) return Rectangle.Empty;
            var pt = new Native.POINT();
            if (!Native.ClientToScreen(Selection.Handle, ref pt)) return Rectangle.Empty;
            return new Rectangle(pt.Value, r.Size);
        }
    }
    public void SetArea(Rectangle area)
    {
        Area = area;
        size = ClientScreen.Size;
        dpi = Native.GetDpiForWindow(Selection!.Handle);
        Notice = null;
    }
    public void Refresh()
    {
        if (!Valid) { if (Selection is not null) Notice = "Hedef pencere kapandı. Yeniden seçin."; Area = null; return; }
        if (Area is not null && (ClientScreen.Size != size || Native.GetDpiForWindow(Selection!.Handle) != dpi))
        { Area = null; Notice = "Pencere boyutu veya ekran ölçeği değişti. Çizim alanını yeniden seçin."; }
    }
    public bool CanDraw(Point? point = null)
    {
        Refresh();
        if (!Configured || !Foreground) return false;
        Native.POINT pt;
        if (point is { } p) pt = new() { X = p.X, Y = p.Y };
        else if (!Native.GetCursorPos(out pt)) return false;
        var client = ClientScreen;
        if (!Area!.Value.Contains(pt.X - client.X, pt.Y - client.Y)) return false;
        return Native.GetAncestor(Native.WindowFromPoint(pt), 2) == Selection!.Handle;
    }
    public string? ActivationError()
    {
        Refresh();
        if (!Configured) return "Önce hedef pencereyi ve çizim alanını seçin.";
        var target = Native.Integrity(Selection!.ProcessId);
        var own = Native.Integrity((uint)Environment.ProcessId);
        if (target is null || own is null) return "Hedef pencerenin yetkisi doğrulanamadı. Başka bir normal kullanıcı penceresi seçin.";
        return target > own ? "Hedef program daha yüksek yetkiyle çalışıyor. Programı normal kullanıcı olarak açın." : null;
    }
}

internal sealed class Watchdog : IDisposable
{
    private readonly NamedPipeServerStream pipe;
    private readonly Process child;
    private bool broken;
    public bool Healthy => !broken && pipe.IsConnected && !child.HasExited;
    public Watchdog()
    {
        var name = "TouchpadDraw-" + Guid.NewGuid().ToString("N");
        pipe = new(name, PipeDirection.Out, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden };
        if (string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase))
            start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "TouchpadDrawHelper.dll"));
        start.ArgumentList.Add("--watchdog");
        start.ArgumentList.Add(Environment.ProcessId.ToString());
        start.ArgumentList.Add(name);
        try
        {
            child = Process.Start(start) ?? throw new IOException("Kurtarma süreci başlatılamadı.");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            pipe.WaitForConnectionAsync(cts.Token).GetAwaiter().GetResult();
        }
        catch { pipe.Dispose(); throw; }
    }
    public bool Ownership(bool owns)
    {
        if (!Healthy) return false;
        try { pipe.WriteByte(owns ? (byte)1 : (byte)0); return true; }
        catch (IOException) { broken = true; return false; }
    }
    public void Dispose() { pipe.Dispose(); child.Dispose(); }
    internal static async Task Run(int parentId, string pipeName)
    {
        Process parent;
        try { parent = Process.GetProcessById(parentId); } catch (ArgumentException) { return; }
        using (parent)
        using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.In, PipeOptions.Asynchronous))
        {
            try
            {
                await client.ConnectAsync(5000);
                await RecoveryMonitor.RunAsync(client, parent.WaitForExitAsync(), async () =>
                {
                    for (var i = 0; i < 3; i++)
                    { if (Native.SendButton(false)) break; await Task.Delay(100); }
                });
            }
            catch (IOException) { }
            catch (TimeoutException) { }
        }
    }
}

internal sealed class MouseInputService : IMouseInput, IDisposable
{
    private readonly Watchdog watchdog;
    private readonly Native.HookProc callback;
    private readonly nint hook;
    public bool PhysicalLeftDown { get; private set; } = Native.Down(1);
    public event Action<Point>? Moved;
    public event Action? PhysicalPressed;
    public MouseInputService(Watchdog watchdog)
    {
        this.watchdog = watchdog;
        callback = Hook;
        hook = Native.SetWindowsHookEx(14, callback, Native.GetModuleHandle(null), 0);
        if (hook == 0) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Fare dinleyicisi kurulamadı.");
    }
    private nint Hook(int code, nint wp, nint lp)
    {
        if (code >= 0)
        {
            var data = Marshal.PtrToStructure<Native.MS>(lp);
            if (data.Extra != Native.InputTag)
            {
                if ((int)wp == Native.WM_MOUSEMOVE) Moved?.Invoke(data.Pt.Value);
                if ((data.Flags & 1) == 0)
                {
                    if ((int)wp == Native.WM_LBUTTONDOWN) { PhysicalLeftDown = true; PhysicalPressed?.Invoke(); }
                    if ((int)wp == Native.WM_LBUTTONUP) PhysicalLeftDown = false;
                }
            }
        }
        return Native.CallNextHookEx(hook, code, wp, lp);
    }
    public bool Press()
    {
        if (!watchdog.Ownership(true)) return false;
        if (Native.SendButton(true)) return true;
        watchdog.Ownership(false);
        return false;
    }
    public bool Release()
    {
        if (!Native.SendButton(false)) return false;
        watchdog.Ownership(false);
        return true;
    }
    public void Dispose() => Native.UnhookWindowsHookEx(hook);
}

internal sealed class KeyboardInputService : IDisposable
{
    private readonly DrawingController controller;
    private readonly Action refresh;
    private readonly Native.HookProc callback;
    private readonly nint hook;
    private readonly KeyboardTriggerRouter router;
    public KeyboardInputService(DrawingController controller, Action refresh, Keys drawingKey)
    {
        this.controller = controller;
        this.refresh = refresh;
        router = new(controller, (int)drawingKey, Native.Down((int)drawingKey));
        callback = Hook;
        hook = Native.SetWindowsHookEx(13, callback, Native.GetModuleHandle(null), 0);
        if (hook == 0) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Klavye dinleyicisi kurulamadı.");
    }
    private nint Hook(int code, nint wp, nint lp)
    {
        if (code >= 0)
        {
            var data = Marshal.PtrToStructure<Native.KBD>(lp);
            if (data.Extra != Native.InputTag)
            {
                var down = (int)wp is Native.WM_KEYDOWN or Native.WM_SYSKEYDOWN;
                if (router.Handle((int)data.Vk, down, static () => Native.Modifiers, refresh)) return 1;
                if (data.Vk == 0x1B && controller.Escape(down)) return 1;
            }
        }
        return Native.CallNextHookEx(hook, code, wp, lp);
    }
    public void SetDrawingKey(Keys key) => router.SetKey((int)key, Native.Down((int)key));
    public void Dispose() => Native.UnhookWindowsHookEx(hook);
}

internal sealed record UserSettings(DrawingMode Mode = DrawingMode.Toggle, Keys Shortcut = Keys.D, Keys DrawingKey = Keys.Space);
internal static class SettingsStore
{
    private static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TouchpadDrawHelper");
    public static UserSettings Load()
    {
        try
        {
            var s = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(Path.Combine(Folder, "settings.json")));
            if (s is not null && Enum.IsDefined(s.Mode) && s.Shortcut >= Keys.A && s.Shortcut <= Keys.Z)
                return DrawingKeyPolicy.IsAllowed((int)s.DrawingKey) ? s : s with { DrawingKey = Keys.Space };
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) { }
        return new();
    }
    public static void Save(UserSettings settings)
    {
        Directory.CreateDirectory(Folder);
        var path = Path.Combine(Folder, "settings.json");
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(settings));
        File.Move(path + ".tmp", path, true);
    }
}
