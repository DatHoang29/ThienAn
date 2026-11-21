namespace Services.Vds.Configuration;

public static class SubscriptionRecovery
{
    public static async Task RunAsync(Func<CancellationToken, Task<bool>> connect,
        Func<CancellationToken, Task> subscribe, Func<CancellationToken, Task> delay,
        Action<Exception> onError, CancellationToken ct)
    {
        var subscribed = false;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (await connect(ct) && !subscribed)
                {
                    await subscribe(ct);
                    subscribed = true;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex) { onError(ex); }
            try { await delay(ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        }
    }
}
