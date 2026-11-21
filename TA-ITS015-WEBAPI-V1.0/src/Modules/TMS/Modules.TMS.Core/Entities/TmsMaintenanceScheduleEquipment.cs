using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Chi tiết thiết bị trong lịch bảo trì
    /// Author: System
    /// Created date: 10/12/2025
    /// </summary>
    [SugarTable("TmsMaintenanceScheduleEquipment")]
    [SugarIndex("index_{table}_MS", nameof(MaintenanceScheduleId), OrderByType.Asc)]
    [SugarIndex("index_{table}_E", nameof(EquipmentId), OrderByType.Asc)]
    [SugarIndex("index_{table}_S", nameof(Status), OrderByType.Asc)]
    public class TmsMaintenanceScheduleEquipment : EntityTenant
    {
        /// <summary>
        /// ID lịch bảo trì (FK → TmsMaintenanceSchedule)
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID lịch bảo trì")]
        public string MaintenanceScheduleId { get; set; } = string.Empty;

        /// <summary>
        /// ID thiết bị (FK → TmsEquipment)
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID thiết bị")]
        public string EquipmentId { get; set; } = string.Empty;

        /// <summary>
        /// Snapshot: ID loại thiết bị tại thời điểm thêm vào lịch
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "Snapshot - ID loại thiết bị")]
        public string? EquipmentTypeIdSnapshot { get; set; }

        /// <summary>
        /// Snapshot: Tên loại thiết bị tại thời điểm thêm vào lịch
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256, ColumnDescription = "Snapshot - Tên loại thiết bị")]
        public string? EquipmentTypeNameSnapshot { get; set; }

        /// <summary>
        /// Snapshot: Trạng thái thiết bị tại thời điểm thêm vào lịch
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Snapshot - Trạng thái thiết bị")]
        public int? EquipmentStateSnapshot { get; set; }

        /// <summary>
        /// Xác nhận đã kiểm tra thiết bị
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Đã kiểm tra")]
        public bool? IsChecked { get; set; }

        /// <summary>
        /// Người xác nhận đã kiểm tra
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "Người xác nhận đã kiểm tra")]
        public string? CheckedBy { get; set; }

        /// <summary>
        /// Thời điểm xác nhận đã kiểm tra
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Thời điểm xác nhận đã kiểm tra")]
        public DateTime? CheckedAt { get; set; }

        /// <summary>
        /// Ghi chú khi kiểm tra
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = "text", ColumnDescription = "Ghi chú khi kiểm tra")]
        public string? CheckedNote { get; set; }

        /// <summary>
        /// ID hướng dẫn bảo trì áp dụng (FK → TmsMaintenanceGuide)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID hướng dẫn bảo trì")]
        public string? MaintenanceGuideId { get; set; }

        /// <summary>
        /// Trạng thái: pending, in_progress, checked, skipped
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length32, ColumnDescription = "Trạng thái")]
        public string Status { get; set; } = "pending";

        /// <summary>
        /// Kết quả bảo trì thiết bị này
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = "text", ColumnDescription = "Kết quả")]
        public string? Result { get; set; }

        /// <summary>
        /// Ngày hoàn thành
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Ngày hoàn thành")]
        public DateTime? CompletedDate { get; set; }

        /// <summary>
        /// Người hoàn thành
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "Người hoàn thành")]
        public string? CompletedBy { get; set; }

        /// <summary>
        /// Checklist đã hoàn thành (JSON - lưu trạng thái từng item)
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = "text", ColumnDescription = "Checklist đã hoàn thành")]
        public string? CompletedChecklist { get; set; }

        /// <summary>
        /// Ghi chú
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength, ColumnDescription = "Ghi chú")]
        public string? Remark { get; set; }
    }
}
