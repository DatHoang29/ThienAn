using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Lịch sử thay đổi lịch bảo trì
    /// Author: System
    /// Created date: 10/12/2025
    /// </summary>
    [SugarTable("TmsMaintenanceHistory")]
    [SugarIndex("index_{table}_MS", nameof(MaintenanceScheduleId), OrderByType.Asc)]
    [SugarIndex("index_{table}_CT", nameof(CreateTime), OrderByType.Desc)]
    [SugarIndex("index_{table}_AT", nameof(ActionType), OrderByType.Asc)]
    public class TmsMaintenanceHistory : EntityTenant
    {
        /// <summary>
        /// ID lịch bảo trì (FK → TmsMaintenanceSchedule)
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID lịch bảo trì")]
        public string MaintenanceScheduleId { get; set; } = string.Empty;

        /// <summary>
        /// Loại thay đổi: Create, Update, Delete, StatusChange, AssignTask, CompleteTask, AddEquipment, RemoveEquipment
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length32, ColumnDescription = "Loại thay đổi")]
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        /// ID người thực hiện
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID người thực hiện")]
        public string ActionBy { get; set; } = string.Empty;

        /// <summary>
        /// ID người bị ảnh hưởng (nếu có)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID người bị ảnh hưởng")]
        public string? AffectedUserId { get; set; }

        /// <summary>
        /// ID thiết bị bị ảnh hưởng (nếu có)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID thiết bị bị ảnh hưởng")]
        public string? AffectedEquipmentId { get; set; }

        /// <summary>
        /// Dữ liệu trước khi thay đổi (JSON)
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = "text", ColumnDescription = "Dữ liệu trước thay đổi")]
        public string? OldData { get; set; }

        /// <summary>
        /// Dữ liệu sau khi thay đổi (JSON)
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = "text", ColumnDescription = "Dữ liệu sau thay đổi")]
        public string? NewData { get; set; }

        /// <summary>
        /// Mô tả chi tiết thay đổi
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "Mô tả thay đổi")]
        public string? Description { get; set; }

        /// <summary>
        /// Lý do thay đổi
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "Lý do thay đổi")]
        public string? Reason { get; set; }

        /// <summary>
        /// IP Address của người thực hiện
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "IP Address")]
        public string? IpAddress { get; set; }

        /// <summary>
        /// User Agent
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256, ColumnDescription = "User Agent")]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Ghi chú
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength, ColumnDescription = "Ghi chú")]
        public string? Remark { get; set; }
    }
}
