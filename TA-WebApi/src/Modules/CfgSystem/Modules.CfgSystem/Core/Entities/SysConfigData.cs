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
    [SugarTable("SysConfigData")]
    public class SysConfigData : EntityTenant
    {
        //SysConfigData {
            // ID varchar(64) pk
            // ConfigTypeId varchar(64) null > SysConfigType.ID
            // Name string(256) null
            // Value string(128) null
            // StyleSetting string(512) null
            // ClassSetting string(512) null
            // OrderNo integer null
            // Remark string(256) null
            // Status integer null
            // TagType string(16) null
        //}

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? ConfigTypeId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Name { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Value { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? TagType { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? StyleSetting { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? ClassSetting { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "0 = Disable, 1 = Enable")]
        public BaseEnums.StatusEnum? Status { get; set; } 
    }
}
