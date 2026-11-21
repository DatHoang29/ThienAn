// Author: Codex
// Description: Runs an SSE callback with an independent heartbeat and quiet timer shutdown.
// Created date: 02/10/2026

using System.IO;
using System.Runtime.ExceptionServices;
using Shared.Infrastructure.Sse;

namespace Shared.Utility.Sse;

/// <summary>Runs an SSE callback with an independent heartbeat and quiet timer shutdown.</summary>
public static class SseConnection
{
    /// <summary>
    /// Sends a heartbeat every 15 seconds, including while fetching data is blocked.
    /// The endpoint must disable the infrastructure heartbeat with <c>HeartbeatSeconds = 0</c>.
    /// Callbacks must return after writing a terminal frame and honor their cancellation token.
    /// </summary>
    public static SseResult Stream(Func<SseWriter, CancellationToken, Task> callback,
        TimeSpan? heartbeatInterval = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var interval = heartbeatInterval ?? TimeSpan.FromSeconds(15);
        if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(heartbeatInterval));

        return SseResult.Stream(async (writer, requestToken) =>
        {
            using var diagnostic = SseDiagnosticOperation.Begin("sse-connection", requestToken);
            diagnostic.Phase = "callback";
            using var stopped = new CancellationTokenSource();
            using var connection = CancellationTokenSource.CreateLinkedTokenSource(requestToken, stopped.Token);
            var heartbeat = HeartbeatAsync(writer, interval, connection.Token, requestToken, stopped, diagnostic);
            ExceptionDispatchInfo? callbackFailure = null;
            try
            {
                await callback(writer, connection.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (connection.IsCancellationRequested) { diagnostic.Cancelled(ex); }
            catch (IOException ex) when (requestToken.IsCancellationRequested) { diagnostic.Event("request-closed", ex); }
            catch (Exception ex)
            {
                diagnostic.Failed(ex);
                callbackFailure = ExceptionDispatchInfo.Capture(ex);
            }
            finally
            {
                diagnostic.Phase = "stopping-heartbeat";
                // Complete the heartbeat wait normally and await any in-progress write before return.
                stopped.Cancel();
            }

            try
            {
                await heartbeat.ConfigureAwait(false);
            }
            catch when (callbackFailure is not null)
            {
                // Keep the original callback error if both data and heartbeat writes fail.
            }
            callbackFailure?.Throw();
            diagnostic.Complete();
        });
    }

    private static async Task HeartbeatAsync(SseWriter writer, TimeSpan interval, CancellationToken ct,
        CancellationToken requestToken, CancellationTokenSource stopped, SseDiagnosticOperation diagnostic)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await SseCancellation.DelayAsync(interval, ct).ConfigureAwait(false);
                if (ct.IsCancellationRequested) return;
                // The SharedWorker excludes heartbeat payloads from its latest business snapshot.
                await writer.WriteAsync(SseFrame.Ok(new { alive = true, heartbeat = true }).WithEvent("heartbeat"), ct)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (IOException) when (requestToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            diagnostic.Event("heartbeat-failed", ex);
            // A broken heartbeat must stop a blocked data reader, then surface the transport error.
            stopped.Cancel();
            throw;
        }
    }
}
