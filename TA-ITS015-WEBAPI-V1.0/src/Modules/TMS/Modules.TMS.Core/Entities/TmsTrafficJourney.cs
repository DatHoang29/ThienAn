using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Description: (8) Hành trình một xe qua đoạn A→B — khớp theo biển số ở 2 camera.
    ///              AvgSpeedKmh = DistanceMeter / TravelSeconds * 3.6
    /// FromAnalysisId/ToAnalysisId tham chiếu TmsTrafficAnalysis tại A/B; DistanceMeter giữ khoảng cách lúc tính.
    /// Type là cỡ xe short/medium/long suy từ chiều dài; IsValid kiểm biên thời gian đi của đoạn.
    /// SpeedLimitKmh giữ ngưỡng lớn nhất của hai camera lúc xét chuyến, null khi chưa cấu hình.
    /// IsOverSpeed lưu kết quả xét tốc độ trung bình, độc lập cờ bật sự kiện, làm nguồn speedingAvg trên SSE.
    /// Created date: 27/07/2026
    /// </summary>
    [SugarTable("TmsTrafficJourney")]
    public class TmsTrafficJourney : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? SegmentId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? LicensePlate { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? FromAnalysisId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? ToAnalysisId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? FromEquipmentId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? ToEquipmentId { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? FromTime { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? ToTime { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? TravelSeconds { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? DistanceMeter { get; set; }

        [SugarColumn(IsNullable = true)]
        public double? AvgSpeedKmh { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? Type { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? Direction { get; set; }

        [SugarColumn(IsNullable = true)]
        public bool? IsValid { get; set; } = true;

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? InvalidReason { get; set; }

        [SugarColumn(IsNullable = true)]
        public double? SpeedLimitKmh { get; set; }

        [SugarColumn(IsNullable = true)]
        public bool? IsOverSpeed { get; set; }
    }
}
