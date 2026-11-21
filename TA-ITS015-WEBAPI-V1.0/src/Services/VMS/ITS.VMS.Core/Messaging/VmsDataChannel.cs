using System.Collections.Concurrent;
using System.Text.Json;
using ITS.VMS.Core.Logging;
using NATS.Client.Core;

namespace ITS.VMS.Core.Messaging
{
    /// <summary>
    /// Kênh dữ liệu do DỊCH VỤ khởi phát, phục vụ luồng tự thu thập telemetry:
    ///   - <see cref="RequestEquipmentAsync"/> : hỏi BE danh sách thiết bị, chờ gói trả về theo RequestId.
    ///   - <see cref="PublishTelemetryAsync"/> : đẩy số liệu đã đọc về BE để lưu DB.
    ///
    /// Cả 3 loại gói đi CHUNG một subject (Telemetry:DataSubject), phân biệt bằng trường Type.
    /// Subject này BE cũng dùng bắn vms-success/vms-fail cho FE, nên bộ lọc Type là BẮT BUỘC —
    /// gói không thuộc 3 loại của luồng telemetry đều bị bỏ qua.
    ///
    /// Giữ kết nối NATS RIÊNG, không dùng chung với <see cref="VmsControlNatsListener"/> (kênh điều khiển)
    /// để hai luồng không ảnh hưởng nhau.
    ///
    /// Author: ChuongLP
    /// Created date: 23/09/2026
    /// </summary>
    public sealed class VmsDataChannel : IAsyncDisposable
    {
        private readonly VmsControlNatsOption _natsOptions;
        private readonly VmsTelemetryOption _options;
        private readonly VmsLogger _logger;

        private static readonly JsonSerializerOptions JsonRead = new(JsonSerializerDefaults.Web);
        private static readonly JsonSerializerOptions JsonWrite = new(JsonSerializerDefaults.Web) { WriteIndented = false };

        /// <summary>Các yêu cầu đồng bộ đang chờ BE trả lời, khóa là RequestId.</summary>
        private readonly ConcurrentDictionary<string, TaskCompletionSource<VmsEquipmentSyncResult>> _pending = new();

        private NatsConnection? _conn;
        private CancellationTokenSource? _cts;
        private Task? _loop;

        /// <summary>Phát mỗi khi có gói của luồng telemetry đi qua kênh: (Type, JSON) — để form hiển thị.</summary>
        public event Action<string, string>? PacketObserved;

        public VmsDataChannel(VmsControlNatsOption natsOptions, VmsTelemetryOption options, VmsLogger logger)
        {
            _natsOptions = natsOptions;
            _options = options;
            _logger = logger;
        }

        /// <summary>Đã kết nối và đang theo dõi kênh hay chưa.</summary>
        public bool IsConnected => _conn is not null && _loop is { IsCompleted: false };

        /// <summary>Subject đang dùng.</summary>
        public string Subject => string.IsNullOrWhiteSpace(_options.DataSubject)
            ? VmsTelemetrySubject.Data : _options.DataSubject.Trim();

        /// <summary>
        /// Kết nối NATS và lắng nghe gói trả về trên kênh dữ liệu.
        /// Gọi lại nhiều lần an toàn. Ném exception khi không kết nối được để người gọi biết mà thử lại.
        /// </summary>
        public async Task StartAsync(CancellationToken ct = default)
        {
            if (IsConnected) return;

            var conn = new NatsConnection(VmsNatsFactory.BuildOpts(_natsOptions, "DATA"));
            await conn.ConnectAsync().ConfigureAwait(false);

            _conn = conn;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _loop = Task.Run(() => ObserveAsync(_cts.Token));

            _logger.Success(LogCategory.Control, $"[Dữ liệu] ĐÃ KẾT NỐI {_natsOptions.Url}. Kênh '{Subject}'.");
        }

        /// <summary>Ngắt kết nối và dừng theo dõi.</summary>
        public async Task StopAsync()
        {
            try { _cts?.Cancel(); } catch (ObjectDisposedException) { }

            if (_loop is not null)
            {
                try { await _loop.ConfigureAwait(false); } catch { /* đã ghi log bên trong */ }
            }

            if (_conn is not null)
            {
                try { await _conn.DisposeAsync().ConfigureAwait(false); } catch { }
                _conn = null;
            }

            _cts?.Dispose();
            _cts = null;
            _loop = null;

            _logger.Info(LogCategory.Control, "[Dữ liệu] Đã ngắt kết nối.");
        }

        /// <summary>
        /// Hỏi BE danh sách thiết bị VMS/LCS. Trả null khi chưa kết nối, lỗi phát gói, hoặc BE không trả lời
        /// trong thời gian chờ — người gọi giữ nguyên danh sách đang dùng, không xoá dữ liệu đang chạy tốt.
        /// </summary>
        public async Task<VmsEquipmentSyncResult?> RequestEquipmentAsync(CancellationToken ct)
        {
            if (_conn is null)
            {
                _logger.Warning(LogCategory.Config, "[Equipment] Chưa kết nối NATS nên chưa đồng bộ được danh sách thiết bị.");
                return null;
            }

            var request = new VmsEquipmentSyncRequest
            {
                Type = VmsDataType.EquipmentRequest,
                RequestId = Guid.NewGuid().ToString("N"),
                TypeCodes = _options.DeviceTypeCodes?.ToList(),
                RequestTime = DateTime.Now,
            };

            var timeout = _options.EquipmentSyncTimeoutSeconds > 0 ? _options.EquipmentSyncTimeoutSeconds : 30;
            var tcs = new TaskCompletionSource<VmsEquipmentSyncResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[request.RequestId] = tcs;

            try
            {
                var json = JsonSerializer.Serialize(request, JsonWrite);
                await _conn.PublishAsync(Subject, json, cancellationToken: ct).ConfigureAwait(false);
                RaiseObserved(VmsDataType.EquipmentRequest, json);

                _logger.Info(LogCategory.Config, $"[Equipment] Đã hỏi BE danh sách thiết bị · RequestId={request.RequestId}");

                var finished = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(timeout), ct)).ConfigureAwait(false);
                if (finished == tcs.Task) return tcs.Task.Result;

                _logger.Error(LogCategory.Config,
                    $"[Equipment] BE không trả lời sau {timeout}s · RequestId={request.RequestId}. Giữ danh sách hiện có.");
                return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error(LogCategory.Config, $"[Equipment] Lỗi hỏi danh sách thiết bị: {ex.Message}");
                return null;
            }
            finally
            {
                _pending.TryRemove(request.RequestId, out _);
            }
        }

        /// <summary>
        /// Đẩy một gói telemetry về BE. Trả false khi chưa kết nối hoặc phát gói lỗi —
        /// người gọi đưa gói vào bộ đệm để gửi lại sau.
        /// </summary>
        public async Task<bool> PublishTelemetryAsync(VmsTelemetryPush push, CancellationToken ct)
        {
            if (push == null) return false;
            if (_conn is null) return false;

            try
            {
                push.Type = VmsDataType.Telemetry;
                var json = JsonSerializer.Serialize(push, JsonWrite);
                await _conn.PublishAsync(Subject, json, cancellationToken: ct).ConfigureAwait(false);
                RaiseObserved(VmsDataType.Telemetry, json);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(LogCategory.Control, $"[Telemetry] Không đẩy được gói về BE: {ex.Message}");
                return false;
            }
        }

        /// <summary>Theo dõi kênh, chỉ nhận gói trả danh sách thiết bị của BE.</summary>
        private async Task ObserveAsync(CancellationToken token)
        {
            if (_conn is null) return;

            try
            {
                await foreach (var msg in _conn.SubscribeAsync<string>(Subject, cancellationToken: token).ConfigureAwait(false))
                {
                    if (token.IsCancellationRequested) break;
                    HandlePacket(msg.Data);
                }
            }
            catch (OperationCanceledException)
            {
                // Dừng theo yêu cầu.
            }
            catch (Exception ex)
            {
                _logger.Error(LogCategory.Config, $"[Dữ liệu] Theo dõi kênh lỗi: {ex.Message}");
            }
        }

        private void HandlePacket(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return;

            try
            {
                // Đọc nhẹ để lấy Type trước. Kênh này còn chở gói vms-success/vms-fail của BE
                // (không có trường "type") nên phải bỏ qua sớm, tránh deserialize sai kiểu.
                using var doc = JsonDocument.Parse(payload);
                if (!doc.RootElement.TryGetProperty("type", out var typeProp)) return;

                var type = typeProp.GetString();
                if (string.IsNullOrWhiteSpace(type)) return;

                // Chỉ quan tâm gói BE trả danh sách thiết bị. Gói request/telemetry là do chính
                // dịch vụ phát ra nên bỏ qua để không tự xử lý lại.
                if (!string.Equals(type, VmsDataType.EquipmentResult, StringComparison.OrdinalIgnoreCase)) return;

                RaiseObserved(type, payload);

                var result = JsonSerializer.Deserialize<VmsEquipmentSyncResult>(payload, JsonRead);
                if (result == null) return;

                if (!string.IsNullOrEmpty(result.RequestId) && _pending.TryGetValue(result.RequestId, out var tcs))
                    tcs.TrySetResult(result);
            }
            catch (Exception ex)
            {
                _logger.Warning(LogCategory.Config, $"[Dữ liệu] Gói tin không đọc được JSON: {ex.Message}");
            }
        }

        private void RaiseObserved(string type, string json)
        {
            try { PacketObserved?.Invoke(type, json); } catch { /* lỗi hiển thị không ảnh hưởng luồng */ }
        }

        public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);
    }
}
