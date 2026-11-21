using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Yêu cầu thay đổi lịch trực (swap, absent, replace)
    /// Author: System
    /// Created date: 19/11/2025
    /// </summary>
    [SugarTable("TmsDutyScheduleChangeRequest")]
    [SugarIndex("index_{table}_DS", nameof(DutyScheduleId), OrderByType.Asc)]
    [SugarIndex("index_{table}_RU", nameof(RequesterId), OrderByType.Asc)]
    [SugarIndex("index_{table}_S", nameof(Status), OrderByType.Asc)]
    [SugarIndex("index_{table}_T", nameof(Type), OrderByType.Asc)]
    public class TmsDutyScheduleChangeRequest : EntityTenant
    {
        /// <summary>
        /// ID lịch trực (FK → TmsDutySchedule)
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID lịch trực")]
        public string DutyScheduleId { get; set; } = string.Empty;

        /// <summary>
        /// Loại yêu cầu: Swap (đổi ca), Absent (xin nghỉ), Replace (thay thế)
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Loại yêu cầu")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// ID người yêu cầu
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID người yêu cầu")]
        public string RequesterId { get; set; } = string.Empty;

        /// <summary>
        /// ID người liên quan:
        /// - Swap: Người cần trao đổi lịch
        /// - Replace: Người bị thay thế (phải có trong lịch)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID người liên quan")]
        public string? TargetUserId { get; set; }

        /// <summary>
        /// ID người thay thế (chỉ dùng cho Replace):
        /// - Nếu không chỉ định khi tạo request: Mặc định = người đang thao tác (requester)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID người thay thế")]
        public string? ReplacerId { get; set; }

        /// <summary>
        /// Vai trò mới trong lịch đích (leader/member) - Áp dụng cho Transfer
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Vai trò mới")]
        public string? NewRole { get; set; }

        /// <summary>
        /// ID lịch trực đích (với loại Transfer - chuyển sang lịch khác)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID lịch trực đích")]
        public string? TargetScheduleId { get; set; }

        /// <summary>
        /// Lý do yêu cầu
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "Lý do")]
        public string? Reason { get; set; }

        /// <summary>
        /// Trạng thái: Pending, Approved, Rejected, Cancelled
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Trạng thái")]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// ID người phê duyệt (người tạo lịch hoặc đội trưởng)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID người phê duyệt")]
        public string? ApproverId { get; set; }

        /// <summary>
        /// Thời gian phê duyệt
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Thời gian phê duyệt")]
        public DateTime? ApprovedTime { get; set; }

        /// <summary>
        /// Ghi chú phê duyệt
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "Ghi chú phê duyệt")]
        public string? ApprovalNote { get; set; }

        /// <summary>
        /// Ghi chú
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength, ColumnDescription = "Ghi chú")]
        public string? Remark { get; set; }
    }
}
