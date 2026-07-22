using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Modules.CfgSystem.Core.Entities
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 09/08/2024
    /// Note: Bảng dịch thuật
    /// </summary>
    [SugarTable("SysTerminology", TableDescription = "")]
    public class SysTerminology : EntityTenant
    {
        //SysTerminology {
            // ID varchar(64) pk
            // Code string(256) unique
            // Name string(256) null
            // Value string(256) null
            // Lang varchar(16) def(vi-VN)
            // OrderNo integer null
            // Remark string(1024) null
            // Status integer null def(1)
        //}

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public override string? Code { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Name { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Value { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? Lang { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length1024)]
        public string? Remark { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "0 = Disable, 1 = Enable")]
        public BaseEnums.StatusEnum? Status { get; set; } 
    }
}
