namespace TouchpadDraw.Core;

public enum DrawingMode { Toggle, Hold }
public enum SessionState { NotReady, Paused, Ready, Drawing, Error }

public interface IMouseInput
{
    bool PhysicalLeftDown { get; }
    bool Press();
    bool Release();
}

/// <summary>Owns only the synthetic button; never generates pointer movement.</summary>
public sealed class DrawingController(IMouseInput mouse, Func<bool> canStart)
{
    private bool triggerDown;
    private bool capturedTrigger;
    private bool capturedEscape;
    private bool ownsButton;
    private bool configured;
    private bool available;
    public bool Enabled { get; private set; }
    public bool OwnsButton => ownsButton;
    public DrawingMode Mode { get; private set; }
    public string? Error { get; private set; }
    public SessionState State => Error is not null ? SessionState.Error : !configured ? SessionState.NotReady :
        ownsButton ? SessionState.Drawing : Enabled && available ? SessionState.Ready : SessionState.Paused;
    public event Action? Changed;

    public void InitializeTrigger(bool down) { triggerDown = down; capturedTrigger = false; }

    public void SetEnvironment(bool isConfigured, bool isAvailable)
    {
        var changed = configured != isConfigured || available != isAvailable;
        configured = isConfigured;
        available = isAvailable;
        if (!available) Stop();
        if (!configured) Enabled = false;
        if (changed) Changed?.Invoke();
    }

    public void SetMode(DrawingMode mode)
    {
        Pause();
        Mode = mode;
        Changed?.Invoke();
    }

    public void Enable()
    {
        if (!configured || ownsButton) return;
        Error = null;
        Enabled = true;
        Changed?.Invoke();
    }

    public void Pause()
    {
        Enabled = false;
        Stop();
        Changed?.Invoke();
    }

    public void Fail(string message)
    {
        Enabled = false;
        Stop();
        Error = message;
        Changed?.Invoke();
    }

    // Return true to suppress this event. A captured down always owns its matching up.
    public bool Trigger(bool down, bool hasModifiers) => Trigger(down, () => hasModifiers, null);

    // Native checks are lazy: auto-repeat and release must take the short path.
    // Focus/area safety is also monitored independently while a key is held.
    public bool Trigger(bool down, Func<bool> readModifiers, Action? beforeFreshPress)
    {
        if (!down)
        {
            triggerDown = false;
            var suppress = capturedTrigger;
            capturedTrigger = false;
            if (suppress && Mode == DrawingMode.Hold) Stop();
            return suppress;
        }
        if (triggerDown) return capturedTrigger;
        triggerDown = true;
        beforeFreshPress?.Invoke();
        if (!Enabled || Error is not null || readModifiers() || !available) return false;
        capturedTrigger = true;
        if (ownsButton) Stop();
        else if (!mouse.PhysicalLeftDown && canStart())
        {
            if (mouse.Press()) ownsButton = true;
            else { Enabled = false; Error = "Sol fare tuşu basılamadı. Oturum duraklatıldı."; }
            Changed?.Invoke();
        }
        return true;
    }

    public bool Escape(bool down)
    {
        if (!down) { var result = capturedEscape; capturedEscape = false; return result; }
        if (capturedEscape) return true;
        if (!Enabled && !ownsButton) return false;
        capturedEscape = true;
        Pause();
        return true;
    }

    public void PhysicalPress() { if (ownsButton) Pause(); }

    public void Stop()
    {
        if (!ownsButton) return;
        if (mouse.Release()) ownsButton = false;
        else { Enabled = false; Error = "Sol fare tuşu bırakılamadı; yeniden deneniyor."; }
        Changed?.Invoke();
    }
}
