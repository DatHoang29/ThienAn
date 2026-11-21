using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 19/08/2024
    /// </summary>
    [SugarTable("TmsTrafficData")]
    [SugarIndex("index_{table}_T", nameof(DetectTime), OrderByType.Desc)]
    [SugarIndex("index_{table}_Z", nameof(ZoneId),OrderByType.Asc)]
    [SugarIndex("index_{table}_E", nameof(EquipmentId),OrderByType.Asc)]

    public class TmsTrafficData : EntityTenant
    {
        //TmsTrafficData {
        // DetectTime datetime null
        // Location varchar(16) null
        // Lane varchar(10)
        // Speed float(4,1) null
        // IpAddress varchar(32) null
        // Height integer null
        // Width integer null
        // Length integer null
        // AlarmHeight boolean null def(false)
        // AlarmWidth boolean null def(false)
        // ID varchar(64) unique
        // EquipmentId varchar(64) null
        // LicensePlate varchar(32) null
        // Type varchar(16) null
        // Direction varchar(16) null
        // AlarmLength boolean null def(false)
        // AlarmSpeed boolean null def(false)
        // ZoneId varchar(64) null
        //}

        [SugarColumn(IsNullable = true)]
        public DateTime? DetectTime { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? Location { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? Lane { get; set; }

        [SugarColumn(IsNullable = true)]
        public float? Speed { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? IpAddress { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? Height { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? Width { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? Length { get; set; }

        [SugarColumn(IsNullable = true)]
        public bool? AlarmHeight { get; set; } = false;

        [SugarColumn(IsNullable = true)]
        public bool? AlarmWidth { get; set; } = false;

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? EquipmentId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? LicensePlate { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? Type { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? Direction { get; set; }

        [SugarColumn(IsNullable = true)]
        public bool? AlarmLength { get; set; } = false;

        [SugarColumn(IsNullable = true)]
        public bool? AlarmSpeed { get; set; } = false;

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? ZoneId { get; set; }

        // ===== VDS V3: dữ liệu lấy từ payload API v1 =====

        /// <summary>id của bản tin phía VDS — khoá idempotency (UNIQUE theo TenantId).</summary>
        [SugarColumn(IsNullable = false)]
        public long VendorEventId { get; set; }

        /// <summary>type của VDS: Normal / Speeding / Congestion / ... (khác Type = cỡ xe S/M/L).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? EventType { get; set; }

        /// <summary>source: traffic_vds_aid / terminal_aid / ...</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? Source { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? MacAddress { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? PlateColor { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? VehicleBrand { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? VehicleColor { get; set; }

        /// <summary>Loại xe nguyên văn vendor (Truck/SUV/Car/Van/Bus/Motorbike/...).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? VehicleType { get; set; }

        // Không lưu nguyên văn payload ở đây: VdsRawLogger đã ghi toàn bộ frame ra file log thô
        // (không TTL, không xoá) nên giữ thêm một bản trong DB chỉ làm phình bảng.
    }

}
