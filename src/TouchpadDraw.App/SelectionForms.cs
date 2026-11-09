namespace TouchpadDraw;

internal sealed class WindowPicker : Form
{
    private readonly ListBox list = new() { Dock = DockStyle.Fill, DisplayMember = "Title", IntegralHeight = false };
    public TargetWindowSelection? Selected => list.SelectedItem as TargetWindowSelection;
    public WindowPicker()
    {
        Text = "Hedef pencereyi seç";
        ClientSize = new(600, 350);
        StartPosition = FormStartPosition.CenterParent;
        Font = new("Segoe UI", 10);
        BackColor = Theme.Canvas; ForeColor = Theme.Ink; Icon = BrandAssets.AppIcon;
        list.BackColor = Color.White; list.ForeColor = Theme.Ink; list.BorderStyle = BorderStyle.None;
        MinimizeBox = MaximizeBox = false;
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft, Padding = new(6) };
        var choose = new ModernButton { Text = "Seç", DialogResult = DialogResult.OK, Width = 96, Primary = true };
        var cancel = new ModernButton { Text = "İptal", DialogResult = DialogResult.Cancel, Width = 96 };
        var refresh = new ModernButton { Text = "Yenile", Width = 96 };
        refresh.Click += (_, _) => Reload();
        buttons.Controls.AddRange([choose, cancel, refresh]);
        Controls.Add(list); Controls.Add(buttons);
        AcceptButton = choose; CancelButton = cancel;
        list.DoubleClick += (_, _) => { if (Selected is not null) { DialogResult = DialogResult.OK; Close(); } };
        Reload();
    }
    private void Reload() => list.DataSource = TargetWindowService.List();
}

internal sealed class AreaSelector : Form
{
    private Point start;
    private bool dragging;
    private Rectangle selection;
    public Rectangle SelectedArea => selection;
    public AreaSelector(Rectangle screenBounds)
    {
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = screenBounds;
        BackColor = Color.FromArgb(20, 35, 55);
        Opacity = .45;
        TopMost = true;
        ShowInTaskbar = false;
        Cursor = Cursors.Cross;
        DoubleBuffered = true;
        KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); } };
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        start = e.Location; dragging = true; Capture = true;
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!dragging) return;
        var p = new Point(Math.Clamp(e.X, 0, ClientSize.Width), Math.Clamp(e.Y, 0, ClientSize.Height));
        selection = Rectangle.FromLTRB(Math.Min(start.X, p.X), Math.Min(start.Y, p.Y), Math.Max(start.X, p.X), Math.Max(start.Y, p.Y));
        Invalidate();
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!dragging || e.Button != MouseButtons.Left) return;
        dragging = false; Capture = false;
        if (selection.Width < 20 || selection.Height < 20) { selection = Rectangle.Empty; Invalidate(); return; }
        DialogResult = DialogResult.OK; Close();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var font = new Font("Segoe UI", 14, FontStyle.Bold);
        e.Graphics.DrawString("Tuval alanını sürükleyerek seçin • Esc: İptal", font, Brushes.White, new PointF(18, 18));
        if (selection.IsEmpty) return;
        using var pen = new Pen(Color.Lime, 4);
        e.Graphics.DrawRectangle(pen, selection);
    }
}
