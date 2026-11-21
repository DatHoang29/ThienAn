using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 25/11/2024
    /// </summary>
    [SugarTable("TmsVehicleFilter")]
    public class TmsVehicleFilter : EntityTenant
    {
        //TmsVehicleFilter {
        // ID varchar(64) pk
        // LicensePlate string(32) null
        // StartDate datetime null
        // EndDate datetime null
        // Type string(16) null
        // Alarm string(16) null
        // Status integer null
        // Remark string(256) null
        //}

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? LicensePlate { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? StartDate { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? EndDate { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? Type { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "0 = Disable, 1 = Enable")]
        public BaseEnums.StatusEnum? Status { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? Alarm { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}
