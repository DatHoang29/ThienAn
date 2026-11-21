using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Lịch bảo trì duy tu (Header)
    /// Author: System
    /// Created date: 10/12/2025
    /// </summary>
    [SugarTable("TmsMaintenanceSchedule")]
    [SugarIndex("index_{table}_C", nameof(Code), OrderByType.Asc)]
    [SugarIndex("index_{table}_SD", nameof(ScheduledDate), OrderByType.Desc)]
    [SugarIndex("index_{table}_S", nameof(Status), OrderByType.Asc)]
    [SugarIndex("index_{table}_P", nameof(Priority), OrderByType.Asc)]
    public class TmsMaintenanceSchedule : EntityTenant
    {
        /// <summary>
        /// Mã lịch bảo trì (auto-generate: MS-{yyyyMMddHHmmss}-{random4})
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length32, ColumnDescription = "Mã lịch bảo trì")]
        public new string Code { get; set; } = string.Empty;

        /// <summary>
        /// Tên lịch bảo trì
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length128, ColumnDescription = "Tên lịch bảo trì")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Ngày dự kiến bắt đầu
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "Ngày dự kiến bắt đầu")]
        public DateTime ScheduledDate { get; set; }

        /// <summary>
        /// Ngày hạn hoàn thành
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Ngày hạn hoàn thành")]
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Mức ưu tiên: high, medium, low
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length16, ColumnDescription = "Mức ưu tiên")]
        public string Priority { get; set; } = "medium";

        /// <summary>
        /// Số ngày nhắc trước lịch bảo trì
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Số ngày nhắc trước")]
        public int? ReminderDaysBefore { get; set; } = 1;

        /// <summary>
        /// Trạng thái: draft, schedule, in_progress, finish, cancel
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length32, ColumnDescription = "Trạng thái")]
        public string Status { get; set; } = "draft";

        /// <summary>
        /// Ngày hoàn thành thực tế
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Ngày hoàn thành")]
        public DateTime? CompletedDate { get; set; }

        /// <summary>
        /// Kết quả/ghi chú hoàn thành
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = "text", ColumnDescription = "Kết quả")]
        public string? Result { get; set; }

        /// <summary>
        /// Đã gửi reminder chưa
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Đã gửi reminder")]
        public bool? ReminderSent { get; set; } = false;

        /// <summary>
        /// Ghi chú
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength, ColumnDescription = "Ghi chú")]
        public string? Remark { get; set; }
    }
}
