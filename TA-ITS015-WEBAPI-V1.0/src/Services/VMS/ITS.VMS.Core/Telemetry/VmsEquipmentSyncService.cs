using ITS.VMS.Core.Logging;
using ITS.VMS.Core.Messaging;
using ITS.VMS.Core.Storage;

namespace ITS.VMS.Core.Telemetry
{
    /// <summary>
    /// Đồng bộ danh sách thiết bị VMS/LCS từ BE về dịch vụ.
    ///
    /// Đặt ở Core để dùng chung: ITS.VMS.Worker gọi theo chu kỳ trong BackgroundService,
    /// còn ITS.VMS.Wpf gọi trực tiếp khi người dùng bấm nút "Đồng bộ thiết bị".
    ///
    /// Author: ChuongLP
    /// Created date: 23/09/2026
    /// </summary>
    public sealed class VmsEquipmentSyncService
    {
        private readonly VmsDataChannel _channel;
        private readonly VmsEquipmentStore _store;
        private readonly VmsLogger _logger;

        public VmsEquipmentSyncService(VmsDataChannel channel, VmsEquipmentStore store, VmsLogger logger)
        {
            _channel = channel;
            _store = store;
            _logger = logger;
        }

        /// <summary>Nạp danh sách đã lưu từ lần chạy trước để dịch vụ chạy được ngay, không phải chờ BE.</summary>
        public void LoadFromFile() => _store.Load();

        /// <summary>
        /// Một lượt đồng bộ với BE. KHÔNG ném exception ra ngoài để một lần BE lỗi không làm chết
        /// BackgroundService (host sẽ không tự khởi động lại nó).
        /// </summary>
        /// <returns>Mô tả kết quả để hiển thị trên form hoặc ghi log.</returns>
        public async Task<string> SyncOnceAsync(CancellationToken ct)
        {
            try
            {
                // Kết nối lười: NATS có thể chưa sẵn sàng lúc dịch vụ mới bật.
                if (!_channel.IsConnected)
                    await _channel.StartAsync(ct).ConfigureAwait(false);

                var result = await _channel.RequestEquipmentAsync(ct).ConfigureAwait(false);

                // null = timeout / lỗi phát gói -> giữ nguyên danh sách đang dùng, lần sau thử lại.
                if (result == null)
                    return $"BE không trả lời. Giữ nguyên {_store.Count} thiết bị đang dùng.";

                if (!result.Success)
                {
                    _logger.Error(LogCategory.Config, $"[Equipment] BE báo lỗi khi lấy danh sách thiết bị: {result.Message}");
                    return $"BE báo lỗi: {result.Message}";
                }

                _store.Update(result.Equipments, result.SyncedAt);

                var notReady = _store.GetNotReadyEquipments().Count;
                return $"Đã đồng bộ {_store.Count} thiết bị" +
                       (notReady > 0 ? $", trong đó {notReady} thiết bị thiếu Vendor/Ip." : ".");
            }
            catch (OperationCanceledException)
            {
                return "Đã hủy.";
            }
            catch (Exception ex)
            {
                _logger.Error(LogCategory.Config, $"[Equipment] Lượt đồng bộ lỗi: {ex.Message}");
                return $"Lỗi: {ex.Message}";
            }
        }
    }
}
