using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Tạo bảng tmsTrafficStatistic thông qua SqlSugar ORM Framework
    /// Created date: 19/08/2024
    /// Note: Tạo bảng chứa thông tin đăng kiểm của phương tiện
    /// </summary>
    [SugarTable("TmsTrafficStatistic", TableDescription = "")]
    public class TmsTrafficStatistic : EntityTenant
    {
        /* - - - - - - - - - - - - - - - - - - - - *
         * tmsTrafficStatistic:
         * 
            ID varchar(64) pk
	        Code string(64) null
	        Name string(128) null
	        Phone string(64) null
	        Address string(128) null
	        Info string(128) null
	        Type string(16) null
	        Status integer null
	        Remark string(256) null
        * - - - - - - - - - - - - - - - - - - - - */

        [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "")]
        public string? Direction { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public int? KmNumber { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public int? MetNumber { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public float? AvgSpeed { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "")]
        public int? LocationDetail { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public DateTime? FromTime { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public DateTime? ToTime { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "")]
        public string? Location { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256, ColumnDescription = "")]
        public string? Remark { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32, ColumnDescription = "")]
        public string? IpAddress { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "")]
        public string? EquipmentId { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public int? TotalVehicleNumber { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public int? SVehicleNumber { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public int? MVehicleNumber { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public int? LVehicleNumber { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public int? OVehicleNumber { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public double? SpaceOccupy { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public double? TimeOccupy { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public double? SpaceHeadway { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public double? TimeHeadway { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public double? StopTime { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public double? QueueLength { get; set; }

        // ===== VDS V3 =====

        /// <summary>id bản tin Statistic phía VDS — khoá idempotency.</summary>
        [SugarColumn(IsNullable = false)]
        public long VendorEventId { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? PeriodMinutes { get; set; }

        /// <summary>ext.flow_vph — xe/giờ.</summary>
        [SugarColumn(IsNullable = true)]
        public int? FlowVph { get; set; }

        /// <summary>ext.density_vpkm — xe/km.</summary>
        [SugarColumn(IsNullable = true)]
        public double? DensityVpkm { get; set; }

        /// <summary>ext.space_mean_kmh.</summary>
        [SugarColumn(IsNullable = true)]
        public double? SpaceMeanKmh { get; set; }

        /// <summary>ext.speed_sample — số mẫu.</summary>
        [SugarColumn(IsNullable = true)]
        public int? SpeedSample { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? ZoneId { get; set; }
    }
}
