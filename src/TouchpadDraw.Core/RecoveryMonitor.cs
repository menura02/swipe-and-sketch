namespace TouchpadDraw.Core;

/// <summary>One-byte ownership journal: 1 = may own button, 0 = released.</summary>
public static class RecoveryMonitor
{
    public static async Task RunAsync(Stream journal, Task parentExited, Func<Task> recover)
    {
        var owns = false;
        var buffer = new byte[1];
        try
        {
            while (await journal.ReadAsync(buffer).ConfigureAwait(false) > 0)
            {
                // Unknown bytes are conservatively ignored, never clear known ownership.
                if (buffer[0] == 1) owns = true;
                else if (buffer[0] == 0) owns = false;
            }
        }
        catch (IOException) { }
        await parentExited.ConfigureAwait(false);
        if (owns) await recover().ConfigureAwait(false);
    }
}
