using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Chi tiết lịch trực - Danh sách người dùng liên quan
    /// Author: System
    /// Created date: 07/11/2025
    /// </summary>
    [SugarTable("TmsDutyScheduleDetail")]
    [SugarIndex("index_{table}_DS", nameof(DutyScheduleId), OrderByType.Asc)]
    [SugarIndex("index_{table}_U", nameof(UserId), OrderByType.Asc)]
    [SugarIndex("index_{table}_S", nameof(Status), OrderByType.Asc)]
    [SugarIndex("index_{table}_R", nameof(Role), OrderByType.Asc)]
    public class TmsDutyScheduleDetail : EntityTenant
    {
        /// <summary>
        /// ID lịch trực (FK → TmsDutySchedule)
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID lịch trực")]
        public string DutyScheduleId { get; set; } = string.Empty;

        /// <summary>
        /// ID người dùng
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID người dùng")]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Vai trò: Leader, Member
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Vai trò")]
        public string? Role { get; set; }

        /// <summary>
        /// Trạng thái: Active, Swapped, Absent, Replaced
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Trạng thái")]
        public string? Status { get; set; }

        /// <summary>
        /// ID người dùng đã swap (nếu có)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID người được swap")]
        public string? SwappedFromUserId { get; set; }

        /// <summary>
        /// Lý do swap/thay đổi
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "Lý do")]
        public string? Reason { get; set; }

        /// <summary>
        /// Ghi chú
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength, ColumnDescription = "Ghi chú")]
        public string? Remark { get; set; }
    }
}
