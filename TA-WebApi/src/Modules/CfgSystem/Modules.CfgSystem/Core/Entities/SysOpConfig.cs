using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Modules.CfgSystem.Core.Entities
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 09/08/2024
    /// </summary>
    [SugarTable("SysOpConfig")]
    public class SysOpConfig : EntityTenant
    {
        
        //SysOpConfig {
	       // ID varchar(64) pk
	       // Code string(128) null
	       // GroupCode string(64) null
	       // Name string(64) null
	       // Value string(128) null
	       // IsSysConfig boolean null def(true)
	       // OrderNo integer null
	       // Status integer null
	       // Remark string(1024) null
        //}

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? GroupCode { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Name { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Value { get; set; }

        [SugarColumn(IsNullable = true)]
        public bool? IsSysConfig { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length1024)]
        public string? Remark { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "0 = Disable, 1 = Enable")]
        public BaseEnums.StatusEnum? Status { get; set; }
    }
}
