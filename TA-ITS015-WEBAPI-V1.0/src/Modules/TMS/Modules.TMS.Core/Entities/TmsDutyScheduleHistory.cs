using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Lịch sử thay đổi lịch trực
    /// Author: System
    /// Created date: 20/11/2025
    /// </summary>
    [SugarTable("TmsDutyScheduleHistory")]
    [SugarIndex("index_{table}_DS", nameof(DutyScheduleId), OrderByType.Asc)]
    [SugarIndex("index_{table}_CT", nameof(CreateTime), OrderByType.Desc)]
    [SugarIndex("index_{table}_AT", nameof(ActionType), OrderByType.Asc)]
    public class TmsDutyScheduleHistory : EntityTenant
    {
        /// <summary>
        /// ID lịch trực (FK → TmsDutySchedule)
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID lịch trực")]
        public string DutyScheduleId { get; set; } = string.Empty;

        /// <summary>
        /// ID yêu cầu thay đổi (FK → TmsDutyScheduleChangeRequest, nếu có)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID yêu cầu thay đổi")]
        public string? ChangeRequestId { get; set; }

        /// <summary>
        /// ID lịch trực liên quan (dùng cho Swap, Transfer - lịch trực đích/nguồn)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID lịch trực liên quan")]
        public string? RelatedScheduleId { get; set; }

        /// <summary>
        /// Loại thay đổi: Create, Update, Delete, Transfer, Swap, Approve, Reject, Cancel
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Loại thay đổi")]
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
        /// Dữ liệu trước khi thay đổi (JSON)
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = "text", ColumnDescription = "Dữ liệu trước khi thay đổi")]
        public string? OldData { get; set; }

        /// <summary>
        /// Dữ liệu sau khi thay đổi (JSON)
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = "text", ColumnDescription = "Dữ liệu sau khi thay đổi")]
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
