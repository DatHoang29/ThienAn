using Shared.DTO.Enums;

namespace ShareDataWorker.Core.Exceptions
{
    /// <summary>
    /// Ngoại lệ nghiệp vụ dùng chung cho toàn hệ ShareData (Outbound và Inbound).
    /// Mang mã cảnh báo ESH, mức độ nghiêm trọng và nguồn để boundary
    /// duy nhất (LockedSubscription / LockedInbound) ghi đúng alert/activity
    /// mà không cần biết loại lỗi cụ thể bên trong pipeline.
    /// Author: Đạt
    /// Created date: 24/09/2026
    /// </summary>
    public sealed class ShareDataException : Exception
    {
        /// <summary>Mã cảnh báo ESH (ví dụ: "ESH-1302").</summary>
        public string AlertCode { get; }

        /// <summary>Mức độ nghiêm trọng của cảnh báo.</summary>
        public BaseEnums.AlertSeverity Severity { get; }

        /// <summary>Nguồn phát sinh cảnh báo (Subscription / Funnel / Protocol).</summary>
        public BaseEnums.AlertSource AlertSource { get; }

        /// <summary>Số bản ghi đã xử lý ở trang hiện tại (nếu có — dùng khi lỗi xảy ra trong vòng lặp phân trang).</summary>
        public int? RecordCount { get; init; }

        /// <summary>Kích thước byte file đã ghi (nếu có — dùng khi HTTP send thất bại sau khi file đã ghi).</summary>
        public long? ByteSize { get; init; }

        /// <summary>Payload JSON bổ sung để debug (nếu có — từ response HTTP lỗi).</summary>
        public string? DetailJson { get; init; }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tạo ngoại lệ nghiệp vụ kèm mã cảnh báo ESH, mức độ và nguồn để ghi nhật ký.
        /// Created date: 24/09/2026
        /// </summary>
        public ShareDataException(
            string alertCode,
            BaseEnums.AlertSeverity severity,
            BaseEnums.AlertSource alertSource,
            string message,
            Exception? inner = null)
            : base(message, inner)
        {
            AlertCode = alertCode;
            Severity = severity;
            AlertSource = alertSource;
        }
    }
}
