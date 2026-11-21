using System.Text.Json;
using ITS.VMS.Core.Logging;
using ITS.VMS.Core.Messaging;

namespace ITS.VMS.Core.Storage
{
    /// <summary>
    /// Giữ tạm các gói telemetry chưa đẩy được về BE (BE đang tắt, NATS mất kết nối...) và gửi lại sau.
    ///
    /// Vì sao cần: telemetry là số liệu theo thời điểm, mất là mất vĩnh viễn — không đọc lại được quá khứ.
    /// Có trần <see cref="VmsTelemetryOption.BufferMaxPackets"/> để BE chết dài ngày không làm phình đĩa;
    /// vượt trần thì bỏ gói CŨ NHẤT vì số liệu mới có giá trị hơn.
    ///
    /// Ghi ra file để sống qua lần khởi động lại dịch vụ.
    ///
    /// Author: ChuongLP
    /// Created date: 23/09/2026
    /// </summary>
    public sealed class VmsTelemetryBuffer
    {
        private readonly VmsTelemetryOption _options;
        private readonly VmsLogger _logger;
        private readonly object _gate = new();

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private readonly Queue<VmsTelemetryPush> _pending = new();

        public VmsTelemetryBuffer(VmsTelemetryOption options, VmsLogger logger)
        {
            _options = options;
            _logger = logger;
        }

        private string FilePath => ResolveFromBase(_options.BufferFile, "State/vms_telemetry_buffer.json");

        /// <summary>Số gói đang chờ gửi lại.</summary>
        public int Count
        {
            get { lock (_gate) return _pending.Count; }
        }

        /// <summary>Đưa một gói vào cuối hàng chờ. Vượt trần thì bỏ gói cũ nhất.</summary>
        public void Enqueue(VmsTelemetryPush push)
        {
            if (push == null) return;

            var max = _options.BufferMaxPackets > 0 ? _options.BufferMaxPackets : 200;
            var dropped = 0;

            lock (_gate)
            {
                _pending.Enqueue(push);
                while (_pending.Count > max)
                {
                    _pending.Dequeue();
                    dropped++;
                }
            }

            if (dropped > 0)
                _logger.Warning(LogCategory.Control,
                    $"[Đệm] Bộ đệm đầy ({max} gói), đã bỏ {dropped} gói cũ nhất. Kiểm tra BE có đang nhận telemetry không.");

            Save();
        }

        /// <summary>
        /// Lấy lần lượt các gói đang chờ và gửi lại bằng <paramref name="sendAsync"/>.
        /// Gặp gói gửi thất bại thì DỪNG NGAY và trả gói đó lại đầu hàng chờ — giữ đúng thứ tự thời gian,
        /// tránh việc cứ thử hết hàng chờ mỗi chu kỳ khi BE vẫn đang tắt.
        /// </summary>
        public async Task<int> FlushAsync(Func<VmsTelemetryPush, Task<bool>> sendAsync, CancellationToken ct)
        {
            var sent = 0;

            while (!ct.IsCancellationRequested)
            {
                VmsTelemetryPush? push;
                lock (_gate)
                {
                    if (_pending.Count == 0) break;
                    push = _pending.Peek();
                }

                var ok = await sendAsync(push).ConfigureAwait(false);
                if (!ok) break;   // BE vẫn chưa nhận được -> để lại, chu kỳ sau thử tiếp

                lock (_gate)
                {
                    if (_pending.Count > 0) _pending.Dequeue();
                }
                sent++;
            }

            if (sent > 0)
            {
                Save();
                _logger.Success(LogCategory.Control, $"[Đệm] Đã gửi lại {sent} gói telemetry tồn, còn {Count} gói.");
            }

            return sent;
        }

        /// <summary>Nạp hàng chờ từ file lúc khởi động.</summary>
        public void Load()
        {
            try
            {
                var path = FilePath;
                if (!File.Exists(path)) return;

                var items = JsonSerializer.Deserialize<List<VmsTelemetryPush>>(File.ReadAllText(path), Json);
                if (items == null || items.Count == 0) return;

                lock (_gate)
                {
                    _pending.Clear();
                    foreach (var item in items) _pending.Enqueue(item);
                }

                _logger.Info(LogCategory.Control, $"[Đệm] Đã nạp {Count} gói telemetry tồn từ lần chạy trước.");
            }
            catch (Exception ex)
            {
                _logger.Error(LogCategory.Control, $"[Đệm] Không nạp được file đệm: {ex.Message}");
            }
        }

        /// <summary>Ghi hàng chờ xuống file. Lỗi ghi file không được làm gãy vòng lặp telemetry.</summary>
        private void Save()
        {
            try
            {
                var path = FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                List<VmsTelemetryPush> items;
                lock (_gate) { items = _pending.ToList(); }

                File.WriteAllText(path, JsonSerializer.Serialize(items, Json));
            }
            catch (Exception ex)
            {
                _logger.Error(LogCategory.Control, $"[Đệm] Không ghi được file đệm: {ex.Message}");
            }
        }

        private static string ResolveFromBase(string? path, string fallback)
        {
            var value = string.IsNullOrWhiteSpace(path) ? fallback : path.Trim();
            return Path.IsPathRooted(value) ? value : Path.Combine(AppContext.BaseDirectory, value);
        }
    }
}
