using System.Drawing.Imaging;
using System.Text.Json;
using TouchpadDraw.Core;

namespace TouchpadDraw;

// Renders the real controls without installing hooks, reading user settings,
// taking desktop focus, or starting a watchdog. The preview window is transparent
// and placed off-screen while WinForms realizes its controls.
internal static class Preview
{
    internal static void Save(string path)
    {
        var legacy=JsonSerializer.Deserialize<UserSettings>("{\"Mode\":1,\"Shortcut\":68}")!;
        if(legacy.DrawingKey!=Keys.Space)throw new InvalidOperationException("Eski ayarlar uyumsuz.");
        var choice=new UserSettings(DrawingMode.Hold,Keys.D,Keys.F6);
        if(JsonSerializer.Deserialize<UserSettings>(JsonSerializer.Serialize(choice))!=choice)throw new InvalidOperationException("Tuş tercihi korunmadı.");
        foreach(var state in Enum.GetValues<SessionState>())
            if(BrandAssets.TrayIcon(state).Handle==0)throw new InvalidOperationException("Tepsi simgesi yüklenemedi.");
        using var form=new DashboardForm(choice,preview:true);
        form.TargetLabel.Text="Photoshop · Yeni çalışma";
        form.AreaLabel.Text="1280 × 800 px · Alan seçildi";
        form.StateLabel.Text="Çizime hazırsın.";
        form.DetailLabel.Text="F6 ile çiz · Esc ile durdur.";
        form.ShowInTaskbar=false;form.Opacity=0;form.StartPosition=FormStartPosition.Manual;
        form.Location=new Point(Screen.AllScreens.Max(x=>x.Bounds.Right)+100,0);
        form.Show();Application.DoEvents();form.PerformLayout();
        using var bitmap=new Bitmap(form.Width,form.Height);
        form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        bitmap.Save(path,ImageFormat.Png);
        File.WriteAllText(path+".json",JsonSerializer.Serialize(new{legacySettings="passed",customKeyRoundTrip="passed",icons="passed",drawingKeyChoices=DrawingKeyOption.All.Length,width=bitmap.Width,height=bitmap.Height}));
    }
}
