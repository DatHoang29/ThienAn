using ITS.VMS.Core.Logging;
using ITS.VMS.Core.Messaging;
using ITS.VMS.Core.Telemetry;

namespace ITS.VMS.Worker.Messaging;

/// <summary>
/// Giữ cho danh sách thiết bị VMS/LCS ở dịch vụ luôn mới:
///   1. Nạp danh sách cũ từ file để chạy được ngay, không phải chờ BE.
///   2. Đồng bộ với BE ngay khi khởi động.
///   3. Đồng bộ lại theo chu kỳ — lưới an toàn cho trường hợp gói cập nhật từ BE bị mất.
///
/// Toàn bộ logic nằm ở <see cref="VmsEquipmentSyncService"/> (Core) để form ITS.VMS.Wpf gọi lại được.
/// Lớp này chỉ lo phần lập lịch.
///
/// Author: ChuongLP
/// Created date: 23/09/2026
/// </summary>
public sealed class VmsEquipmentSyncWorker : BackgroundService
{
    private readonly VmsTelemetryOption _options;
    private readonly VmsEquipmentSyncService _syncService;
    private readonly VmsLogger _logger;

    public VmsEquipmentSyncWorker(VmsTelemetryOption options, VmsEquipmentSyncService syncService, VmsLogger logger)
    {
        _options = options;
        _syncService = syncService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.Info(LogCategory.Config, "[Equipment] Luồng telemetry đang TẮT (Telemetry:Enabled = false), không đồng bộ thiết bị.");
            return;
        }

        _syncService.LoadFromFile();

        var interval = TimeSpan.FromMinutes(_options.EquipmentSyncMinutes > 0 ? _options.EquipmentSyncMinutes : 30);

        while (!stoppingToken.IsCancellationRequested)
        {
            await _syncService.SyncOnceAsync(stoppingToken).ConfigureAwait(false);

            try { await Task.Delay(interval, stoppingToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }

        _logger.Info(LogCategory.Config, "[Equipment] Đã dừng đồng bộ danh sách thiết bị.");
    }
}
