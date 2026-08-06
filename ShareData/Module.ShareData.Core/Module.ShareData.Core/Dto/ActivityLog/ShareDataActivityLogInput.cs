using Shared.Core.Utilities;

namespace Module.ShareData.Core.Dto.ActivityLog
{
    /// <summary>
    /// Description: DTO phân trang nhật ký hoạt động.
    ///              Tab "Cấu hình" truyền LogType = CONFIG, tab "Truyền nhận" truyền LogType = TRANSFER.
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataPageActivityLogInput : BasePageInput
    {
        /// <summary>CONFIG | TRANSFER. Bỏ trống = lấy cả hai.</summary>
        public string? LogType { get; set; }

        /// <summary>Lọc theo hành động (CREATE, UPDATE, SEND...).</summary>
        public string? Action { get; set; }

        /// <summary>Lọc theo loại đối tượng (Partner, Subscription...).</summary>
        public string? TargetType { get; set; }

        public string? TargetId { get; set; }
        public string? PartnerId { get; set; }
        public string? SubscriptionId { get; set; }
        public string? SessionId { get; set; }

        /// <summary>SND | RCV — chỉ dùng cho tab Truyền nhận.</summary>
        public string? TransferDirection { get; set; }

        public int? DatatypeId { get; set; }

        /// <summary>SUCCESS | FAILED.</summary>
        public string? Status { get; set; }

        /// <summary>Tài khoản thực hiện.</summary>
        public string? OperatorName { get; set; }

        /// <summary>Tìm gần đúng trong nội dung mô tả.</summary>
        public string? Keyword { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>
    /// Description: DTO tìm kiếm nhật ký hoạt động (không phân trang, dùng cho xuất báo cáo)
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataActivityLogInput
    {
        public string? ID { get; set; }
        public string? LogType { get; set; }
        public string? Action { get; set; }
        public string? TargetType { get; set; }
        public string? TargetId { get; set; }
        public string? PartnerId { get; set; }
        public string? SubscriptionId { get; set; }
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        /// <summary>Giới hạn số bản ghi trả về, tránh kéo cả bảng. Mặc định 1000.</summary>
        public int? TopN { get; set; }
    }

    /// <summary>
    /// Description: DTO lấy chi tiết 1 bản ghi nhật ký theo ID
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataIdActivityLogInput : BaseIdInput { }

    /// <summary>
    /// Description: DTO thống kê nhanh cho 2 tab nhật ký (đếm theo hành động / kết quả)
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataSummaryActivityLogInput
    {
        public string? LogType { get; set; }
        public string? PartnerId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
