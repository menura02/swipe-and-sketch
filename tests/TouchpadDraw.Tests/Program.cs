using TouchpadDraw.Core;

var passed = 0;
var failed = 0;
void Run(string name, Action test)
{
    try { test(); Console.WriteLine("PASS " + name); passed++; }
    catch (Exception e) { Console.WriteLine("FAIL " + name + ": " + e.Message); failed++; }
}
void Check(bool condition, string message = "Assertion failed") { if (!condition) throw new Exception(message); }
(DrawingController c, FakeMouse m, EnvironmentState env) Create(DrawingMode mode = DrawingMode.Toggle)
{
    var mouse = new FakeMouse(); var env = new EnvironmentState();
    var c = new DrawingController(mouse, () => env.CanStart);
    c.SetMode(mode); c.SetEnvironment(true, true); c.Enable();
    return (c, mouse, env);
}
void Tap(DrawingController c) { c.Trigger(true, false); c.Trigger(false, false); }

Run("Toggle: down starts, up keeps drawing, second down stops", () =>
{
    var (c,m,_) = Create(); Check(c.Trigger(true,false)); Check(c.State == SessionState.Drawing);
    Check(c.Trigger(false,false)); Check(c.OwnsButton); Check(c.Trigger(true,false)); Check(!c.OwnsButton);
    Check(c.Trigger(false,false)); Check(m.Presses == 1 && m.Releases == 1);
});
Run("Hold: only down-to-up interval draws", () =>
{
    var(c,m,_) = Create(DrawingMode.Hold); Check(c.Trigger(true,false)); Check(c.OwnsButton);
    Check(c.Trigger(false,false)); Check(!c.OwnsButton); Check(m.Presses == 1 && m.Releases == 1);
});
foreach (var mode in Enum.GetValues<DrawingMode>())
{
    Run(mode + ": auto-repeat does not retoggle", () =>
    {
        var(c,m,_) = Create(mode); c.Trigger(true,false);
        for(var i=0;i<100;i++) Check(c.Trigger(true,false));
        Check(m.Presses==1 && m.Releases==0); c.Trigger(false,false); c.Pause(); Check(m.Releases==1);
    });
    Run(mode + ": pause releases and consumes matching up", () =>
    {
        var(c,m,_) = Create(mode); c.Trigger(true,false); c.Pause();
        Check(!c.Enabled && !c.OwnsButton); Check(c.Trigger(false,false)); Check(m.Releases==1);
        Check(!c.Trigger(true,false)); Check(!c.Trigger(false,false));
    });
    Run(mode + ": focus loss preserves captured key pair", () =>
    {
        var(c,m,_) = Create(mode); c.Trigger(true,false); c.SetEnvironment(true,false);
        Check(!c.OwnsButton); Check(c.Trigger(true,false)); Check(c.Trigger(false,false)); Check(m.Releases==1);
    });
    Run(mode + ": re-entering drawing area while held never restarts", () =>
    {
        var(c,m,env) = Create(mode); c.Trigger(true,false); env.CanStart=false; c.Stop();
        env.CanStart=true; Check(c.Trigger(true,false)); Check(!c.OwnsButton); c.Trigger(false,false);
        c.Trigger(true,false); Check(c.OwnsButton && m.Presses==2);
    });
    Run(mode + ": physical mouse press pauses synthetic ownership", () =>
    {
        var(c,m,_) = Create(mode); Tap(c);
        if(mode==DrawingMode.Hold)c.Trigger(true,false);
        m.PhysicalLeftDown=true; c.PhysicalPress(); Check(!c.Enabled && !c.OwnsButton);
    });
}
Run("Modified Space passes through", () =>
{
    var(c,m,_) = Create(); Check(!c.Trigger(true,true)); Check(!c.Trigger(true,false)); Check(!c.Trigger(false,false)); Check(m.Presses==0);
});
Run("Hold: repeat flood performs native validation only on fresh down", () =>
{
    var(c,m,_) = Create(DrawingMode.Hold);
    var refreshes=0; var modifierReads=0;
    bool ReadModifiers() { modifierReads++; return false; }
    void Refresh() { refreshes++; }
    Check(c.Trigger(true,ReadModifiers,Refresh));
    for(var i=0;i<10000;i++) Check(c.Trigger(true,ReadModifiers,Refresh));
    Check(refreshes==1 && modifierReads==1 && m.Presses==1 && m.Releases==0);
    Check(c.Trigger(false,ReadModifiers,Refresh));
    Check(refreshes==1 && modifierReads==1 && m.Releases==1);
    Check(c.Trigger(true,ReadModifiers,Refresh));
    Check(refreshes==2 && modifierReads==2 && m.Presses==2);
});
Run("Fresh press validation can cancel stale target before mouse injection", () =>
{
    var(c,m,_) = Create(DrawingMode.Hold);
    Check(!c.Trigger(true,()=>false,()=>c.SetEnvironment(true,false)));
    Check(m.Presses==0 && !c.OwnsButton);
    Check(!c.Trigger(false,()=>throw new Exception("Release must not query modifiers"),()=>throw new Exception("Release must not refresh")));
});
Run("Captured release bypasses slow environment and modifier readers", () =>
{
    var(c,m,_) = Create(DrawingMode.Hold);
    Check(c.Trigger(true,()=>false,()=>{}));
    Check(c.Trigger(false,()=>throw new Exception("Unexpected modifier read"),()=>throw new Exception("Unexpected refresh")));
    Check(!c.OwnsButton && m.Releases==1);
});
Run("Uncaptured Space up remains uncaptured after focus enters target", () =>
{
    var(c,m,_) = Create(); c.SetEnvironment(true,false); Check(!c.Trigger(true,false));
    c.SetEnvironment(true,true); Check(!c.Trigger(true,false)); Check(!c.Trigger(false,false)); Check(m.Presses==0); Tap(c); Check(m.Presses==1);
});
Run("Space held at startup requires release and fresh press", () =>
{
    var(c,m,_) = Create(); c.InitializeTrigger(true); Check(!c.Trigger(true,false)); Check(!c.Trigger(false,false));
    Check(m.Presses==0); Tap(c); Check(m.Presses==1);
});
Run("Enabling while an uncaptured Space is held never starts drawing", () =>
{
    var(c,m,_) = Create(); c.Pause(); Check(!c.Trigger(true,false)); c.Enable();
    Check(!c.Trigger(true,false)); Check(m.Presses==0); Check(!c.Trigger(false,false)); Tap(c); Check(m.Presses==1);
});
Run("Physical button already held prevents synthetic press", () =>
{
    var(c,m,_) = Create(); m.PhysicalLeftDown=true; Tap(c); Check(m.Presses==0); c.Pause(); Check(m.Releases==0);
});
Run("Space outside selected area is suppressed without drawing", () =>
{
    var(c,m,env) = Create(); env.CanStart=false; Check(c.Trigger(true,false)); Check(c.Trigger(false,false)); Check(m.Presses==0);
});
Run("Failed down enters Error without owning button", () =>
{
    var(c,m,_) = Create(); m.PressSucceeds=false; Tap(c);
    Check(c.State==SessionState.Error && !c.OwnsButton && !c.Enabled); Check(m.Releases==0);
    m.PressSucceeds=true; c.Enable(); Tap(c); Check(c.OwnsButton);
});
Run("Failed up retains ownership and retries", () =>
{
    var(c,m,_) = Create(); Tap(c); m.ReleaseSucceeds=false; c.Pause();
    Check(c.OwnsButton && c.State==SessionState.Error); c.Enable(); Check(!c.Enabled);
    m.ReleaseSucceeds=true; c.Stop(); Check(!c.OwnsButton); c.Enable(); Check(c.State==SessionState.Ready);
});
Run("Mode change releases and pauses", () =>
{
    var(c,m,_) = Create(); Tap(c); c.SetMode(DrawingMode.Hold);
    Check(!c.OwnsButton && !c.Enabled && c.Mode==DrawingMode.Hold && m.Releases==1);
});
Run("Escape retries an owned button even after a release failure", () =>
{
    var(c,m,_) = Create(); Tap(c); m.ReleaseSucceeds=false; c.Pause();
    m.ReleaseSucceeds=true; Check(c.Escape(true)); Check(!c.OwnsButton); Check(c.Escape(false));
});
Run("Invalid target releases, disarms, and requires reconfiguration", () =>
{
    var(c,m,_) = Create(); Tap(c); c.SetEnvironment(false,false); c.Enable();
    Check(c.State==SessionState.NotReady && !c.Enabled && m.Releases==1);
});
Run("Escape consumes complete key sequence and pauses", () =>
{
    var(c,m,_) = Create(); Tap(c); Check(c.Escape(true)); Check(c.Escape(true)); Check(c.Escape(false));
    Check(!c.Enabled && !c.OwnsButton && m.Releases==1); Check(!c.Escape(true)); Check(!c.Escape(false));
});
Run("Cleanup is idempotent and does not release unowned input", () =>
{
    var(c,m,_) = Create(); c.Stop(); c.Pause(); Check(m.Releases==0);
    c.Enable(); Tap(c); c.Pause(); c.Stop(); c.Pause(); Check(m.Releases==1);
});
Run("Fault while drawing releases and disables", () =>
{
    var(c,m,_) = Create(); Tap(c); c.Fail("test"); Check(!c.OwnsButton && !c.Enabled && c.State==SessionState.Error && m.Releases==1);
});
Run("Recovery: no ownership means no release", () =>
{
    var releases=0;
    RecoveryMonitor.RunAsync(new MemoryStream([]),Task.CompletedTask,()=>{releases++;return Task.CompletedTask;}).GetAwaiter().GetResult();
    Check(releases==0);
});
Run("Recovery: last ownership state wins", () =>
{
    foreach(var (bytes,expected) in new (byte[],int)[] { (new byte[]{1},1), (new byte[]{1,0},0), (new byte[]{1,0,1},1), (new byte[]{0,0},0), (new byte[]{1,99},1) })
    {
        var releases=0;
        RecoveryMonitor.RunAsync(new MemoryStream(bytes),Task.CompletedTask,()=>{releases++;return Task.CompletedTask;}).GetAwaiter().GetResult();
        Check(releases==expected);
    }
});
Run("Recovery: disconnected pipe cannot release a live parent", () =>
{
    var releases=0; var exited=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var recovery=RecoveryMonitor.RunAsync(new MemoryStream(new byte[]{1}),exited.Task,()=>{Interlocked.Increment(ref releases);return Task.CompletedTask;});
    Check(!recovery.IsCompleted && releases==0); exited.SetResult(); recovery.GetAwaiter().GetResult(); Check(releases==1);
});
Run("Recovery: read failure preserves known ownership", () =>
{
    var releases=0;
    RecoveryMonitor.RunAsync(new BrokenJournal(),Task.CompletedTask,()=>{releases++;return Task.CompletedTask;}).GetAwaiter().GetResult();
    Check(releases==1);
});
foreach(var mode in Enum.GetValues<DrawingMode>())
{
    foreach(var key in new[]{0x20,0x41,0x35,0x75,0x25})
    {
        Run($"Router {mode}: selected key {key} controls drawing",()=>
        {
            var(c,m,_) = Create(mode);var router=new KeyboardTriggerRouter(c,key);
            Check(router.Handle(key,true,()=>false,()=>{}));Check(m.Presses==1);
            Check(router.Handle(key,false,()=>false,()=>{}));
            Check(c.OwnsButton==(mode==DrawingMode.Toggle));c.Pause();Check(m.Releases==1);
        });
    }
}
Run("Router: unselected keys pass without native queries",()=>
{
    var(c,m,_)=Create();var router=new KeyboardTriggerRouter(c,0x75);
    Check(!router.Handle(32,true,()=>throw new Exception("read"),()=>throw new Exception("refresh")));
    Check(!router.Handle(32,false,()=>false,()=>{}));Check(m.Presses==0);
});
Run("Router: changing a held key releases drawing and preserves old key-up",()=>
{
    var(c,m,_)=Create(DrawingMode.Hold);var router=new KeyboardTriggerRouter(c,32);
    Check(router.Handle(32,true,()=>false,()=>{}));router.SetKey(0x75,false);
    Check(!c.Enabled&&!c.OwnsButton&&m.Releases==1);
    Check(router.Handle(32,true,()=>false,()=>{}));Check(router.Handle(32,false,()=>false,()=>{}));
    c.Enable();Check(router.Handle(0x75,true,()=>false,()=>{}));Check(m.Presses==2);
    Check(router.Handle(0x75,false,()=>false,()=>{}));Check(m.Releases==2);
});
Run("Router: new key already held requires release and fresh press",()=>
{
    var(c,m,_)=Create();var router=new KeyboardTriggerRouter(c,32);router.SetKey(0x75,true);c.Enable();
    Check(!router.Handle(0x75,true,()=>false,()=>{}));Check(m.Presses==0);
    Check(!router.Handle(0x75,false,()=>false,()=>{}));Check(router.Handle(0x75,true,()=>false,()=>{}));Check(m.Presses==1);
});
Run("Router: old captured key-up cannot stop drawing with the new key",()=>
{
    var(c,m,_)=Create(DrawingMode.Hold);var router=new KeyboardTriggerRouter(c,32);
    router.Handle(32,true,()=>false,()=>{});router.SetKey(0x75,false);c.Enable();
    Check(router.Handle(0x75,true,()=>false,()=>{}));Check(c.OwnsButton);
    Check(router.Handle(32,false,()=>false,()=>{}));Check(c.OwnsButton&&m.Releases==1);
    Check(router.Handle(0x75,false,()=>false,()=>{}));Check(!c.OwnsButton&&m.Releases==2);
});
Run("Router: reselecting an old captured held key does not leak repeats",()=>
{
    var(c,m,_)=Create(DrawingMode.Hold);var router=new KeyboardTriggerRouter(c,32);
    router.Handle(32,true,()=>false,()=>{});router.SetKey(0x75,false);router.SetKey(32,true);c.Enable();
    Check(router.Handle(32,true,()=>false,()=>{}));Check(m.Presses==1);
    Check(router.Handle(32,false,()=>false,()=>{}));Check(router.Handle(32,true,()=>false,()=>{}));Check(m.Presses==2);
});
Run("Router: custom-key modifier chords pass through",()=>
{
    var(c,m,_)=Create();var router=new KeyboardTriggerRouter(c,0x41);
    Check(!router.Handle(0x41,true,()=>true,()=>{}));Check(!router.Handle(0x41,false,()=>false,()=>{}));Check(m.Presses==0);
});
Run("Router: held custom-key repeats retain fast path",()=>
{
    var(c,m,_)=Create(DrawingMode.Hold);var router=new KeyboardTriggerRouter(c,0x75);var calls=0;
    bool Read(){calls++;return false;}void Refresh(){calls++;}
    router.Handle(0x75,true,Read,Refresh);
    for(var i=0;i<10000;i++)Check(router.Handle(0x75,true,Read,Refresh));
    Check(router.Handle(0x75,false,Read,Refresh));Check(calls==2&&m.Presses==1&&m.Releases==1);
});
Run("Drawing key policy rejects modifiers, Escape and reserved keys",()=>
{
    foreach(var key in new[]{0,0x10,0x11,0x12,0x1B,0x14,0x5B,0x5C,0x7B,0x90,0xFF})Check(!DrawingKeyPolicy.IsAllowed(key));
    var(c,_,_)=Create();try{new KeyboardTriggerRouter(c,0x1B);throw new Exception("Invalid key accepted");}catch(ArgumentOutOfRangeException){}
});
Console.WriteLine($"\n{passed} passed; {failed} failed.");
return failed == 0 ? 0 : 1;

sealed class FakeMouse : IMouseInput
{
    public bool PhysicalLeftDown { get; set; }
    public bool PressSucceeds { get; set; } = true;
    public bool ReleaseSucceeds { get; set; } = true;
    public int Presses { get; private set; }
    public int Releases { get; private set; }
    public bool Press() { Presses++; return PressSucceeds; }
    public bool Release() { Releases++; return ReleaseSucceeds; }
}
sealed class EnvironmentState { public bool CanStart { get; set; } = true; }
sealed class BrokenJournal : MemoryStream
{
    private bool read;
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken=default)
    {
        if(read) throw new IOException("simulated pipe disconnect");
        read=true; buffer.Span[0]=1; return ValueTask.FromResult(1);
    }
}
