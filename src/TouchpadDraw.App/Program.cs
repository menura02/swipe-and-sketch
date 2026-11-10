namespace TouchpadDraw;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length == 3 && args[0] == "--watchdog" && int.TryParse(args[1], out var parent))
        { Watchdog.Run(parent, args[2]).GetAwaiter().GetResult(); return; }
        if (args.Length == 2 && args[0] == "--preview")
        {
            ApplicationConfiguration.Initialize();
            Preview.Save(args[1]);
            return;
        }
        using var single = new Mutex(true, @"Local\TouchpadDrawHelper.v1", out var first);
        if (!first)
        {
            Native.PostMessage((nint)0xFFFF, TrayApplicationContext.ShowMessage, 0, 0);
            return;
        }
        ApplicationConfiguration.Initialize();
        TrayApplicationContext? context = null;
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
        {
            context?.EmergencyStop();
            MessageBox.Show("Beklenmeyen hata nedeniyle çizim duraklatıldı.\n" + e.Exception.Message, "Swipe & Sketch");
        };
        AppDomain.CurrentDomain.UnhandledException += (_, _) => { context?.EmergencyStop(); };
        try
        {
            using var watchdog = new Watchdog();
            using (context = new TrayApplicationContext(watchdog)) Application.Run(context);
        }
        catch (Exception e)
        { context?.EmergencyStop(); MessageBox.Show("Uygulama başlatılamadı.\n" + e.Message, "Swipe & Sketch", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
