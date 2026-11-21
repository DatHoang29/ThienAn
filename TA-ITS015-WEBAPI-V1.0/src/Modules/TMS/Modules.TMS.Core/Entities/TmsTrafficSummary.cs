using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Description: (4) Lưu trữ lâu dài số liệu lưu lượng theo giờ/ngày/tháng.
    ///              Phạm vi chỉ gồm equipment / direction / zone — KHÔNG có đoạn A→B.
    ///              Nguồn ghi là TrafficSummaryJob của Services.Vds, gộp thẳng từ bảng thô trên QuestDB
    ///              bằng SAMPLE BY. Bảng chốt 5 phút TmsTrafficCount của mô hình cũ đã bỏ.
    /// Created date: 27/07/2026
    /// </summary>
    [SugarTable("TmsTrafficSummary")]
    public class TmsTrafficSummary : EntityTenant
    {
        /// <summary>hour / day / month.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length8)]
        public string? PeriodType { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? FromTime { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? ToTime { get; set; }

        /// <summary>equipment / direction / zone / segment.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? ScopeType { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? ScopeId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? Direction { get; set; }

        /// <summary>Xe ngắn (Type = short, suy ra từ chiều dài xe).</summary>
        [SugarColumn(IsNullable = true)]
        public int? SVehicleNumber { get; set; }

        /// <summary>Xe trung (Type = medium, suy ra từ chiều dài xe).</summary>
        [SugarColumn(IsNullable = true)]
        public int? MVehicleNumber { get; set; }

        /// <summary>Xe dài (Type = long, suy ra từ chiều dài xe).</summary>
        [SugarColumn(IsNullable = true)]
        public int? LVehicleNumber { get; set; }

        /// <summary>Xe không xác định được cỡ (thiếu chiều dài hoặc không đọc được).</summary>
        [SugarColumn(IsNullable = true)]
        public int? OtherVehicleNumber { get; set; }

        [SugarColumn(IsNullable = true)]
        public double? AvgSpeed { get; set; }

        [SugarColumn(IsNullable = true)]
        public double? MinSpeed { get; set; }

        [SugarColumn(IsNullable = true)]
        public double? MaxSpeed { get; set; }

        [SugarColumn(IsNullable = true)]
        public double? AvgSpeedS { get; set; }

        [SugarColumn(IsNullable = true)]
        public double? AvgSpeedM { get; set; }

        [SugarColumn(IsNullable = true)]
        public double? AvgSpeedL { get; set; }

        /// <summary>Tốc độ trung bình của nhóm xe không xác định được cỡ.</summary>
        [SugarColumn(IsNullable = true)]
        public double? AvgSpeedOther { get; set; }

        /// <summary>Trọng số khi cộng dồn trung bình.</summary>
        [SugarColumn(IsNullable = true)]
        public int? SampleCount { get; set; }

        [SugarColumn(IsNullable = true)] public int? SampleCountS { get; set; }
        [SugarColumn(IsNullable = true)] public int? SampleCountM { get; set; }
        [SugarColumn(IsNullable = true)] public int? SampleCountL { get; set; }
        [SugarColumn(IsNullable = true)] public int? SampleCountOther { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? ComputedAt { get; set; }
    }
}
