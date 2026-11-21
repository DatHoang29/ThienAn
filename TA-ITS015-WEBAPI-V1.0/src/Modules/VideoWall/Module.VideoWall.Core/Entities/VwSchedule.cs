using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Module.VideoWall.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Schedule
    /// Created date: 14/08/2026
    /// </summary>
    [SugarTable("VwSchedule")]
    public class VwSchedule : EntityTenant
    {
        /// <summary>
        /// Description: Tên lịch trình chạy kịch bản.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Name { get; set; }

        /// <summary>
        /// Description: Mã kịch bản đích TargetSceneId áp dụng khi lịch kích hoạt.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? TargetSceneId { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? ScheduleType { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Time{ get; set; }
        
        /// <summary>
        /// Description: Danh sách các ngày trong tuần áp dụng lịch trình (Weekdays).
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Weekdays { get; set; }

        /// <summary>
        /// Description: Ngày kích hoạt đối với lịch trình chạy một lần (OnceDate).
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? OnceDate { get; set; }

        /// <summary>
        /// Description: Hành động thực hiện khi lịch trình được kích hoạt (Action).
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Action { get; set; }

        /// <summary>
        /// Description: Trạng thái hoạt động của lịch trình (Status).
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status { get; set; }

        /// <summary>
        /// Description: Thời điểm dự kiến chạy lịch trình lần kế tiếp (NextRun).
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public DateTime? NextRun { get; set; }

        /// <summary>
        /// Description: Thời điểm chạy lịch trình lần gần nhất (LastRun).
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? LastRun { get; set; }

        /// <summary>
        /// Description: Ghi chú hoặc thông tin bổ sung về lịch trình.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}