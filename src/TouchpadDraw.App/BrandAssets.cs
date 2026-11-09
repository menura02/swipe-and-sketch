using System.Drawing.Drawing2D;
using System.Reflection;
using TouchpadDraw.Core;

namespace TouchpadDraw;

internal static class BrandAssets
{
    private static readonly Dictionary<string, Icon> icons = [];
    internal static Icon AppIcon => LoadIcon("app");
    internal static Icon TrayIcon(SessionState state) => LoadIcon(state switch
    {
        SessionState.Drawing => "drawing", SessionState.Ready => "ready", SessionState.Error => "error", _ => "paused"
    });
    private static Icon LoadIcon(string name)
    {
        if (icons.TryGetValue(name, out var icon)) return icon;
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"TouchpadDraw.Assets.{name}.ico")
            ?? throw new InvalidOperationException("Uygulama simgesi bulunamadı.");
        using var source = new Icon(stream, name=="app" ? SystemInformation.IconSize : SystemInformation.SmallIconSize);
        return icons[name] = (Icon)source.Clone();
    }
    internal static GraphicsPath Rounded(RectangleF r, float radius)
    {
        var path = new GraphicsPath();
        var d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        path.AddArc(r.X, r.Y, d, d, 180, 90); path.AddArc(r.Right-d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right-d, r.Bottom-d, d, d, 0, 90); path.AddArc(r.X, r.Bottom-d, d, d, 90, 90); path.CloseFigure();
        return path;
    }
    internal static Bitmap RenderLogo(int size, Color? badge = null)
    {
        var image = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(image);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.ScaleTransform(size / 64f, size / 64f);
        using var background = new SolidBrush(Color.FromArgb(19, 57, 55));
        using var shape = Rounded(new RectangleF(3, 3, 58, 58), 16);
        g.FillPath(background, shape);
        using var outline = new Pen(Color.FromArgb(241, 250, 246), 3.5f);
        using var pad = Rounded(new RectangleF(13, 22, 38, 28), 7);
        g.DrawPath(outline, pad);
        using var accent = new Pen(Color.FromArgb(210, 244, 143), 6) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(accent, 27, 37, 44, 19);
        using var lime = new SolidBrush(Color.FromArgb(210, 244, 143));
        g.FillPolygon(lime, [new PointF(23, 42), new PointF(25, 33), new PointF(32, 39)]);
        using var line = new Pen(Color.FromArgb(241, 250, 246), 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(line, 25, 45, 37, 45);
        if (badge is { } color)
        {
            using var border = new SolidBrush(Color.White);
            using var dot = new SolidBrush(color);
            g.FillEllipse(border, 43, 43, 20, 20); g.FillEllipse(dot, 46, 46, 14, 14);
        }
        return image;
    }
}
