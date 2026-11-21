using Services.Shared.Messaging;
using ShareDataWorker.Infrastructure.Services.DataOutbound;

namespace ShareDataWorker.Infrastructure.Services.DataNats
{
    /// <summary>
    /// Description: Dịch vụ lắng nghe tín hiệu NATS báo có dữ liệu mới và điều phối luồng gửi tức thì.
    /// Created date: 28/09/2026
    /// Modified date: 10/11/2026
    /// </summary>
    public class DataNatsService(
        DataOutboundService DataOutboundService,
        ILogger<DataNatsService> Logger,
        TransportManager Transport,
        IConfiguration? Configuration = null)
    {
        /// <summary>
        /// Description: NATS subject mặc định dùng để lắng nghe thông báo có dữ liệu mới.
        /// Created date: 25/09/2026
        /// </summary>
        public const string DEFAULT_NATS_SUBJECT = "ta.its.event.sharedata.newdata";

        private readonly string _natsSubject = Configuration?["Nats:NewDataSubject"]
            ?? Configuration?["Nats:Subject"]
            ?? DEFAULT_NATS_SUBJECT;

        /// <summary>
        /// Description: Khởi tạo kết nối NATS và đăng ký lắng nghe trên NATS subject.
        /// Created date: 25/09/2026
        /// Modified date: 10/11/2026
        /// </summary>
        public async Task InitSubscribe(CancellationToken cancelToken = default)
        {
            await Transport.ConnectAsync(cancelToken);

            // Đăng ký cả khi NATS chưa nối được: tầng kết nối giữ đăng ký và tự lắng nghe khi nối được, không phải khởi động lại dịch vụ.
            var subject = _natsSubject;
            await Transport.SubscribeAsync(
                subject,
                payload => HandleMessages(payload, cancelToken),
                cancelToken);

            if (Transport.IsConnected)
            {
                Logger.LogInformation("📡 [DataNatsService] Đã kết nối NATS thành công.");
                Logger.LogInformation("👂 [DataNatsService] Đã đăng ký lắng nghe NATS subject: '{Subject}'", subject);
            }
            else
            {
                Logger.LogWarning("⚠️ [DataNatsService] NATS chưa kết nối hoặc chưa cấu hình URL. Đã đăng ký NATS subject '{Subject}', sẽ lắng nghe khi kết nối được.", subject);
            }
        }

        /// <summary>
        /// Description: Xử lý payload JSON nhận được từ NATS, phân giải mã loại gói tin và gọi DataOutboundService.
        /// Created date: 25/09/2026
        /// </summary>
        private async Task HandleMessages(string payloadJson, CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            try
            {
                string? packetCode = null;

                using (var doc = JsonDocument.Parse(payloadJson))
                {
                    if (doc.RootElement.TryGetProperty("PacketCode", out var pEl) ||
                        doc.RootElement.TryGetProperty("Type", out pEl) ||
                        doc.RootElement.TryGetProperty("PacketType", out pEl))
                    {
                        packetCode = pEl.GetString();
                    }
                }

                if (string.IsNullOrWhiteSpace(packetCode))
                {
                    Logger.LogWarning("⚠️ Nhận payload trigger NATS không có thông tin loại gói (PacketCode/Type) hợp lệ: {Payload}", payloadJson);
                    return;
                }

                Logger.LogInformation("⚡ [NATS Trigger] Nhận sự kiện có dữ liệu mới cho gói tin [{PacketCode}]", packetCode);

                await DataOutboundService.ProcessSubscriptions(packetCode, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "❌ Lỗi không xác định khi xử lý tín hiệu NATS trigger: {Message}", ex.Message);
            }
        }
    }
}
