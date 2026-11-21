using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Lịch trực tuần đường
    /// Author: System
    /// Created date: 07/11/2025
    /// </summary>
    [SugarTable("TmsDutySchedule")]
    [SugarIndex("index_{table}_N", nameof(Name), OrderByType.Asc)]
    [SugarIndex("index_{table}_ST", nameof(StartTime), OrderByType.Desc)]
    [SugarIndex("index_{table}_ET", nameof(EndTime), OrderByType.Desc)]
    [SugarIndex("index_{table}_PT", nameof(PatrolTeamId), OrderByType.Asc)]
    [SugarIndex("index_{table}_S", nameof(Status), OrderByType.Asc)]
    public class TmsDutySchedule : EntityTenant
    {
        /// <summary>
        /// Tên ca trực
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Tên ca trực")]
        public string? Name { get; set; }

        /// <summary>
        /// Thời gian bắt đầu ca
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "Thời gian bắt đầu")]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Thời gian kết thúc ca
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "Thời gian kết thúc")]
        public DateTime EndTime { get; set; }

        /// <summary>
        /// ID đội tuần tra (FK → TmsPatrolTeam)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID đội tuần tra")]
        public string? PatrolTeamId { get; set; }

        /// <summary>
        /// Trạng thái: Draft, InProgress, Completed, Cancelled
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Trạng thái")]
        public string? Status { get; set; }

        /// <summary>
        /// Ghi chú
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength, ColumnDescription = "Ghi chú")]
        public string? Remark { get; set; }
    }
}
