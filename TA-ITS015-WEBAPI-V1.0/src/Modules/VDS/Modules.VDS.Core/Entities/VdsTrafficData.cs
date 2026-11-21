using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.VDS.Core.Entities
{
    /// <summary>QuestDB traffic entity with camelCase columns and timestamp soft deletion.</summary>
    [SugarTable("VdsTrafficData")]
    [Tenant("VdsDefault")]
    public class VdsTrafficData : VdsQuestEntityTenant
    {
        // ==================== THỜI GIAN ====================

        /// <summary>
        /// Designated timestamp của QuestDB, phân vùng theo NGÀY.
        /// <c>IsOnlyIgnoreUpdate</c> vì cột phân vùng không được UPDATE.
        /// </summary>
        [TimeDbSplitField(DateType.Day)]
        [SugarColumn(ColumnName = "detectTime", IsNullable = false, IsOnlyIgnoreUpdate = true)]
        public DateTime DetectTime { get; set; }

        // ==================== ĐỊNH DANH (cột SYMBOL trên QuestDB) ====================

        /// <summary>
        /// ID bản tin phía VDS. Cùng với <see cref="DetectTime"/> tạo thành
        /// <c>DEDUP UPSERT KEYS</c> — ghi lại cùng cặp này thì QuestDB ghi đè, không sinh dòng mới.
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

        [SugarColumn(ColumnName = "lane", IsNullable = true, Length = EntityConst.Length16, ColumnDataType = "symbol")]
        public string? Lane { get; set; }

        // ==================== LOẠI SỰ KIỆN ====================

        /// <summary>
        /// Loại sự kiện LƯU TRỮ. Là <c>TmsEventType.Code</c> theo ánh xạ <c>vds_mapping_event_type</c>
        /// (vd <c>Speeding</c> ⇒ <c>speeding</c>, <c>Slow Moving</c> ⇒ <c>chaycham</c>); loại chưa có ánh xạ lưu unknown.
        /// <c>Normal</c> (lượt xe thường) LUÔN giữ nguyên vì đếm lưu lượng và nhánh lưu ảnh dựa vào nó.
        /// Bản ghi cũ giữ nguyên; không tự cập nhật khi đổi ánh xạ.
        /// </summary>
        [SugarColumn(ColumnName = "eventType", IsNullable = true, Length = EntityConst.Length32, ColumnDataType = "symbol")]
        public string? EventType { get; set; }

        /// <summary>
        /// Tên loại phía AID lưu độc lập với mã TMS. Mọi luật trong VDS service (skip, so biển, chống
        /// trùng, nâng sự cố, trạng thái vùng) cấu hình theo tên AID nên phải đọc trường này, không đọc
        /// <see cref="EventType"/>. Bản ghi cũ chưa có giá trị thì đọc fallback EventType.
        /// </summary>
        [SugarColumn(ColumnName = "aidEventType", IsNullable = true, Length = EntityConst.Length64, ColumnDataType = "symbol")]
        public string? AidEventType { get; set; }

        // ==================== PHƯƠNG TIỆN ====================

        [SugarColumn(ColumnName = "licensePlate", IsNullable = true, Length = EntityConst.Length64, ColumnDataType = "varchar")]
        public string? LicensePlate { get; set; }

        [SugarColumn(ColumnName = "vehicleType", IsNullable = true, Length = EntityConst.Length32, ColumnDataType = "symbol")]
        public string? VehicleType { get; set; }

        [SugarColumn(ColumnName = "vehicleColor", IsNullable = true, Length = EntityConst.Length32, ColumnDataType = "symbol")]
        public string? VehicleColor { get; set; }

        /// <summary>Cỡ xe suy từ chiều dài: <c>short</c> (&lt;6m) / <c>medium</c> (6–12m) / <c>long</c> (&gt;12m).</summary>
        [SugarColumn(ColumnName = "vehicleSize", IsNullable = true, Length = EntityConst.Length16, ColumnDataType = "symbol")]
        public string? VehicleSize { get; set; }

        // ==================== ĐO LƯỜNG ====================

        [SugarColumn(ColumnName = "speed", IsNullable = true)]
        public double? Speed { get; set; }

        [SugarColumn(ColumnName = "lengthMm", IsNullable = true)]
        public int? LengthMm { get; set; }

        // ==================== VỊ TRÍ ====================

        [SugarColumn(ColumnName = "kmNumber", IsNullable = true)]
        public int? KmNumber { get; set; }

        [SugarColumn(ColumnName = "metNumber", IsNullable = true)]
        public int? MetNumber { get; set; }

        /// <summary>
        /// Ngưỡng tốc độ tối thiểu (km/h) TẠI THỜI ĐIỂM phát hiện: <c>lane_speed_limit_min_kmh</c> của payload (luật
        /// chạy chậm) nếu có, không thì ngưỡng min của camera (vùng chính → mặc định hướng).
        /// </summary>
        [SugarColumn(ColumnName = "speedMin", IsNullable = true)]
        public int? SpeedMin { get; set; }

        /// <summary>
        /// Ngưỡng tốc độ tối đa (km/h) TẠI THỜI ĐIỂM phát hiện = ngưỡng max của camera đã đẩy xuống AID. Lưu lại để
        /// báo cáo tính phần vượt theo đúng ngưỡng lúc vi phạm, không lệch khi cấu hình đổi.
        /// </summary>
        [SugarColumn(ColumnName = "speedMax", IsNullable = true)]
        public int? SpeedMax { get; set; }

        /// <summary>Màu biển số do camera nhận dạng (Yellow/White/Blue/Red/Black…).</summary>
        [SugarColumn(ColumnName = "plateColor", IsNullable = true, Length = EntityConst.Length16, ColumnDataType = "symbol")]
        public string? PlateColor { get; set; }
    }
}
