using Shared.Utility.Serialization;
namespace Modules.VDS.Controllers.TrafficCount.Dto
{
    /// <summary>
    /// Author: KienLT
    /// Description: Một ô đếm 5 phút (slice) trả cho FE. Phẳng hoá <c>TrafficCountSlice</c> dùng chung.
    /// Created date: 03/09/2026
    /// </summary>
    public class TrafficCountSliceOutput
    {
        public string? ScopeType { get; set; }
        public string? ScopeId { get; set; }
        public string? Direction { get; set; }
        public string? ZoneId { get; set; }
        [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
        public DateTime From { get; set; }
        [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
        public DateTime To { get; set; }
        public int Short { get; set; }
        public int Medium { get; set; }
        public int Long { get; set; }
        public int Other { get; set; }
        public int Total { get; set; }
        public double? AvgSpeed { get; set; }
        public double? MinSpeed { get; set; }
        public double? MaxSpeed { get; set; }
        public int SpeedSampleCount { get; set; }
    }

    /// <summary>Số đếm GỘP một cửa sổ.</summary>
    public class TrafficCountSummaryOutput
    {
        public int Short { get; set; }
        public int Medium { get; set; }
        public int Long { get; set; }
        public int Other { get; set; }
        public int Total { get; set; }
        public double? AvgSpeed { get; set; }
        public double? MinSpeed { get; set; }
        public double? MaxSpeed { get; set; }
    }

    /// <summary>Một điểm của chuỗi theo giờ (mốc đầu giờ + khối số liệu giờ đó).</summary>
    public class TrafficCountHourlyOutput
    {
        [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
        public DateTime Hour { get; set; }
        public int Short { get; set; }
        public int Medium { get; set; }
        public int Long { get; set; }
        public int Other { get; set; }
        public int Total { get; set; }
        public double? AvgSpeed { get; set; }
        public double? MinSpeed { get; set; }
        public double? MaxSpeed { get; set; }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Khung bản tin SSE của số đếm xe.
    ///
    /// <para>Trước 21/09/2026 đây là anonymous object dựng ngay trong controller. Kiểu trả về của action SSE
    /// là <c>SseResult</c> nên swagger KHÔNG thấy payload, và test hợp đồng cũng không bắt được — nghĩa là
    /// schema có thể đổi âm thầm mà không ai biết. Khai thành lớp để có thể kiểm và tra được.</para>
    /// Created date: 21/09/2026
    /// </summary>
    public class TrafficCountSseOutput
    {
        /// <summary>Phiên bản schema của bản tin; client dùng để biết có hiểu được payload hay không.</summary>
        public string Schema { get; set; } = SchemaVersion;

        /// <summary>Luôn là <c>snapshot</c>: mỗi nhịp gửi trạng thái đầy đủ, không gửi delta.</summary>
        public string Mode { get; set; } = SnapshotMode;

        [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
        public DateTime ComputedAt { get; set; }
        [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
        public DateTime From { get; set; }
        [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
        public DateTime To { get; set; }
        public TrafficCountSummaryOutput Data { get; set; } = new();

        /// <summary>Phiên bản schema hiện hành — đổi payload thì PHẢI tăng số này.</summary>
        public const string SchemaVersion = "vds.count.sse/v1";

        /// <summary>Chế độ gửi: trạng thái đầy đủ mỗi nhịp.</summary>
        public const string SnapshotMode = "snapshot";
    }
}
