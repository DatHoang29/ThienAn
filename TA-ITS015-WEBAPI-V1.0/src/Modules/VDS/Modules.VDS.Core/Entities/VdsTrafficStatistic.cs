using Shared.DTO.Constants.Application;

namespace Modules.VDS.Core.Entities
{
    /// <summary>QuestDB traffic entity with camelCase columns and timestamp soft deletion.</summary>
    [SugarTable("VdsTrafficStatistic")]
    [Tenant("VdsDefault")]
    public class VdsTrafficStatistic : VdsQuestEntityTenant
    {
        // ==================== THỜI GIAN ====================

        /// <summary>
        /// Đầu chu kỳ — designated timestamp của QuestDB, phân vùng theo NGÀY.
        /// <c>IsOnlyIgnoreUpdate</c> vì cột phân vùng không được UPDATE.
        /// </summary>
        [TimeDbSplitField(DateType.Day)]
        [SugarColumn(ColumnName = "fromTime", IsNullable = false, IsOnlyIgnoreUpdate = true)]
        public DateTime FromTime { get; set; }

        /// <summary>Cuối chu kỳ.</summary>
        [SugarColumn(ColumnName = "toTime", IsNullable = true)]
        public DateTime? ToTime { get; set; }

        /// <summary>Độ dài chu kỳ thiết bị gửi (phút).</summary>
        [SugarColumn(ColumnName = "periodMinutes", IsNullable = true)]
        public short? PeriodMinutes { get; set; }

        // ==================== ĐỊNH DANH (cột SYMBOL trên QuestDB) ====================

        /// <summary>
        /// ID bản tin phía VDS. Cùng <see cref="FromTime"/> và <see cref="Lane"/> tạo thành
        /// <c>DEDUP UPSERT KEYS</c> — ghi lại cùng bộ này thì QuestDB ghi đè, không sinh dòng mới.
        /// </summary>
        [SugarColumn(ColumnName = "vendorEventId", IsNullable = false)]
        public long VendorEventId { get; set; }

        [SugarColumn(ColumnName = "cameraIp", IsNullable = true, Length = EntityConst.Length32, ColumnDataType = "symbol")]
        public string? CameraIp { get; set; }

        [SugarColumn(ColumnName = "equipmentId", IsNullable = true, Length = EntityConst.Length64, ColumnDataType = "symbol")]
        public string? EquipmentId { get; set; }

        [SugarColumn(ColumnName = "direction", IsNullable = true, Length = EntityConst.Length16, ColumnDataType = "symbol")]
        public string? Direction { get; set; }

        [SugarColumn(ColumnName = "zoneId", IsNullable = true, Length = EntityConst.Length64, ColumnDataType = "symbol")]
        public string? ZoneId { get; set; }

        /// <summary>Làn; rỗng nghĩa là dòng này gộp mọi làn của bản tin.</summary>
        [SugarColumn(ColumnName = "lane", IsNullable = true, Length = EntityConst.Length16, ColumnDataType = "symbol")]
        public string? Lane { get; set; }

        // ==================== SỐ LƯỢNG ====================

        [SugarColumn(ColumnName = "totalVehicle", IsNullable = true)]
        public int? TotalVehicle { get; set; }

        /// <summary>Số xe cỡ nhỏ. NULL = thiết bị không cấp, 0 = có số liệu và bằng 0.</summary>
        [SugarColumn(ColumnName = "shortVehicle", IsNullable = true)]
        public int? ShortVehicle { get; set; }

        [SugarColumn(ColumnName = "mediumVehicle", IsNullable = true)]
        public int? MediumVehicle { get; set; }

        [SugarColumn(ColumnName = "longVehicle", IsNullable = true)]
        public int? LongVehicle { get; set; }

        /// <summary>Số xe không phân được cỡ.</summary>
        [SugarColumn(ColumnName = "otherVehicle", IsNullable = true)]
        public int? OtherVehicle { get; set; }

        // ==================== ĐO LƯỜNG ====================

        [SugarColumn(ColumnName = "avgSpeed", IsNullable = true)]
        public double? AvgSpeed { get; set; }

        [SugarColumn(ColumnName = "spaceMeanKmh", IsNullable = true)]
        public double? SpaceMeanKmh { get; set; }

        [SugarColumn(ColumnName = "flowVph", IsNullable = true)]
        public int? FlowVph { get; set; }

        [SugarColumn(ColumnName = "densityVpkm", IsNullable = true)]
        public double? DensityVpkm { get; set; }

        [SugarColumn(ColumnName = "spaceOccupy", IsNullable = true)]
        public double? SpaceOccupy { get; set; }

        [SugarColumn(ColumnName = "timeOccupy", IsNullable = true)]
        public double? TimeOccupy { get; set; }

        [SugarColumn(ColumnName = "spaceHeadway", IsNullable = true)]
        public double? SpaceHeadway { get; set; }

        [SugarColumn(ColumnName = "timeHeadway", IsNullable = true)]
        public double? TimeHeadway { get; set; }

        // ==================== VỊ TRÍ ====================

        [SugarColumn(ColumnName = "kmNumber", IsNullable = true)]
        public int? KmNumber { get; set; }

        [SugarColumn(ColumnName = "metNumber", IsNullable = true)]
        public int? MetNumber { get; set; }
    }
}
