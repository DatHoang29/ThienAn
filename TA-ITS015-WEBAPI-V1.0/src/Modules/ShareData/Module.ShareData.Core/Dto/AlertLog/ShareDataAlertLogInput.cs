using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Module.ShareData.Core.Dto.AlertLog
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO phân trang danh sách cảnh báo và lỗi
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataPageAlertLogInput : BasePageInput
    {
        /// <summary>Lọc theo khoảng thời gian phát sinh (OccurredAt).</summary>
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public BaseEnums.AlertSeverity? Severity { get; set; }
        public BaseEnums.AlertSource? AlertSource { get; set; }
        public string? AlertCode { get; set; }
        public string? PartnerId { get; set; }
        public string? SessionId { get; set; }
        public string? SubscriptionId { get; set; }
        public string? DatatypeId { get; set; }
        public bool? Acknowledged { get; set; }
        public string? Keyword { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tìm kiếm cảnh báo (không phân trang — dùng cho xuất báo cáo, thẻ số liệu)
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataAlertLogInput
    {
        /// <summary>Lọc theo ID cảnh báo. Bỏ trống thì lấy tất cả (DTO tìm kiếm nên ID KHÔNG bắt buộc).</summary>
        public string? ID { get; set; }
        public BaseEnums.AlertSeverity? Severity { get; set; }
        public BaseEnums.AlertSource? AlertSource { get; set; }
        public string? AlertCode { get; set; }
        public string? PartnerId { get; set; }
        public string? DatatypeId { get; set; }
        public bool? Acknowledged { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? TopN { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy chi tiết 1 cảnh báo theo ID
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataIdAlertLogInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO đếm số cảnh báo chưa xác nhận — phục vụ chấm đỏ trên menu và thẻ số liệu.
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataCountUnackedAlertLogInput
    {
        public string? PartnerId { get; set; }
        public BaseEnums.AlertSeverity? Severity { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thống kê nhanh cho dải thẻ số liệu đầu màn Cảnh báo.
    ///              Tách riêng khỏi ShareDataPageAlertLogInput vì mỗi kiểu message chỉ ứng
    ///              với một handler.
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataSummaryAlertLogInput
    {
        public string? PartnerId { get; set; }
        public BaseEnums.AlertSource? AlertSource { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO xác nhận đã xử lý một hoặc nhiều cảnh báo.
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataAcknowledgeAlertInput
    {
        public List<string> Ids { get; set; } = new();
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO xác nhận toàn bộ cảnh báo chưa xử lý theo bộ lọc hiện tại.
    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataAcknowledgeAllAlertInput
    {
        public string? PartnerId { get; set; }
        public BaseEnums.AlertSeverity? Severity { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
