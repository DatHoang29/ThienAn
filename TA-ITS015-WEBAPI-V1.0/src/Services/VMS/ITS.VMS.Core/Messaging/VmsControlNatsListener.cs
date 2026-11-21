using System.Collections.Concurrent;
using System.Text.Json;
using ITS.VMS.Core.Integration;
using ITS.VMS.Core.Logging;
using NATS.Client.Core;

namespace ITS.VMS.Core.Messaging
{
    /// <summary>
    /// Lắng nghe kênh NATS ta.its.control.vms để điều khiển / đọc dữ liệu bảng VMS.
    /// Luồng: BE phát {Type="request", RequestId, Action, Targets[]} -> dịch vụ xử lý song song từng thiết bị
    ///   -> phát {Type="result", RequestId, Success, Results[]} lên CÙNG kênh để BE đối chiếu theo RequestId.
    /// Dùng chung 1 kênh 2 chiều nên dịch vụ BỎ QUA mọi gói Type != "request" (tránh tự xử lý lại gói result của mình).
    /// Dịch vụ STATELESS: không chạm CSDL, không ghi VmsCurrent/TmsSignalLog/socket — đó là việc của BE.
    /// Tự kết nối lại vô hạn khi mất kết nối; không ném exception ra ngoài vòng lặp.
    /// Author: ChuongLP
    /// Created date: 22/09/2026
    /// </summary>
    public sealed class VmsControlNatsListener : IAsyncDisposable
    {
        private readonly VmsControlNatsOption _options;
        private readonly VmsDeviceExecutor _executor;
        private readonly VmsLogger _logger;

        private static readonly JsonSerializerOptions JsonRead = new(JsonSerializerDefaults.Web);
        private static readonly JsonSerializerOptions JsonWrite = new(JsonSerializerDefaults.Web) { WriteIndented = false };

        private CancellationTokenSource? _cts;
        private Task? _loop;

        public VmsControlNatsListener(VmsControlNatsOption options, VmsDeviceExecutor executor, VmsLogger logger)
        {
            _options = options;
            _executor = executor;
            _logger = logger;
        }

        /// <summary>Đang lắng nghe hay không.</summary>
        public bool IsListening => _loop is { IsCompleted: false };

        /// <summary>
        /// Bắt đầu lắng nghe (không chặn). Gọi lại nhiều lần an toàn — nếu đang chạy thì bỏ qua
        /// Author: ChuongLP
        /// Date: 22/09/2026
        /// .</summary>
        public void Start()
        {
            if (IsListening) return; // Nếu đã đang lắng nghe thì bỏ qua

            if (!_options.Enabled) // Nếu Nats:Enabled = false thì không lắng nghe
            {
                _logger.Info(LogCategory.Control, "[NATS] Lắng nghe điều khiển VMS đang TẮT (Nats:Enabled = false).");
                return;
            }

            _cts = new CancellationTokenSource(); // Khai báo mới để có token hủy cho vòng lặp
            _loop = Task.Run(() => RunAsync(_cts.Token));
        }

        /// <summary>
        /// Yêu cầu dừng (không chặn).
        /// Author: ChuongLP
        /// Date: 22/09/2026
        /// </summary>
        public void Stop()
        {
            try { _cts?.Cancel(); } catch (ObjectDisposedException) { }
        }

        /// <summary>Dừng và chờ vòng lặp kết thúc hẳn.</summary>
        public async Task StopAsync()
        {
            Stop();
            if (_loop is not null)
            {
                try { await _loop.ConfigureAwait(false); } catch { /* đã ghi log bên trong */ }
            }
            _cts?.Dispose();
            _cts = null;
            _loop = null;
        }

        /// <summary>
        /// Hàm xử lý vòng lặp lắng nghe kênh NATS. Tự kết nối lại vô hạn khi mất kết nối
        /// Author: ChuongLP
        /// Date: 22/09/2026
        /// </summary>
        private async Task RunAsync(CancellationToken token)
        {
            var subject = VmsNatsFactory.ResolveSubject(_options);  // vd: ta.its.control.vms
            var retry = TimeSpan.FromSeconds(_options.RetryIntervalSeconds > 0 ? _options.RetryIntervalSeconds : 5); // Thời gian chờ trước khi thử kết nối lại khi mất kết nối

            while (!token.IsCancellationRequested) // Sử dụng while để tự kết nối lại khi mất kết nối, không ném exception ra ngoài vòng lặp
            {
                try
                {
                    _logger.Info(LogCategory.Control, $"[NATS] Đang kết nối {_options.Url} ...");

                    await using var conn = new NatsConnection(VmsNatsFactory.BuildOpts(_options)); // Tạo kết nối NATS mới cho mỗi lần thử, tránh giữ kết nối cũ khi server restart
                    await conn.ConnectAsync().ConfigureAwait(false);    // Kết nối NATS (có thể throw exception nếu server chưa bật)

                    _logger.Success(LogCategory.Control, $"[NATS] ĐÃ KẾT NỐI {_options.Url}. Lắng nghe kênh '{subject}'.");

                    await foreach (var msg in conn.SubscribeAsync<string>(subject, cancellationToken: token).ConfigureAwait(false)) // Lắng nghe kênh NATS
                    {
                        if (token.IsCancellationRequested) break; // Nếu token hủy thì bỏ qua vòng lặp

                        // Không await tuần tự: 1 gói đang gửi ra thiết bị (chậm) không được chặn gói kế tiếp.
                        _ = HandleMessageSafeAsync(conn, subject, msg.Data, token);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error(LogCategory.Control, $"[NATS] Kết nối/lắng nghe lỗi: {ex.Message}. Thử lại sau {retry.TotalSeconds:0}s.");
                }

                if (token.IsCancellationRequested) break;

                try { await Task.Delay(retry, token).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }

            _logger.Info(LogCategory.Control, "[NATS] Đã dừng lắng nghe điều khiển VMS.");
        }

        /// <summary>Bọc HandleMessageAsync để exception không làm rớt vòng subscribe (task chạy rời).</summary>
        private async Task HandleMessageSafeAsync(NatsConnection conn, string subject, string? payload, CancellationToken ct)
        {
            try
            {
                // Phân tích gói tin; chỉ xử lý Type="request", xử lý song song các thiết bị rồi phát kết quả.
                await HandleMessageAsync(conn, subject, payload, ct).ConfigureAwait(false); 
            }
            catch (Exception ex)
            {
                _logger.Error(LogCategory.Control, $"[NATS] Xử lý gói tin lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Phân tích gói tin; chỉ xử lý Type="request", xử lý song song các thiết bị rồi phát kết quả.
        /// Author: ChuongLP
        /// Date: 22/09/2026
        /// </summary>
        private async Task HandleMessageAsync(NatsConnection conn, string subject, string? payload, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(payload)) return; // Nếu payload null/empty thì bỏ qua

            VmsControlRequest? req; // Gói tin yêu cầu điều khiển VMS (Type="request") từ BE, sẽ được xử lý song song từng thiết bị rồi phát kết quả lên cùng kênh.
            try
            {
                // Deserialize JSON payload thành VmsControlRequest. Nếu payload không hợp lệ thì log lỗi và bỏ qua.
                req = JsonSerializer.Deserialize<VmsControlRequest>(payload, JsonRead);
            }
            catch (Exception ex)
            {
                _logger.Error(LogCategory.Control, $"[NATS] Gói tin không đọc được JSON: {ex.Message}");
                return;
            }

            if (req is null) return;

            // Dùng chung 1 kênh 2 chiều: bỏ qua mọi gói không phải yêu cầu (vd result do chính dịch vụ phát).
            if (!string.Equals(req.Type?.Trim(), VmsControlType.Request, StringComparison.OrdinalIgnoreCase))
                return;

            // Chuẩn hóa rồi GÁN LẠI vào req: executor lấy Action trực tiếp từ req nên phải cùng giá trị.
            var action = string.IsNullOrWhiteSpace(req.Action) ? VmsControlAction.SendContent : req.Action.Trim().ToLowerInvariant();
            req.Action = action;
            var targets = req.Targets?.Where(t => t is not null).ToList() ?? new List<VmsControlTarget>();

            var result = new VmsControlResult { RequestId = req.RequestId, Action = action };

            if (targets.Count == 0)
            {
                result.Success = false;
                result.Message = "Yêu cầu không có thiết bị nào.";
                await PublishResultAsync(conn, subject, result, ct).ConfigureAwait(false);
                return;
            }

            // Ghi rõ nguồn ảnh để khi vận hành biết ngay gói này dùng base64 nhúng hay còn phụ thuộc đường dẫn.
            var imageSource = !string.IsNullOrEmpty(req.ImageBase64)
                ? $" · ảnh: base64 ~{req.ImageBase64!.Length / 1024} KB"
                : (action == VmsControlAction.SendContent ? " · ảnh: theo url" : string.Empty);

            _logger.Info(LogCategory.Control,
                $"[NATS] Nhận yêu cầu · RequestId={req.RequestId} · Action={action} · {targets.Count} thiết bị" +
                imageSource + (req.Simulate ? " · GIẢ LẬP" : string.Empty));

            var bag = new ConcurrentBag<VmsTargetResult>();

            if (req.Simulate)
            {
                // Giả lập: trả thành công ngay, không gọi thiết bị thật (test xuyên suốt luồng NATS).
                foreach (var t in targets)
                {
                    bag.Add(new VmsTargetResult
                    {
                        EquipmentId = t.EquipmentId ?? string.Empty,
                        Success = true,
                        SendImageOk = true,
                        SetBrightnessOk = true,
                        Message = "Giả lập thành công (simulate = true).",
                    });
                }
            }
            else
            {
                var parallel = new ParallelOptions
                {
                    MaxDegreeOfParallelism = _options.MaxParallelDevice > 0 ? _options.MaxParallelDevice : 8,
                    CancellationToken = ct,
                };

                // Mỗi thiết bị một driver riêng; lỗi/timeout của một biển không làm gián đoạn các biển khác.
                await Parallel.ForEachAsync(targets, parallel, async (target, c) =>
                {
                    var one = await _executor.ExecuteAsync(req, target, c).ConfigureAwait(false);
                    bag.Add(one);
                }).ConfigureAwait(false);
            }

            // Giữ đúng thứ tự thiết bị như request để BE dễ đối chiếu khi đọc log.
            result.Results = targets
                .Select(t => bag.FirstOrDefault(r => r.EquipmentId == (t.EquipmentId ?? string.Empty))
                             ?? new VmsTargetResult { EquipmentId = t.EquipmentId ?? string.Empty, Success = false, Message = "Không có kết quả." })
                .ToList();

            var okCount = result.Results.Count(r => r.Success);
            result.Success = okCount == result.Results.Count;
            result.Message = $"Đã xử lý {result.Results.Count} thiết bị (Thành công: {okCount}, Thất bại: {result.Results.Count - okCount})";
            result.FinishedAt = DateTime.Now;

            await PublishResultAsync(conn, subject, result, ct).ConfigureAwait(false);
        }

        /// <summary>Phát kết quả (Type="result") lên cùng kênh cho BE/FE.</summary>
        private async Task PublishResultAsync(NatsConnection conn, string subject, VmsControlResult result, CancellationToken ct)
        {
            try
            {
                var json = JsonSerializer.Serialize(result, JsonWrite);
                await conn.PublishAsync(subject, json, cancellationToken: ct).ConfigureAwait(false);

                _logger.Log(LogCategory.Control, result.Success ? "Success" : "Error",
                    $"[NATS] Đã phát kết quả · RequestId={result.RequestId} · {result.Message}");
            }
            catch (Exception ex)
            {
                _logger.Error(LogCategory.Control, $"[NATS] Không phát được kết quả · RequestId={result.RequestId}: {ex.Message}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
        }
    }
}
