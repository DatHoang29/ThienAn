using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Công việc/Phân công nhân sự trong lịch bảo trì
    /// Author: System
    /// Created date: 10/12/2025
    /// </summary>
    [SugarTable("TmsMaintenanceTask")]
    [SugarIndex("index_{table}_MS", nameof(MaintenanceScheduleId), OrderByType.Asc)]
    [SugarIndex("index_{table}_U", nameof(AssignedUserId), OrderByType.Asc)]
    [SugarIndex("index_{table}_S", nameof(Status), OrderByType.Asc)]
    [SugarIndex("index_{table}_R", nameof(Role), OrderByType.Asc)]
    public class TmsMaintenanceTask : EntityTenant
    {
        /// <summary>
        /// ID lịch bảo trì (FK → TmsMaintenanceSchedule)
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID lịch bảo trì")]
        public string MaintenanceScheduleId { get; set; } = string.Empty;

        /// <summary>
        /// ID thiết bị cụ thể được phân công (FK → TmsMaintenanceScheduleEquipment, nullable = phụ trách tất cả)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID thiết bị được phân công")]
        public string? ScheduleEquipmentId { get; set; }

        /// <summary>
        /// ID người được phân công
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID người được phân công")]
        public string AssignedUserId { get; set; } = string.Empty;

        /// <summary>
        /// Tên công việc
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "Tên công việc")]
        public string? TaskName { get; set; }

        /// <summary>
        /// Mô tả công việc
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = "text", ColumnDescription = "Mô tả công việc")]
        public string? Description { get; set; }

        /// <summary>
        /// Vai trò: leader, member
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32, ColumnDescription = "Vai trò")]
        public string? Role { get; set; }

        /// <summary>
        /// Trạng thái: pending, finish, skipped
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length32, ColumnDescription = "Trạng thái")]
        public string Status { get; set; } = "pending";

        /// <summary>
        /// Thời gian bắt đầu thực tế
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Thời gian bắt đầu")]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Thời gian kết thúc thực tế
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Thời gian kết thúc")]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Kết quả công việc
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = "text", ColumnDescription = "Kết quả")]
        public string? Result { get; set; }

        /// <summary>
        /// Ghi chú
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength, ColumnDescription = "Ghi chú")]
        public string? Remark { get; set; }
    }
}
