using System.Drawing.Drawing2D;

namespace TouchpadDraw;

internal static class Theme
{
    internal static readonly Color Canvas = Color.FromArgb(242, 245, 244);
    internal static readonly Color Ink = Color.FromArgb(24, 49, 48);
    internal static readonly Color Muted = Color.FromArgb(104, 123, 119);
    internal static readonly Color Accent = Color.FromArgb(27, 118, 104);
    internal static readonly Color Lime = Color.FromArgb(210, 244, 143);
    internal static Color Background(Control control)
    {
        for(var parent=control.Parent;parent is not null;parent=parent.Parent)
            if(parent.BackColor.A==255)return parent.BackColor;
        return Canvas;
    }
    internal static Label Text(string text, float size = 10, bool bold = false, Color? color = null) => new()
    {
        Text = text, AutoSize = true, Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
        ForeColor = color ?? Ink, Margin = new Padding(0, 0, 0, 8), UseMnemonic = false
    };
    internal static void Combo(ComboBox combo)
    {
        combo.FlatStyle = FlatStyle.Flat; combo.BackColor = Color.FromArgb(242, 246, 244);
        combo.ForeColor = Ink; combo.Font = new Font("Segoe UI", 11); combo.DropDownHeight = 340;
        combo.IntegralHeight = false; combo.Margin = new Padding(0, 0, 0, 8);
    }
}

internal sealed class RoundedCard : Panel
{
    public RoundedCard() { DoubleBuffered = true; BackColor = Color.White; Padding = new(24); }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = BrandAssets.Rounded(new RectangleF(.5f, .5f, Width-1, Height-1), 18);
        using var fill = new SolidBrush(BackColor); using var border = new Pen(Color.FromArgb(223, 232, 227));
        e.Graphics.Clear(Parent?.BackColor ?? Theme.Canvas); e.Graphics.FillPath(fill, path); e.Graphics.DrawPath(border, path);
    }
}

internal class ModernButton : Button
{
    [System.ComponentModel.DefaultValue(false)]
    public bool Primary { get; set; }
    private bool hover;
    public ModernButton()
    {
        FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand; Height = 42; Font = new Font("Segoe UI", 10, FontStyle.Bold);
        Margin = new(0, 4, 0, 8); UseVisualStyleBackColor = false;
    }
    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Theme.Background(this));
        using var shape = BrandAssets.Rounded(new RectangleF(.5f, .5f, Width-1, Height-1), 11);
        var fill = !Enabled ? Color.FromArgb(226, 233, 230) : Primary
            ? hover ? Color.FromArgb(18, 94, 83) : Theme.Accent
            : hover ? Color.FromArgb(224, 237, 229) : Color.FromArgb(237, 244, 240);
        using var brush = new SolidBrush(fill); e.Graphics.FillPath(brush, shape);
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Enabled ? Primary ? Color.White : Theme.Ink : Theme.Muted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        if (Focused) { using var pen = new Pen(Theme.Accent) { DashStyle = DashStyle.Dot }; e.Graphics.DrawRectangle(pen, 5, 5, Width-11, Height-11); }
    }
}

internal sealed class ModeOption : ModernButton
{
    [System.ComponentModel.DefaultValue("")]
    public string Detail { get; set; } = "";
    [System.ComponentModel.DefaultValue(false)]
    public bool Selected { get; set; }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Theme.Background(this));
        using var shape = BrandAssets.Rounded(new RectangleF(.5f,.5f,Width-1,Height-1),12);
        using var brush = new SolidBrush(Selected ? Color.FromArgb(232, 243, 235) : Color.FromArgb(247, 249, 248));
        using var pen = new Pen(Selected ? Theme.Accent : Color.FromArgb(224, 232, 227), Selected ? 1.5f : 1);
        e.Graphics.FillPath(brush,shape); e.Graphics.DrawPath(pen,shape);
        var scale = DeviceDpi/96f;
        var circle = new Rectangle((int)(16*scale),(int)(18*scale),(int)(15*scale),(int)(15*scale));
        e.Graphics.DrawEllipse(pen,circle);
        if(Selected) { circle.Inflate(-(int)(4*scale),-(int)(4*scale)); using var dot=new SolidBrush(Theme.Accent);e.Graphics.FillEllipse(dot,circle); }
        var x=(int)(44*scale);
        TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(x,(int)(12*scale),Width-x-12,(int)(25*scale)),Theme.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);
        using var small=new Font("Segoe UI",9);
        TextRenderer.DrawText(e.Graphics,Detail,small,new Rectangle(x,(int)(38*scale),Width-x-12,(int)(25*scale)),Theme.Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);
        if(Focused) { using var focus=new Pen(Theme.Accent){DashStyle=DashStyle.Dot};e.Graphics.DrawRectangle(focus,5,5,Width-11,Height-11); }
    }
}
