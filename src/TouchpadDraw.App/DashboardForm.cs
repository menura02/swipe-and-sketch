using TouchpadDraw.Core;

namespace TouchpadDraw;

internal sealed record DrawingKeyOption(Keys Key, string Name)
{
    public override string ToString() => Name;
    internal static string Label(Keys key) => key switch
    {
        Keys.Space => "Space", Keys.Return => "Enter", Keys.Back => "Backspace", Keys.Prior => "Page Up",
        Keys.Next => "Page Down", Keys.Left => "← Sol", Keys.Up => "↑ Yukarı", Keys.Right => "→ Sağ", Keys.Down => "↓ Aşağı",
        >= Keys.D0 and <= Keys.D9 => ((int)key-(int)Keys.D0).ToString(), _ => key.ToString()
    };
    internal static DrawingKeyOption[] All => Enumerable.Range(1,255).Where(DrawingKeyPolicy.IsAllowed)
        .OrderBy(k => k==32 ? 0 : k>=0x70 ? 1 : k>=0x41 && k<=0x5A ? 2 : 3)
        .ThenBy(k=>k).Select(k=>new DrawingKeyOption((Keys)k,Label((Keys)k))).ToArray();
}

internal sealed class DashboardForm : Form
{
    internal static readonly uint ShowMessage = Native.RegisterWindowMessage("TouchpadDrawHelper.ShowSettings.v1");
    internal event Action? ShowRequested, Hotkey, PickWindow, PickArea, ToggleSession;
    internal event Action<DrawingMode>? ModeSelected;
    internal event Action<Keys>? DrawingKeySelected, ShortcutSelected;
    internal readonly Label TargetLabel = Theme.Text("Henüz pencere seçilmedi",11,true);
    internal readonly Label AreaLabel = Theme.Text("Tuval alanı seçilmedi",10,false,Theme.Muted);
    internal readonly Label StateLabel = Theme.Text("Hazır değil",16,true,Color.White);
    internal readonly Label DetailLabel = Theme.Text("Başlamak için pencere ve tuval alanını seç.",9,false,Color.FromArgb(183,207,198));
    internal readonly ModernButton EnableButton = new() { Text="Oturumu etkinleştir",Primary=true,Dock=DockStyle.Fill };
    private readonly ComboBox drawingKeyBox=new(){DropDownStyle=ComboBoxStyle.DropDownList};
    private readonly ComboBox shortcutBox=new(){DropDownStyle=ComboBoxStyle.DropDownList};
    private readonly ModeOption toggleMode=new(){Text="Aç / kapat",Detail="Bir basışla başla, bir basışla bitir.",Height=78};
    private readonly ModeOption holdMode=new(){Text="Basılı tut",Detail="Tuş basılıyken çiz, bırakınca bitir.",Height=78};
    private readonly Label footer=Theme.Text("",9,false,Theme.Muted);
    private readonly PictureBox logo=new(){SizeMode=PictureBoxSizeMode.Zoom};
    private readonly bool preview;

    internal DashboardForm(UserSettings settings, bool preview = false)
    {
        this.preview=preview;
        SuspendLayout();
        Font=new("Segoe UI",10); AutoScaleDimensions=new(96,96); AutoScaleMode=AutoScaleMode.Dpi;
        Text="Touchpad Çizim Yardımcısı"; BackColor=Theme.Canvas; ForeColor=Theme.Ink;
        ClientSize=new(920,760); MinimumSize=new(880,740); StartPosition=FormStartPosition.CenterScreen;
        Icon=BrandAssets.AppIcon;
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new(28,20,28,12),ColumnCount=1,RowCount=4,BackColor=Theme.Canvas};
        root.RowStyles.Add(new(SizeType.Absolute,130));root.RowStyles.Add(new(SizeType.Percent,100));
        root.RowStyles.Add(new(SizeType.Absolute,112));root.RowStyles.Add(new(SizeType.Absolute,38));
        Controls.Add(root);

        var header=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=new(0)};
        header.RowStyles.Add(new(SizeType.Percent,100));
        header.ColumnStyles.Add(new(SizeType.Percent,100));header.ColumnStyles.Add(new(SizeType.Absolute,100));
        var heading=Flow();heading.Controls.Add(Theme.Text("TOUCHPAD  /  ÇİZİM YARDIMCISI",9,true,Theme.Accent));
        heading.Controls.Add(Theme.Text("Çizime odaklan.",29,true));
        heading.Controls.Add(Theme.Text("Sevdiğin uygulamada, parmağını özgürce hareket ettir.",10,false,Theme.Muted));
        logo.Image=BrandAssets.RenderLogo(256);logo.Dock=DockStyle.Fill;logo.Margin=new(12,0,0,20);
        header.Controls.Add(heading,0,0);header.Controls.Add(logo,1,0);root.Controls.Add(header,0,0);

        var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=new(0)};
        grid.RowStyles.Add(new(SizeType.Percent,100));
        grid.ColumnStyles.Add(new(SizeType.Percent,45));grid.ColumnStyles.Add(new(SizeType.Percent,55));
        root.Controls.Add(grid,0,1);
        var leftCard=new RoundedCard{Dock=DockStyle.Fill,Margin=new(0,0,16,16)};
        var rightCard=new RoundedCard{Dock=DockStyle.Fill,Margin=new(0,0,0,16)};
        grid.Controls.Add(leftCard,0,0);grid.Controls.Add(rightCard,1,0);
        var left=Flow();leftCard.Controls.Add(left);
        left.Controls.Add(Theme.Text("01  /  ÇALIŞMA ALANI",9,true,Theme.Muted));
        left.Controls.Add(Theme.Text("Nerede çizeceksin?",17,true));
        var intro=Theme.Text("Fırçanı hedef uygulamada seç.\nArdından pencereyi ve tuvali belirle.",10,false,Theme.Muted);
        left.Controls.Add(intro);
        var targetCaption=Theme.Text("HEDEF PENCERE",8,true,Theme.Muted);targetCaption.Margin=new(0,22,0,6);left.Controls.Add(targetCaption);
        left.Controls.Add(TargetLabel);
        var pick=new ModernButton{Text="Pencere seç"};pick.Click+=(_,_)=>PickWindow?.Invoke();left.Controls.Add(pick);
        var areaCaption=Theme.Text("TUVAL",8,true,Theme.Muted);areaCaption.Margin=new(0,18,0,6);left.Controls.Add(areaCaption);
        left.Controls.Add(AreaLabel);
        var area=new ModernButton{Text="Çizim alanını seç"};area.Click+=(_,_)=>PickArea?.Invoke();left.Controls.Add(area);
        var tip=Theme.Text("Menüleri alanın dışında tut.",9,false,Theme.Muted);tip.Margin=new(0,10,0,0);left.Controls.Add(tip);

        var right=Flow();rightCard.Controls.Add(right);
        right.Controls.Add(Theme.Text("02  /  KONTROLLER",9,true,Theme.Muted));
        right.Controls.Add(Theme.Text("Kendi ritmini seç.",17,true));
        var keyCaption=Theme.Text("Çizim tuşu",9,true);keyCaption.Margin=new(0,8,0,6);right.Controls.Add(keyCaption);
        Theme.Combo(drawingKeyBox);drawingKeyBox.Items.AddRange(DrawingKeyOption.All);
        drawingKeyBox.SelectedItem=drawingKeyBox.Items.Cast<DrawingKeyOption>().First(x=>x.Key==settings.DrawingKey);
        drawingKeyBox.SelectedIndexChanged+=(_,_)=>{if(drawingKeyBox.SelectedItem is DrawingKeyOption k)DrawingKeySelected?.Invoke(k.Key);};right.Controls.Add(drawingKeyBox);
        toggleMode.Click+=(_,_)=>ModeSelected?.Invoke(DrawingMode.Toggle);holdMode.Click+=(_,_)=>ModeSelected?.Invoke(DrawingMode.Hold);
        right.Controls.Add(toggleMode);right.Controls.Add(holdMode);
        var pauseCaption=Theme.Text("Duraklat / devam et",9,true);pauseCaption.Margin=new(0,8,0,6);right.Controls.Add(pauseCaption);
        var shortcut=new FlowLayoutPanel{Height=42,Margin=new(0),WrapContents=false};
        shortcut.Controls.Add(new Label{Text="Ctrl + Alt +",AutoSize=true,Margin=new(0,8,10,0),ForeColor=Theme.Muted});
        Theme.Combo(shortcutBox);shortcutBox.Width=66;shortcutBox.Items.AddRange(Enumerable.Range('A',26).Select(x=>((char)x).ToString()).ToArray());
        shortcutBox.SelectedIndex=(int)settings.Shortcut-(int)Keys.A;shortcut.Controls.Add(shortcutBox);
        var save=new ModernButton{Text="Kaydet",Width=90,Height=32,Margin=new(10,0,0,0)};
        save.Click+=(_,_)=>ShortcutSelected?.Invoke((Keys)((int)Keys.A+shortcutBox.SelectedIndex));shortcut.Controls.Add(save);right.Controls.Add(shortcut);

        // Flexible widths keep the cards usable after resizing and DPI changes.
        Fit(left,TargetLabel,intro,tip,pick,area);
        Fit(right,drawingKeyBox,toggleMode,holdMode,shortcut);
        var status=new RoundedCard{Dock=DockStyle.Fill,BackColor=Theme.Ink,Padding=new(24,16,24,16),Margin=new(0,0,0,8)};
        var statusGrid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=new(0)};
        statusGrid.RowStyles.Add(new(SizeType.Percent,100));
        statusGrid.ColumnStyles.Add(new(SizeType.Percent,100));statusGrid.ColumnStyles.Add(new(SizeType.Absolute,208));
        var statusText=Flow();StateLabel.Margin=new(0,0,0,4);statusText.Controls.Add(StateLabel);statusText.Controls.Add(DetailLabel);
        statusText.SizeChanged+=(_,_)=>DetailLabel.MaximumSize=new(Math.Max(100,statusText.ClientSize.Width-12),0);
        EnableButton.Click+=(_,_)=>ToggleSession?.Invoke();EnableButton.Margin=new(16,10,0,10);
        statusGrid.Controls.Add(statusText,0,0);statusGrid.Controls.Add(EnableButton,1,0);status.Controls.Add(statusGrid);root.Controls.Add(status,0,2);
        footer.Dock=DockStyle.Fill;footer.TextAlign=ContentAlignment.MiddleLeft;footer.AutoSize=false;footer.Margin=new(0);root.Controls.Add(footer,0,3);
        SetMode(settings.Mode);SetKeyHint(settings.DrawingKey,settings.Shortcut);
        ResumeLayout(true);
    }
    private static FlowLayoutPanel Flow() => new(){Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Margin=new(0),BackColor=Color.Transparent};
    private static void Fit(FlowLayoutPanel panel, params Control[] controls)
    {
        void ResizeControls()
        {
            var width=Math.Max(100,panel.ClientSize.Width-8-SystemInformation.VerticalScrollBarWidth);
            foreach(var control in controls)
            {
                if(control is Label label)label.MaximumSize=new(width,0);
                else control.Width=width;
            }
        }
        panel.SizeChanged+=(_,_)=>ResizeControls();ResizeControls();
    }
    internal void SetMode(DrawingMode mode)
    {
        toggleMode.Selected=mode==DrawingMode.Toggle;holdMode.Selected=mode==DrawingMode.Hold;
        toggleMode.AccessibleName="Aç / kapat"+(toggleMode.Selected?" — seçili":"");
        holdMode.AccessibleName="Basılı tut"+(holdMode.Selected?" — seçili":"");toggleMode.Invalidate();holdMode.Invalidate();
    }
    internal void SetKeyHint(Keys drawingKey, Keys shortcut)
    {
        footer.Text=$"{DrawingKeyOption.Label(drawingKey)}  Çizim   ·   Ctrl + Alt + {shortcut}  Duraklat   ·   Esc  Durdur   ·   v1.1.0";
    }
    protected override void WndProc(ref Message m)
    {
        if(m.Msg==ShowMessage)ShowRequested?.Invoke();if(m.Msg==Native.WM_HOTKEY&&m.WParam==1)Hotkey?.Invoke();base.WndProc(ref m);
    }
    protected override bool ShowWithoutActivation => preview;
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        var work=Screen.FromControl(this).WorkingArea;
        var limit=new Size(Math.Max(400,work.Width-32),Math.Max(400,work.Height-32));
        MinimumSize=new(Math.Min(MinimumSize.Width,limit.Width),Math.Min(MinimumSize.Height,limit.Height));
        Size=new(Math.Min(Width,limit.Width),Math.Min(Height,limit.Height));
    }
    protected override void Dispose(bool disposing) { if(disposing)logo.Image?.Dispose();base.Dispose(disposing); }
}
