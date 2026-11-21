using Microsoft.Extensions.Logging;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Enums;
using Modules.Integration.Delivery.Base.Models;
using Modules.Integration.Delivery.Senders;

namespace Modules.Integration.Delivery
{
    /// <summary>
    /// Description: Dịch vụ điều phối kết nối gửi dữ liệu tới đối tác (HandleConnection Dispatcher)
    ///              thuần, nhận 3 tham số chuẩn (JsonPayload, SecurityInfo, ConnectionConfig) và rẽ nhánh
    ///              theo ConnectionType (API / Kafka / File / FTP / FTPS) của đối tác.
    /// Created date: 15/10/2026
    /// Modified date: 19/10/2026 (Gỡ IDeliverySender, rẽ nhánh trực tiếp theo lớp cụ thể)
    /// </summary>
    public class DeliveryDispatcher
    {
        private readonly HttpDeliverySender _httpSender;
        private readonly FileDeliverySender _fileSender;
        private readonly FtpDeliverySender _ftpSender;
        private readonly KafkaDeliverySender _kafkaSender;
        private readonly ILogger<DeliveryDispatcher>? _logger;

        public DeliveryDispatcher(
            HttpDeliverySender httpSender,
            FileDeliverySender fileSender,
            FtpDeliverySender ftpSender,
            KafkaDeliverySender kafkaSender,
            ILogger<DeliveryDispatcher>? logger = null)
        {
            _httpSender = httpSender;
            _fileSender = fileSender;
            _ftpSender = ftpSender;
            _kafkaSender = kafkaSender;
            _logger = logger;
        }

        /// <summary>
        /// Description: Điều phối vận chuyển gói tin theo ConnectionType của đối tác nhận.
        /// </summary>
        public async Task<DeliveryResult> Dispatch(DeliveryRequest request, DeliveryContext ctx, CancellationToken ct)
        {
            var connectionType = request.ConnectType;

            if (request.ConnectConfig != null && request.ConnectConfig.ContainsKey(ConnectKeys.RawConfig))
            {
                var invalidMsg = "ConnectionConfig của đối tác không phải JSON Dictionary hợp lệ, hủy gửi và giữ nguyên mốc.";
                _logger?.LogWarning("❌ [HandleConnection] {Message}", invalidMsg);
                return new DeliveryResult
                {
                    Success = false,
                    Channel = connectionType.ToString(),
                    ErrorCode = "InvalidConnectionConfig",
                    ErrorMessage = invalidMsg
                };
            }

            if (connectionType is not (ConnectionType.Api or ConnectionType.File or ConnectionType.Ftp or ConnectionType.Ftps or ConnectionType.Kafka))
            {
                var errorMsg = $"Loại kênh kết nối [{connectionType}] không được hỗ trợ bởi hệ thống.";
                _logger?.LogWarning("❌ [HandleConnection] {Message}", errorMsg);
                return new DeliveryResult
                {
                    Success = false,
                    Channel = connectionType.ToString(),
                    ErrorCode = "UnsupportedConnectionType",
                    ErrorMessage = errorMsg
                };
            }

            _logger?.LogInformation("➡️ [HandleConnection] Điều phối gửi dữ liệu gói [{Packet}] qua kênh [{Channel}] tới đối tác [{Partner}]",
                ctx.PacketCode ?? "Unknown", connectionType, ctx.PartnerCode ?? "Unknown");

            if (connectionType.IsFtps() &&
                request.ConnectConfig != null &&
                !request.ConnectConfig.ContainsKey(ConnectKeys.UseSsl))
            {
                request.ConnectConfig[ConnectKeys.UseSsl] = "true";
            }

            switch (connectionType)
            {
                case ConnectionType.Api:
                    return await _httpSender.Send(request, ctx, ct);
                case ConnectionType.File:
                    return await _fileSender.Send(request, ctx, ct);
                case ConnectionType.Ftp:
                case ConnectionType.Ftps:
                    return await _ftpSender.Send(request, ctx, ct);
                default:
                    return await _kafkaSender.Send(request, ctx, ct);
            }
        }
    }
}
