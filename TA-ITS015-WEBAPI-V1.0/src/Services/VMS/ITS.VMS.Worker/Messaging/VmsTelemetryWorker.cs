using ITS.VMS.Core.Logging;
using ITS.VMS.Core.Messaging;
using ITS.VMS.Core.Telemetry;

namespace ITS.VMS.Worker.Messaging;

/// <summary>
/// Vòng lặp tự đọc thông số thiết bị VMS/LCS theo chu kỳ rồi đẩy về BE lưu DB.
/// Thay thế job Hangfire cũ ở BE.
///
/// Toàn bộ logic nằm ở <see cref="VmsTelemetryCollector"/> (Core) để form ITS.VMS.Wpf gọi lại được.
/// Lớp này chỉ lo phần lập lịch.
///
/// Author: ChuongLP
/// Created date: 23/09/2026
/// </summary>
public sealed class VmsTelemetryWorker : BackgroundService
{
    /// <summary>Nguồn số liệu ghi vào lịch sử — khớp VmsStatConst.Source.Auto bên BE.</summary>
    private const string SourceAuto = "auto";

    private readonly VmsTelemetryOption _options;
    private readonly VmsTelemetryCollector _collector;
    private readonly VmsLogger _logger;

    public VmsTelemetryWorker(VmsTelemetryOption options, VmsTelemetryCollector collector, VmsLogger logger)
    {
        _options = options;
        _collector = collector;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.Info(LogCategory.Device, "[Telemetry] Luồng telemetry đang TẮT (Telemetry:Enabled = false).");
            return;
        }

        _collector.LoadBuffer();

        var interval = TimeSpan.FromMinutes(_options.CollectIntervalMinutes > 0 ? _options.CollectIntervalMinutes : 15);

        // Chờ một nhịp ngắn để VmsEquipmentSyncWorker kịp nạp/đồng bộ danh sách trước lượt quét đầu.
        try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { return; }

        _logger.Success(LogCategory.Device, $"[Telemetry] Bắt đầu vòng lặp đọc thông số, chu kỳ {interval.TotalMinutes:0} phút.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await _collector.CollectOnceAsync(SourceAuto, stoppingToken).ConfigureAwait(false);

            try { await Task.Delay(interval, stoppingToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }

        _logger.Info(LogCategory.Device, "[Telemetry] Đã dừng vòng lặp đọc thông số.");
    }
}
