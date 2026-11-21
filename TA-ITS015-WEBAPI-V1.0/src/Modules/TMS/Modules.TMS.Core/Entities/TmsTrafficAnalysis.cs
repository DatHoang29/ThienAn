using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Author: TuanHA
    /// Created date: 10/02/2025
    /// Note: Tạo bảng chứa thông tin phân tích lưu lượng giao thông
    /// </summary>
    [SugarTable("TmsTrafficAnalysis", TableDescription = "")]
    public class TmsTrafficAnalysis : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? LicensePlate { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? DetectTime { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? Type { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? Direction { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? Location { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? Lane { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? EquipmentId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? IpAddress { get; set; }

        [SugarColumn(IsNullable = true)]
        public float? Speed { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? Height { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? Width { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? Length { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? MoveType { get; set; }

        // ===== VDS V3 =====

        /// <summary>
        /// ID bản ghi raw sinh ra lượt đếm này (TmsTrafficData.ID) — đồng thời là khoá chống trùng
        /// của tầng đếm (UNIQUE TenantId + SourceId). Cần id phía VDS hoặc loại bản tin gốc thì
        /// join sang TmsTrafficData theo cột này, không lưu trùng ở đây.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? SourceId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? ZoneId { get; set; }

        /// <summary>Loại xe nguyên văn vendor.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? VehicleType { get; set; }

        /// <summary>Độ tin cậy đọc biển số — giám sát chất lượng nhận dạng của camera.</summary>
        [SugarColumn(IsNullable = true)]
        public double? PlateConfidence { get; set; }

        // Không lưu vai trò đoạn A→B ở đây: JourneyJob ghép hành trình bằng cách so EquipmentId với
        // FromEquipmentId/ToEquipmentId của TmsTrafficSegment, nên cột này không có ai đọc.
    }

}
