namespace Services.Vds.Manager;

public static class VdsShutdown
{
    public static async Task StopInOrderAsync(Func<Task> stopSources, Func<Task> stopHost)
    {
        try { await stopSources().ConfigureAwait(false); }
        finally { await stopHost().ConfigureAwait(false); }
    }
}
