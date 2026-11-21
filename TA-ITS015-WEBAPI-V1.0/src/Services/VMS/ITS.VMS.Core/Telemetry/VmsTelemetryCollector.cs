using System.Collections.Concurrent;
using System.Text.Json;
using ITS.VMS.Core.Integration;
using ITS.VMS.Core.Logging;
using ITS.VMS.Core.Messaging;
using ITS.VMS.Core.Storage;

namespace ITS.VMS.Core.Telemetry
{
    /// <summary>Kết quả một lượt quét telemetry, dùng để hiển thị trên form và ghi log.</summary>
    public sealed class VmsCollectSummary
    {
        public DateTime CollectedAt { get; set; } = DateTime.Now;

        /// <summary>Tổng số thiết bị trong lượt quét (kể cả thiết bị thiếu cấu hình).</summary>
        public int Total { get; set; }

        /// <summary>Số thiết bị đọc được thông số.</summary>
        public int Succeeded { get; set; }

        /// <summary>Số gói đã đẩy về BE thành công.</summary>
        public int PushedPackets { get; set; }

        /// <summary>Số gói phải đưa vào bộ đệm vì chưa đẩy được.</summary>
        public int BufferedPackets { get; set; }

        public string Message { get; set; } = string.Empty;

        /// <summary>Chi tiết từng thiết bị để form vẽ lên lưới.</summary>
        public List<VmsTelemetryItem> Items { get; set; } = new();

        public int Failed => Total - Succeeded;
    }

    /// <summary>
    /// Đọc thông số thiết bị VMS/LCS một lượt rồi đẩy về BE lưu DB.
    ///
    /// Đặt ở Core để dùng chung: ITS.VMS.Worker gọi theo chu kỳ trong BackgroundService,
    /// còn ITS.VMS.Wpf gọi trực tiếp khi người dùng bấm "Quét telemetry ngay".
    ///
    /// Đây là phần thay thế job Hangfire cũ ở BE. Nhờ dịch vụ tự giữ nhịp nên KHÔNG còn chuyện
    /// BE phải chờ một gói tin cho toàn bộ thiết bị rồi vỡ timeout khi số biển tăng lên.
    ///
    /// Author: ChuongLP
    /// Created date: 23/09/2026
    /// </summary>
    public sealed class VmsTelemetryCollector
    {
        private readonly VmsTelemetryOption _options;
        private readonly VmsDataChannel _channel;
        private readonly VmsEquipmentStore _store;
        private readonly VmsTelemetryBuffer _buffer;
        private readonly VmsDeviceExecutor _executor;
        private readonly VmsLogger _logger;

        public VmsTelemetryCollector(VmsTelemetryOption options, VmsDataChannel channel, VmsEquipmentStore store,
                                     VmsTelemetryBuffer buffer, VmsDeviceExecutor executor, VmsLogger logger)
        {
            _options = options;
            _channel = channel;
            _store = store;
            _buffer = buffer;
            _executor = executor;
            _logger = logger;
        }

        /// <summary>Nạp các gói tồn từ lần chạy trước.</summary>
        public void LoadBuffer() => _buffer.Load();

        /// <summary>Số gói đang chờ đẩy lại về BE.</summary>
        public int PendingPackets => _buffer.Count;

        /// <summary>
        /// Một lượt quét: đọc thông số toàn bộ thiết bị rồi đẩy về BE.
        /// KHÔNG ném exception ra ngoài để một lượt lỗi không làm chết vòng lặp.
        /// </summary>
        /// <param name="source">"auto" (vòng lặp) hoặc "manual" (bấm trên form).</param>
        public async Task<VmsCollectSummary> CollectOnceAsync(string source, CancellationToken ct)
        {
            var summary = new VmsCollectSummary();

            try
            {
                // Gửi lại các gói tồn trước: giữ đúng thứ tự thời gian của dữ liệu.
                await FlushBufferAsync(ct).ConfigureAwait(false);

                var ready = _store.GetReadyEquipments();
                var notReady = _store.GetNotReadyEquipments();

                if (ready.Count == 0 && notReady.Count == 0)
                {
                    summary.Message = "Chưa có danh sách thiết bị. Hãy đồng bộ thiết bị với BE trước.";
                    _logger.Warning(LogCategory.Device, $"[Telemetry] {summary.Message}");
                    return summary;
                }

                var items = new ConcurrentBag<VmsTelemetryItem>();

                // Thiết bị thiếu cấu hình: không gọi ra thiết bị nhưng vẫn báo về BE để lịch sử đúng thực tế.
                foreach (var equipment in notReady)
                {
                    items.Add(new VmsTelemetryItem
                    {
                        EquipmentId = equipment.EquipmentId,
                        EquipmentCode = equipment.Code,
                        Success = false,
                        Message = "Thiết bị chưa cấu hình Vendor hoặc IP.",
                    });
                }

                if (ready.Count > 0)
                {
                    var parallel = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = _options.MaxParallelDevice > 0 ? _options.MaxParallelDevice : 4,
                        CancellationToken = ct,
                    };

                    await Parallel.ForEachAsync(ready, parallel, async (equipment, token) =>
                    {
                        items.Add(await ReadOneAsync(equipment, token).ConfigureAwait(false));
                    }).ConfigureAwait(false);
                }

                // Giữ thứ tự theo danh sách thiết bị để form đọc cho dễ.
                var order = _store.GetAll().Select(e => e.EquipmentId).ToList();
                summary.Items = items.OrderBy(i => order.IndexOf(i.EquipmentId)).ToList();
                summary.Total = summary.Items.Count;
                summary.Succeeded = summary.Items.Count(i => i.Success);

                await PushAsync(summary, source, ct).ConfigureAwait(false);

                summary.Message = $"Quét {summary.Total} thiết bị: đọc được {summary.Succeeded}, lỗi {summary.Failed}. " +
                                  $"Đẩy về BE {summary.PushedPackets} gói" +
                                  (summary.BufferedPackets > 0 ? $", tồn {summary.BufferedPackets} gói." : ".");

                _logger.Log(LogCategory.Device, summary.Failed == 0 ? "Success" : "Warning", $"[Telemetry] {summary.Message}");
                return summary;
            }
            catch (OperationCanceledException)
            {
                summary.Message = "Đã hủy.";
                return summary;
            }
            catch (Exception ex)
            {
                summary.Message = $"Lỗi: {ex.Message}";
                _logger.Error(LogCategory.Device, $"[Telemetry] Lượt quét lỗi: {ex.Message}");
                return summary;
            }
        }

        /// <summary>Đọc thông số một thiết bị, tái dùng đúng executor của luồng điều khiển.</summary>
        private async Task<VmsTelemetryItem> ReadOneAsync(VmsEquipmentInfo equipment, CancellationToken ct)
        {
            var request = new VmsControlRequest
            {
                Type = VmsControlType.Request,
                Action = VmsControlAction.GetStat,
                StatTypes = _options.StatTypes,
            };

            var target = new VmsControlTarget
            {
                EquipmentId = equipment.EquipmentId,
                Vendor = equipment.Vendor,
                Ip = equipment.Ip,
                Port = equipment.Port,
                Config = equipment.Config,
            };

            // ExecuteAsync luôn trả kết quả, không ném exception ra ngoài.
            var result = await _executor.ExecuteAsync(request, target, ct).ConfigureAwait(false);

            return new VmsTelemetryItem
            {
                EquipmentId = equipment.EquipmentId,
                EquipmentCode = equipment.Code,
                Success = result.Success,
                Message = result.Message,
                Stats = ToStats(result.Data),
            };
        }

        /// <summary>
        /// Chia kết quả thành nhiều gói rồi đẩy về BE. Gói đẩy không được thì đưa vào bộ đệm.
        /// Chia gói để tránh vượt max_payload của NATS khi số thiết bị lớn.
        /// </summary>
        private async Task PushAsync(VmsCollectSummary summary, string source, CancellationToken ct)
        {
            if (summary.Items.Count == 0) return;

            var size = _options.PushBatchSize > 0 ? _options.PushBatchSize : 50;

            for (var i = 0; i < summary.Items.Count; i += size)
            {
                var push = new VmsTelemetryPush
                {
                    Source = source,
                    CollectedAt = summary.CollectedAt,
                    Items = summary.Items.GetRange(i, Math.Min(size, summary.Items.Count - i)),
                };

                if (!await EnsureConnectedAsync(ct).ConfigureAwait(false))
                {
                    _buffer.Enqueue(push);
                    summary.BufferedPackets++;
                    continue;
                }

                if (await _channel.PublishTelemetryAsync(push, ct).ConfigureAwait(false))
                {
                    summary.PushedPackets++;
                    continue;
                }

                // BE/NATS chưa nhận được -> giữ lại để gửi lần sau, không bỏ số liệu.
                _buffer.Enqueue(push);
                summary.BufferedPackets++;
                _logger.Warning(LogCategory.Control,
                    $"[Telemetry] Chưa đẩy được {push.Items.Count} bản ghi về BE, đã đưa vào bộ đệm (tổng {_buffer.Count} gói).");
            }
        }

        /// <summary>Gửi lại các gói tồn trong bộ đệm.</summary>
        private async Task FlushBufferAsync(CancellationToken ct)
        {
            if (_buffer.Count == 0) return;
            if (!await EnsureConnectedAsync(ct).ConfigureAwait(false)) return;

            await _buffer.FlushAsync(push => _channel.PublishTelemetryAsync(push, ct), ct).ConfigureAwait(false);
        }

        /// <summary>Bảo đảm đã kết nối NATS. Trả false nếu không kết nối được (để người gọi đệm lại).</summary>
        private async Task<bool> EnsureConnectedAsync(CancellationToken ct)
        {
            if (_channel.IsConnected) return true;

            try
            {
                await _channel.StartAsync(ct).ConfigureAwait(false);
                return _channel.IsConnected;
            }
            catch (Exception ex)
            {
                _logger.Error(LogCategory.Control, $"[Telemetry] Không kết nối được NATS: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Đổi trường Data của kết quả get-stat về dictionary thông số.
        /// Executor trả IntgVmsGetStatOutput.Value nên thường đã là Dictionary, vẫn phòng kiểu khác.
        /// </summary>
        private static Dictionary<string, object>? ToStats(object? data)
        {
            if (data == null) return null;
            if (data is Dictionary<string, object> dict) return dict;

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(data));
            }
            catch
            {
                return null;
            }
        }
    }
}
