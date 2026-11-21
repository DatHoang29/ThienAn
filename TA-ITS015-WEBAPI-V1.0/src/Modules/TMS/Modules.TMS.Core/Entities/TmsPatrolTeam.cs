using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Đội tuần tra
    /// Author: System
    /// Created date: 07/11/2025
    /// </summary>
    [SugarTable("TmsPatrolTeam")]
    [SugarIndex("index_{table}_N", nameof(Name), OrderByType.Asc)]
    [SugarIndex("index_{table}_C", nameof(Code), OrderByType.Asc)]
    [SugarIndex("index_{table}_S", nameof(Status), OrderByType.Asc)]
    public class TmsPatrolTeam : EntityTenant
    {
        /// <summary>
        /// Tên đội tuần tra
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Tên đội tuần tra")]
        public string? Name { get; set; }

        /// <summary>
        /// Mô tả đội tuần tra
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "Mô tả")]
        public string? Description { get; set; }

        /// <summary>
        /// Trạng thái: 0-Không hoạt động, 1-Hoạt động
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Trạng thái")]
        public BaseEnums.StatusEnum? Status { get; set; }

        /// <summary>
        /// Ghi chú
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength, ColumnDescription = "Ghi chú")]
        public string? Remark { get; set; }
    }
}
