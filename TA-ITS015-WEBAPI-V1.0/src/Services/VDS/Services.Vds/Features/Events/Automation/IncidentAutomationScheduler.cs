using Microsoft.Extensions.Hosting;
using Services.Vds.Configuration;

namespace Services.Vds.Features.Events.Automation;

/// <summary>
/// Polls durable SQL incident dispatch markers and replays the legacy journal.
/// Transient failures remain pending until the retention cutoff; shutdown preserves pending work.
/// Queue entries are hints. TMS receipts make successful scheduling retries idempotent.
/// </summary>
public sealed class IncidentAutomationScheduler : BackgroundService
{
    private readonly IncidentAutomationQueue _queue;
    private readonly IncidentAutomationApiClient _client;
    private readonly IncidentDispatchStore _dispatch;
    private readonly IncidentRepository _incidents;
    private readonly IVdsConfigManager _config;
    private readonly ILogger<IncidentAutomationScheduler> _logger;

    public IncidentAutomationScheduler(
        IncidentAutomationQueue queue,
        IncidentAutomationApiClient client,
        IncidentDispatchStore dispatch,
        IncidentRepository incidents,
        IVdsConfigManager config,
        ILogger<IncidentAutomationScheduler> logger)
    {
        _queue = queue;
        _client = client;
        _dispatch = dispatch;
        _incidents = incidents;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string? afterId = null;
        string? cursorTenant = null;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var options = _config.Current.TmsApi;
                if (options.Enabled)
                {
                    var expired = await _incidents.ExpireDispatchAsync(
                        DateTime.Now.AddMinutes(-options.EffectiveDispatchRetentionMinutes), stoppingToken);
                    if (expired > 0)
                        _logger.LogWarning("[TmsApi] {Count} dispatch quá retention; đã lưu expired/RetentionExceeded trong Metadata, cần đối soát thủ công.", expired);
                }
                if (options.IsUsable)
                {
                    var tenant = _config.Current.TenantId;
                    if (cursorTenant != tenant) { afterId = null; cursorTenant = tenant; }
                    // SQL marker is committed in the incident insert. Queue/file loss cannot erase it.
                    var pending = await _incidents.ReadPendingDispatchAsync(
                        DateTime.Now.AddMinutes(-options.EffectiveDispatchRetentionMinutes), stoppingToken, afterId);
                    // Advance even when this page retries; wrap to retry it after visiting later pages.
                    afterId = pending.Count == 500 ? pending[^1].ID : null;
                    var work = pending.Select(x => new PendingDispatchEntry { IncidentId = x.ID, Code = x.Code, CreatedUtc = x.CreateTime.ToUniversalTime() })
                        .Concat(_dispatch.TakeReplayable()).GroupBy(x => x.IncidentId).Select(x => x.First());
                    while (_queue.Reader.TryRead(out _)) { } // Hints only; SQL is authoritative.
                    foreach (var entry in work)
                    {
                        stoppingToken.ThrowIfCancellationRequested();
                        try
                        {
                            // A slow earlier request may consume the remaining retention of this entry.
                            if (entry.CreatedUtc < DateTime.UtcNow.AddMinutes(-_config.Current.TmsApi.EffectiveDispatchRetentionMinutes))
                                continue; // Next maintenance pass persists expiry; never start stale work.
                            var result = await _client.ScheduleAsync(entry.IncidentId, stoppingToken);
                            if (!result.Called || result.Retryable) continue;
                            await _incidents.CompleteDispatchAsync(entry.IncidentId, stoppingToken, result.ReasonCode, result.IsTerminalFailure);
                            if (result.IsTerminalFailure)
                            {
                                _dispatch.MarkFailed(entry.IncidentId, result.ReasonCode);
                                _logger.LogError("[TmsApi] Dispatch {Incident} thất bại vĩnh viễn: {Reason}. Đã lưu failed để đối soát.", entry.IncidentId, result.ReasonCode);
                            }
                            else _dispatch.MarkDone(entry.IncidentId);
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                        catch (Exception ex) { _logger.LogError(ex, "[TmsApi] Giữ dispatch pending {Incident} để thử lại.", entry.IncidentId); }
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "[TmsApi] Không đọc được outbox; sẽ thử lại."); }
            try { await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _config.Current.TmsApi.EffectiveRetryDelaySeconds)), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
