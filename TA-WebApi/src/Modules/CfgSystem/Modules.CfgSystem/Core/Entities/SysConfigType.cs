using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Modules.CfgSystem.Core.Entities
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 09/08/2024
    /// Note: Tạo bảng phân loại cấu hình của hệ thống
    /// </summary>
    [SugarTable("SysConfigType")]
    public class SysConfigType: EntityTenant
    {
        //SysConfigType {
            // ID varchar(64) pk
            // Code string(128) null
            // Name string(64) null
            // OrderNo integer null
            // Remark string(256) null
            // Status integer null
        //}

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Name { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "0 = Disable, 1 = Enable")]
        public BaseEnums.StatusEnum? Status { get; set; } 
    }
}
