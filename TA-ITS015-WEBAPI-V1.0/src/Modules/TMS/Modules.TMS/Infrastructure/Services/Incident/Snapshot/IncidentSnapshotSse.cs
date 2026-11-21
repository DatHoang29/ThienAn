using Furion;
using Microsoft.Extensions.Localization;
using Modules.TMS.Core.Exceptions;
using Shared.Infrastructure.Sse;
using Shared.Utility.Sse;

namespace Modules.TMS.Infrastructure.Services.Incident.Snapshot;

/// <summary>Sequential snapshot polling: no overlapping reads, bounded interval and retry backoff.</summary>
public static class IncidentSnapshotSse
{
    private static readonly SemaphoreSlim QuerySlots = new(4, 4);
    public static SseResult Poll<T>(Func<CancellationToken, Task<T>> fetch, int refresh, bool changesOnly = false)
    {
        if (refresh is < 1 or > 60) throw new ArgumentOutOfRangeException(nameof(refresh));

        // Lấy câu thông báo MỘT LẦN khi mở stream: bên trong vòng lặp thì localizer của request có thể đã
        // không còn dùng được. Trước 22/09/2026 chỗ này là một chuỗi tiếng Việt cứng nên client en-US vẫn
        // nhận tiếng Việt. Bọc try/catch để lớp này còn gọi được ngoài host (unit test).
        var unavailable = Message(BaseMsg.TmsIncident.Exception.SnapshotUnavailable);

        return SseConnection.Stream(async (writer, ct) =>
        {
            using var diagnostic = SseDiagnosticOperation.Begin("incident-snapshot-poll", ct);
            var failures = 0;
            string? previous = null;
            var heartbeatAt = DateTime.UtcNow;
            while (!ct.IsCancellationRequested)
            {
                T data;
                try
                {
                    diagnostic.Phase = "waiting-query-slot";
                    await QuerySlots.WaitAsync(ct);
                    try { diagnostic.Phase = "fetch"; data = await fetch(ct); failures = 0; }
                    finally { QuerySlots.Release(); }
                }
                catch (OperationCanceledException ex) when (ct.IsCancellationRequested) { diagnostic.Cancelled(ex); return; }
                catch (Exception ex)
                {
                    diagnostic.Event("fetch-failed", ex);
                    failures = Math.Min(failures + 1, 6);
                    if (ct.IsCancellationRequested) return;
                    await writer.WriteAsync(SseFrame.Fail(unavailable, 503).WithEvent("unavailable"), ct);
                    await SseCancellation.DelayAsync(TimeSpan.FromSeconds(Math.Min(60, refresh * Math.Pow(2, failures))), ct);
                    continue;
                }
                if (ct.IsCancellationRequested) return;
                var fingerprint = changesOnly ? System.Text.Json.JsonSerializer.Serialize(data) : null;
                if (!changesOnly || previous != fingerprint)
                {
                    diagnostic.Phase = "writing-snapshot";
                    await writer.WriteAsync(data, ct);
                    previous = fingerprint;
                    heartbeatAt = DateTime.UtcNow;
                }
                else if (DateTime.UtcNow - heartbeatAt >= TimeSpan.FromSeconds(15))
                {
                    diagnostic.Phase = "writing-heartbeat";
                    await writer.WriteAsync(new { heartbeat = true }, ct);
                    heartbeatAt = DateTime.UtcNow;
                }
                diagnostic.Phase = "poll-delay";
                await SseCancellation.DelayAsync(TimeSpan.FromSeconds(refresh), ct);
            }
        });
    }

    /// <summary>Câu thông báo đã dịch, hoặc chính khóa nếu chạy ngoài host.</summary>
    private static string Message(string key)
    {
        try { return App.GetService<IStringLocalizer>()?[key].Value ?? key; }
        catch { return key; }
    }
}
