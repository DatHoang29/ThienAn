using System.Text.Json;
using ShareDataWorker.Infrastructure.Logging;

namespace ShareDataWorker.Infrastructure.Services.DataInbound
{
    /// <summary>
    /// Ghi nhật ký và cảnh báo cho luồng nhận. Dùng chung bảng với luồng gửi,
    /// phân biệt bằng TransferDirection = RCV và Action = Receive.
    /// Author: HieuNV
    /// Created date: 07/09/2026
    /// </summary>
    public partial class DataInboundService
    {
        /// <summary>
        /// Author: DatHQ
        /// Description: Ghi log mức Information, mức này đang tắt thì bỏ qua.
        /// Created date: 27/08/2026
        /// </summary>
        private void LogInformationMsg(string message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation(message, args);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Ghi log mức Warning, mức này đang tắt thì bỏ qua.
        /// Created date: 27/08/2026
        /// </summary>
        private void LogWarningMsg(string message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Warning))
                logger.LogWarning(message, args);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Ghi log mức Error kèm ngoại lệ, mức này đang tắt thì bỏ qua.
        /// Created date: 07/09/2026
        /// </summary>
        private void LogErrorMsg(Exception? ex, string message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Error))
                logger.LogError(ex, message, args);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Ghi kết quả XỬ LÝ một gói tin vào ShareDataActivityLog (Bước 2: Ánh xạ & Lưu CSDL)
        ///              đồng thời cập nhật kết quả cuối cùng lên dòng cha (nếu có ReceiveLogId).
        /// Created date: 07/09/2026
        /// </summary>
        private static async Task<string> LogInboundResult(
            ISqlSugarClient db,
            ShareDataSubscription sub,
            ShareDataPartner? partner,
            ShareDataInboundPacket packet,
            long recordCount,
            string? status,
            string? errorMessage,
            string? description,
            string? mappingId,
            string? parentSummary = null)
        {
            var isSuccess = status == ShareDataEnum.ExportStatus.Success;

            const int stepMapAndStore = 2;
            var stepNbr = packet.ReceiveLogId == null ? (int?)null : stepMapAndStore;

            var desc = stepNbr != null && !string.IsNullOrWhiteSpace(description) && !description.StartsWith("Bước ")
                ? $"Bước {stepNbr}/2: {description}"
                : description;

            var logId = await ShareDataTransferLog.WriteActivityAsync(
                db, sub, partner,
                BaseEnums.TransferDirection.RCV,
                recordCount, packet.ByteSize ?? 0, null, status, errorMessage, null, mappingId, packet.PacketVersion,
                serialNbr: packet.SerialNbr,
                pduType: packet.PduType,
                description: desc,
                parentId: packet.ReceiveLogId,
                stepNbr: stepNbr);

            if (!string.IsNullOrWhiteSpace(packet.ReceiveLogId))
            {
                var partnerName = partner?.Name ?? packet.PartnerCode;
                var parentDesc = isSuccess
                    ? $"Nhận gói tin loại {packet.PacketCode} từ đối tác \"{partnerName}\" — {parentSummary ?? description}"
                    : $"Lỗi nhận gói tin loại {packet.PacketCode} từ đối tác \"{partnerName}\": {errorMessage}";

                await ShareDataTransferLog.WriteActivityAsync(
                    db,
                    sub,
                    partner,
                    BaseEnums.TransferDirection.RCV,
                    recordCount: recordCount,
                    byteSize: packet.ByteSize ?? 0,
                    status: status,
                    errorMessage: errorMessage,
                    description: parentDesc,
                    logId: packet.ReceiveLogId,
                    isUpdate: true);
            }

            return logId;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Ghi cảnh báo luồng nhận. Đăng ký có thể null (gói chưa có đăng ký nhận).
        /// Created date: 07/09/2026
        /// </summary>
        private static Task<string> LogAlertInbound(
            ISqlSugarClient db,
            ShareDataSubscription? sub,
            ShareDataPartner? partner,
            string alertCode,
            BaseEnums.AlertSeverity severity,
            string message,
            object? detail = null)
            => ShareDataTransferLog.WriteAlertAsync(
                db, sub, alertCode, severity, BaseEnums.AlertSource.Subscription, message,
                detail == null ? null : JsonSerializer.Serialize(detail),
                partner);
    }
}
