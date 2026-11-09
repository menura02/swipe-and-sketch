using Microsoft.Win32;
using TouchpadDraw.Core;

namespace TouchpadDraw;

internal sealed class TrayApplicationContext : ApplicationContext
{
    internal static uint ShowMessage => DashboardForm.ShowMessage;
    private readonly DashboardForm form;
    private readonly NotifyIcon tray;
    private readonly TargetWindowService target=new();
    private readonly MouseInputService mouse;
    private readonly KeyboardInputService keyboard;
    private readonly DrawingController controller;
    private readonly Watchdog watchdog;
    private readonly System.Windows.Forms.Timer timer=new(){Interval=40};
    private readonly Native.WinEventProc foregroundCallback;
    private readonly nint foregroundHook;
    private UserSettings settings=SettingsStore.Load();
    private readonly ToolStripMenuItem toggleItem=new("Aç / kapat");
    private readonly ToolStripMenuItem holdItem=new("Basılı tut");
    private bool exiting, updatePending;
    private string? shortcutError, lastDisplay;

    public TrayApplicationContext(Watchdog watchdog)
    {
        this.watchdog=watchdog;
        form=new(settings);_ = form.Handle;
        mouse=new(watchdog);
        controller=new(mouse,()=>target.CanDraw());controller.SetMode(settings.Mode);
        keyboard=new(controller,RefreshEnvironment,settings.DrawingKey);
        mouse.Moved+=p=>{if(controller.OwnsButton&&!target.CanDraw(p))controller.Stop();};
        mouse.PhysicalPressed+=controller.PhysicalPress;controller.Changed+=QueueUpdate;
        var menu=new ContextMenuStrip{BackColor=Color.White,ForeColor=Theme.Ink,Font=new("Segoe UI",10),ShowImageMargin=false};
        menu.Items.Add("Ayarları aç",null,(_,_)=>ShowSettings());
        menu.Items.Add("Etkinleştir / duraklat",null,(_,_)=>Toggle());menu.Items.Add(new ToolStripSeparator());
        toggleItem.Click+=(_,_)=>ChangeMode(DrawingMode.Toggle);holdItem.Click+=(_,_)=>ChangeMode(DrawingMode.Hold);
        menu.Items.AddRange([toggleItem,holdItem]);menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Çıkış",null,(_,_)=>ExitThread());
        tray=new(){Text="Touchpad: Hazır değil",Icon=BrandAssets.TrayIcon(SessionState.NotReady),ContextMenuStrip=menu,Visible=true};
        tray.DoubleClick+=(_,_)=>ShowSettings();
        form.FormClosing+=(_,e)=>{if(!exiting){e.Cancel=true;form.Hide();}};
        form.ShowRequested+=ShowSettings;form.Hotkey+=Toggle;form.ToggleSession+=Toggle;
        form.PickWindow+=PickWindow;form.PickArea+=PickArea;form.ModeSelected+=ChangeMode;form.DrawingKeySelected+=ChangeDrawingKey;
        form.ShortcutSelected+=key=>{if(RegisterShortcut(key)){settings=settings with{Shortcut=key};Save();form.SetKeyHint(settings.DrawingKey,key);}UpdateUi();};
        RegisterShortcut(settings.Shortcut);
        foregroundCallback=(_,_,_,_,_,_,_)=>RefreshEnvironment();
        foregroundHook=Native.SetWinEventHook(3,3,0,foregroundCallback,0,0,0);
        if(foregroundHook==0)controller.Fail("Pencere odağı dinleyicisi kurulamadı.");
        SystemEvents.SessionSwitch+=SessionChanged;SystemEvents.PowerModeChanged+=PowerChanged;
        timer.Tick+=(_,_)=>
        {
            RefreshEnvironment();
            if(!watchdog.Healthy)controller.Fail("Kurtarma bağlantısı kesildi. Uygulamayı yeniden başlatın.");
            if(controller.OwnsButton&&!controller.Enabled)controller.Stop();UpdateUi();
        };
        timer.Start();UpdateUi();form.Show();
    }
    private bool RegisterShortcut(Keys key)
    {
        Native.UnregisterHotKey(form.Handle,1);
        if(Native.RegisterHotKey(form.Handle,1,0x4000|1|2,(uint)key)){shortcutError=null;return true;}
        shortcutError=$"Ctrl+Alt+{key} kullanılıyor. Başka bir harf seçin.";
        if(key!=settings.Shortcut)Native.RegisterHotKey(form.Handle,1,0x4000|1|2,(uint)settings.Shortcut);
        controller.Pause();return false;
    }
    private void Save()
    {
        try{SettingsStore.Save(settings);}
        catch(Exception e)when(e is IOException or UnauthorizedAccessException){MessageBox.Show(form,"Tercihler kaydedilemedi: "+e.Message,"Kayıt hatası");}
    }
    private void ChangeMode(DrawingMode mode)
    {
        if(controller.Mode==mode)return;
        controller.SetMode(mode);settings=settings with{Mode=mode};form.SetMode(mode);Save();QueueUpdate();
    }
    private void ChangeDrawingKey(Keys key)
    {
        if(settings.DrawingKey==key)return;
        keyboard.SetDrawingKey(key);settings=settings with{DrawingKey=key};
        form.SetKeyHint(key,settings.Shortcut);Save();QueueUpdate();
    }
    private void PickWindow()
    {
        controller.Pause();using var picker=new WindowPicker();
        if(picker.ShowDialog(form)==DialogResult.OK&&picker.Selected is{}selected)target.Select(selected);
        RefreshEnvironment();UpdateUi();
    }
    private void PickArea()
    {
        controller.Pause();
        if(!target.Valid){MessageBox.Show(form,"Önce hedef pencereyi seçin.");return;}
        if(Native.IsIconic(target.Selection!.Handle))Native.ShowWindow(target.Selection.Handle,9);
        Native.SetForegroundWindow(target.Selection.Handle);form.Hide();
        using(var selector=new AreaSelector(target.ClientScreen))
            if(selector.ShowDialog()==DialogResult.OK&&target.Valid)target.SetArea(selector.SelectedArea);
        ShowSettings();RefreshEnvironment();UpdateUi();
    }
    private void Toggle()
    {
        RefreshEnvironment();if(controller.Enabled){controller.Pause();return;}
        var error=target.ActivationError();
        if(error is not null||!watchdog.Healthy||foregroundHook==0)
        {controller.Fail(error??"Giriş güvenliği hazır değil. Uygulamayı yeniden başlatın.");QueueUpdate();return;}
        controller.Enable();
        if(form.ContainsFocus){form.Hide();Native.SetForegroundWindow(target.Selection!.Handle);}
        RefreshEnvironment();
    }
    private void RefreshEnvironment()
    {
        target.Refresh();controller.SetEnvironment(target.Configured,target.Foreground);
        if(controller.OwnsButton&&!target.CanDraw())controller.Stop();
    }
    private void QueueUpdate()
    {
        if(updatePending||exiting||form.IsDisposed)return;updatePending=true;
        form.BeginInvoke(()=>{updatePending=false;if(!exiting)UpdateUi();});
    }
    private void UpdateUi()
    {
        var text=controller.State switch
        {
            SessionState.NotReady=>"Alanını seç, çizime başla.",SessionState.Paused=>controller.Enabled?"Hedef pencere bekleniyor":"Duraklatıldı",
            SessionState.Ready=>"Çizime hazırsın.",SessionState.Drawing=>"Çiziyorsun.",_=>"Bir kontrol gerekiyor."
        };
        var display=$"{text}|{target.Selection?.Title}|{target.Area}|{controller.Enabled}|{controller.Mode}|{settings.DrawingKey}|{controller.Error}|{target.Notice}|{shortcutError}";
        if(display==lastDisplay)return;lastDisplay=display;
        form.TargetLabel.Text=target.Selection?.Title??"Henüz pencere seçilmedi";
        form.AreaLabel.Text=target.Area is{}r?$"{r.Width} × {r.Height} px · Alan seçildi":"Tuval alanı seçilmedi";
        form.StateLabel.Text=text;
        form.DetailLabel.Text=controller.Error??target.Notice??shortcutError??(controller.Enabled
            ?$"{DrawingKeyOption.Label(settings.DrawingKey)} ile çiz · Esc ile durdur."
            :target.Configured?"Etkinleştir ve hedef uygulamada çizmeye başla.":"Önce hedef pencereyi ve tuval alanını belirle.");
        form.EnableButton.Text=controller.Enabled?"Oturumu duraklat":"Oturumu etkinleştir";
        form.EnableButton.Enabled=target.Configured||controller.Enabled||controller.OwnsButton;
        toggleItem.Checked=controller.Mode==DrawingMode.Toggle;holdItem.Checked=controller.Mode==DrawingMode.Hold;
        var shortText=controller.State switch{SessionState.Drawing=>"Çiziyor",SessionState.Ready=>"Hazır",SessionState.Error=>"Hata",_=>"Duraklatıldı"};
        tray.Text=$"Touchpad · {DrawingKeyOption.Label(settings.DrawingKey)} · {shortText}";
        tray.Icon=BrandAssets.TrayIcon(controller.State);
    }
    private void ShowSettings(){if(exiting)return;form.Show();form.WindowState=FormWindowState.Normal;form.Activate();}
    private void SessionChanged(object sender,SessionSwitchEventArgs e)
    {if(e.Reason is SessionSwitchReason.SessionLock or SessionSwitchReason.SessionLogoff or SessionSwitchReason.RemoteDisconnect or SessionSwitchReason.ConsoleDisconnect)SafePause();}
    private void PowerChanged(object sender,PowerModeChangedEventArgs e){if(e.Mode==PowerModes.Suspend)SafePause();}
    private void SafePause(){if(!exiting&&!form.IsDisposed)form.BeginInvoke(controller.Pause);}
    internal void EmergencyStop()=>controller.Pause();
    protected override void ExitThreadCore()
    {
        if(exiting)return;exiting=true;controller.Pause();timer.Stop();
        SystemEvents.SessionSwitch-=SessionChanged;SystemEvents.PowerModeChanged-=PowerChanged;
        Native.UnregisterHotKey(form.Handle,1);if(foregroundHook!=0)Native.UnhookWinEvent(foregroundHook);
        keyboard.Dispose();mouse.Dispose();tray.Visible=false;tray.Dispose();form.Close();base.ExitThreadCore();
    }
    protected override void Dispose(bool disposing)
    {if(disposing){ExitThreadCore();timer.Dispose();form.Dispose();}base.Dispose(disposing);}
}
