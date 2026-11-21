using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Thành viên đội tuần tra
    /// Author: System
    /// Created date: 07/11/2025
    /// </summary>
    [SugarTable("TmsPatrolTeamMember")]
    [SugarIndex("index_{table}_PT", nameof(PatrolTeamId), OrderByType.Asc)]
    [SugarIndex("index_{table}_U", nameof(UserId), OrderByType.Asc)]
    [SugarIndex("index_{table}_R", nameof(Role), OrderByType.Asc)]
    public class TmsPatrolTeamMember : EntityTenant
    {
        /// <summary>
        /// ID đội tuần tra (FK → TmsPatrolTeam)
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID đội tuần tra")]
        public string PatrolTeamId { get; set; } = string.Empty;

        /// <summary>
        /// ID người dùng (FK → SysUser)
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID người dùng")]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Vai trò: Leader, Member
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Vai trò")]
        public string? Role { get; set; }

        /// <summary>
        /// Ghi chú
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength, ColumnDescription = "Ghi chú")]
        public string? Remark { get; set; }
    }
}
