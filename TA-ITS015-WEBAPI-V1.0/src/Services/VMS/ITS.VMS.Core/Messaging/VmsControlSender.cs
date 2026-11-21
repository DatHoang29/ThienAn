using System.Collections.Concurrent;
using System.Text.Json;
using ITS.VMS.Core.Logging;
using NATS.Client.Core;

namespace ITS.VMS.Core.Messaging
{
    /// <summary>
    /// Bên GỬI lệnh trên kênh ta.its.control.vms — đóng vai BE để kiểm thử và giám sát luồng.
    /// Phát gói {Type="request"} rồi CHỜ gói {Type="result"} khớp RequestId, hết hạn thì trả null.
    /// Cơ chế đối chiếu (RequestId -> TaskCompletionSource) giống VmsControlBridge phía BE,
    /// nên kết quả chạy trên form phản ánh đúng những gì BE sẽ nhận được.
    ///
    /// Ngoài ra phát sự kiện <see cref="PacketObserved"/> cho MỌI gói tin đi qua kênh (cả 2 chiều)
    /// để form hiển thị nguyên văn JSON, phục vụ theo dõi luồng.
    ///
    /// Author: ChuongLP
    /// Created date: 22/09/2026
    /// </summary>
    public sealed class VmsControlSender : IAsyncDisposable
    {
        private readonly VmsControlNatsOption _options;
        private readonly VmsLogger _logger;

        private static readonly JsonSerializerOptions JsonRead = new(JsonSerializerDefaults.Web);
        private static readonly JsonSerializerOptions JsonWrite = new(JsonSerializerDefaults.Web) { WriteIndented = false };

        /// <summary>Các lệnh đang chờ kết quả, khóa là RequestId.</summary>
        private readonly ConcurrentDictionary<string, TaskCompletionSource<VmsControlResult>> _pending = new();

        private NatsConnection? _conn;
        private CancellationTokenSource? _cts;
        private Task? _loop;

        /// <summary>Phát mỗi khi có gói tin đi qua kênh: (loại gói "request"/"result"/"?", JSON nguyên văn).</summary>
        public event Action<string, string>? PacketObserved;

        public VmsControlSender(VmsControlNatsOption options, VmsLogger logger)
        {
            _options = options;
            _logger = logger;
        }

        /// <summary>Đã kết nối và đang theo dõi kênh hay chưa.</summary>
        public bool IsConnected => _conn is not null && _loop is { IsCompleted: false };

        /// <summary>
        /// Trần payload (byte) do NATS server báo về sau khi kết nối. 0 = chưa kết nối / server không báo.
        /// Dùng để xác minh có nhúng base64 được hay không thay vì đoán theo mặc định 1MB.
        /// </summary>
        public long ServerMaxPayload { get; private set; }

        /// <summary>Kênh đang dùng.</summary>
        public string Subject => VmsNatsFactory.ResolveSubject(_options);

        /// <summary>Kết nối NATS và bắt đầu theo dõi kênh. Gọi lại nhiều lần an toàn.</summary>
        public async Task StartAsync()
        {
            if (IsConnected) return;

            _logger.Info(LogCategory.Control, $"[Gửi lệnh] Đang kết nối {_options.Url} ...");

            var conn = new NatsConnection(VmsNatsFactory.BuildOpts(_options, "SENDER"));
            await conn.ConnectAsync().ConfigureAwait(false);

            _conn = conn;
            _cts = new CancellationTokenSource();
            _loop = Task.Run(() => ObserveAsync(_cts.Token));

            // Đọc trần payload THẬT của hạ tầng: quyết định ảnh base64 có gửi được hay không.
            try
            {
                ServerMaxPayload = conn.ServerInfo?.MaxPayload ?? 0;
            }
            catch
            {
                ServerMaxPayload = 0;
            }

            _logger.Success(LogCategory.Control,
                $"[Gửi lệnh] ĐÃ KẾT NỐI {_options.Url}. Theo dõi kênh '{Subject}'." +
                (ServerMaxPayload > 0 ? $" Trần payload server báo về: {ServerMaxPayload / 1024} KB." : string.Empty));
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

            _logger.Info(LogCategory.Control, "[Gửi lệnh] Đã ngắt kết nối.");
        }

        /// <summary>
        /// Phát lệnh và chờ kết quả. Trả về null khi quá thời gian chờ (dịch vụ chưa bật / thiết bị không phản hồi).
        /// </summary>
        /// <param name="request">Gói lệnh; tự gán Type và RequestId nếu còn trống.</param>
        /// <param name="timeoutSeconds">Số giây chờ; &lt;= 0 sẽ dùng DeviceTimeoutSeconds + 5.</param>
        public async Task<VmsControlResult?> SendAsync(VmsControlRequest request, int timeoutSeconds = 0)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (_conn is null) throw new InvalidOperationException("Chưa kết nối NATS. Bấm 'Kết nối' trước khi gửi lệnh.");

            request.Type = VmsControlType.Request;
            if (string.IsNullOrWhiteSpace(request.RequestId)) request.RequestId = Guid.NewGuid().ToString("N");
            request.RequestTime = DateTime.Now;

            // Chờ lâu hơn dịch vụ một nhịp để phân biệt "thiết bị lỗi" với "dịch vụ không phản hồi".
            var wait = timeoutSeconds > 0 ? timeoutSeconds : Math.Max(5, _options.DeviceTimeoutSeconds) + 5;

            var tcs = new TaskCompletionSource<VmsControlResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[request.RequestId] = tcs;

            try
            {
                var json = JsonSerializer.Serialize(request, JsonWrite);
                await _conn.PublishAsync(Subject, json, cancellationToken: CancellationToken.None).ConfigureAwait(false);

                _logger.Info(LogCategory.Control,
                    $"[Gửi lệnh] Đã phát · RequestId={request.RequestId} · Action={request.Action} · " +
                    $"{request.Targets?.Count ?? 0} thiết bị" + (request.Simulate ? " · GIẢ LẬP" : string.Empty));

                var finished = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(wait))).ConfigureAwait(false);
                if (finished == tcs.Task) return tcs.Task.Result;

                _logger.Error(LogCategory.Control,
                    $"[Gửi lệnh] TIMEOUT sau {wait}s · RequestId={request.RequestId} · " +
                    "Kiểm tra dịch vụ đã bật chưa và có cùng kênh/NATS không.");
                return null;
            }
            finally
            {
                _pending.TryRemove(request.RequestId, out _);
            }
        }

        /// <summary>Vòng theo dõi kênh: đối chiếu gói result và bắn sự kiện cho form.</summary>
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
                _logger.Error(LogCategory.Control, $"[Gửi lệnh] Theo dõi kênh lỗi: {ex.Message}");
            }
        }

        private void HandlePacket(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return;

            var type = "?";
            try
            {
                // Đọc nhẹ để biết loại gói trước khi deserialize đầy đủ.
                using var doc = JsonDocument.Parse(payload);
                if (doc.RootElement.TryGetProperty("type", out var typeProp))
                    type = typeProp.GetString() ?? "?";

                if (string.Equals(type, VmsControlType.Result, StringComparison.OrdinalIgnoreCase))
                {
                    var result = JsonSerializer.Deserialize<VmsControlResult>(payload, JsonRead);
                    if (result != null && !string.IsNullOrEmpty(result.RequestId)
                        && _pending.TryGetValue(result.RequestId, out var tcs))
                    {
                        tcs.TrySetResult(result);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(LogCategory.Control, $"[Gửi lệnh] Gói tin không đọc được JSON: {ex.Message}");
            }

            // Luôn báo cho form để xem nguyên văn, kể cả gói lỗi định dạng.
            try { PacketObserved?.Invoke(type, payload); } catch { /* lỗi hiển thị không được ảnh hưởng luồng */ }
        }

        public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);
    }
}
