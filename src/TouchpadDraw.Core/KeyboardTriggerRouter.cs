namespace TouchpadDraw.Core;

public static class DrawingKeyPolicy
{
    // Ordinary keys only. Escape remains the emergency stop; modifier/lock keys
    // and F12 (Windows debugger key) are intentionally excluded.
    public static bool IsAllowed(int key) => key is 8 or 9 or 13 or 32 or 33 or 34 or 35 or 36 or 37 or 38 or 39 or 40 or 45 or 46
        || key is >= 0x30 and <= 0x39 || key is >= 0x41 and <= 0x5A
        || key is >= 0x60 and <= 0x6B || key is 0x6D or 0x6E or 0x6F
        || key is >= 0x70 and <= 0x7A || key is >= 0x7C and <= 0x87;
}

/// <summary>Preserves captured key-up pairs even when the drawing key changes.</summary>
public sealed class KeyboardTriggerRouter
{
    private readonly DrawingController controller;
    private readonly HashSet<int> captured = [];
    public int Key { get; private set; }
    public KeyboardTriggerRouter(DrawingController controller, int key, bool initiallyDown = false)
    {
        if (!DrawingKeyPolicy.IsAllowed(key)) throw new ArgumentOutOfRangeException(nameof(key));
        this.controller = controller;
        Key = key;
        controller.InitializeTrigger(initiallyDown);
    }
    public void SetKey(int key, bool alreadyDown)
    {
        if (!DrawingKeyPolicy.IsAllowed(key)) throw new ArgumentOutOfRangeException(nameof(key));
        if (key == Key) return;
        controller.Pause();
        Key = key;
        controller.InitializeTrigger(alreadyDown);
    }
    public bool Handle(int key, bool down, Func<bool> readModifiers, Action refresh)
    {
        if (key != Key) return down ? captured.Contains(key) : captured.Remove(key);
        var suppress = controller.Trigger(down, readModifiers, refresh);
        if (down) { if (suppress) captured.Add(key); suppress |= captured.Contains(key); }
        else suppress |= captured.Remove(key);
        return suppress;
    }
}
