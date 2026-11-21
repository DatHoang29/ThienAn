using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Cảnh báo bảo trì (tạo tự động hoặc thủ công)
    /// Author: System
    /// Created date: 10/12/2025
    /// </summary>
    [SugarTable("TmsMaintenanceAlert")]
    [SugarIndex("index_{table}_E", nameof(EquipmentId), OrderByType.Asc)]
    [SugarIndex("index_{table}_S", nameof(Status), OrderByType.Asc)]
    [SugarIndex("index_{table}_P", nameof(Priority), OrderByType.Asc)]
    [SugarIndex("index_{table}_CT", nameof(CreateTime), OrderByType.Desc)]
    public class TmsMaintenanceAlert : EntityTenant
    {
        /// <summary>
        /// ID thiết bị (FK → TmsEquipment)
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID thiết bị")]
        public string EquipmentId { get; set; } = string.Empty;

        /// <summary>
        /// ID rule đã trigger cảnh báo (FK → TmsMaintenanceAlertRule, nullable nếu tạo thủ công)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID rule")]
        public string? AlertRuleId { get; set; }

        /// <summary>
        /// Loại cảnh báo: rule_triggered, manual_request, schedule_due
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length32, ColumnDescription = "Loại cảnh báo")]
        public string AlertType { get; set; } = "rule_triggered";

        /// <summary>
        /// Tiêu đề cảnh báo
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length256, ColumnDescription = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Mô tả chi tiết cảnh báo
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = "text", ColumnDescription = "Mô tả")]
        public string? Description { get; set; }

        /// <summary>
        /// Số lỗi được phát hiện (nếu từ rule)
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Số lỗi")]
        public int? ErrorCount { get; set; }

        /// <summary>
        /// Mức độ ưu tiên: high, medium, low
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length16, ColumnDescription = "Mức độ ưu tiên")]
        public string Priority { get; set; } = "medium";

        /// <summary>
        /// ID hướng dẫn bảo trì gợi ý (FK → TmsMaintenanceGuide)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID guide gợi ý")]
        public string? SuggestedGuideId { get; set; }

        /// <summary>
        /// Trạng thái: pending, acknowledged, schedule, dismissed
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length32, ColumnDescription = "Trạng thái")]
        public string Status { get; set; } = "pending";

        /// <summary>
        /// ID lịch bảo trì đã tạo từ alert này (FK → TmsMaintenanceSchedule)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID lịch bảo trì")]
        public string? ScheduleId { get; set; }

        /// <summary>
        /// Người xác nhận cảnh báo
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "Người xác nhận")]
        public string? AcknowledgedBy { get; set; }

        /// <summary>
        /// Thời điểm xác nhận
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Thời điểm xác nhận")]
        public DateTime? AcknowledgedTime { get; set; }

        /// <summary>
        /// Người bỏ qua cảnh báo
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "Người bỏ qua")]
        public string? DismissedBy { get; set; }

        /// <summary>
        /// Thời điểm bỏ qua
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Thời điểm bỏ qua")]
        public DateTime? DismissedTime { get; set; }

        /// <summary>
        /// Lý do bỏ qua
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "Lý do bỏ qua")]
        public string? DismissReason { get; set; }

        /// <summary>
        /// Thông báo cảnh báo
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "Thông báo")]
        public string? AlertMessage { get; set; }

        /// <summary>
        /// Số ngày của kỳ kiểm tra lỗi
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Số ngày kỳ kiểm tra")]
        public int? ErrorPeriodDays { get; set; }

        /// <summary>
        /// Người tạo alert thủ công
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Người tạo")]
        public long? CreatedByUserId { get; set; }

        /// <summary>
        /// Ghi chú
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength, ColumnDescription = "Ghi chú")]
        public string? Remark { get; set; }
    }
}
