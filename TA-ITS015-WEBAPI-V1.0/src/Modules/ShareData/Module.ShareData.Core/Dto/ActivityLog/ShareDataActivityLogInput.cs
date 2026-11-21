using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Module.ShareData.Core.Dto.ActivityLog
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO phân trang nhật ký hoạt động.
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataPageActivityLogInput : BasePageInput
    {
        public BaseEnums.LogTypeEnum? LogType { get; set; }
        public BaseEnums.ActivityAction? Action { get; set; }
        public BaseEnums.ShareDataTargetType? TargetType { get; set; }
        public string? TargetId { get; set; }
        public string? PartnerId { get; set; }
        public string? SubscriptionId { get; set; }
        public string? SessionId { get; set; }
        public BaseEnums.TransferDirection? TransferDirection { get; set; }
        public string? DatatypeId { get; set; }

        public BaseEnums.SuccessEnums? Success { get; set; }
        public string? OperatorName { get; set; }
        public string? Keyword { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tìm kiếm nhật ký hoạt động (không phân trang, dùng cho xuất báo cáo)
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataActivityLogInput
    {
        public string? ID { get; set; }
        public BaseEnums.LogTypeEnum? LogType { get; set; }
        public BaseEnums.ActivityAction? Action { get; set; }
        public BaseEnums.ShareDataTargetType? TargetType { get; set; }
        public string? TargetId { get; set; }
        public string? PartnerId { get; set; }
        public string? SubscriptionId { get; set; }
        public BaseEnums.SuccessEnums? Success { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? TopN { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy chi tiết 1 bản ghi nhật ký theo ID
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataIdActivityLogInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thống kê nhanh cho 2 tab nhật ký (đếm theo hành động / kết quả)
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataSummaryActivityLogInput
    {
        /// <summary>Nhóm nhật ký: 0 CONFIG | 1 Transfer.</summary>
        public BaseEnums.LogTypeEnum? LogType { get; set; }

        public string? PartnerId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>
    /// Description: DTO lấy danh sách bước (dòng con) của một dòng nhật ký cha theo ID cha
    /// Created date: 02/10/2026
    /// </summary>
    public class ShareDataStepActivityLogInput : BaseIdInput { }
}
