using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 19/08/2024
    /// </summary>
    [SugarTable("TmsWorkContact")]
    public class TmsWorkContact : EntityTenant
    {
        //ID varchar(64) pk
        //Code string(64) null
        //Name string(128) null
        //Phone string(32) null
        //Address string(128) null
        //Info string(128) null
        //Type string(16) null
        //RefId varchar(64) null
        //Status integer null
        //Remark string(256) null

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "")]
        public string? Name { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32, ColumnDescription = "")]
        public string? Phone { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "")]
        public string? Address { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "")]
        public string? Info { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "")]
        public string? Type { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "0 = Disable, 1 = Enable")]
        public BaseEnums.StatusEnum? Status { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "")]
        public string? RefId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256, ColumnDescription = "")]
        public string? Remark { get; set; }
    }
}
